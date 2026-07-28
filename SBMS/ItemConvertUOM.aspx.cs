using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SBMS
{
    public partial class ItemConvertUOM : BasePage
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
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
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
                showhidebuttons();
                loadstores();
            }
        }

        private void showhidebuttons()
        {
            if (CurrentUser.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (CurrentUser.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (CurrentUser.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (CurrentUser.UseModule2 == true)
            {
                if (CurrentUser.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (CurrentUser.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (CurrentUser.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                ibtnFCasts.Style.Add("display", "none");
                ibtnmrp.Style.Add("display", "none");
                ibtnJobTrack.Style.Add("display", "none");
            }
            if (CurrentUser.UseModule3 == true)
            {
                if (CurrentUser.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (CurrentUser.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (CurrentUser.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
            }
            else
            {
                ibtmWorksOrders.Style.Add("display", "none");
                ibtmWOrdMgment.Style.Add("display", "none");
                ibtnRMD.Style.Add("display", "none");
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void imgbTrf_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Transfer.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/FGDemands.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/JobTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ProductionRMD.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        protected void imgdash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoD" && x.StoreCode != "CoR").ToList();
                if (Stores.Any())
                {
                    DDStore.DataSource = Stores;
                    DDStore.DataTextField = "StoreDescript";
                    DDStore.DataValueField = "StoreCode";
                    DDStore.DataBind();
                    DDStore.Items.Insert(0, "- Any/All -");
                }
            }
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    string Storeid =DDStore.SelectedValue;
                    var _items = _db.GetOpeningBalancesByStore(Storeid, CurrentUser.CoID).ToList();
                    if (_items.Any())
                    {
                        // Show "CODE - Description" so the searchable picker matches on either.
                        // The list showed the description alone, so the code was not even visible.
                        ddConvertFrom.DataSource = _items.Select(i => new { i.ItemID, Display = i.Code + " - " + i.ItemDescription }).ToList();
                        ddConvertFrom.DataTextField = "Display";
                        ddConvertFrom.DataValueField = "ItemID";
                        ddConvertFrom.DataBind();
                        ddConvertFrom.Items.Insert(0, new System.Web.UI.WebControls.ListItem("- Select Item -", "0"));
                    }
                }
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        // Opening balances for the currently-selected store, queried on demand. Avoids caching a
        // large list (up to ~5000 items) in ViewState, which bloats the page on every postback.
        private List<GetOpeningBalancesByStore_Result> GetOpeningBalances()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                return _db.GetOpeningBalancesByStore(DDStore.SelectedValue, CurrentUser.CoID).ToList();
            }
        }

        protected void ddConvertFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                // Re-query (avoids caching a large list in ViewState).
                var _items = GetOpeningBalances();

                if (_items != null && ddConvertFrom.SelectedIndex > 0)
                {
                    long itemid = Convert.ToInt32(ddConvertFrom.SelectedValue);

                    // Find the selected item in the list
                    var selectedItem = _items.FirstOrDefault(x => x.ItemID == itemid);

                    if (selectedItem != null)
                    {
                        decimal qoh = selectedItem.QOH ?? 0;
                        lblQOH.Text = qoh.ToString();
                        lblFromUOM.Text = selectedItem.Unit ?? "";
                        ViewState["SelectedFromItemID"] = itemid;

                        // Populate DDConvertTo with all items EXCEPT the selected one
                        var itemsToConvert = _items.Where(x => x.ItemID != itemid).ToList();

                        if (itemsToConvert.Any())
                        {
                            DDConvertTo.DataSource = itemsToConvert.Select(i => new { i.ItemID, Display = i.Code + " - " + i.ItemDescription }).ToList();
                            DDConvertTo.DataTextField = "Display";
                            DDConvertTo.DataValueField = "ItemID";
                            DDConvertTo.DataBind();
                            DDConvertTo.Items.Insert(0, new System.Web.UI.WebControls.ListItem("- Select Target Item -", "0"));
                        }
                        else
                        {
                            DDConvertTo.DataSource = null;
                            DDConvertTo.Items.Clear();
                            DDConvertTo.Items.Insert(0, new System.Web.UI.WebControls.ListItem("- No other items available -", "0"));
                        }

                    }
                }
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected async void lbtnConvert_Click(object sender, EventArgs e)
        {
            if (DDStore.SelectedIndex < 1 || ddConvertFrom.SelectedIndex < 1 || DDConvertTo.SelectedIndex < 1)
            {
                string message = "Please select a valid store and item codes";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }
            if (!decimal.TryParse(txtFromQty.Text, out decimal convertFromQty) || convertFromQty <= 0)
            {
                string message = "Please enter a valid Qty To Convert.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // Re-query (avoids caching a large list in ViewState).
            var _items = GetOpeningBalances();
            if (_items == null || _items.Count == 0)
            {
                string message = "Unable to retrieve item data. Please refresh the page and try again.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // Get the selected FROM item
            long fromItemId = Convert.ToInt64(ddConvertFrom.SelectedValue);
            var fromItem = _items.FirstOrDefault(x => x.ItemID == fromItemId);

            if (fromItem == null)
            {
                string message = "Selected source item not found.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // Get the selected TO item
            long toItemId = Convert.ToInt64(DDConvertTo.SelectedValue);
            var toItem = _items.FirstOrDefault(x => x.ItemID == toItemId);

            if (toItem == null)
            {
                string message = "Selected target item not found.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // Validate QOH against the fromItem's quantity
            decimal qoh = fromItem.QOH ?? 0;
            if (convertFromQty > qoh)
            {
                string message = $"Insufficient quantity on hand. Available quantity: {qoh} {fromItem.Unit}";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            if (!decimal.TryParse(txtToQty.Text, out decimal convertToQty) || convertToQty <= 0)
            {
                string message = "Please enter a valid Convert To Quantity.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // Calculate the conversion ratio
            // Ratio = convertToQty / convertFromQty
            decimal conversionRatio = convertToQty / convertFromQty;

            // Calculate the new unit cost for the target item
            // The total cost of the source items = fromAverageCost * convertFromQty
            // This total cost should be distributed to the target items
            // Therefore, new unit cost for target = (fromAverageCost * convertFromQty) / convertToQty
            decimal fromAverageCost = fromItem.AverageCost ?? 0;
            // toCost = per-unit cost of the target units for the LOCAL per-store ledger. Set below,
            // once the source item's per-store average is known, so the store's value is conserved.
            decimal toCost = 0;

            // 1. Add item transaction for From Qty Down (OUT)
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long storeid = getstoreid(DDStore.SelectedValue.ToString());
                // per-store weighted average drives the outbound cost; item-wide average only when the store has no history
                decimal fromStoreCost = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, (long)fromItem.ItemID, storeid);
                if (fromStoreCost == 0) fromStoreCost = fromAverageCost;
                // Local ledger: the target units carry the SAME per-store value that left the source
                // item, so the store's stock value is conserved. (The OUT leg books at fromStoreCost;
                // the IN leg must match. Previously toCost came from the item-wide average, which did
                // not match the per-store cost booked out, quietly creating/destroying ledger value.)
                toCost = (fromStoreCost * convertFromQty) / convertToQty;
                ItemTransaction ItemTransOUT = new ItemTransaction();
                ItemTransOUT.CompanyID = CurrentUser.CoID;
                ItemTransOUT.DocumentID = 0;
                ItemTransOUT.TransactionType = "CON";
                ItemTransOUT.ItemID = fromItem.ItemID;
                ItemTransOUT.ItemCode = fromItem.Code;
                ItemTransOUT.ItemDescription = fromItem.ItemDescription.Replace(fromItem.Code + " - ", "");
                ItemTransOUT.LotNumber = null;
                ItemTransOUT.Unit = fromItem.Unit;
                ItemTransOUT.ToID = storeid;
                ItemTransOUT.FromID = storeid;
                ItemTransOUT.Qty = convertFromQty * -1;
                ItemTransOUT.DocumentType = 5;
                ItemTransOUT.TransactionDate = DateTime.Now;
                ItemTransOUT.ByRoleID = CurrentUser.RoleID;
                ItemTransOUT.AdditionalCosts = 0;
                ItemTransOUT.PriceExclusive = fromStoreCost;
                ItemTransOUT.TotalUnitPriceExclInclAdd = fromStoreCost;
                ItemTransOUT.TotalLineValExcl = fromStoreCost * (convertFromQty * -1);
                ItemTransOUT.StoreAvgCost = fromStoreCost;
                string refD = $"{fromItem.Code} Item Conversion {convertFromQty} to {toItem.Code} ({conversionRatio:F2}:1 ratio)";
                if (refD.Length > 100) refD = refD.Substring(0, 100);
                ItemTransOUT.TransactionReference = refD;
                ItemTransOUT.ExchRate = 1;

                _db.ItemTransactions.Add(ItemTransOUT);

                // 2. Add transaction for To Qty Up (IN)
                ItemTransaction ItemTransIN = new ItemTransaction();
                ItemTransIN.CompanyID = CurrentUser.CoID;
                ItemTransIN.DocumentID = 0;
                ItemTransIN.TransactionType = "CON";
                ItemTransIN.ItemID = toItem.ItemID;
                ItemTransIN.ItemCode = toItem.Code;
                ItemTransIN.ItemDescription = toItem.ItemDescription.Replace(fromItem.Code + " - ", "");
                ItemTransIN.LotNumber = null;
                ItemTransIN.Unit = toItem.Unit;
                ItemTransIN.ToID = storeid;
                ItemTransIN.FromID = storeid;
                ItemTransIN.Qty = convertToQty;
                ItemTransIN.DocumentType = 5;
                ItemTransIN.TransactionDate = DateTime.Now;
                ItemTransIN.ByRoleID = CurrentUser.RoleID;
                ItemTransIN.AdditionalCosts = 0;
                ItemTransIN.PriceExclusive = toCost;  // New calculated cost
                ItemTransIN.TotalUnitPriceExclInclAdd = toCost;
                ItemTransIN.TotalLineValExcl = toCost * convertToQty;
                refD = $"{toItem.Code} Item Conversion {convertToQty} from {fromItem.Code} (from {convertFromQty} {fromItem.Unit})";
                if (refD.Length > 100) refD = refD.Substring(0, 100);
                ItemTransIN.TransactionReference = refD;
                ItemTransIN.ExchRate = 1;
                ItemTransIN.StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, (long)toItem.ItemID, storeid, convertToQty, toCost * convertToQty, out var _v);

                _db.ItemTransactions.Add(ItemTransIN);
                _db.SaveChanges();

                // Target item's new ITEM-WIDE average (Sage keeps one average per item). Blend the
                // item-wide value that left the source item (fromAverageCost x convertFromQty) with the
                // target's existing item-wide value, over the item-wide quantity. Previously this mixed
                // the item-wide cost with a single store's QOH - the same defect as ItemStkAdjustment.
                var itemMaster = _db.ItemsMasters.FirstOrDefault(x => x.ID == toItem.ItemID && x.CompanyID == CurrentUser.CoID);
                decimal ToCurrAvcost = (decimal)(itemMaster?.AverageCost ?? 0);
                decimal ToItemWideQty = (decimal)(itemMaster?.QuantityOnHand ?? 0);
                decimal ConvertedValue = fromAverageCost * convertFromQty;
                // Denominator is always > 0 (convertToQty is validated > 0 above).
                decimal toItemNewAvg = (ConvertedValue + (ToCurrAvcost * ToItemWideQty)) / (convertToQty + ToItemWideQty);
                if (itemMaster != null)
                {
                    itemMaster.AverageCost = toItemNewAvg;
                    _db.SaveChanges();
                }

                if (CurrentUser.UATMode == false)
                {
                    // 3. Add Sage stock adjustment of From Qty down
                    ItemAdjustment iAdj = new ItemAdjustment();
                    iAdj.Date = DateTime.Now;
                    iAdj.ItemID = (long)ItemTransOUT.ItemID;
                    iAdj.AverageCost = (decimal)fromAverageCost;
                    iAdj.Quantity = (decimal)ItemTransOUT.Qty;
                    iAdj.Reason = ItemTransOUT.TransactionReference;
                    iAdj.Created = DateTime.Now;
                    string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                    await SendItemAdjustment(jsonBody);

                    // 4. Add Sage stock adjustment for To Qty up
                    ItemAdjustment iAdjIn = new ItemAdjustment();
                    iAdjIn.Date = DateTime.Now;
                    iAdjIn.ItemID = (long)ItemTransIN.ItemID;
                    iAdjIn.AverageCost = toItemNewAvg;   // Sage SETS the item average to this value, so send the correctly-blended item-wide average, not the raw incoming unit cost
                    iAdjIn.Quantity = (decimal)ItemTransIN.Qty;
                    iAdjIn.Reason = ItemTransIN.TransactionReference;
                    iAdjIn.Created = DateTime.Now;
                    string jsonBodyIn = JsonConvert.SerializeObject(iAdjIn, Formatting.Indented);
                    await SendItemAdjustment(jsonBodyIn);
                }
            }

            // Show success message with conversion details
            string successMessage = $"Successfully converted {convertFromQty} {fromItem.Unit} of {fromItem.Code} to {convertToQty} {toItem.Unit} of {toItem.Code} " +
                                   $"(Ratio: 1:{conversionRatio:F2}). Cost per {toItem.Unit}: {toCost}";
            AlertHelper.ShowSweetAlert(this, successMessage, "success");

            // Clear the form
            txtFromQty.Text = "";
            txtToQty.Text = "";
            DDStore.SelectedIndex = 0;
            ddConvertFrom.SelectedIndex = 0;
            DDConvertTo.SelectedIndex = 0;
            lblFromUOM.Text = "";
            lblToUOM.Text = "";
            lblQOH.Text = "";     
        }
        protected long getstoreid(string stcode)
        {
            long storeid = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var store = _db.Stores.Where(x => x.StoreCode == stcode && x.CompanyID == CurrentUser.CoID).FirstOrDefault();
                storeid = store.StoreID;
            }
            return storeid;
        }

        public async Task SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
        }

    }
}
