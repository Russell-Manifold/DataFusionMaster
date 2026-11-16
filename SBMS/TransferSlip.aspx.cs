using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class TransferSlip : BasePage
    {
        int TrfThisID;

        [Serializable]
        public class StoresDto
        {
            public int StoreID { get; set; }
            public string StoreCode { get; set; }
            public string StoreDescript { get; set; }
           
        }
        private List<StoresDto> _stores
        {
            get { return ViewState["StoreList"] as List<StoresDto>; }
            set { ViewState["StoreList"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            TrfThisID = Convert.ToInt32(Request.QueryString["trfid"]);
            string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
            imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");
            if (!IsPostBack)
            {
                LoadWarehouses();
                LoadTransfer();
                if (PnlAddLine.Visible) LoadAddItems();
            }
        }

        private void LoadTransfer()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Trf = (from ith in _db.ItemTransferHeaders
                            where ith.CompanyID == CurrentUser.CoID
                            let fromStore = _db.Stores.Where(s => s.StoreID == ith.TrfFromID).Select(s => s.StoreCode).FirstOrDefault()
                            let toStore = _db.Stores.Where(s => s.StoreID == ith.TrfToID).Select(s => s.StoreCode).FirstOrDefault()
                            select new
                            {
                                ith.TrfID,
                                FrmStore = fromStore,
                                ToStore = toStore,
                                ith.TrfStatus,
                                ith.TrfNotes,
                                ith.TrfDate,
                                ith.TrfActive,
                                ith.TrfStarted,
                                ith.TrfComplete,
                                ith.TransferID,
                                ith.TrfFromID,
                                ith.TrfToID,
                                ith.TrfReference, 
                                ith.TrfCompleteDate
                            }).Where(x => x.TrfID == TrfThisID).FirstOrDefault();

                    if (Trf != null)
                    {         
                        lblDocNum.Text = Trf.TransferID.ToString().PadLeft(8, '0');
                        txtxTrfRef.Text = Trf.TrfReference;
                        txtNotes.Text = Trf.TrfNotes;
                        txtDate.Text = Convert.ToDateTime(Trf.TrfDate).ToString("dd MMM yyyy");
                        if (Trf.TrfCompleteDate != null) txtCompleteDate.Text = Convert.ToDateTime(Trf.TrfCompleteDate).ToString("dd MMM yyyy");
                    
                        if (Trf.TrfFromID != null && ddlFromWarehouse.Items.FindByValue(Trf.TrfFromID.ToString()) != null)
                        ddlFromWarehouse.SelectedValue = Trf.TrfFromID.ToString();
                            else
                        ddlFromWarehouse.SelectedIndex = 0; // or handle as needed

                    if (Trf.TrfToID != null && ddlToWarehouse.Items.FindByValue(Trf.TrfToID.ToString()) != null)
                        ddlToWarehouse.SelectedValue = Trf.TrfToID.ToString();
                    else
                        ddlToWarehouse.SelectedIndex = 0; // or handle as needed

                    DDStatus.Text = Trf.TrfStatus;
                    if (Trf.TrfStatus == "Started")
                    {
                        lbtnCompleteTrf.Style.Add("display", "inline-block");
                        lbtnStart.Style.Add("display", "none");
                        LbtnSaveEdits.Style.Add("display", "none");
                        lbtnAddShip.Style.Add("display", "none");
                    }
                    else if (Trf.TrfStatus == "Complete" || Trf.TrfStatus == "Deleted")
                    {
                        lbtnCompleteTrf.Style.Add("display", "none");
                        lbtnStart.Style.Add("display", "none");
                        LbtnSaveEdits.Style.Add("display", "none");
                        lbtnAddShip.Style.Add("display", "none");
                    }
                    else
                    {
                        lbtnStart.Style.Add("display", "inline-block");
                        lbtnAddShip.Style.Add("display", "inline-block");
                        LbtnSaveEdits.Style.Add("display", "inline-block");
                        lbtnCompleteTrf.Style.Add("display", "none");
                    }
                    PnlAddLine.Visible = Trf.TrfStatus != "Started" && Trf.TrfStatus != "Complete" && Trf.TrfStatus != "Deleted";
                    BindLines();
                }
            }
        }

        // The slip's lines. Blank lines (the empty row TransferHeaders creates with a new slip,
        // and old slips' unsaved rows) are skipped - lines are only created by Add now.
        private void BindLines()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TrfLines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID && x.ItemSelectionId != null).OrderBy(x => x.TrfLID).ToList();
                if (DDStatus.Text != "New")
                {
                    TrfLines = TrfLines.Where(x => x.TrfOutQty > 0).ToList();
                }

                foreach (var Trl in TrfLines)
                {
                    if (Trl.TrfOutQty != null && Trl.TrfOutQty > 0)
                    {
                        string TrfQty = ApiUrlCall.NumberToDecimal(Trl.TrfOutQty.ToString(), CurrentUser.CompanyDecPlaces);
                        Trl.TrfOutQty = Convert.ToDecimal(TrfQty);
                    }
                }
                GridTrfLines.DataSource = TrfLines;
                GridTrfLines.DataBind();
            }
        }
        private void LoadWarehouses()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD" && x.StoreCode.ToLower() != "scr").ToList();
                ddlFromWarehouse.DataSource = stores;
                ddlFromWarehouse.DataTextField = "StoreDescript";
                ddlFromWarehouse.DataValueField = "StoreID";
                ddlFromWarehouse.DataBind();
                ddlFromWarehouse.Items.Insert(0, "-Select-");
                ddlToWarehouse.DataSource = stores;
                ddlToWarehouse.DataTextField = "StoreDescript";
                ddlToWarehouse.DataValueField = "StoreID";
                ddlToWarehouse.DataBind();
                ddlToWarehouse.Items.Insert(0, "-Select-");
                // Map to StoresDto for ViewState
                _stores = stores.Select(s => new StoresDto
                {
                    StoreID = s.StoreID,
                    StoreCode = s.StoreCode,
                    StoreDescript = s.StoreDescript
                }).ToList();
            }
        }

        protected void GridTrfLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.Header && e.Row.RowType != DataControlRowType.DataRow) return;
            if (CurrentUser.CompanyUseLotNumbers == false) e.Row.Cells[2].Visible = false;
            if (DDStatus.Text != "New") e.Row.Cells[4].Visible = false;   // delete only while the slip is New
        }

        // ---- Add bar ---------------------------------------------------------------------------
        // Stock is read for the one warehouse (and item) needed, when needed. This page used to run
        // GetOpeningBalancesAllStores - every item, every store, every lot - and carry the result in
        // ViewState on every click, plus a full item dropdown on every line. That was the minute-long load.

        // Items with stock in the From warehouse.
        private void LoadAddItems()
        {
            DDAddItem.Items.Clear();
            DDAddLot.Items.Clear();
            lblAddFrom.Text = lblAddTo.Text = "";
            txtAddQty.Text = "";
            phAddLot.Visible = CurrentUser.CompanyUseLotNumbers;

            if (!long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId))
            {
                DDAddItem.Items.Add(new System.Web.UI.WebControls.ListItem("- Select the From warehouse first -", ""));
                return;
            }

            long co = CurrentUser.CoID;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Same rule as the proc: listed when any of the item's lots has a non-zero balance here.
                var stocked = _db.ItemTransactions
                    .Where(t => t.CompanyID == co && t.ToID == fromId)
                    .GroupBy(t => new { t.ItemID, t.LotNumber })
                    .Where(g => g.Sum(t => t.Qty) != 0)
                    .Select(g => g.Key.ItemID)
                    .Distinct();

                var items = _db.ItemsMasters
                    .Where(im => im.CompanyID == co && stocked.Contains((long?)im.ID))
                    .Select(im => new { im.ID, im.Code, im.Description })
                    .ToList();

                DDAddItem.DataSource = items
                    .OrderBy(x => x.Code)
                    .Select(x => new { x.ID, Text = (x.Code ?? "") + (string.IsNullOrWhiteSpace(x.Code) ? "" : " - ") + (x.Description ?? "") })
                    .ToList();
                DDAddItem.DataTextField = "Text";
                DDAddItem.DataValueField = "ID";
                DDAddItem.DataBind();
            }
            DDAddItem.Items.Insert(0, new System.Web.UI.WebControls.ListItem("- Select -", ""));
        }

        // Stock on hand for one item in one store, optionally one lot: the proc's sum (ToID = store),
        // for just that item. Uses IX_ItemTransaction_Co_Item_To_Qty.
        private decimal StoreQty(SBMSEntities _db, long itemId, long storeId, string lot)
        {
            long co = CurrentUser.CoID;
            var q = _db.ItemTransactions.Where(t => t.CompanyID == co && t.ItemID == itemId && t.ToID == storeId);
            if (lot != null) q = q.Where(t => t.LotNumber == lot);
            return q.Sum(t => t.Qty) ?? 0m;
        }

        // On-hand figures for the item in the add bar (lot-specific on the From side once a lot is picked).
        private void ShowAddStock()
        {
            lblAddFrom.Text = lblAddTo.Text = "";
            if (!long.TryParse(DDAddItem.SelectedValue, out long itemId)) return;
            string lot = DDAddLot.SelectedIndex > 0 ? DDAddLot.SelectedValue : null;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId))
                    lblAddFrom.Text = ApiUrlCall.NumberToDecimal(StoreQty(_db, itemId, fromId, lot).ToString(), CurrentUser.CompanyDecPlaces);
                if (long.TryParse(ddlToWarehouse.SelectedValue, out long toId))
                    lblAddTo.Text = ApiUrlCall.NumberToDecimal(StoreQty(_db, itemId, toId, null).ToString(), CurrentUser.CompanyDecPlaces);
            }
        }

        protected void DDAddItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            DDAddLot.Items.Clear();
            if (CurrentUser.CompanyUseLotNumbers && long.TryParse(DDAddItem.SelectedValue, out long itemId)
                && long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId))
            {
                long co = CurrentUser.CoID;
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    // Lots of this item with a non-zero balance in the From store.
                    DDAddLot.DataSource = _db.ItemTransactions
                        .Where(t => t.CompanyID == co && t.ItemID == itemId && t.ToID == fromId
                                 && t.LotNumber != null && t.LotNumber != "" && t.LotNumber != "N/A")
                        .GroupBy(t => t.LotNumber)
                        .Where(g => g.Sum(t => t.Qty) != 0)
                        .Select(g => g.Key)
                        .OrderBy(l => l)
                        .ToList();
                    DDAddLot.DataBind();
                }
                DDAddLot.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-Select-", ""));
            }
            ShowAddStock();
            ScriptManager1.SetFocus(CurrentUser.CompanyUseLotNumbers ? (System.Web.UI.Control)DDAddLot : txtAddQty);
        }

        protected void DDAddLot_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowAddStock();
            ScriptManager1.SetFocus(txtAddQty);
        }

        protected void lbtnAddLine_Click(object sender, EventArgs e)
        {
            string err = AddLine();
            if (err != null)
            {
                AlertHelper.ShowSweetAlert(this, err, "error");
                return;
            }
            ScriptManager1.SetFocus(DDAddItem);
        }

        // Adds the item / lot / qty in the add bar as a line. Returns the error, or null when added.
        private string AddLine()
        {
            if (!long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId) || !long.TryParse(ddlToWarehouse.SelectedValue, out long toId))
                return "Please select both From and To warehouses before continuing.";
            if (fromId == toId) return "From and To Warehouses cannot be the same.";
            if (!long.TryParse(DDAddItem.SelectedValue, out long itemId)) return "Please select an item.";
            if (!decimal.TryParse(txtAddQty.Text, out decimal qty)) qty = 0;
            qty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(qty.ToString(), CurrentUser.CompanyDecPlaces));
            if (qty <= 0) return "Enter a transfer quantity greater than zero.";
            string lot = DDAddLot.SelectedIndex > 0 ? DDAddLot.SelectedValue : null;

            long co = CurrentUser.CoID;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (qty > StoreQty(_db, itemId, fromId, lot)) return "Insufficient stock available for this item.";

                // Code and description from the item record, not from splitting the dropdown text
                // on "-": codes like SG7011-81 would print as code SG7011, description 81.
                var item = _db.ItemsMasters.Where(x => x.CompanyID == co && x.ID == itemId)
                                           .Select(x => new { x.Code, x.Description }).FirstOrDefault();

                // Unit and prices from the latest movement of this item (and lot) in the From store -
                // the row the proc took them from. Information only: Complete costs at the store average.
                var last = _db.ItemTransactions
                    .Where(t => t.CompanyID == co && t.ItemID == itemId && t.ToID == fromId && (lot == null || t.LotNumber == lot))
                    .OrderByDescending(t => t.TrnID)
                    .Select(t => new { t.Unit, t.PriceExclusive, t.TotalUnitPriceExclInclAdd })
                    .FirstOrDefault();
                decimal unitCost = last?.TotalUnitPriceExclInclAdd ?? 0m;

                _db.ItemTransferLines.Add(new ItemTransferLine
                {
                    CompanyID = co,
                    TrfID = TrfThisID,
                    ItemSelectionId = itemId,
                    ItemCode = item?.Code,
                    ItemDescription = item?.Description ?? DDAddItem.SelectedItem.Text,
                    LotNumber = lot,
                    TrfOutQty = qty,
                    Unit = last?.Unit,
                    PriceExclusive = last?.PriceExclusive ?? 0m,
                    TotalUnitPriceExclInclAdd = unitCost,
                    TotalLineValExcl = unitCost * qty,
                    TransactionReference = txtxTrfRef.Text.Trim()
                });
                _db.SaveChanges();
            }

            DDAddItem.SelectedIndex = 0;
            DDAddLot.Items.Clear();
            txtAddQty.Text = "";
            lblAddFrom.Text = lblAddTo.Text = "";
            BindLines();
            return null;
        }

        protected void ddlFromWarehouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlFromWarehouse.SelectedIndex <= 0)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    header.TrfFromID = null;
                    _db.SaveChanges();
                }
            }
            else
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    if (header != null && long.TryParse(ddlFromWarehouse.SelectedValue, out long newFromStoreId))
                    {
                        header.TrfFromID = newFromStoreId;
                        _db.SaveChanges();
                    }
                }
            }
            // Only the add bar depends on From. Reloading the whole header here also reset the
            // To warehouse and any unsaved Reference / Notes back to what was in the database.
            LoadAddItems();
        }

        // Saved the moment it's picked, the same as From. It used to wait for Save Edits / Start,
        // so leaving the slip - or changing From, which reloaded the header - lost it.
        protected void ddlToWarehouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfToID = long.TryParse(ddlToWarehouse.SelectedValue, out long toId) ? toId : (long?)null;
                    _db.SaveChanges();
                }
            }
            ShowAddStock();
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

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                LinkButton lbtn = sender as LinkButton;
                int Lid = Convert.ToInt32(lbtn.CommandArgument);
                _db.ItemTransferLines.RemoveRange(_db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfLID == Lid));
                _db.SaveChanges();
                BindLines();
            }
        }

        protected void lbtnPrintDN_Click(object sender, EventArgs e)
        {
        if (!SaveLastFilledLine()) return;
        CreatePDF();
        Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\Trf_" + lblDocNum.Text, false);
    }

        protected void lbtnComplete_Click(object sender, EventArgs e)
        {

        }

        protected void lbtnStart_Click(object sender, EventArgs e)
        {
            if (!SaveLastFilledLine()) return;

            string result = SaveTransfer();
            if (result != "OK")
            {
                AlertHelper.ShowSweetAlert(this, result, "error");
            }
            else
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    // Lines only exist once added now, so a slip can have none at all - the old
                    // "Count == 1" test relied on the grid always holding one blank row.
                    var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID && x.ItemSelectionId != null).ToList();
                    if (lines.Count == 0)
                    {
                        AlertHelper.ShowSweetAlert(this, "No Lines Saved. Please add lines to transfer.", "error");
                        return;
                    }
                    foreach (var ln in lines)
                    {
                        if (ln.TrfOutQty == null || ln.TrfOutQty < 1)
                        {
                            AlertHelper.ShowSweetAlert(this, "A line has a quantity below 1. Delete it and add it again.", "error");
                            return;
                        }
                    }

                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    if (header != null)
                    {
                        header.TrfStarted = true;
                        header.TrfStatus = "Started";
                        header.TrfDate = DateTime.Now;
                    }
                    _db.SaveChanges();
                    LoadTransfer();
                    AlertHelper.ShowSweetAlert(this, "Transfer Started", "success");
                }
            }
        }
        

        protected void lbtnDelTrf_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Delete all lines for this transfer
                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                _db.ItemTransferLines.RemoveRange(lines);

                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfActive = false;
                    header.TrfStatus = "Deleted";
                }
                _db.SaveChanges();
            }
            Response.Redirect("~/TransferHeaders.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void LbtnSaveEdits_Click(object sender, EventArgs e)
        {
            if (!SaveLastFilledLine()) return;

            string result = SaveTransfer();
            if (result != "OK")
            {
                AlertHelper.ShowSweetAlert(this, result, "error");
            }
            else
            {
                AlertHelper.ShowSweetAlert(this, "Transfer Saved.", "success");
            }           
        }

        // An item + qty still sitting in the add bar is added before Start / Save / Print so it
        // isn't silently dropped. False (reason shown) when that add fails - the caller stops.
        private bool SaveLastFilledLine()
        {
            if (!PnlAddLine.Visible || DDAddItem.SelectedIndex <= 0 || string.IsNullOrWhiteSpace(txtAddQty.Text)) return true;
            string err = AddLine();
            if (err == null) return true;
            AlertHelper.ShowSweetAlert(this, err, "error");
            return false;
        }


        protected string SaveTransfer()
        {
            if (string.IsNullOrWhiteSpace(txtxTrfRef.Text))
            {
                return "Invalid Reference, please enter a reference before continuing";
            }

            // Validate date
            if (!DateTime.TryParse(txtDate.Text, out DateTime trfDate))
            {
                return "Invalid Date format. Please enter a valid date.";
            }

            // Validate From Warehouse
            if (ddlFromWarehouse.SelectedIndex <= 0 || !long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId))
            {
                return "Please select a valid From Warehouse.";
            }

            // Validate To Warehouse
            if (ddlToWarehouse.SelectedIndex <= 0 || !long.TryParse(ddlToWarehouse.SelectedValue, out long toId))
            {
                return "Please select a valid To Warehouse.";
            }

            if (ddlFromWarehouse.SelectedValue == ddlToWarehouse.SelectedValue)
            {
                return "From and To Warehouses cannot be the same.";
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Save header
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfReference = txtxTrfRef.Text.Trim();
                    header.TrfNotes = txtNotes.Text.Trim();
                    header.TrfDate = trfDate;
                    header.TrfFromID = fromId;
                    header.TrfToID = toId;
                }
                try
                {
                    _db.SaveChanges();
                    return "OK";
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        }
        protected async void lbtnCompleteTrf_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // perform Item transfer for each item line
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfComplete = true;
                    header.TrfStatus = "Complete";
                    header.TrfActive = false;
                    header.TrfCompleteDate = DateTime.Now;
                }

                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID && x.ItemSelectionId != null && x.TrfOutQty > 0).ToList();

                long fromStoreId = header.TrfFromID ?? 0;
                long toStoreId = header.TrfToID ?? 0;

                // Read the additional costs straight off the panel (they post with the form regardless
                // of the Save click), falling back to anything saved earlier via the costs panel.
                decimal totalAddCost = 0m;
                decimal.TryParse(txtAddCost.Text, out totalAddCost);
                bool sageUpd = chkSageUpdate.Checked;
                string addReason = (txtAddCostReason.Text ?? "").Trim();
                if (totalAddCost <= 0 && (header.TrfAddCost ?? 0m) > 0)
                {
                    totalAddCost = header.TrfAddCost ?? 0m;
                    sageUpd = header.TrfAddCostSageUpd ?? false;
                    addReason = header.TrfAddCostReason ?? "";
                }
                // Persist what is actually being applied.
                header.TrfAddCost = totalAddCost > 0 ? (decimal?)totalAddCost : header.TrfAddCost;
                header.TrfAddCostReason = string.IsNullOrEmpty(addReason) ? header.TrfAddCostReason : addReason;
                header.TrfAddCostSageUpd = sageUpd;

                // Freight is one charge for the whole slip; amortise by quantity across every unit moved.
                decimal totalQty = lines.Sum(l => l.TrfOutQty ?? 0m);
                decimal freightPerUnit = (totalAddCost > 0 && totalQty > 0) ? totalAddCost / totalQty : 0m;

                foreach (var line in lines)
                {
                    line.TrfInQty = line.TrfOutQty;
                    DoAdjustments(header.TrfReference, line, fromStoreId, toStoreId, freightPerUnit);
                }

                _db.SaveChanges();

                // Optional: lift the item average cost in Sage so qty x avg cost matches both platforms.
                if (sageUpd && totalAddCost > 0 && CurrentUser.UATMode == false)
                {
                    var byItem = lines.Where(l => l.ItemSelectionId != null)
                                      .GroupBy(l => l.ItemSelectionId.Value)
                                      .Select(g => new { ItemID = g.Key, Qty = g.Sum(x => x.TrfOutQty ?? 0m) })
                                      .ToList();

                    foreach (var it in byItem)
                    {
                        var Itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == it.ItemID);
                        if (Itm == null || (Itm.QuantityOnHand ?? 0m) == 0m) continue;

                        decimal totQOH = (decimal)Itm.QuantityOnHand;
                        decimal oldAvg = (decimal)(Itm.AverageCost ?? 0m);
                        decimal itemFreight = freightPerUnit * it.Qty;

                        // Remove qty at the current average, then add it back at the new average that
                        // blends the existing on-hand value with the freight just added.
                        ItemAdjustment iAdj = new ItemAdjustment
                        {
                            Date = DateTime.Now,
                            ItemID = it.ItemID,
                            AverageCost = oldAvg,
                            Quantity = it.Qty * -1,
                            Reason = "ADJ Out Trf " + header.TrfReference + " - " + addReason,
                            Created = DateTime.Now
                        };
                        await SendItemAdjustment(JsonConvert.SerializeObject(iAdj, Formatting.Indented));

                        decimal newAvg = (totQOH * oldAvg + itemFreight) / totQOH;
                        iAdj = new ItemAdjustment
                        {
                            Date = DateTime.Now,
                            ItemID = it.ItemID,
                            AverageCost = newAvg,
                            Quantity = it.Qty,
                            Reason = "ADJ In Trf " + header.TrfReference + " - " + addReason,
                            Created = DateTime.Now
                        };
                        await SendItemAdjustment(JsonConvert.SerializeObject(iAdj, Formatting.Indented));
                    }
                }

                LoadTransfer();
                AlertHelper.ShowSweetAlert(this, "Transfer Completed", "success");
            }
        }

        private async Task SendItemAdjustment(string Item)
        {
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync("ItemAdjustment", Item, CurrentUser);
        }

        private void DoAdjustments(string trfref, ItemTransferLine line, long fromStoreId, long toStoreId, decimal freightPerUnit)
        {
            decimal trfQty = 0, TrfUnitCost = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Record for Receiving store
                ItemTransaction ItemTrans = new ItemTransaction();
                ItemTrans.CompanyID = line.CompanyID;
                ItemTrans.DocumentID = 0;
                ItemTrans.TransactionType = "TRF";
                ItemTrans.ItemID = line.ItemSelectionId;
                ItemTrans.ItemCode = line.ItemCode;
                ItemTrans.ItemDescription = line.ItemDescription;
                ItemTrans.LotNumber = line.LotNumber ?? null;
                ItemTrans.Unit = line.Unit;
                ItemTrans.FromID = toStoreId;
                ItemTrans.ToID = fromStoreId;
                // Same rule as the single transfer: a serial lot moves one unit at a time.
                string serialBlockSlip = SerialGuard.CheckUnitQty(_db, CurrentUser.CoID,
                                                                  line.ItemSelectionId ?? 0, line.ItemCode,
                                                                  (decimal)line.TrfOutQty);
                if (serialBlockSlip != null)
                {
                    AlertHelper.ShowSweetAlert(this, serialBlockSlip + " Nothing was transferred.", "error");
                    return;
                }

                ItemTrans.Qty = line.TrfOutQty * -1;
                trfQty = (decimal)line.TrfOutQty * -1;
                // OUT leg (issuing store): stock leaves at the source store's running weighted
                // average; an outbound movement never changes the source average.
                long itemId = (long)line.ItemSelectionId;
                long coId = (long)line.CompanyID;
                decimal srcAvg = StoreCosting.GetStoreAvgCost(_db, coId, itemId, fromStoreId);
                ItemTrans.PriceExclusive = srcAvg;
                TrfUnitCost = srcAvg;
                ItemTrans.TransactionDate = DateTime.Now;
                ItemTrans.DocumentType = 4;
                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                ItemTrans.AdditionalCosts = 0;
                ItemTrans.TotalUnitPriceExclInclAdd = srcAvg;
                ItemTrans.TotalLineValExcl = srcAvg * trfQty;
                ItemTrans.StoreAvgCost = srcAvg;
                ItemTrans.TransactionReference = line.ItemCode + " " + trfref + " " + trfQty + " out to " + _stores.Where(x => x.StoreID == toStoreId).Select(x => x.StoreCode).FirstOrDefault();
                ItemTrans.ExchRate = 1;
                
                _db.ItemTransactions.Add(ItemTrans);

                // check for itemstore link
                long itmID = Convert.ToInt64(ItemTrans.ItemID);
                // Link the item to the RECEIVING store (toStoreId). This used ItemTrans.ToID, which on
                // the OUT record is the sending store, so the destination never got a link and the item
                // could not be adjusted or picked there afterwards.
                var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == toStoreId && x.ItemID == itmID).FirstOrDefault();
                if (ItS == null)
                {
                    // create item/store link
                    ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                    {
                        CompanyID = CurrentUser.CoID,
                        ItemID = itmID,
                        StoreID = (int?)toStoreId,
                        Active = true,
                    };
                    _db.ItemStoreLinkMasters.Add(isL);
                }

                //// Record for Issuing store
                ItemTransaction ItemTransOUT = new ItemTransaction();
                ItemTransOUT.CompanyID = ItemTrans.CompanyID;
                ItemTransOUT.DocumentID = 0;
                ItemTransOUT.TransactionType = "TRF";
                ItemTransOUT.ItemID = ItemTrans.ItemID;
                ItemTransOUT.ItemCode = ItemTrans.ItemCode;
                ItemTransOUT.ItemDescription = line.ItemDescription;
                ItemTransOUT.LotNumber = line.LotNumber ?? null;
                ItemTransOUT.Unit = line.Unit;
                ItemTransOUT.FromID = fromStoreId;
                ItemTransOUT.ToID = toStoreId;
                ItemTransOUT.Qty = trfQty * -1;
                ItemTransOUT.DocumentType = 4;
                ItemTransOUT.TransactionDate = DateTime.Now;
                ItemTransOUT.ByRoleID = CurrentUser.RoleID; // roleid

                // IN leg (receiving store): arrives at source average + amortised freight, and the
                // destination's running weighted average re-blends against stock already there.
                decimal inQty = trfQty * -1;
                decimal unitLanded = srcAvg + freightPerUnit;
                decimal lineVal;
                decimal destAvg = StoreCosting.ComputeMovement(_db, coId, itemId, toStoreId, inQty, unitLanded * inQty, out lineVal);
                ItemTransOUT.PriceExclusive = srcAvg;
                ItemTransOUT.AdditionalCosts = freightPerUnit;
                ItemTransOUT.TotalUnitPriceExclInclAdd = unitLanded;
                ItemTransOUT.TotalLineValExcl = lineVal;
                ItemTransOUT.StoreAvgCost = destAvg;
                ItemTransOUT.TransactionReference = ItemTrans.ItemCode + " " + trfref + " " + trfQty * -1 + " in from " + _stores.Where(x => x.StoreID == fromStoreId).Select(x => x.StoreCode).FirstOrDefault();
                ItemTransOUT.ExchRate = 1;
                _db.ItemTransactions.Add(ItemTransOUT);
                _db.SaveChanges();
            }
        }

        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
          
            //Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TrL = _db.ItemTransferLines.Where(x => x.TrfID == TrfThisID && x.TrfOutQty >0).OrderBy(x=>x.TrfLID).ToList();
                var DH = _db.ItemTransferHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).FirstOrDefault();
                // Transfer INTO a consignment store (store at a customer's premises) prints as a
                // Consignment Delivery Note: our name + address, the customer's name + delivery
                // address, and "Delivered by / Received by" signature lines. Otherwise unchanged.
                var toStore = DH == null ? null : _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreID == DH.TrfToID);
                bool consign = CurrentUser.UseConsignment && toStore != null && !string.IsNullOrWhiteSpace(toStore.ConsignCustomer);
                string fromText = null, toText = null;
                var co = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
                if (consign)
                {
                    var fromLines = new List<string> { co?.CompanyName };
                    foreach (string a in new[] { co?.BillAddress1, co?.BillAddress2, co?.BillAddress3, co?.BillAddress4 })
                        if (!string.IsNullOrWhiteSpace(a)) fromLines.Add(a.Trim());
                    fromText = string.Join("\n", fromLines.Where(l => !string.IsNullOrWhiteSpace(l)));
                    toText = toStore.ConsignCustomer.Trim() + (string.IsNullOrWhiteSpace(toStore.ConsignAddress) ? "" : "\n" + toStore.ConsignAddress.Trim());
                }
                if (TrL != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
                    PdfWriter writer = null;

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\Trf_" + lblDocNum.Text + ".PDF");
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        writer = PdfWriter.GetInstance(doc, new FileStream(filepath, FileMode.Create));
                        writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_7);
                        writer.SetFullCompression();
                        var footer = new PDFFooter();
                        if (consign) { footer.LeftLabel = "Delivered by"; footer.CenterLabel = "Received by (sign)"; }
                        writer.PageEvent = footer;
                    }
                    catch { }
                    doc.Open();
                    doc.SetMargins(28f, 28f, 100f, 80f);

                    #region Headerinfo
                    PdfPTable table = new PdfPTable(3);
                    PdfPCell cell;
                    table.SetWidths(new int[] { 150, 285, 150 });
                    table.TotalWidth = doc.PageSize.Width - 80;
                    table.LockedWidth = true;
                    iTextSharp.text.Image gif;
                    string imgpath = Server.MapPath("~/images/CoImages/" + CurrentUser.CoID + ".png");
                    if (File.Exists(imgpath))
                    {
                        gif = iTextSharp.text.Image.GetInstance(imgpath);
                        gif.ScaleToFit(125.0F, 60.0F);   // must stay inside the 132pt logo column or it overlaps the barcode
                    }
                    else
                    {
                        gif = null;
                    }

                    try
                    {
                        cell = new PdfPCell(gif);
                    }
                    catch
                    {
                        cell = new PdfPCell(new Phrase(""));
                    }
                    cell.Border = 0;
                    cell.HorizontalAlignment = 0;
                    cell.Rowspan = 5;
                    table.AddCell(cell);

                    Phrase tsHead = new Phrase();
                    if (CurrentUser.MobileModule)
                    {
                        Barcode128 bc = new Barcode128();
                        bc.Code = (DH.TransferID ?? 0).ToString();
                        bc.Font = null;
                        iTextSharp.text.Image bcImg = bc.CreateImageWithBarcode(writer.DirectContent, BaseColor.BLACK, BaseColor.BLACK);
                        bcImg.ScaleToFit(90.0F, 24.0F);   // ~90pt is what the header row leaves beside the 18pt title text
                        tsHead.Add(new Chunk(bcImg, 0, -8, true));
                        tsHead.Add(new Chunk("   ", headfont));
                    }
                    var titlefont = consign ? FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 14, BaseColor.BLACK) : headfont;
                    tsHead.Add(new Chunk(consign ? "Consignment Delivery Note " : "Transfer Slip ", titlefont));
                    cell = new PdfPCell(tsHead);
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.TrfReference ?? "").ToString(), consign ? medfont : headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("From:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    var fromStoreDto = _stores.FirstOrDefault(x => x.StoreID == DH.TrfFromID);
                    cell = new PdfPCell(new Phrase(consign ? fromText : (fromStoreDto != null ? fromStoreDto.StoreDescript : string.Empty), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("To:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    var toStoreDto = _stores.FirstOrDefault(x => x.StoreID == DH.TrfToID);
                    cell = new PdfPCell(new Phrase(consign ? toText : (toStoreDto != null ? toStoreDto.StoreDescript : string.Empty), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.TrfDate).ToString("dd MMM yyyy"), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Completed Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                   if (DH.TrfCompleteDate != null)
                    {
                        cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.TrfCompleteDate).ToString("dd MMM yyyy"), medfont));
                    }
                    else
                    {
                        cell = new PdfPCell(new Phrase("", medfont));
                    }
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(DH.TrfNotes, medfont));
                    cell.Colspan = 2;
                    cell.Border = 0;
                    cell.HorizontalAlignment = 0;
                    table.AddCell(cell);

                    

                    doc.Add(table);
                    #endregion

                    //#region messages
                    //PdfPTable tableM = new PdfPTable(3);
                    //PdfPCell cellM;
                    //tableM.SpacingBefore = 15f;
                    //tableM.TotalWidth = doc.PageSize.Width - 80;
                    //tableM.LockedWidth = true;

                    //cellM = new PdfPCell(new Phrase("Picking Notes:- " + Environment.NewLine + (PS.PSPickMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 80f; ;
                    //tableM.AddCell(cellM);

                    //cellM = new PdfPCell(new Phrase("Packing Notes:- " + Environment.NewLine + (PS.PSPackMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 60f; ;
                    //tableM.AddCell(cellM);

                    //cellM = new PdfPCell(new Phrase("Delivery Notes:- " + Environment.NewLine + (PS.PSDeliverMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 60f; ;
                    //tableM.AddCell(cellM);


                    //doc.Add(tableM);
                    //#endregion


                    #region HeaderRow
                    // Columns follow the company set-up: Bar Code only when UseBarcodes, Lot Number only
                    // when lot tracking is on. Their width goes to the description.
                    bool showBc = co != null && co.UseBarcodes;
                    bool showLot = CurrentUser.CompanyUseLotNumbers;
                    var colWidths = new List<int> { 50, 150 + (showBc ? 0 : 60) + (showLot ? 0 : 100) };
                    if (showBc) colWidths.Add(60);
                    colWidths.Add(30);
                    if (showLot) colWidths.Add(100);
                    colWidths.AddRange(new[] { 40, 40, 40 });
                    PdfPTable table4 = new PdfPTable(colWidths.Count);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(colWidths.ToArray());
                    table4.TotalWidth = doc.PageSize.Width - 80;
                    table4.LockedWidth = true;

                    cell4 = new PdfPCell(new Phrase("Item Code", regfont));
                    cell4.HorizontalAlignment = 0;
                    cell4.FixedHeight = 20f; ;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Description", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    if (showBc)
                    {
                    cell4 = new PdfPCell(new Phrase("Bar Code", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);
                    }

                    cell4 = new PdfPCell(new Phrase("Unit", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    if (showLot)
                    {
                    cell4 = new PdfPCell(new Phrase("Lot Number", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);
                    }

                    cell4 = new PdfPCell(new Phrase("Out Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("In Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Note", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    #endregion

                    
                    foreach (var DL in TrL)
                    {
                        cell4 = new PdfPCell(new Phrase(DL.ItemCode ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.FixedHeight = 20f;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.ItemDescription ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        if (showBc)
                        {
                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);
                        }

                        cell4 = new PdfPCell(new Phrase(DL.Unit ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        if (showLot)
                        {
                        cell4 = new PdfPCell(new Phrase(DL.LotNumber ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);
                        }

                        string outQty = "";
                        if (DL.TrfOutQty != null)
                        {
                            outQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.TrfOutQty.ToString(), CurrentUser.CompanyDecPlaces)).ToString();
                        }
                        cell4 = new PdfPCell(new Phrase(outQty.ToString(), regfont));
                        //cell4 = new PdfPCell(new Phrase(DL.Quantity.ToString(), regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        string InQty = "";
                        if (DL.TrfInQty != null)
                        {
                            InQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.TrfInQty.ToString(), CurrentUser.CompanyDecPlaces)).ToString();
                        }
                        cell4 = new PdfPCell(new Phrase(InQty, regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }

                    doc.Add(table4);


                    doc.AddTitle(consign ? "Consignment Delivery Note: " : "Transfer Slip: ");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
        }

        protected void lbtnAddShip_Click(object sender, EventArgs e)
        {

        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {

        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            decimal addCost = 0;
            decimal.TryParse(txtAddCost.Text, out addCost);
            if (addCost <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "Please enter a valid additional cost amount.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfAddCost = addCost;
                    header.TrfAddCostReason = (txtAddCostReason.Text ?? "").Trim();
                    header.TrfAddCostSageUpd = chkSageUpdate.Checked;
                    _db.SaveChanges();
                }
            }

            AlertHelper.ShowSweetAlert(this, "Additional costs saved. Complete the transfer to apply.", "success");
        }
    }
}
