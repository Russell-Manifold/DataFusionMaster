using System;
using System.Web;
using System.Web.Security;

namespace SBMS.Classes
{
    internal class SessionValidator
    {
        public static void ValidateUserSession(UserDetails user)
        {
            if (user == null || HttpContext.Current.Session == null)
                return;

            string currentSessionId = HttpContext.Current.Session.SessionID;

            if (!string.Equals(user.LoggedInSessionID, currentSessionId, StringComparison.Ordinal))
            {
                // Session mismatch → force logout
                FormsAuthentication.SignOut();
                HttpContext.Current.Session.Abandon();

                // Kill cookie
                if (HttpContext.Current.Request.Cookies["Login"] != null)
                {
                    HttpContext.Current.Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
                }

                // Redirect to login
                HttpContext.Current.Response.Redirect("~/Login.aspx?sessionExpired=true", true);
            }
        }
    }
}