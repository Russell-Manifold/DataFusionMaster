using System;
using System.Linq;
using System.Web;
using SBMS.Models;
using System.IO;  // For logging
using System.Web.SessionState;
using SBMS.Classes;

namespace SBMS
{
    public class KeepAlive : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                context.Response.ContentType = "text/plain";

                // Ensure session exists
                if (context.Session == null)
                {
                    context.Response.StatusCode = 440; // Custom: Session Expired
                    context.Response.Write("Session is null. Possible expiration.");
                    return;
                }

                // Ensure UserID is available in session
                if (context.Session["UserID"] == null)
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    context.Response.Write("UserID not found in session.");
                    return;
                }

                Guid userId = (Guid)context.Session["UserID"];

                // Update last activity in database
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var user = _db.UsersMasters.SingleOrDefault(u => u.UserGUID == userId);
                    if (user != null)
                    {
                        user.LoggedInSessionID = context.Session.SessionID;
                        user.LastActivity = DateTime.Now;
                        _db.SaveChanges();
                    }
                    else
                    {
                        context.Response.StatusCode = 404; // Not Found
                        context.Response.Write("User not found in database.");
                        return;
                    }
                }

                // Return success response
                context.Response.Write($"Session active. Last ping: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                // Log errors
                string logPath = HttpContext.Current.Server.MapPath("~/App_Data/ErrorLog.txt");
                File.AppendAllText(logPath, $"{DateTime.Now}: {ex}\n");

                // Return error response
                context.Response.StatusCode = 500; // Internal Server Error
                context.Response.Write("Server error: " + ex.Message);
            }
        }

        public bool IsReusable => false;
    }
}