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

            // Log EVERY unhandled exception, not just 500s, and before any redirect.
            // This was commented out, so crashes left no trace at all - a receipt that
            // died mid-finalise (see AsyncTimeout note on Receiving.aspx) produced an
            // empty error log and there was nothing to diagnose from.
            try
            {
                if (ex != null)
                {
                    string url = HttpContext.Current?.Request?.RawUrl ?? "(no url)";
                    // Behind Cloudflare the connection IP is Cloudflare's; the real caller is in CF-Connecting-IP.
                    string ip = " ip:" + (HttpContext.Current?.Request?.Headers?["CF-Connecting-IP"] ?? HttpContext.Current?.Request?.UserHostAddress ?? "?");
                    string who = "";
                    try
                    {
                        var u = HttpContext.Current?.Session?["UserDetails"] as SBMS.Classes.UserDetails;
                        if (u != null) who = $" [CoID:{u.CoID} {u.UserEmail ?? u.LoginName}]";
                    }
                    catch { }
                    string detail = "";
                    var ev = (ex as System.Data.Entity.Validation.DbEntityValidationException)
                          ?? (ex.InnerException as System.Data.Entity.Validation.DbEntityValidationException);
                    if (ev != null)
                        foreach (var en in ev.EntityValidationErrors)
                            foreach (var ve in en.ValidationErrors)
                                detail += $" [{en.Entry.Entity.GetType().Name}.{ve.PropertyName}: {ve.ErrorMessage}]";
                    // Tampered postbacks (vulnerability scanners, stale pages) are rejected by
                    // ASP.NET event validation. One line each, no stack - a scan on 2026-10-19
                    // wrote 19,000 of these with full traces and made the log 70 MB.
                    var inner = ex.InnerException ?? ex;
                    if (inner is ArgumentException && inner.Message.StartsWith("Invalid postback or callback argument"))
                    {
                        new SBMS.Classes.ApiUrlCall().LogErrorToFile(
                            "REJECTED POSTBACK on " + url + who + ip);
                    }
                    else
                    new SBMS.Classes.ApiUrlCall().LogErrorToFile(
                        "UNHANDLED on " + url + who + ip + detail + " - " + ex.ToString());
                }
            }
            catch { }   // logging must never mask the original error

            // The app owns the crash redirect from here. It used to be left to Web.config
            // customErrors, and the live server's copy pointed at an index.aspx that does not
            // exist - users landed on a 404 and nobody knew. Local runs keep the full error page.
            if (ex != null && HttpContext.Current != null && !HttpContext.Current.Request.IsLocal
                && string.IsNullOrEmpty(HttpContext.Current.Request.Headers["X-MicrosoftAjax"]))   // UpdatePanel postbacks report through ScriptManager
            {
                Server.ClearError();
                string rawUrl = HttpContext.Current.Request.RawUrl ?? "";
                if (HttpContext.Current.Session == null || HttpContext.Current.Session["UserDetails"] == null)
                {
                    HttpContext.Current.Response.Redirect("~/Login.aspx?returnUrl=" + HttpUtility.UrlEncode(rawUrl), false);
                }
                else
                {
                    // Page name only (letters, digits, dot, underscore) - shown back to the user on the dashboard,
                    // with the root error message (kept in Session, read once by the dashboard).
                    string page = System.Text.RegularExpressions.Regex.Match(
                        System.IO.Path.GetFileName(rawUrl.Split('?')[0]), @"^[A-Za-z0-9_.]+").Value;
                    try { HttpContext.Current.Session["LastErrorMessage"] = ex.GetBaseException().Message; } catch { }
                    HttpContext.Current.Response.Redirect("~/Dashboard.aspx?err=" + page, false);
                }
                HttpContext.Current.ApplicationInstance.CompleteRequest();
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