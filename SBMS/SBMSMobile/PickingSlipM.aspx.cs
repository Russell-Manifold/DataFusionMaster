using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class PickingSlipM : BasePage
    {
        private List<GetActiveLotNumbersLinkedToStores_Result> _activeLotNums;

        // ── Session/ViewState helpers ──────────────────────────────────────────────
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private int PSID
        {
            get { return ViewState["PSID"] != null ? (int)ViewState["PSID"] : 0; }
            set { ViewState["PSID"] = value; }
        }

        private long DocID
        {
            get { return ViewState["DocID"] != null ? (long)ViewState["DocID"] : 0L; }
            set { ViewState["DocID"] = value; }
        }

        private string PSIntNumber
        {
            get { return ViewState["PSIntNumber"] as string ?? ""; }
            set { ViewState["PSIntNumber"] = value; }
        }

        public int MatchedLineID
        {
            get { return ViewState["MatchedLineID"] != null ? (int)ViewState["MatchedLineID"] : 0; }
            set { ViewState["MatchedLineID"] = value; }
        }

        private bool IsProcessing
        {
            get { return ViewState["IsProcessing"] != null && (bool)ViewState["IsProcessing"]; }
            set { ViewState["IsProcessing"] = value; }
        }

        private string SelectedStore
        {
            get { return ViewState["SelectedStore"] as string ?? ""; }
            set { ViewState["SelectedStore"] = value; }
        }

        private string SelectedStoreName
        {
            get { return ViewState["SelectedStoreName"] as string ?? ""; }
            set { ViewState["SelectedStoreName"] = value; }
        }

        private HashSet<int> PickedLineIDs
        {
            get
            {
                if (Session["PSPickedLineIDs"] == null)
                    Session["PSPickedLineIDs"] = new HashSet<int>();
                return (HashSet<int>)Session["PSPickedLineIDs"];
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────
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

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                _activeLotNums = db.GetActiveLotNumbersLinkedToStores(CurrentUser.CoID)
                                   .Where(x => x.AllowPicking)
                                   .OrderBy(x => x.LotNumber)
                                   .ToList();
            }

            if (!IsPostBack)
            {
                Session.Remove("PSPickedLineIDs");
                LoadStoresModal();
                pnlStoreModal.Visible = true;

                if (!int.TryParse(Request.QueryString["psid"], out int psid) || psid == 0)
                {
                    Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
                    return;
                }

                PSID = psid;
                LoadPSHeader();
                BindLines();
            }

            UpdateStoreIndicator();
        }

        // ── Store modal ────────────────────────────────────────────────────────────
        private void LoadStoresModal()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var storeCodes = db.GetLinkedStoredFromItem(CurrentUser.CoID)
                    .Where(x => x.AllowPicking)
                    .Select(x => x.StoreCode)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                ddStoreGlobal.Items.Clear();
                ddStoreGlobal.Items.Add(new ListItem("— Select a store —", ""));
                foreach (var code in storeCodes)
                    ddStoreGlobal.Items.Add(new ListItem(code, code));
            }
        }

        private void UpdateStoreIndicator()
        {
            if (!string.IsNullOrEmpty(SelectedStore))
            {
                lblSelectedStoreName.Text = SelectedStoreName;
                pnlStoreIndicator.Visible = true;
            }
            else
            {
                pnlStoreIndicator.Visible = false;
            }
        }

        protected void lbtnConfirmStore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddStoreGlobal.SelectedValue))
            {
                lblStoreModalError.Text    = "Please select a store.";
                lblStoreModalError.Visible = true;
                return;
            }

            SelectedStore         = ddStoreGlobal.SelectedValue;
            SelectedStoreName     = ddStoreGlobal.SelectedItem.Text;
            pnlStoreModal.Visible = false;
            UpdateStoreIndicator();
        }

        protected void lbtnModalCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnChangeStore_Click(object sender, EventArgs e)
        {
            LoadStoresModal();
            if (!string.IsNullOrEmpty(SelectedStore))
                try { ddStoreGlobal.SelectedValue = SelectedStore; } catch { }
            lblStoreModalError.Visible = false;
            pnlStoreModal.Visible      = true;
        }

        // ── Header ─────────────────────────────────────────────────────────────────
        private void LoadPSHeader()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var docHeader = db.DocHeaders
                    .FirstOrDefault(h => h.CompanyID == CurrentUser.CoID && h.LinkedPSID == PSID);

                if (docHeader == null)
                {
                    Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
                    return;
                }

                string docGuid = docHeader.DocGUID?.ToString();
                var ps = db.GetOnePickingSlipFromDocHeaderID(CurrentUser.CoID, docGuid)
                            .FirstOrDefault();

                if (ps == null)
                {
                    Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
                    return;
                }

                DocID       = ps.DocID;
                PSIntNumber = ps.PSIntNumber ?? "";

                lblPSNum.Text    = ps.PSIntNumber ?? "-";
                lblCustomer.Text = ps.CustSupName ?? "-";
                lblSONum.Text    = ps.DocumentNumber ?? "-";
                lblDueDate.Text  = ps.DueDelDate.HasValue
                                     ? ps.DueDelDate.Value.ToString("dd MMM yyyy")
                                     : "-";

                if (!string.IsNullOrWhiteSpace(ps.PSPickMessage))
                {
                    lblPickMsg.Text    = ps.PSPickMessage;
                    pnlPickMsg.Visible = true;
                }

                if (ps.Complete == true)
                {
                    lbtnFinalise.Enabled = false;
                    lbtnPickAll.Enabled  = false;
                }
            }
        }

        // ── Data ───────────────────────────────────────────────────────────────────
        private List<PickSlipLine> GetLines()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = db.PickSlipLines
                    .Where(l => l.PSID == PSID && l.CompanyID == CurrentUser.CoID && l.LineType == 0)
                    .OrderBy(l => l.LineID)
                    .ToList();

                foreach (var l in lines)
                    if (l.PickQty == null) l.PickQty = 0;

                return lines;
            }
        }

        private void BindLines()
        {
            var lines = GetLines();
            lblLineCount.Text   = lines.Count(l => l.PickComplete != true).ToString();
            lblEmpty.Visible    = lines.All(l => l.PickComplete == true);
            rptLines.DataSource = lines;
            rptLines.DataBind();
        }

        // ── Repeater binding ───────────────────────────────────────────────────────
        protected void rptLines_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var line      = (PickSlipLine)e.Item.DataItem;
            bool isMatched = line.LineID == MatchedLineID;
            bool isPicked  = PickedLineIDs.Contains(line.LineID) || line.PickComplete == true;

            var badge = (Label)e.Item.FindControl("lblSavedBadge");
            if (badge != null) badge.Visible = isPicked;

            var pickBtn = (LinkButton)e.Item.FindControl("lbtnPickLine");
            if (pickBtn != null && isPicked) pickBtn.Enabled = false;

            var txtQty = (TextBox)e.Item.FindControl("txtPickQty");
            if (txtQty != null && isMatched &&
                (line.PickQty == null || line.PickQty == 0))
                txtQty.Text = line.Quantity.HasValue
                    ? line.Quantity.Value.ToString("0.##") : "0";

            // ── Lot number dropdown ──
            var ddLot = (DropDownList)e.Item.FindControl("ddLotNum");
            if (ddLot != null)
            {
                if (CurrentUser.CompanyUseLotNumbers && line.IsLotTracked)
                {
                    var lots = _activeLotNums
                        .Where(x => x.ItemId == line.SelectionId
                                 && (string.IsNullOrEmpty(SelectedStore) || x.StoreCode == SelectedStore))
                        .ToList();

                    ddLot.Items.Clear();
                    ddLot.Items.Add(new ListItem("— Lot Number —", ""));
                    foreach (var lot in lots)
                        ddLot.Items.Add(new ListItem(
                            $"{lot.LotNumber} ({lot.QtyHandToStore:0.##})", lot.LotNumber));

                    if (!string.IsNullOrEmpty(line.LotNumber))
                        try { ddLot.SelectedValue = line.LotNumber; } catch { }
                }
                else
                {
                    ddLot.Visible = false;
                }
            }
        }

        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e) { }

        // ── Barcode scan ───────────────────────────────────────────────────────────
        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            string raw = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(raw)) { ClearFeedback(); return; }

            GetOneItemFromBarcode_Result barcodeItem = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                barcodeItem = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            if (barcodeItem == null)
            {
                SetFeedback(false, $"&#128683; Barcode not recognised: {raw}");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            PickSlipLine matchedLine = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                matchedLine = db.PickSlipLines.FirstOrDefault(l =>
                    l.PSID        == PSID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode  == barcodeItem.Code
                    && l.LineType  == 0
                    && (l.PickComplete == null || l.PickComplete == false));

            if (matchedLine == null)
            {
                SetFeedback(false,
                    $"&#9888; <strong>{barcodeItem.Code}</strong> ({barcodeItem.Description}) " +
                    "is not on this slip or is already picked.");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            SetFeedback(true,
                $"&#10003; Found: <strong>{barcodeItem.Code}</strong> &mdash; {barcodeItem.Description}");
            MatchedLineID   = matchedLine.LineID;
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

        // ── Per-line Pick ──────────────────────────────────────────────────────────
        protected void lbtnPickLine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedStore))
            {
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            var btn  = (LinkButton)sender;
            var item = btn.NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf     = (HiddenField)item.FindControl("hfLineID");
            var txtQty = (TextBox)item.FindControl("txtPickQty");
            var ddLot  = (DropDownList)item.FindControl("ddLotNum");

            if (!int.TryParse(hf?.Value, out int lineId)) return;

            if (!decimal.TryParse(txtQty?.Text.Trim(), out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Please enter a valid pick quantity.");
                return;
            }

            string lotNum = ddLot?.Visible == true ? (ddLot.SelectedValue ?? "") : "";

            string error = TrySavePickLine(lineId, qty, SelectedStore, lotNum);
            if (error != null)
            {
                SetFeedback(false, "&#9888; " + error);
                return;
            }

            PickedLineIDs.Add(lineId);
            MatchedLineID = 0;
            ClearFeedback();
            BindLines();
        }

        // ── Pick All ───────────────────────────────────────────────────────────────
        protected void lbtnPickAll_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedStore))
            {
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            var lines   = GetLines().Where(l => l.PickComplete != true).ToList();
            var skipped = new List<string>();

            foreach (var line in lines)
            {
                if (PickedLineIDs.Contains(line.LineID)) continue;

                if (line.IsLotTracked && CurrentUser.CompanyUseLotNumbers)
                {
                    skipped.Add($"{line.ItemCode} (lot tracked)");
                    continue;
                }

                decimal qty = line.Quantity ?? 0;
                if (qty <= 0) continue;

                string error = TrySavePickLine(line.LineID, qty, SelectedStore, "");
                if (error != null)
                    skipped.Add($"{line.ItemCode} ({error})");
                else
                    PickedLineIDs.Add(line.LineID);
            }

            MatchedLineID = 0;

            if (skipped.Any())
                SetFeedback(false, $"&#9888; Pick All done — skipped: {string.Join(", ", skipped)}");
            else
                SetFeedback(true, "&#10003; All lines marked as picked.");

            BindLines();
        }

        // ── Core pick-line logic ───────────────────────────────────────────────────
        private string TrySavePickLine(int lineId, decimal qty, string storeCode, string lotNum)
        {
            if (PickedLineIDs.Contains(lineId))
                return "This line has already been saved.";

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.PickSlipLines
                    .FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return "Line not found.";

                int storeId = db.Stores
                    .Where(x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID)
                    .Select(x => x.StoreID)
                    .FirstOrDefault();

                if (line.IsLotTracked && CurrentUser.CompanyUseLotNumbers &&
                    string.IsNullOrEmpty(lotNum))
                    return $"{line.ItemCode} is Lot Tracked — select a lot number before picking.";

                decimal priceExcl = 0m, priceInclAdd = 0m;

                var itm = db.ItemsMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == line.SelectionId);

                bool isPhysical = itm?.Physical == true;

                if (isPhysical)
                {
                    var trnQuery = db.ItemTransactions.Where(t =>
                        t.CompanyID == CurrentUser.CoID
                        && t.ItemID  == line.SelectionId
                        && t.ToID    == storeId);

                    if (!string.IsNullOrEmpty(lotNum))
                        trnQuery = trnQuery.Where(t => t.LotNumber == lotNum);

                    decimal qoh = trnQuery.Select(t => (decimal?)t.Qty)
                                          .DefaultIfEmpty(0).Sum() ?? 0m;

                    if (qoh < qty)
                        return $"Insufficient stock for {line.ItemCode}: only {qoh:0.##} available in {storeCode}.";

                    // Outbound: stock leaves at the pick store's running weighted average;
                    // an out never changes the store average. Falls back to the item's
                    // Sage average below if the store has no stamped cost yet.
                    priceExcl = priceInclAdd = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID,
                        Convert.ToInt64(line.SelectionId), storeId);
                }

                if (line.ItemTransLineID != null && line.ItemTransLineID > 0)
                {
                    long prevId  = (long)line.ItemTransLineID;
                    var  prevTrn = db.ItemTransactions
                        .FirstOrDefault(t => t.CompanyID == CurrentUser.CoID && t.TrnID == prevId);

                    if (prevTrn != null)
                    {
                        // Reversal puts the earlier pick back INTO the store at its original
                        // cost; the inbound re-blends the store's running weighted average
                        // (computed BEFORE the row is added to the ledger).
                        decimal revQty = (decimal)prevTrn.Qty * -1;
                        decimal revVal = (decimal)(prevTrn.TotalUnitPriceExclInclAdd ?? 0m) * revQty;
                        decimal revLineVal;
                        decimal revAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID,
                            Convert.ToInt64(prevTrn.ItemID), storeId, revQty, revVal, out revLineVal);

                        db.ItemTransactions.Add(new ItemTransaction
                        {
                            CompanyID                 = CurrentUser.CoID,
                            DocumentID                = prevTrn.DocumentID,
                            TransactionType           = "PS",
                            ItemID                    = prevTrn.ItemID,
                            ItemCode                  = prevTrn.ItemCode ?? "",
                            ItemDescription           = prevTrn.ItemDescription ?? "",
                            Unit                      = prevTrn.Unit,
                            FromID                    = 0,
                            ToID                      = storeId,
                            Qty                       = revQty,
                            DocumentType              = 10,
                            PriceExclusive            = prevTrn.PriceExclusive,
                            TotalUnitPriceExclInclAdd = prevTrn.TotalUnitPriceExclInclAdd,
                            AdditionalCosts           = 0,
                            TotalLineValExcl          = prevTrn.TotalUnitPriceExclInclAdd * prevTrn.Qty * -1,
                            StoreAvgCost              = revAvg,
                            TransactionDate           = DateTime.Now,
                            ByRoleID                  = CurrentUser.RoleID,
                            TransactionReference      = PSIntNumber + " Reversal",
                            LotNumber                 = prevTrn.LotNumber,
                            ExchRate                  = 1
                        });

                        line.ItemTransLineID = null;
                    }
                }

                line.PickQty       = qty;
                line.PickComplete  = true;
                line.PickTime      = DateTime.Now;
                line.StoreCodeFrom = storeCode;
                if (!string.IsNullOrEmpty(lotNum)) line.LotNumber = lotNum;
                db.SaveChanges();

                if (priceExcl == 0m && itm != null)
                    priceExcl = priceInclAdd = (decimal)(itm.AverageCost ?? 0m);

                var newTrn = new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = PSID,
                    TransactionType           = "PS",
                    ItemID                    = line.SelectionId,
                    ItemCode                  = line.ItemCode  ?? "",
                    ItemDescription           = line.ItemDescription ?? "",
                    Unit                      = line.Unit,
                    FromID                    = 0,
                    ToID                      = storeId,
                    Qty                       = qty * -1,
                    DocumentType              = 10,
                    PriceExclusive            = priceExcl,
                    TotalUnitPriceExclInclAdd = priceInclAdd,
                    AdditionalCosts           = 0,
                    TotalLineValExcl          = priceInclAdd * qty * -1,
                    StoreAvgCost              = priceInclAdd,
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = PSIntNumber,
                    LotNumber                 = lotNum,
                    ExchRate                  = 1
                };
                db.ItemTransactions.Add(newTrn);
                db.SaveChanges();

                line.ItemTransLineID = newTrn.TrnID;
                db.SaveChanges();
            }

            return null;
        }

        // ── Finalise ───────────────────────────────────────────────────────────────
        protected void lbtnFinalise_Click(object sender, EventArgs e)
        {
            var unpicked = GetLines().Where(l => l.PickComplete != true).ToList();
            if (unpicked.Any())
            {
                SetFeedback(false,
                    $"&#9888; {unpicked.Count} line(s) not yet picked. " +
                    "Pick all lines before closing off.");
                return;
            }

            lblFinaliseError.Visible = false;
            pnlFinalise.Visible      = true;
        }

        protected void lbtnCancelFinalise_Click(object sender, EventArgs e)
        {
            pnlFinalise.Visible = false;
        }

        protected void lbtnConfirmFinalise_Click(object sender, EventArgs e)
        {
            if (IsProcessing) return;
            IsProcessing                = true;
            lbtnConfirmFinalise.Enabled = false;

            ApiUrlCall api = new ApiUrlCall();

            try
            {
                int  slipId = PSID;
                long docId  = DocID;

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var psLines = db.PickSlipLines
                        .Where(x => x.PSID == slipId)
                        .OrderBy(x => x.LineID)
                        .ToList();

                    foreach (var psl in psLines)
                    {
                        decimal pickQty = psl.PickQty ?? psl.Quantity ?? 0;

                        if (psl.SBCALineID != null && psl.SBCALineID != 0)
                        {
                            var soLine = db.DocLines
                                .FirstOrDefault(x => x.SBCALineID == psl.SBCALineID);

                            if (soLine != null)
                            {
                                soLine.QtyLeft         = soLine.Quantity - pickQty;
                                soLine.ReceiveQty      = pickQty;
                                soLine.ReceiveComplete = true;
                                soLine.StoreCode       = psl.StoreCodeFrom;
                                soLine.LotNumber       = psl.LotNumber;
                                soLine.Exclusive       = soLine.UnitPriceExclusive * pickQty;
                                soLine.Discount        = (soLine.UnitPriceExclusive * pickQty)
                                                         * soLine.DiscountPercentage;
                                soLine.Tax             = (soLine.UnitPriceExclusive * pickQty)
                                                         * soLine.TaxPercentage;
                                soLine.Total           = soLine.Exclusive - soLine.Discount + soLine.Tax;
                                soLine.ExchRate        = 1;
                                soLine.localCurrLineVal = soLine.Exclusive - soLine.Discount;
                            }
                        }
                    }

                    var docH = db.DocHeaders
                        .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.DocID == docId);

                    if (docH != null)
                    {
                        docH.Complete     = true;
                        docH.Active       = true;
                        docH.CompBy       = CurrentUser.RoleID;
                        docH.CompleteDate = DateTime.Today;

                        decimal docCost = db.ItemTransactions
                            .Where(x => x.CompanyID == CurrentUser.CoID && x.DocumentID == slipId)
                            .Select(x => (decimal?)x.TotalLineValExcl)
                            .DefaultIfEmpty(0).Sum() ?? 0m;

                        docH.DocCost = docCost != 0 ? Math.Abs(docCost) * -1 : 0;

                        decimal docValue = db.DocLines
                            .Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docId)
                            .Select(x => (decimal?)x.Exclusive)
                            .DefaultIfEmpty(0).Sum() ?? 0m;

                        decimal cost = docH.DocCost ?? 0m;
                        docH.DocGP = docValue != 0 ? (docValue - cost) / docValue : 0m;
                    }

                    var finalStat = db.PickSlipProcesses
                        .Where(ws => ws.CompanyID == CurrentUser.CoID)
                        .OrderByDescending(ws => ws.Seq)
                        .Select(ws => new { ws.PSName, ws.PSPID })
                        .FirstOrDefault();

                    var psm = db.PickingSlipMasters
                        .FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.PSID == slipId);

                    int fromStat = 0;
                    if (psm != null && finalStat != null)
                    {
                        fromStat           = (int)(psm.PSStationID ?? 0);
                        psm.PSStationID    = finalStat.PSPID;
                        psm.PSStatus       = finalStat.PSName;
                        psm.PSComplete     = true;
                        psm.PSCompleteDate = DateTime.Now;
                        psm.PSCompleteBy   = CurrentUser.RoleID;
                        psm.PSActive       = false;
                        psm.LinkedSOrdID   = docId;
                    }

                    if (finalStat != null)
                    {
                        db.PickSlipTransactions.Add(new PickSlipTransaction
                        {
                            PSID          = slipId,
                            MoveQty       = 1,
                            RejectQty     = 0,
                            MoveDate      = DateTime.Now,
                            FromStationID = fromStat,
                            ToStationID   = finalStat.PSPID,
                            CompanyID     = CurrentUser.CoID,
                            MoveBy        = CurrentUser.RoleID
                        });
                    }

                    var psUsers = db.RolesMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyPSMove == true)
                        .ToList();

                    foreach (var usr in psUsers)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Message    = (psm?.PSIntNumber ?? PSIntNumber) +
                                         " Moved to " + (finalStat?.PSName ?? "Complete"),
                            IsRead     = false,
                            CreatedAt  = DateTime.Now,
                            CompanyID  = CurrentUser.CoID,
                            UserRoleID = usr.RoleID
                        });
                    }

                    db.SaveChanges();
                }

                pnlFinalise.Visible  = false;
                lbtnFinalise.Enabled = false;
                lbtnPickAll.Enabled  = false;
                Session.Remove("PSPickedLineIDs");

                SetFeedback(true, "&#10003; Picking slip closed off successfully.");
                BindLines();
            }
            catch (Exception ex)
            {
                SetFinaliseError($"An error occurred: {ex.Message}");
                api.LogErrorToFile(
                    $"CoID:{CurrentUser.CoID} PickingSlipM lbtnConfirmFinalise – {ex}");
            }
            finally
            {
                IsProcessing                = false;
                lbtnConfirmFinalise.Enabled = true;
            }
        }

        // ── Navigation ─────────────────────────────────────────────────────────────
        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnTopBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
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

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ── Feedback helpers ───────────────────────────────────────────────────────
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

        private void SetFinaliseError(string msg)
        {
            lblFinaliseError.Text    = "&#9888; " + msg;
            lblFinaliseError.Visible = true;
            pnlFinalise.Visible      = true;
        }
    }
}
