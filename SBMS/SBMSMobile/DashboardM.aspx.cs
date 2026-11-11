using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;

namespace SBMS
{
    public partial class DashboardM : System.Web.UI.Page
    {
        UserDetails userDets;
        //Guid userGuid;
        protected void Page_Load(object sender, EventArgs e)
        {
            userDets = Session["UserDetails"] as UserDetails;
            SessionValidator.ValidateUserSession(userDets);
            if (userDets == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            // Mobile module gate: company not licensed for mobile -> back to the web dashboard.
            if (!userDets.MobileModule)
            {
                Response.Redirect("~/Dashboard.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

            // Mobile manufacture backflushes the components itself, so it is only offered to
            // companies on Auto Manufacture, and never to lot-tracked ones.
            imgbManufacture.Visible = userDets.UseAutoManf && !userDets.CompanyUseLotNumbers;

            // Manual lot numbers → scanner receiving (Count / Receive) is not allowed; the GRN must
            // be done on the web so the operator can enter the real lot. Put-away is unaffected.
            if (userDets.CompanyUseLotNumbers && !userDets.CompanyAllowSystemLotNumbers)
            {
                imgbCount.Visible   = false;
                imgbReceive.Visible = false;
            }

            // Role (Configuration -> Roles): the same rights that show / hide the web dashboard buttons.
            //   View Pick Slips -> Picking Slips, Confirm Delivery      (web: Sales Orders / Picking Slips button)
            //   Fill Works Orders (Module 3 only) -> Manufacture        (web: Works Order filling button)
            // Not Stock Control: the default "Stock Controller" role is seeded with it OFF, so it
            // would hide Stock Count from the very people who count (checked 2026-11-11).
            if (userDets.CanViewPickSlips != true)
            {
                ibtnPickSlips.Visible = ibtnConfirmDelivery.Visible = false;
            }
            if (userDets.UseModule3 && userDets.CanFillWorksOrders != true) imgbManufacture.Visible = false;

            // First mobile release (agreed 2026-11-11): basics only - Item Lookup, Receive, Picking
            // Slips, Confirm Delivery, Stock Count. The other tiles are HIDDEN, not removed:
            // set ShowAdvancedTiles = true to bring them all back.
            // Slim manufacturing keeps Manufacture - it is the only tile those companies have.
            const bool ShowAdvancedTiles = false;
            if (!ShowAdvancedTiles)
            {
                imgbCount.Visible = imgbRec.Visible = false;               // Count and Check, Put-away
                lbtnBinTransfer.Visible = ibtnStockMove.Visible = false;   // Quick Move, Stock Move
                if (!userDets.SageStockOnly) imgbManufacture.Visible = false;
            }

            // Slim manufacturing: the Manufacturing tile only.
            phNotSlimTop.Visible = phNotSlimBottom.Visible = phLookup.Visible = !userDets.SageStockOnly;

            // A section with no tiles left loses its heading too.
            phNotSlimTop.Visible   &= imgbCount.Visible || imgbReceive.Visible || imgbRec.Visible;
            phManufacture.Visible  = imgbManufacture.Visible;
            phTransfers.Visible    = lbtnBinTransfer.Visible || ibtnStockMove.Visible;
            phPicking.Visible      = ibtnPickSlips.Visible || ibtnConfirmDelivery.Visible;
            phCount.Visible        = ibtnStockCount.Visible;
        }
        // Mode 1 — Count: pick a PO, scan accept/reject counts; the web posts the GRN.
        protected void imgbCount_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx?mode=count&user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
        }

        // Simple manufacture process (page to follow).
        protected void imgbManufacture_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/ManufactureM.aspx?user=" + userDets.UserGuiD, false);
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

        // Stock Move — bulk, multi-item transfer via a GIT store; Out then Receive against a desktop transfer.
        protected void ibtnStockMove_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/StockMoveM.aspx?user=" + userDets.UserGuiD, false);
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

        // Read-only stock enquiry: item -> on hand per store / bin (and lot).
        protected void ibtnItemLookup_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/ItemLookupM.aspx?user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
        }

        // After picking is closed off: who received the goods, and a comment.
        protected void ibtnConfirmDelivery_Click(object sender, EventArgs e)
        {
            if (userDets != null)
                Response.Redirect("~/SBMSMobile/ConfirmDeliveryM.aspx?user=" + userDets.UserGuiD, false);
            else
                Response.Redirect("~/Dashboard.aspx", true);
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