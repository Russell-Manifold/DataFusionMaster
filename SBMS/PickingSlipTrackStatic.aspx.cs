using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class PickingSlipTrackStatic : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            if (File.Exists(Server.MapPath(imgPath)))
            {
                imgCoImg.ImageUrl = ResolveUrl(imgPath);
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            if (!IsPostBack)
            {
            
            }
        }

        [WebMethod (EnableSession = true)]
        public static string GetUpdatedKanbanBoard()
        {
            UserDetails userDetails = HttpContext.Current.Session["UserDetails"] as UserDetails;
            StringBuilder sb = new StringBuilder();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var pickprocesses = _db.PickSlipProcesses
                                       .Where(x => x.CompanyID == userDetails.CoID && x.PSActive == true && x.PSName.ToLower() != "complete")
                                       .Select(x => new { x.PSPID, x.PSName, x.Seq })
                                       .OrderBy(x => x.Seq)
                                       .ToList();

                foreach (var proc in pickprocesses)
                {
                    sb.Append(AddColumnHtml(proc.PSPID, proc.PSName, userDetails.CoID));
                }
            }

            return sb.ToString();
        }

        private static string AddColumnHtml(int stationId, string stationName, long companyId)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<div class='column' id='{stationId}'>");
            sb.Append($"<h2>{stationName}</h2>");

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var pslips = _db.GetAllActivePickingSlips(companyId)
                                .Where(j => j.PSStationID == stationId)
                                .OrderBy(x => x.DueDelDate)
                                .ToList();

                foreach (var slip in pslips)
                {
                    string jobDiv = $"<div class='job' id='ps_{slip.PSID}' draggable='false' >" +
                              $"<span style='color:#4A82AB; font-weight:600'>{slip.PSIntNumber}</span>" +
                              $"Due: {slip.DueDelDate:dd MMM}<br/>{slip.CustSupName}" +
                              $"</div>";
                    sb.Append(jobDiv);
                }
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
