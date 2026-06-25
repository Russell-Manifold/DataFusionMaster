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
using System.Web.UI.WebControls;

namespace SBMS
{
    // Mobile/scanner DIRECT RECEIVE (Scenario 1: scan straight onto receiving).
    // Opens a PO, scans each line's qty + a destination location, then Finalise posts ONE Sage
    // supplier invoice (GRN) and lands each line's stock directly into its scanned location.
    // (Scenario 2 = receive on the web into a holding store, then put away with ReceivingM.)
    public partial class ReceiveScanM : BasePage
    {
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

            // Manual lot numbers → scanner receiving disabled (web only).
            if (CurrentUser.CompanyUseLotNumbers && !CurrentUser.CompanyAllowSystemLotNumbers)
            {
                Response.Redirect("~/SBMSMobile/DashboardM.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                string docGuidStr = Request.QueryString["docid"];
                if (string.IsNullOrEmpty(docGuidStr) || !Guid.TryParse(docGuidStr, out Guid docGuid))
                {
                    Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
                    return;
                }

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = db.DocHeaders.FirstOrDefault(h => h.DocGUID == docGuid
                                                                && h.CompanyID == CurrentUser.CoID);
                    if (header == null)
                    {
                        Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
                        return;
                    }

                    DocID            = header.DocID;
                    lblPONum.Text    = header.DocumentNumber ?? "-";
                    lblSupplier.Text = header.CustSupName ?? "-";
                    lblDueDate.Text  = header.DueDelDate.HasValue
                                         ? header.DueDelDate.Value.ToString("dd MMM yyyy") : "-";

                    if (header.Complete == true)
                        lbtnFinalize.Enabled = false;
                }

                txtRecDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                BindLines();
            }
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private static decimal BaseOutstanding(DocLine l)
        {
            return l.QtyLeft ?? l.Quantity ?? 0;
        }

        private List<DocLine> GetOutstandingLines()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Keep fully-staged lines visible (greyed) until the GRN is finalised, so the
                // operator can see what's done. Submitted lines drop off via ReceiveComplete.
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

        protected void rptLines_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var line = (DocLine)e.Item.DataItem;
            bool isMatched = line.LineID == MatchedLineID;

            var hf = (HiddenField)e.Item.FindControl("hfLineID");
            if (hf != null && isMatched) hf.Value = "matched:" + line.LineID;

            var pnlCard = (Panel)e.Item.FindControl("pnlCard");
            if (pnlCard != null && isMatched) pnlCard.CssClass = "mob-linecard matched";

            var lblRemaining = (Label)e.Item.FindControl("lblRemaining");
            if (lblRemaining != null) lblRemaining.Text = BaseOutstanding(line).ToString("0.##");

            var txtQty = (TextBox)e.Item.FindControl("txtRecQty");
            if (txtQty != null && isMatched)
                txtQty.Text = BaseOutstanding(line).ToString("0.##");

            var txtLoc = (TextBox)e.Item.FindControl("txtLoc");
            if (txtLoc != null && !string.IsNullOrEmpty(line.StoreCode))
                txtLoc.Text = line.StoreCode;

            var badge = (Label)e.Item.FindControl("lblSavedBadge");
            if (badge != null) badge.Visible = (line.ToReceive == true) && (line.ReceiveQty ?? 0) > 0;

            // Fully received → grey out (and disable) the save button.
            var saveBtn = (LinkButton)e.Item.FindControl("lbtnSaveLine");
            if (saveBtn != null && BaseOutstanding(line) <= 0)
            {
                saveBtn.Enabled = false;
                saveBtn.Attributes["style"] = "background:#cccccc;border-color:#cccccc;color:#777;";
            }

