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
            }
        }
    }
}