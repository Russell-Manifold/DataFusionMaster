using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ProductionTrackingStatic : BasePage
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
            if (!IsPostBack)
            {
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
            }
        }

        [WebMethod]
        public static string GetUpdatedKanbanBoard()
        {
            UserDetails userDetails = HttpContext.Current.Session["UserDetails"] as UserDetails;
            StringBuilder sb = new StringBuilder();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var workstations = _db.WorkStations
                                      .Where(x => x.CompanyID == userDetails.CoID && x.WSActive == true)
                                      .Select(x => new { x.WSID, x.WSName, x.Seq, x.IsWIP })
                                      .OrderBy(x => x.Seq)
                                      .ToList();

                foreach (var station in workstations)
                {
                    Boolean iswip = false;
                    if (station.IsWIP == true) { iswip = true; };
                    //AddColumn(station.WSID, station.WSName, iswip);
                    sb.Append(AddColumnHtml(station.WSID, station.WSName, iswip, userDetails.CoID));
                }
            }

            return sb.ToString();
        }

        private static string AddColumnHtml(int stationId, string stationName, Boolean isWIP, long CoID)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<div class='column' id='{stationId}'>");
            if (isWIP)
            {
                sb.Append($"<h2>{stationName}  (WIP)</h2>");
            }
            else
            {
                sb.Append($"<h2>{stationName}</h2>");
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ProdJobs = _db.ProdPlanLines.Where(x => x.CompanyID == CoID && x.ProdStationID == stationId && x.Active == true).ToList();
                foreach (var Ln in ProdJobs)
                {
                    if (Ln.ItemCode != null && Ln.PlanQuantity != null)
                    {
                        string jobDiv = $"<div class='job' id='ps_{Ln.LineID}'>" +
                                    $"{Ln.ItemCode} Qty: {Math.Round((decimal)Ln.PlanQuantity, 0)} Due: {Ln.PlanDate:dd MMM}<br/>{Ln.ItemDescription}" +
                                    $"</div>";
                        sb.Append(jobDiv);
                    }
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