            if (CurrentUser.CompanyUseLotNumbers == true && !string.IsNullOrEmpty(line.LotNumber))
            {
                var lotLabel = (Label)e.Item.FindControl("lblLotNum");
                if (lotLabel != null)
                {
                    using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var itm = db.ItemsMasters.FirstOrDefault(
                            x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);
                        if (itm != null && itm.IsLotTracked == true)
                        {
                            lotLabel.Text = "Lot #: " + line.LotNumber;
                            lotLabel.Attributes["style"] =
                                "font-size:.75em;color:#666;display:block;margin-top:.3em;";
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
                return db.LotTrackingMasters.Count(it => it.CompanyID == coID
                    && it.CreatedDate > dtY && it.CreatedDate < dtT) + 1;
            }
        }

        // ── Scan ────────────────────────────────────────────────────────────────

        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            string raw = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(raw)) { ClearFeedback(); return; }

            GetOneItemFromBarcode_Result item = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            if (item == null)
            {
                SetFeedback(false, $"&#128683; Barcode not recognised: {raw}");
                MatchedLineID = 0; BindLines(); return;
            }

            DocLine matched = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                matched = db.DocLines.FirstOrDefault(l => l.DocID == DocID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode == item.Code
                    && l.LineType == 0
                    && (l.ReceiveComplete == null || l.ReceiveComplete == false)
                    && (l.QtyLeft ?? l.Quantity ?? 0) > 0);
            }

            if (matched == null)
            {
                SetFeedback(false,
                    $"&#9888; <strong>{item.Code}</strong> ({item.Description}) is not on this PO or is fully received.");
                MatchedLineID = 0; BindLines(); return;
            }

