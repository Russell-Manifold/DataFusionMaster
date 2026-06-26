using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;

namespace SBMS
{
    public partial class DashboardM : System.Web.UI.Page
    {
        UserDetails userDets;
        Guid userGuid;
        protected void Page_Load(object sender, EventArgs e)
        {
            userDets = Session["UserDetails"] as UserDetails;
            SessionValidator.ValidateUserSession(userDets);
            if (userDets == null)
            {
                Response.Redirect("~/LoginM.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblUsername.Text = userDets.UserName;
            if (!IsPostBack)
            {
                string imgname = userDets.CoID + ".png";
                string imgPath = $"~/images/CoImages/{imgname}";
                if (File.Exists(Server.MapPath(imgPath)))
                {
                  //  imgCoImg.ImageUrl = ResolveUrl(imgPath);
                }
                else
                {
                 //   imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                }

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                  //  lblCoName.Text = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == userDets.CoID).CompanyName.ToString();
                }
                showhidebuttons();
            }
        }

        private void showhidebuttons()
        {
            if (userDets == null) return;

            // Each company-config switch shows / hides its receiving tile.
            imgbCount.Visible   = userDets.AllowScannerCount;
            imgbReceive.Visible = userDets.AllowScannerReceive;
            imgbRec.Visible     = userDets.AllowScannerPutAway;

            // Manual lot numbers → scanner receiving (Count / Receive) is not allowed; the GRN must
            // be done on the web so the operator can enter the real lot. Put-away is unaffected.
            if (userDets.CompanyUseLotNumbers && !userDets.CompanyAllowSystemLotNumbers)
            {
                imgbCount.Visible   = false;
                imgbReceive.Visible = false;
            }
        }
        // Mode 1 — Count: pick a PO, scan accept/reject counts; the web posts the GRN.
        protected void imgbCount_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx?mode=count&user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
        }

        // Mode 2 — Direct receive: pick a PO, scan items straight into locations (posts the GRN to Sage).
        protected void imgbReceive_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx?mode=direct&user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
        }

        // Put-away: relocate already-received stock from the holding store to bins/locations.
        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                Response.Redirect("~/SBMSMobile/ReceivingM.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        // Quick Move — scanner-driven single-item move between two stores/bins (ledger-only).
        protected void imgbQuickMove_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/QuickMoveM.aspx?user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStockCount_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                Response.Redirect("~/SBMSMobile/StockCountsM.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(userDets.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}