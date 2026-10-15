using System;
using System.Web;
using System.Web.UI;


namespace SBMS.Classes
{
    public class BasePage : Page
    {
        protected UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        // The only pages a slim manufacturing company (SageStockOnly) may open. Lower case.
        private static readonly System.Collections.Generic.HashSet<string> SlimPages = new System.Collections.Generic.HashSet<string>
        {
            "dashboard.aspx", "helper.aspx", "viewpdf.aspx",
            "configmaster.aspx", "configcompany.aspx", "configusers.aspx",
            "itemsheaders.aspx", "itemedit.aspx", "itemconvertuom.aspx",
            "bomheaders.aspx", "bomdetailed.aspx", "bomcreate.aspx", "bomcomponentlinks.aspx",
            "worksordersheaders.aspx", "worksordersdetailed.aspx", "worksordersrmedit.aspx",
            "worksordersmanfheaders.aspx", "worksordersmanf.aspx", "worksorderpdfcreate.aspx",
            "dashboardm.aspx", "manufacturem.aspx"
        };

        // A slim company opening any other page is redirected, AND the page itself must not run:
        // it is treated as a first GET (no posted data, so no click/change events) and its
        // Page_Load and rendering are skipped. A thread-aborting redirect is not used - it
        // crashes the Async="true" pages.
        private bool _slimBlocked;
        private bool SlimBlocked()
        {
            string path = Request.Url.AbsolutePath.ToLower();
            return CurrentUser != null && CurrentUser.SageStockOnly
                && !path.EndsWith("login.aspx") && !path.EndsWith("datafusiononboard.aspx") && !path.EndsWith("datafusiononboardfinish.aspx")
                && !(path.Contains("/sbmsmobile/") && !CurrentUser.MobileModule)
                && !SlimPages.Contains(System.IO.Path.GetFileName(path));
        }

        protected override System.Collections.Specialized.NameValueCollection DeterminePostBackMode()
        {
            _slimBlocked = SlimBlocked();
            return _slimBlocked ? null : base.DeterminePostBackMode();
        }

        protected override void OnLoad(EventArgs e)
        {
            if (_slimBlocked) return;
            base.OnLoad(e);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (_slimBlocked) return;
            base.Render(writer);
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // Skip check for Login.aspx or other public pages
            string path = Request.Url.AbsolutePath.ToLower();
            if (!path.EndsWith("login.aspx") && !path.EndsWith("datafusiononboard.aspx") && !path.EndsWith("datafusiononboardfinish.aspx")) 
            {
                if (CurrentUser == null)
                {
                    string returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                    Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                // Mobile module gate: block the whole mobile suite (incl. direct URLs)
                // when the company isn't licensed for it.
                else if (path.Contains("/sbmsmobile/") && !CurrentUser.MobileModule)
                {
                    Response.Redirect("~/Dashboard.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                // Slim manufacturing gate: Works Orders, BOMs and Items only (incl. direct URLs).
                else if (_slimBlocked)
                {
                    Response.Redirect(path.Contains("/sbmsmobile/") ? "~/SBMSMobile/DashboardM.aspx" : "~/Dashboard.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }
    }
}