            SetFeedback(true, $"&#10003; Found: <strong>{item.Code}</strong> &mdash; {item.Description}");
            MatchedLineID = matched.LineID;
            txtBarcode.Text = string.Empty;
            BindLines();
        }

        protected void lbtnClearScan_Click(object sender, EventArgs e)
        {
            txtBarcode.Text = string.Empty;
            MatchedLineID = 0;
            ClearFeedback();
            BindLines();
        }

        // ── Capture a line: qty + destination location (staged on the DocLine) ──

        protected void lbtnSaveLine_Click(object sender, EventArgs e)
        {
            var item = ((LinkButton)sender).NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf     = (HiddenField)item.FindControl("hfLineID");
            var txtQty = (TextBox)item.FindControl("txtRecQty");
            var txtLoc = (TextBox)item.FindControl("txtLoc");
            if (hf == null) return;

            string rawId = hf.Value.Replace("matched:", "").Trim();
            if (!int.TryParse(rawId, out int lineId)) return;

            if (!decimal.TryParse((txtQty?.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Enter a valid quantity.");
                return;
            }

            string locCode = (txtLoc?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(locCode))
            {
                SetFeedback(false, "&#9888; Scan or enter a destination location.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var loc = db.Stores.FirstOrDefault(s => s.CompanyID == CurrentUser.CoID
                    && s.StoreCode == locCode
                    && s.StoreActive == true
                    && (s.AllowPicking || s.IsWip || s.IsRejectStore));
                if (loc == null)
                {
                    SetFeedback(false, $"&#9888; '{locCode}' is not a valid location.");
                    return;
                }

                var line = db.DocLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return;

                line.ReceiveQty = qty;
                line.StoreCode  = loc.StoreCode;
                line.ToReceive  = true;
                decimal remaining = BaseOutstanding(line) - qty;
                line.QtyLeft    = remaining < 0 ? 0 : remaining;

                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    var itm = db.ItemsMasters.FirstOrDefault(
                        x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);
                    if (itm != null && itm.IsLotTracked == true)
                    {
                        int recnum = GetLotNum(CurrentUser.CoID);
                        string lotNum = DateTime.Today.ToString("ddMMyyyy") + loc.StoreCode + recnum.ToString();
                        line.LotNumber = lotNum;
                        db.LotTrackingMasters.Add(new LotTrackingMaster
                        {
                            LotNumber   = lotNum,
                            CreatedDate = DateTime.Now,
                            CompanyID   = CurrentUser.CoID,
                            ItemCode    = line.ItemCode,
                            ItemId      = line.SelectionId,
                            LotActive   = true,
                            LotQuantity = qty
                        });
                    }
                }

                var header = db.DocHeaders.FirstOrDefault(h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                if (header != null && header.Started != true) header.Started = true;

                db.SaveChanges();
            }

            MatchedLineID = 0;
            SetFeedback(true, $"&#10003; {qty:0.##} captured to {locCode}.");
            BindLines();
        }

        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e) { }

        // ── Finalise – show / hide ──────────────────────────────────────────────

        protected void lbtnFinalize_Click(object sender, EventArgs e)
        {
            bool hasLines;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                hasLines = db.DocLines.Any(l => l.DocID == DocID && l.CompanyID == CurrentUser.CoID
                    && l.ToReceive == true && l.ReceiveQty > 0
                    && (l.ReceiveComplete == null || l.ReceiveComplete == false));
            }
            if (!hasLines)
            {
                SetFeedback(false, "&#9888; Nothing captured yet. Scan lines first.");
                return;
            }
            if (string.IsNullOrEmpty(txtRecDate.Text))
                txtRecDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
            lblFinalizeError.Visible = false;
            pnlFinalize.Visible = true;
        }

        protected void lbtnCancelFinalize_Click(object sender, EventArgs e)
        {
            pnlFinalize.Visible = false;
        }

        // ── Finalise – post the GRN to Sage and land stock into the scanned locations ──

        protected async void lbtnConfirmGRN_Click(object sender, EventArgs e)
        {
            if (IsProcessing) return;
            IsProcessing = true;
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
                    SetFinalizeError("Invalid Receive Date.");
                    return;
                }

                List<DocLine> fLines;
                DocHeader poHeader;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    fLines = db.DocLines.Where(l => l.DocID == DocID && l.CompanyID == CurrentUser.CoID
                        && l.ToReceive == true && l.ReceiveQty > 0
                        && (l.ReceiveComplete == null || l.ReceiveComplete == false)).ToList();
                    poHeader = db.DocHeaders.FirstOrDefault(h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                }
                if (!fLines.Any()) { SetFinalizeError("No lines captured."); return; }
                if (poHeader == null) { SetFinalizeError("Purchase order not found."); return; }

                string suppInvRef = invNum.Length >= 3 ? invNum : dnNum;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (db.DocHeaders.Count(h => h.CompanyID == CurrentUser.CoID
                        && h.SupplierInvNum == suppInvRef && h.DocID != DocID) > 0)
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
                        SetFinalizeError($"Item '{dl.ItemDescription}' has no location — cannot continue.");
                        return;
                    }
                    decimal qty = dl.ReceiveQty ?? 0m;
                    documentLines.Add(new DocumentLine
                    {
                        SelectionId         = dl.SelectionId,
                        TaxTypeId           = dl.LineTaxTypeID ?? 0,
                        Description         = dl.ItemDescription ?? "",
                        LineType            = dl.LineType ?? 0,
                        Quantity            = qty,
                        UnitPriceExclusive  = dl.UnitPriceExclusive ?? 0m,
                        UnitPriceInclusive  = dl.UnitPriceInclusive ?? 0m,
                        TaxPercentage       = dl.TaxPercentage ?? 0m,
                        Unit                = dl.Unit ?? "",
                        DiscountPercentage  = dl.DiscountPercentage ?? 0m,
                        Exclusive           = dl.Exclusive ?? 0m,
                        Discount            = dl.Discount ?? 0m,
                        Tax                 = dl.Tax ?? 0m,
                        Total               = dl.Total ?? 0m,
                        Comments            = "Location: " + dl.StoreCode,
                        AnalysisCategoryId1 = dl.AnalysisCategoryId1 ?? 0,
                        AnalysisCategoryId2 = dl.AnalysisCategoryId2 ?? 0,
                        AnalysisCategoryId3 = dl.AnalysisCategoryId3 ?? 0,
                        ExchRate            = exchRate,
                        localCurrLineVal    = (qty * (dl.UnitPriceExclusive ?? 0m)) / exchRate
                    });
                }
                if (!documentLines.Any()) { SetFinalizeError("No postable lines."); return; }

                Document doc = new Document { Header = docHeader, Lines = documentLines };

                object jsonObject;
                if (exchRate == 1m)
                {
                    jsonObject = new
                    {
                        doc.Header.DueDate, doc.Header.SupplierId, doc.Header.SupplierName, doc.Header.StatusId,
                        doc.Header.Date, doc.Header.Inclusive, doc.Header.DiscountPercentage, doc.Header.TaxReference,
                        doc.Header.Reference, doc.Header.Message, doc.Header.FromDocument, doc.Lines
                    };
                }
                else
                {
                    jsonObject = new
                    {
                        doc.Header.DueDate, doc.Header.SupplierId, doc.Header.SupplierName, doc.Header.StatusId,
                        doc.Header.Date, doc.Header.Inclusive, doc.Header.DiscountPercentage, doc.Header.TaxReference,
                        doc.Header.Reference, doc.Header.Message, doc.Header.FromDocument,
                        doc.Header.Supplier_ExchangeRate, doc.Header.Supplier_CurrencyId, doc.Lines
                    };
                }

                var jObj = JObject.Parse(JsonConvert.SerializeObject(jsonObject));
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
                    api.LogErrorToFile($"CoID:{CurrentUser.CoID} ReceiveScanM GRN Sage error – {supInvResult}");
                    return;
                }
                string suppInvNum = parts.Length > 1 ? parts[1] : suppInvRef;

                if (chkReceivingComplete.Checked)
                {
                    try
                    {
                        var poUpdateObj = new
                        {
                            ID = DocID, StatusId = 4,
                            DeliveryDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                        };
                        await api.APIUpdatePurchaseOrderAsync("PurchaseOrder",
                            JsonConvert.SerializeObject(poUpdateObj, Formatting.Indented), CurrentUser);
                    }
                    catch (Exception ex)
                    {
                        api.LogErrorToFile($"CoID:{CurrentUser.CoID} ReceiveScanM PO status update error – {ex.Message}");
                    }
                }

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    long fromStoreId = getstoreid("CoR", db);
                    foreach (var dl in fLines)
                    {
                        decimal recvQty = dl.ReceiveQty ?? 0m;
                        decimal exclPrice = dl.UnitPriceExclusive ?? 0m;

                        if ((dl.ItemType ?? 0) == 0)
                        {
                            decimal convRate = 1m;
                            try
                            {
                                var itm = db.ItemsMasters.FirstOrDefault(
                                    x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId);
                                convRate = (decimal)(itm?.UOMConvert ?? 1m);
                                if (convRate == 0m) convRate = 1m;
                            }
                            catch { }

                            long toStoreId = getstoreid(dl.StoreCode, db);
                            db.ItemTransactions.Add(new ItemTransaction
                            {
                                CompanyID                 = CurrentUser.CoID,
                                DocumentID                = DocID,
                                DocumentType              = 2,
                                TransactionType           = "GRN",
                                ItemID                    = dl.SelectionId,
                                ItemCode                  = dl.ItemCode,
                                ItemDescription           = dl.ItemDescription,
                                LotNumber                 = dl.LotNumber,
                                Unit                      = dl.Unit,
                                FromID                    = fromStoreId,
                                ToID                      = toStoreId,
                                Qty                       = recvQty * convRate,
                                PriceExclusive            = (exclPrice / exchRate) / convRate,
                                AdditionalCosts           = 0m,
                                TotalUnitPriceExclInclAdd = (exclPrice / exchRate) / convRate,
                                TotalLineValExcl          = (exclPrice * recvQty) / exchRate,
                                TransactionDate           = DateTime.Now,
                                ByRoleID                  = CurrentUser.RoleID,
                                TransactionReference      = suppInvNum,
                                ExchRate                  = exchRate
                            });

                            bool linkExists = db.ItemStoreLinkMasters.Any(x => x.CompanyID == CurrentUser.CoID
                                && x.StoreID == (int?)toStoreId && x.ItemID == dl.SelectionId);
                            if (!linkExists)
                                db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                                {
                                    CompanyID = CurrentUser.CoID,
                                    ItemID    = dl.SelectionId,
                                    StoreID   = (int?)toStoreId,
                                    Active    = true
                                });
                        }

                        // Short register: this receipt + what's still owed.
                        decimal remaining = (dl.Quantity ?? 0m) - recvQty;
                        if (remaining < 0) remaining = 0;
                        db.ReceivingOutstandings.Add(new ReceivingOutstanding
                        {
                            CompanyID       = CurrentUser.CoID,
                            PONumber        = poHeader.DocumentNumber,
                            PODocID         = DocID,
                            LineID          = dl.LineID,
                            Supplier        = poHeader.CustSupName,
                            SupplierID      = poHeader.CustSuppID,
                            ItemCode        = dl.ItemCode,
                            SelectionId     = dl.SelectionId,
                            ItemDescription = dl.ItemDescription,
                            OrigQty         = dl.Quantity ?? 0,
                            RecQty          = recvQty,
                            QtyLeft         = remaining,
                            SBCALineID      = dl.SBCALineID,
                            CreatedBy       = CurrentUser.RoleID,
                            CreatedDate     = DateTime.Now,
                            Archive         = false
                        });

                        var dbLine = db.DocLines.FirstOrDefault(l => l.LineID == dl.LineID && l.CompanyID == CurrentUser.CoID);
                        if (dbLine != null)
                        {
                            dbLine.ReceiveQty      = 0;
                            dbLine.QtyLeft         = remaining;
                            dbLine.ToReceive       = false;
                            dbLine.ReceiveComplete = chkReceivingComplete.Checked || remaining <= 0;
                        }
                    }

                    var hdr = db.DocHeaders.FirstOrDefault(h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                    if (hdr != null)
                    {
                        hdr.Complete       = chkReceivingComplete.Checked;
                        hdr.RecStatus      = chkReceivingComplete.Checked ? 2 : 0;
                        hdr.CompleteDate   = DateTime.Now;
                        hdr.CompBy         = 0;
                        hdr.SupplierInvNum = suppInvNum;
                        if (dnNum.Length  > 0) hdr.DNNum  = dnNum;
                        if (invNum.Length > 0) hdr.InvNum = invNum;
                    }

                    db.SaveChanges();
                }

                pnlFinalize.Visible  = false;
                lbtnFinalize.Enabled = false;
                SetFeedback(true, $"&#10003; GRN generated &mdash; {suppInvNum}");
                BindLines();
            }
            catch (Exception ex)
            {
                SetFinalizeError($"An unexpected error occurred: {ex.Message}");
                api.LogErrorToFile($"CoID:{CurrentUser.CoID} ReceiveScanM lbtnConfirmGRN_Click – {ex}");
            }
            finally
            {
                IsProcessing = false;
                lbtnConfirmGRN.Enabled = true;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task<string> SendSupplierInvoice(string jsonBody, ApiUrlCall api)
        {
            try
            {
                JObject result = await api.APIPostDocumentAsync("SupplierInvoice", jsonBody, CurrentUser);
                if (result != null && result.ContainsKey("ID"))
                    return result["ID"].ToString() + "|" + result["DocumentNumber"].ToString();
                return result?.ToString() ?? "Unknown error";
            }
            catch (Exception ex) { return ex.Message; }
        }

        private long getstoreid(string storeCode, SBMSEntities db)
        {
            if (string.IsNullOrEmpty(storeCode)) return 0;
            var store = db.Stores.FirstOrDefault(x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID);
            return store?.StoreID ?? 0;
        }

        private void SetFinalizeError(string msg)
        {
            lblFinalizeError.Text    = "&#9888; " + msg;
            lblFinalizeError.Visible = true;
            pnlFinalize.Visible      = true;
        }

        // ── Navigation ──────────────────────────────────────────────────────────

        protected void lbtnTopBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnTopHome_Click(object sender, EventArgs e)
        {
            Response.Redirect(CurrentUser != null
                ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                : "~/SBMSMobile/DashboardM.aspx", false);
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

        // ── Feedback ──────────────────────────────────────────────────────────────

        private void SetFeedback(bool ok, string html)
        {
            lblScanFeedback.Text     = html;
            lblScanFeedback.CssClass = ok ? "mob-feedback found" : "mob-feedback notfound";
        }

        private void ClearFeedback()
        {
            lblScanFeedback.Text     = string.Empty;
            lblScanFeedback.CssClass = "mob-feedback";
        }
    }
}
