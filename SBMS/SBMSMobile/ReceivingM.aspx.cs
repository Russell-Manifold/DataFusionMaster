using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ReceivingM : BasePage
    {
        // ── State ─────────────────────────────────────────────────────────────

        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private long DocID
        {
            get { return ViewState["DocID"] != null ? (long)ViewState["DocID"] : 0; }
            set { ViewState["DocID"] = value; }
        }

        private int MatchedLineID
        {
            get { return ViewState["MatchedLineID"] != null ? (int)ViewState["MatchedLineID"] : 0; }
            set { ViewState["MatchedLineID"] = value; }
        }

        private bool IsProcessing
        {
            get { return ViewState["IsProcessing"] != null && (bool)ViewState["IsProcessing"]; }
            set { ViewState["IsProcessing"] = value; }
        }

        // StoreCode selected via the one-time store modal (e.g. "FG")
        private string SelectedStore
        {
            get { return ViewState["SelectedStore"] as string; }
            set { ViewState["SelectedStore"] = value; }
        }

        // Full display name for the selected store (e.g. "FG - Finished Goods")
        private string SelectedStoreName
        {
            get { return ViewState["SelectedStoreName"] as string; }
            set { ViewState["SelectedStoreName"] = value; }
        }

        private HashSet<int> SavedLineIDs
        {
            get
            {
                if (Session["SavedLineIDs"] == null)
                    Session["SavedLineIDs"] = new HashSet<int>();
                return (HashSet<int>)Session["SavedLineIDs"];
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            SessionValidator.ValidateUserSession(CurrentUser);

            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false);
                return;
            }

            lblUsername.Text = CurrentUser.UserName;

            if (!IsPostBack)
            {
                Session.Remove("SavedLineIDs");

                string docGuidStr = Request.QueryString["docid"];
                if (string.IsNullOrEmpty(docGuidStr) ||
                    !Guid.TryParse(docGuidStr, out Guid docGuid))
                {
                    Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
                    return;
                }

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = db.DocHeaders
                        .FirstOrDefault(h => h.DocGUID == docGuid
                                          && h.CompanyID == CurrentUser.CoID);
                    if (header == null)
                    {
                        Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
                        return;
                    }

                    DocID = header.DocID;

                    lblPONum.Text    = header.DocumentNumber ?? "-";
                    lblSupplier.Text = header.CustSupName ?? "-";
                    lblDueDate.Text  = header.DueDelDate.HasValue
                                         ? header.DueDelDate.Value.ToString("dd MMM yyyy")
                                         : "-";

                    if (header.Complete == true)
                    {
                        lbtnRecAll.Enabled   = false;
                        lbtnFinalize.Enabled = false;
                    }
                }

                txtRecDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                BindLines();

                // Show store selection modal on first load
                LoadStoresModal();
                pnlStoreModal.Visible = true;
            }

            UpdateStoreIndicator();
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private List<DocLine> GetOutstandingLines()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                return db.DocLines
                    .Where(l => l.DocID == DocID
                             && l.CompanyID == CurrentUser.CoID
                             && l.LineType == 0
                             && (l.ReceiveComplete == null || l.ReceiveComplete == false))
                    .OrderBy(l => l.LineID)
                    .ToList();
            }
        }

        private void BindLines()
        {
            var lines = GetOutstandingLines();
            lblLineCount.Text   = lines.Count.ToString();
            lblEmpty.Visible    = lines.Count == 0;
            rptLines.DataSource = lines;
            rptLines.DataBind();
        }

        private List<Store> GetAllStores()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                return db.Stores
                    .Where(s => s.CompanyID == CurrentUser.CoID
                             && s.StoreActive == true
                             && s.AllowReceiving == true
                             && s.StoreCode != "CoR"
                             && s.StoreCode != "CoD"
                             && s.StoreCode.ToLower() != "scr")
                    .OrderBy(s => s.StoreCode)
                    .ToList();
            }
        }

        private void LoadStoresModal()
        {
            var stores = GetAllStores();
            ddStoreGlobal.Items.Clear();
            ddStoreGlobal.Items.Add(new ListItem("— Select a store —", ""));
            foreach (var s in stores)
                ddStoreGlobal.Items.Add(new ListItem(
                    s.StoreCode + " - " + s.StoreDescript, s.StoreCode));
        }

        private void UpdateStoreIndicator()
        {
            if (!string.IsNullOrEmpty(SelectedStore))
            {
                lblSelectedStoreName.Text = SelectedStoreName ?? SelectedStore;
                pnlStoreIndicator.Visible = true;
            }
            else
            {
                pnlStoreIndicator.Visible = false;
            }
        }

        // ── Store modal ───────────────────────────────────────────────────────

        protected void lbtnConfirmStore_Click(object sender, EventArgs e)
        {
            string chosen = ddStoreGlobal.SelectedValue;
            if (string.IsNullOrEmpty(chosen))
            {
                lblStoreModalError.Text    = "Please select a store to continue.";
                lblStoreModalError.Visible = true;
                return;
            }

            SelectedStore     = chosen;
            SelectedStoreName = ddStoreGlobal.SelectedItem?.Text ?? chosen;

            pnlStoreModal.Visible      = false;
            lblStoreModalError.Visible = false;
            UpdateStoreIndicator();
        }

        protected void lbtnModalCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnChangeStore_Click(object sender, EventArgs e)
        {
            LoadStoresModal();
            if (!string.IsNullOrEmpty(SelectedStore))
            {
                var item = ddStoreGlobal.Items.FindByValue(SelectedStore);
                if (item != null) item.Selected = true;
            }
            lblStoreModalError.Visible = false;
            pnlStoreModal.Visible      = true;
        }

        // ── Repeater binding ──────────────────────────────────────────────────

        protected void rptLines_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var line = (DocLine)e.Item.DataItem;

            bool isMatched = line.LineID == MatchedLineID;
            bool isSaved   = SavedLineIDs.Contains(line.LineID);

            var hf = (HiddenField)e.Item.FindControl("hfLineID");
            if (hf != null && isMatched)
                hf.Value = "matched:" + line.LineID;

            var txtQty = (TextBox)e.Item.FindControl("txtRecQty");
            if (txtQty != null && isMatched)
                txtQty.Text = (line.QtyLeft ?? line.Quantity ?? 1).ToString("0.##");

            var badge = (Label)e.Item.FindControl("lblSavedBadge");
            if (badge != null) badge.Visible = isSaved;

            var saveBtn = (LinkButton)e.Item.FindControl("lbtnSaveLine");
            if (saveBtn != null && isSaved) saveBtn.Enabled = false;

            // Lot number display (global store selection is now used instead of per-line store)
            if (CurrentUser.CompanyUseLotNumbers == true)
            {
                var lotLabel = (Label)e.Item.FindControl("lblLotNum");
                var hfLotNum = (HiddenField)e.Item.FindControl("hfLotNum");

                if (!string.IsNullOrEmpty(line.LotNumber))
                {
                    using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var itm = db.ItemsMasters.FirstOrDefault(
                            x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);

                        if (itm != null && itm.IsLotTracked == true)
                        {
                            if (lotLabel != null)
                            {
                                lotLabel.Text = "Lot #: " + line.LotNumber;
                                lotLabel.Attributes["style"] =
                                    "font-size:.75em;color:#666;display:block;margin-top:.3em;";
                            }
                            if (hfLotNum != null)
                                hfLotNum.Value = line.LotNumber;
                        }
                    }
                }
            }
        }

        private int GetLotNum(long coID)
        {
            DateTime dtY = DateTime.Today.AddDays(-1);
            DateTime dtT = DateTime.Today.AddDays(1);
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                int count = db.LotTrackingMasters
                    .Count(it => it.CompanyID == coID
                              && it.CreatedDate > dtY
                              && it.CreatedDate < dtT);
                return count + 1;
            }
        }

        // ── Barcode scan ──────────────────────────────────────────────────────

        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            string raw = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                ClearFeedback();
                return;
            }

            GetOneItemFromBarcode_Result barcodeItem = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                barcodeItem = db.GetOneItemFromBarcode(CurrentUser.CoID, raw)
                                .FirstOrDefault();
            }

            if (barcodeItem == null)
            {
                SetFeedback(false, $"&#128683; Barcode not recognised: {raw}");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            DocLine matchedLine = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                matchedLine = db.DocLines.FirstOrDefault(l =>
                    l.DocID == DocID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode == barcodeItem.Code
                    && (l.ReceiveComplete == null || l.ReceiveComplete == false));
            }

            if (matchedLine == null)
            {
                SetFeedback(false,
                    $"&#9888; <strong>{barcodeItem.Code}</strong> ({barcodeItem.Description}) " +
                    $"is not on this PO or is already fully received.");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            SetFeedback(true,
                $"&#10003; Found: <strong>{barcodeItem.Code}</strong> &mdash; {barcodeItem.Description}");
            MatchedLineID = matchedLine.LineID;
            txtBarcode.Text = string.Empty;
            BindLines();
        }

        protected void lbtnClearScan_Click(object sender, EventArgs e)
        {
            txtBarcode.Text = string.Empty;
            MatchedLineID   = 0;
            ClearFeedback();
            BindLines();
        }

        // ── Per-line Receive ──────────────────────────────────────────────────

        protected void lbtnSaveLine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedStore))
            {
                SetFeedback(false, "&#9888; Please select a receiving store first.");
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            var btn  = (LinkButton)sender;
            var item = btn.NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf     = (HiddenField)item.FindControl("hfLineID");
            var txtQty = (TextBox)item.FindControl("txtRecQty");
            if (hf == null || txtQty == null) return;

            string rawId = hf.Value.Replace("matched:", "").Trim();
            if (!int.TryParse(rawId, out int lineId)) return;

            if (!decimal.TryParse(txtQty.Text.Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Please enter a valid quantity.");
                return;
            }

            SaveLine(lineId, qty, SelectedStore);
        }

        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            // Handled via lbtnSaveLine_Click
        }

        private void SaveLine(int lineId, decimal qty, string storeCode)
        {
            if (SavedLineIDs.Contains(lineId))
            {
                SetFeedback(false, "&#9888; This line has already been saved.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.DocLines.FirstOrDefault(
                    l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return;

                line.ReceiveQty = qty;
                line.StoreCode  = storeCode;
                line.ToReceive  = true;
                line.QtyLeft    = (line.QtyLeft ?? line.Quantity ?? 0) - qty;
                if (line.QtyLeft < 0) line.QtyLeft = 0;

                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    var itm = db.ItemsMasters.FirstOrDefault(
                        x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);

                    if (itm != null && itm.IsLotTracked == true)
                    {
                        int recnum    = GetLotNum(CurrentUser.CoID);
                        string lotNum = DateTime.Today.ToString("ddMMyyyy")
                                      + storeCode
                                      + recnum.ToString();

                        line.LotNumber = lotNum;

                        LotTrackingMaster lt = new LotTrackingMaster
                        {
                            LotNumber   = lotNum,
                            CreatedDate = DateTime.Now,
                            CompanyID   = CurrentUser.CoID,
                            ItemCode    = line.ItemCode,
                            ItemId      = line.SelectionId,
                            LotActive   = true,
                            LotQuantity = qty
                        };
                        db.LotTrackingMasters.Add(lt);
                    }
                }

                var header = db.DocHeaders.FirstOrDefault(
                    h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                if (header != null && header.Started != true)
                    header.Started = true;

                db.SaveChanges();
            }

            SavedLineIDs.Add(lineId);
            MatchedLineID = 0;
            ClearFeedback();
            BindLines();
        }

        // ── Receive All ───────────────────────────────────────────────────────

        protected void lbtnRecAll_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedStore))
            {
                SetFeedback(false, "&#9888; Please select a receiving store first.");
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            var lines = GetOutstandingLines();
            if (!lines.Any()) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                int recnum = GetLotNum(CurrentUser.CoID);

                foreach (var line in lines)
                {
                    if (SavedLineIDs.Contains(line.LineID)) continue;

                    decimal remainingQty = line.QtyLeft ?? line.Quantity ?? 0;
                    if (remainingQty <= 0) continue;

                    var dbLine = db.DocLines.FirstOrDefault(
                        l => l.LineID == line.LineID && l.CompanyID == CurrentUser.CoID);
                    if (dbLine == null) continue;

                    dbLine.ReceiveQty = remainingQty;
                    dbLine.StoreCode  = SelectedStore;
                    dbLine.ToReceive  = true;

                    if (CurrentUser.CompanyUseLotNumbers == true)
                    {
                        var itm = db.ItemsMasters.FirstOrDefault(
                            x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);

                        if (itm != null && itm.IsLotTracked == true)
                        {
                            string lotNum = DateTime.Today.ToString("ddMMyyyy")
                                          + SelectedStore
                                          + recnum.ToString();

                            dbLine.LotNumber = lotNum;

                            LotTrackingMaster lt = new LotTrackingMaster
                            {
                                LotNumber   = lotNum,
                                CreatedDate = DateTime.Now,
                                CompanyID   = CurrentUser.CoID,
                                ItemCode    = line.ItemCode,
                                ItemId      = line.SelectionId,
                                LotActive   = true,
                                LotQuantity = remainingQty
                            };
                            db.LotTrackingMasters.Add(lt);
                            recnum++;
                        }
                    }

                    SavedLineIDs.Add(line.LineID);
                }

                var header = db.DocHeaders.FirstOrDefault(
                    h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                if (header != null && header.Started != true)
                    header.Started = true;

                db.SaveChanges();
            }

            MatchedLineID = 0;
            SetFeedback(true, "&#10003; All lines marked for receiving.");
            BindLines();
        }

        // ── Finalize GRN – show / hide panel ─────────────────────────────────

        protected void lbtnFinalize_Click(object sender, EventArgs e)
        {
            bool hasLines;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                hasLines = db.DocLines.Any(l =>
                    l.DocID == DocID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ToReceive == true
                    && (l.ReceiveComplete == null || l.ReceiveComplete == false));
            }

            if (!hasLines)
            {
                SetFeedback(false, "&#9888; No lines marked for receiving yet. Mark lines first.");
                return;
            }

            if (string.IsNullOrEmpty(txtRecDate.Text))
                txtRecDate.Text = DateTime.Today.ToString("yyyy-MM-dd");

            lblFinalizeError.Visible = false;
            pnlFinalize.Visible      = true;
        }

        protected void lbtnCancelFinalize_Click(object sender, EventArgs e)
        {
            pnlFinalize.Visible = false;
        }

        // ── Finalize GRN – generate ───────────────────────────────────────────

        protected async void lbtnConfirmGRN_Click(object sender, EventArgs e)
        {
            if (IsProcessing) return;
            IsProcessing           = true;
            lbtnConfirmGRN.Enabled = false;

            ApiUrlCall api = new ApiUrlCall();

            try
            {
                string dnNum  = (txtDNNum.Text  ?? "").Trim();
                string invNum = (txtInvNum.Text ?? "").Trim();

                if (dnNum.Length < 3 && invNum.Length < 3)
                {
                    SetFinalizeError("Please enter a D/N Number or Invoice Number (at least 3 characters).");
                    return;
                }

                if (!DateTime.TryParse(txtRecDate.Text, out DateTime recDate))
                {
                    SetFinalizeError("Invalid Receive Date — please select a valid date.");
                    return;
                }

                List<DocLine> fLines;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    fLines = db.DocLines
                        .Where(l => l.DocID == DocID
                                 && l.CompanyID == CurrentUser.CoID
                                 && l.ToReceive == true
                                 && (l.ReceiveComplete == null || l.ReceiveComplete == false))
                        .ToList();
                }

                if (!fLines.Any())
                {
                    SetFinalizeError("No lines are staged for receiving. Please mark lines first.");
                    return;
                }

                DocHeader poHeader;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    poHeader = db.DocHeaders.FirstOrDefault(
                        h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                }
                if (poHeader == null)
                {
                    SetFinalizeError("Purchase order not found.");
                    return;
                }

                string suppInvRef = invNum.Length >= 3 ? invNum : dnNum;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    int dupCount = db.DocHeaders.Count(h =>
                        h.CompanyID == CurrentUser.CoID
                        && h.SupplierInvNum == suppInvRef
                        && h.DocID != DocID);

                    if (dupCount > 0)
                    {
                        SetFinalizeError("Supplier invoice number already used — cannot continue.");
                        return;
                    }
                }

                decimal exchRate = poHeader.Supplier_ExchangeRate ?? 1m;

                SupplierInvoiceHeader docHeader = new SupplierInvoiceHeader
                {
                    DueDate            = DateTime.Now,
                    SupplierId         = poHeader.CustSuppID ?? 0,
                    SupplierName       = poHeader.CustSupName ?? "",
                    StatusId           = 1,
                    Date               = DateTime.Now,
                    Inclusive          = poHeader.Inclusive ?? false,
                    DiscountPercentage = poHeader.DiscountPercentage ?? 0m,
                    TaxReference       = poHeader.TaxReference ?? "",
                    Reference          = suppInvRef,
                    Message            = poHeader.Message ?? "",
                    FromDocument       = poHeader.DocumentNumber ?? ""
                };

                var documentLines = new List<DocumentLine>();
                foreach (var dl in fLines)
                {
                    if ((dl.ItemType ?? 0) >= 2) continue;

                    if ((dl.ItemType ?? 0) == 0 && string.IsNullOrEmpty(dl.StoreCode))
                    {
                        SetFinalizeError(
                            $"Item '{dl.ItemDescription}' has no store selected — cannot continue.");
                        return;
                    }

                    var dLine = new DocumentLine
                    {
                        SelectionId        = dl.SelectionId,
                        TaxTypeId          = dl.LineTaxTypeID ?? 0,
                        Description        = dl.ItemDescription ?? "",
                        LineType           = dl.LineType ?? 0,
                        Quantity           = dl.ReceiveQty ?? 0m,
                        UnitPriceExclusive = dl.UnitPriceExclusive ?? 0m,
                        UnitPriceInclusive = dl.UnitPriceInclusive ?? 0m,
                        TaxPercentage      = dl.TaxPercentage ?? 0m,
                        Unit               = dl.Unit ?? "",
                        DiscountPercentage = dl.DiscountPercentage ?? 0m,
                        Exclusive          = dl.Exclusive ?? 0m,
                        Discount           = dl.Discount ?? 0m,
                        Tax                = dl.Tax ?? 0m,
                        Total              = dl.Total ?? 0m,
                        Comments           = BuildLineComment(dl),
                        AnalysisCategoryId1 = dl.AnalysisCategoryId1 ?? 0,
                        AnalysisCategoryId2 = dl.AnalysisCategoryId2 ?? 0,
                        AnalysisCategoryId3 = dl.AnalysisCategoryId3 ?? 0,
                        ExchRate           = exchRate,
                        localCurrLineVal   = ((dl.ReceiveQty ?? 0m) * (dl.UnitPriceExclusive ?? 0m)) / exchRate
                    };
                    documentLines.Add(dLine);
                }

                Document doc = new Document { Header = docHeader, Lines = documentLines };

                object jsonObject;
                if (exchRate == 1m)
                {
                    jsonObject = new
                    {
                        doc.Header.DueDate,
                        doc.Header.SupplierId,
                        doc.Header.SupplierName,
                        doc.Header.StatusId,
                        doc.Header.Date,
                        doc.Header.Inclusive,
                        doc.Header.DiscountPercentage,
                        doc.Header.TaxReference,
                        doc.Header.Reference,
                        doc.Header.Message,
                        doc.Header.FromDocument,
                        doc.Lines
                    };
                }
                else
                {
                    jsonObject = new
                    {
                        doc.Header.DueDate,
                        doc.Header.SupplierId,
                        doc.Header.SupplierName,
                        doc.Header.StatusId,
                        doc.Header.Date,
                        doc.Header.Inclusive,
                        doc.Header.DiscountPercentage,
                        doc.Header.TaxReference,
                        doc.Header.Reference,
                        doc.Header.Message,
                        doc.Header.FromDocument,
                        doc.Header.Supplier_ExchangeRate,
                        doc.Header.Supplier_CurrencyId,
                        doc.Lines
                    };
                }

                var rawJson = JsonConvert.SerializeObject(jsonObject);
                var jObj    = JObject.Parse(rawJson);

                foreach (var line in jObj["Lines"])
                {
                    line["CurrencyId"]?.Parent?.Remove();
                    line["ExchRate"]?.Parent?.Remove();
                    line["localCurrLineVal"]?.Parent?.Remove();
                }
                string jsonBody = jObj.ToString(Formatting.Indented);

                string supInvResult = await SendSupplierInvoice(jsonBody, api);

                string[] parts = supInvResult.Split('|');
                if (!long.TryParse(parts[0], out long _))
                {
                    SetFinalizeError($"Sage error sending Supplier Invoice: {supInvResult}");
                    api.LogErrorToFile(
                        $"CoID:{CurrentUser.CoID} ReceivingM GRN Sage error – {supInvResult}");
                    return;
                }
                string suppInvNum = parts.Length > 1 ? parts[1] : suppInvRef;

                if (chkReceivingComplete.Checked)
                {
                    try
                    {
                        var poUpdateObj = new
                        {
                            ID           = DocID,
                            StatusId     = 4,
                            DeliveryDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                        };
                        string poUpdateJson = JsonConvert.SerializeObject(poUpdateObj, Formatting.Indented);
                        await api.APIUpdatePurchaseOrderAsync("PurchaseOrder", poUpdateJson, CurrentUser);
                    }
                    catch (Exception ex)
                    {
                        api.LogErrorToFile(
                            $"CoID:{CurrentUser.CoID} ReceivingM PO status update error – {ex.Message}");
                    }
                }

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    foreach (var dl in fLines)
                    {
                        if ((dl.ItemType ?? 0) == 0)
                        {
                            decimal convRate = 1m;
                            try
                            {
                                var itm = db.ItemsMasters.FirstOrDefault(
                                    x => x.CompanyID == CurrentUser.CoID
                                      && x.ID == dl.SelectionId);
                                convRate = (decimal)(itm?.UOMConvert ?? 1m);
                                if (convRate == 0m) convRate = 1m;
                            }
                            catch { }

                            decimal unitPriceExcl = dl.UnitPriceExclusive ?? 0m;
                            decimal recvQty       = dl.ReceiveQty ?? 0m;

                            long fromStoreId = getstoreid("CoR", db);
                            long toStoreId   = getstoreid(dl.StoreCode, db);

                            var itemTrans = new ItemTransaction
                            {
                                CompanyID                  = CurrentUser.CoID,
                                DocumentID                 = DocID,
                                DocumentType               = 2,
                                TransactionType            = "GRN",
                                ItemID                     = dl.SelectionId,
                                ItemCode                   = dl.ItemCode,
                                ItemDescription            = dl.ItemDescription,
                                LotNumber                  = dl.LotNumber,
                                Unit                       = dl.Unit,
                                FromID                     = fromStoreId,
                                ToID                       = toStoreId,
                                Qty                        = recvQty * convRate,
                                PriceExclusive             = (unitPriceExcl / exchRate) / convRate,
                                AdditionalCosts            = 0m,
                                TotalUnitPriceExclInclAdd  = (unitPriceExcl / exchRate) / convRate,
                                TotalLineValExcl           = (unitPriceExcl * recvQty) / exchRate,
                                TransactionDate            = DateTime.Now,
                                ByRoleID                   = CurrentUser.RoleID,
                                TransactionReference       = suppInvNum,
                                ExchRate                   = exchRate
                            };
                            db.ItemTransactions.Add(itemTrans);

                            bool linkExists = db.ItemStoreLinkMasters.Any(x =>
                                x.CompanyID == CurrentUser.CoID
                                && x.StoreID == (int?)toStoreId
                                && x.ItemID  == dl.SelectionId);

                            if (!linkExists)
                            {
                                db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                                {
                                    CompanyID = CurrentUser.CoID,
                                    ItemID    = dl.SelectionId,
                                    StoreID   = (int?)toStoreId,
                                    Active    = true
                                });
                            }
                        }

                        var dbLine = db.DocLines.FirstOrDefault(
                            l => l.LineID == dl.LineID && l.CompanyID == CurrentUser.CoID);
                        if (dbLine != null)
                            dbLine.ReceiveComplete = true;
                    }

                    var hdr = db.DocHeaders.FirstOrDefault(
                        h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                    if (hdr != null)
                    {
                        hdr.Complete       = chkReceivingComplete.Checked;
                        hdr.CompleteDate   = DateTime.Now;
                        hdr.CompBy         = 0;
                        hdr.SupplierInvNum = suppInvNum;
                        if (dnNum.Length  > 0) hdr.DNNum  = dnNum;
                        if (invNum.Length > 0) hdr.InvNum = invNum;
                    }

                    db.SaveChanges();
                }

                pnlFinalize.Visible   = false;
                lbtnFinalize.Enabled  = false;
                lbtnRecAll.Enabled    = false;
                Session.Remove("SavedLineIDs");

                SetFeedback(true,
                    $"&#10003; GRN generated successfully &mdash; {suppInvNum}");
                BindLines();
            }
            catch (Exception ex)
            {
                SetFinalizeError($"An unexpected error occurred: {ex.Message}");
                api.LogErrorToFile(
                    $"CoID:{CurrentUser.CoID} ReceivingM lbtnConfirmGRN_Click – {ex}");
            }
            finally
            {
                IsProcessing           = false;
                lbtnConfirmGRN.Enabled = true;
            }
        }

        // ── Finalize helpers ──────────────────────────────────────────────────

        private async Task<string> SendSupplierInvoice(string jsonBody, ApiUrlCall api)
        {
            try
            {
                JObject result = await api.APIPostDocumentAsync(
                    "SupplierInvoice", jsonBody, CurrentUser);

                if (result != null && result.ContainsKey("ID"))
                    return result["ID"].ToString() + "|" + result["DocumentNumber"].ToString();

                return result?.ToString() ?? "Unknown error";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private long getstoreid(string storeCode, SBMSEntities db)
        {
            if (string.IsNullOrEmpty(storeCode)) return 0;
            var store = db.Stores.FirstOrDefault(
                x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID);
            return store?.StoreID ?? 0;
        }

        private string BuildLineComment(DocLine dl)
        {
            bool hasStore = !string.IsNullOrEmpty(dl.StoreCode);
            bool hasLot   = !string.IsNullOrEmpty(dl.LotNumber);
            string suffix = dl.Comments ?? "";

            if (hasStore && hasLot)
                return $"Store: {dl.StoreCode} - Lot # {dl.LotNumber} : {suffix}";
            if (hasStore)
                return $"Store: {dl.StoreCode} : {suffix}";
            if (hasLot)
                return $"Lot # {dl.LotNumber} : {suffix}";
            return suffix;
        }

        private void SetFinalizeError(string msg)
        {
            lblFinalizeError.Text    = "&#9888; " + msg;
            lblFinalizeError.Visible = true;
            pnlFinalize.Visible      = true;
        }

        // ── Navigation ────────────────────────────────────────────────────────

        protected void lbtnTopBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnTopHome_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/DashboardM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ── Feedback helpers ──────────────────────────────────────────────────

        private void SetFeedback(bool found, string html)
        {
            lblScanFeedback.Text     = html;
            lblScanFeedback.CssClass = found ? "mob-feedback found" : "mob-feedback notfound";
        }

        private void ClearFeedback()
        {
            lblScanFeedback.Text     = string.Empty;
            lblScanFeedback.CssClass = "mob-feedback";
        }
    }
}
