using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Notifications : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        { }

        [WebMethod]
        public static List<Notification> GetNewNotifications()
        {
            // Store session data in local variables **before** async execution
            var context = HttpContext.Current;
            if (context == null) return new List<Notification>();

            long? coID = context.Session["CoID"] as long?;
            int? roleID = context.Session["RoleID"] as int?;
            UserDetails currentUser = context.Session["UserDetails"] as UserDetails;

            if (coID == null || roleID == null || currentUser == null)
                return new List<Notification>();

            return Task.Run(async () =>
            {
                try
                {
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var notificationsQuery = await _db.Notifications
                            .Where(x => x.IsRead == false && x.CompanyID == coID && x.UserRoleID == roleID)
                            .Select(n => new
                            {
                                n.Id,
                                n.Message
                            })
                            .ToListAsync();

                        var notifications = notificationsQuery
                            .Select(n => new Notification
                            {
                                Id = n.Id,
                                Message = n.Message
                            })
                            .ToList();

                        return currentUser.SendMessages ? notifications : new List<Notification>();
                    }
                }
                catch (Exception ex)
                {
                    return new List<Notification>();
                }
            }).GetAwaiter().GetResult();
        }

        [WebMethod]
        public static void MarkMessageAsRead(int messageId)
        {
            // Same session + tenant guards as GetNewNotifications: without them any
            // caller could mark any company's notifications read by enumerating ids.
            var context = HttpContext.Current;
            if (context == null) return;
            var currentUser = context.Session["UserDetails"] as UserDetails;
            if (currentUser == null) return;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        {
                var Notif = _db.Notifications.FirstOrDefault(x => x.Id == messageId
                    && x.CompanyID == currentUser.CoID);
                if (Notif != null)
            {
                    Notif.IsRead = true;
                    Notif.ReadAt = DateTime.Now;
                    _db.SaveChanges();
                }
            }
        }

        [WebMethod]
        public static string GetNewSalesOrders()
        {
            // Retrieve session values **before** async execution
            var context = HttpContext.Current;
            if (context == null || context.Session["UserDetails"] == null)
                return "Session expired";

            UserDetails currentUser = context.Session["UserDetails"] as UserDetails;
            if (currentUser == null)
                return "Session expired";

            return Task.Run(async () =>
            {
                try
                {
                    // Simulate API calls for new sales orders
                    ApiUrlCall api = new ApiUrlCall();
                    await api.LoadSalesOrders(currentUser);
                    await api.LoadPurchaseOrders(currentUser);
                   var errors = await api.LoadItems(currentUser);
                    return "Success";
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }).GetAwaiter().GetResult();
        }
    }
}
