using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.UI;

namespace SBMS
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {

        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (ex is HttpException httpEx && httpEx.GetHttpCode() == 500)
            {
                // Optional logging
                // LogException(ex);

                if (HttpContext.Current != null &&
                    (HttpContext.Current.Session == null || HttpContext.Current.Session["UserDetails"] == null))
                {
                    string returnUrl = HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl);
                    HttpContext.Current.Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
            }
        }

        protected void Session_End(object sender, EventArgs e)
        {
            try
            {
                if (Session["UserID"] != null && Guid.TryParse(Session["UserID"].ToString(), out Guid userId))
                {
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var user = _db.UsersMasters.SingleOrDefault(u => u.UserGUID == userId);
                        if (user != null)
                        {
                            user.IsLoggedIn = false;
                            _db.SaveChanges();
                        }
                    }
                }
            }
            catch
            {
                // Optional: log the error
            }
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        protected void Application_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            var ctx = HttpContext.Current;

            if (ctx?.Handler is Page page)
            {
                string path = ctx.Request.Url.AbsolutePath.ToLower();

                // Skip check for login or public pages
                if (!path.EndsWith("login.aspx") && !path.EndsWith("datafusiononboard.aspx")&& !path.EndsWith("datafusiononboardfinish.aspx")) // Add other public pages here
                {
                    // Ensure session exists and UserDetails is set
                    if (ctx.Session == null || ctx.Session["UserDetails"] == null)
                    {
                        string returnUrl = HttpUtility.UrlEncode(ctx.Request.RawUrl);
                        ctx.Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, false);
                        ctx.ApplicationInstance.CompleteRequest();
                    }
                }
            }
        }
    }
}