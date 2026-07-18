using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    // Mobile/scanner QUICK ITEM MOVE.
    // Ledger-only relocation of ONE item between two stores/bins, recorded as two
    // ItemTransaction "TRF" legs (no header, no Sage) — the same movement record the
    // desktop Quick Transfer and the mobile put-away write.
    // Cycle: scan source bin → scan item out (+ qty) → scan destination bin → scan item in → finish.
    public partial class QuickMoveM : BasePage
    {
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        // ── Wizard state (ViewState) ───────────────────────────────────────────
        // 1 = scan source, 2 = scan item out (+qty), 3 = scan destination, 4 = scan item in / finish
        private int Step
        {
            get { return ViewState["Step"] != null ? (int)ViewState["Step"] : 1; }
            set { ViewState["Step"] = value; }
        }

        private string SrcCode { get { return ViewState["SrcCode"] as string; } set { ViewState["SrcCode"] = value; } }
        private long   SrcId   { get { return ViewState["SrcId"] != null ? (long)ViewState["SrcId"] : 0; } set { ViewState["SrcId"] = value; } }

        private string DestCode { get { return ViewState["DestCode"] as string; } set { ViewState["DestCode"] = value; } }
        private long   DestId   { get { return ViewState["DestId"] != null ? (long)ViewState["DestId"] : 0; } set { ViewState["DestId"] = value; } }

        private long   ItemId    { get { return ViewState["ItemId"] != null ? (long)ViewState["ItemId"] : 0; } set { ViewState["ItemId"] = value; } }
        private string ItemCode  { get { return ViewState["ItemCode"] as string; } set { ViewState["ItemCode"] = value; } }
        private string ItemDescr { get { return ViewState["ItemDescr"] as string; } set { ViewState["ItemDescr"] = value; } }
        private string ItemUnit  { get { return ViewState["ItemUnit"] as string; } set { ViewState["ItemUnit"] = value; } }
        private string Lot       { get { return ViewState["Lot"] as string; } set { ViewState["Lot"] = value; } }
        private decimal Avail    { get { return ViewState["Avail"] != null ? (decimal)ViewState["Avail"] : 0m; } set { ViewState["Avail"] = value; } }
        private decimal Cost     { get { return ViewState["Cost"] != null ? (decimal)ViewState["Cost"] : 0m; } set { ViewState["Cost"] = value; } }
        private decimal MoveQty  { get { return ViewState["MoveQty"] != null ? (decimal)ViewState["MoveQty"] : 0m; } set { ViewState["MoveQty"] = value; } }

        // step 2: has an item been scanned (reveal the out-detail panel)?
        private bool ItemResolved { get { return ViewState["ItemResolved"] != null && (bool)ViewState["ItemResolved"]; } set { ViewState["ItemResolved"] = value; } }
        // step 4: has the rescanned item matched (enable Finish)?
        private bool InConfirmed  { get { return ViewState["InConfirmed"] != null && (bool)ViewState["InConfirmed"]; } set { ViewState["InConfirmed"] = value; } }

        // Barcode company = scan front-end; otherwise the tap (dropdown + list) front-end.
        private bool ScanMode { get { return CurrentUser.UseBarcodes == true; } }
        private string Search { get { return ViewState["Search"] as string ?? ""; } set { ViewState["Search"] = value; } }

        // ── Lifecycle ──────────────────────────────────────────────────────────
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
                Session["QuickMoveSession"] = new List<MoveDone>();
                if (!ScanMode) { LoadStores(ddFromStore); LoadStores(ddToStore); }
                ResetCycle();
                RenderStep();
                BindDone();
            }
        }

        // All active stores/bins (excl. reserved), for the tap-mode dropdowns.
        private void LoadStores(DropDownList ddl)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = db.Stores
                    .Where(s => s.CompanyID == CurrentUser.CoID && s.StoreActive == true
                             && s.StoreCode != "CoR" && s.StoreCode != "CoD")
                    .OrderBy(s => s.StoreCode).ToList();
                ddl.Items.Clear();
                ddl.Items.Add(new ListItem("- select location -", ""));
                foreach (var s in stores)
                    ddl.Items.Add(new ListItem(
                        string.IsNullOrEmpty(s.StoreDescript) ? s.StoreCode : s.StoreCode + " - " + s.StoreDescript,
                        s.StoreCode));
            }
        }

        // ── Tap mode: From location chosen → list its contents ──
        protected void ddFromStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetCycle();
            string code = ddFromStore.SelectedValue;
            if (string.IsNullOrEmpty(code)) { RenderStep(); return; }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var s = ResolveStore(db, code);
                if (s == null) { SetFeedback(false, $"&#9888; '{code}' is not a valid location."); RenderStep(); return; }
                SrcCode = s.StoreCode;
                SrcId   = s.StoreID;
            }
            Step = 2;
            Search = "";
            txtSearch.Text = "";
            BindContents();
            ClearFeedback();
            RenderStep();
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            Search = (txtSearch.Text ?? "").Trim();
            BindContents();
            RenderStep();
        }

        protected void lbtnClearSearch_Click(object sender, EventArgs e)
        {
            Search = "";
            txtSearch.Text = "";
            BindContents();
            RenderStep();
        }

        // Tap an item+lot row → lock it in as the item to move (same state a scan would set).
        protected void rptContents_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "pick" || Step != 2) return;
            string[] parts = Convert.ToString(e.CommandArgument).Split('|');
            string itemCode = parts[0];
            string lot = parts.Length > 1 ? parts[1] : "";

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var itm = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.Code == itemCode);
                if (itm == null) { SetFeedback(false, $"&#9888; {itemCode} not found."); return; }

                var chosen = SourceLines(db, itemCode).FirstOrDefault(r => (r.LotNumber ?? "") == (lot ?? ""));
                if (chosen == null || (chosen.QOH ?? 0) <= 0)
                {
                    SetFeedback(false, "&#9888; That stock is no longer available."); BindContents(); RenderStep(); return;
                }

                ItemId   = itm.ID;
                ItemCode = itm.Code;
                ItemDescr = itm.Description;
                ItemUnit = itm.Unit;
                ApplyLot(chosen);
                ItemResolved = true;
            }
            RenderStep();
        }

        // Tap mode: destination chosen → confirm (no rescan) and enable Finish.
        protected void ddToStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Step != 3) return;
            string code = ddToStore.SelectedValue;
            if (string.IsNullOrEmpty(code)) { DestCode = null; DestId = 0; InConfirmed = false; RenderStep(); return; }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var s = ResolveStore(db, code);
                if (s == null) { SetFeedback(false, $"&#9888; '{code}' is not a valid location."); RenderStep(); return; }
                if (s.StoreID == SrcId) { SetFeedback(false, "&#9888; Destination must differ from the source."); RenderStep(); return; }
                DestCode = s.StoreCode;
                DestId   = s.StoreID;
            }
            Step = 4;
            InConfirmed = true;
            SetFeedback(true, "&#10003; Ready — press Finish to post the move.");
            RenderStep();
        }

        private void BindContents()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var rows = db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                    .Where(r => r.StoreCode == SrcCode && (r.QOH ?? 0) > 0)
                    .ToList();
                if (!string.IsNullOrEmpty(Search))
                {
                    string q = Search.ToLower();
                    rows = rows.Where(r => (r.ItemCode ?? "").ToLower().Contains(q)
                                        || (r.ItemDescription ?? "").ToLower().Contains(q)).ToList();
                }
                rows = rows.OrderBy(r => r.ItemCode).ThenBy(r => r.LotNumber).ToList();
                rptContents.DataSource = rows;
                rptContents.DataBind();
                lblNoContents.Visible = rows.Count == 0;
            }
        }

        // ── Single scan box — behaviour depends on the current step ──
        protected void txtScan_TextChanged(object sender, EventArgs e)
        {
            string raw = (txtScan.Text ?? "").Trim();
            txtScan.Text = string.Empty;
            if (string.IsNullOrEmpty(raw)) return;

            switch (Step)
            {
                case 1: HandleSourceScan(raw); break;
                case 2: HandleItemOutScan(raw); break;
                case 3: HandleDestScan(raw); break;
                case 4: HandleItemInScan(raw); break;
            }
        }

        private void HandleSourceScan(string code)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var s = ResolveStore(db, code);
                if (s == null) { SetFeedback(false, $"&#9888; '{code}' is not a valid location."); return; }
                SrcCode = s.StoreCode;
                SrcId   = s.StoreID;
            }
            Step = 2;
            ItemResolved = false;
            SetFeedback(true, $"&#10003; From <strong>{SrcCode}</strong> &mdash; scan the item to move.");
            RenderStep();
        }

        private void HandleItemOutScan(string barcode)
        {
            GetOneItemFromBarcode_Result item;
            List<GetOpeningBalancesAllStores_Result> lines;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, barcode).FirstOrDefault();
                if (item == null) { SetFeedback(false, $"&#128683; Barcode not recognised: {barcode}"); return; }
                lines = SourceLines(db, item.Code);
            }

            if (lines.Count == 0)
            {
                SetFeedback(false, $"&#9888; <strong>{item.Code}</strong> has no stock in {SrcCode}.");
                return;
            }

            ItemId       = item.ID;
            ItemCode     = item.Code;
            ItemDescr    = item.Description;
            ItemUnit     = item.Unit;
            ItemResolved = true;

            BindLots(lines);
            ddLot.SelectedIndex = 0;
            ApplyLot(lines[0]);

            SetFeedback(true, lines.Count > 1
                ? $"&#10003; <strong>{item.Code}</strong> &mdash; {lines.Count} lots; choose one."
                : $"&#10003; <strong>{item.Code}</strong> &mdash; {item.Description}");
            RenderStep();
        }

        // Lot changed → re-read availability/cost for the chosen lot.
        protected void ddLot_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines  = SourceLines(db, ItemCode);
                var chosen = lines.FirstOrDefault(r => (r.LotNumber ?? "") == ddLot.SelectedValue) ?? lines.FirstOrDefault();
                if (chosen == null)
                {
                    SetFeedback(false, "&#9888; That stock is no longer available.");
                    Step = 2; ItemResolved = false; RenderStep(); return;
                }
                ApplyLot(chosen);
            }
            RenderStep();
        }

        private void BindLots(List<GetOpeningBalancesAllStores_Result> lines)
        {
            ddLot.DataSource = lines.Select(r => new
            {
                Lot  = r.LotNumber ?? "",
                Text = (string.IsNullOrEmpty(r.LotNumber) ? "(no lot)" : "Lot " + r.LotNumber)
                       + " — " + (r.QOH ?? 0).ToString("0.##") + " on hand"
            }).ToList();
            ddLot.DataTextField  = "Text";
            ddLot.DataValueField = "Lot";
            ddLot.DataBind();
        }

        private void ApplyLot(GetOpeningBalancesAllStores_Result r)
        {
            Lot   = string.IsNullOrEmpty(r.LotNumber) ? null : r.LotNumber;
            Avail = r.QOH ?? 0m;
            Cost  = r.TotalUnitPriceExclInclAdd ?? r.PriceExclusive ?? 0m;
        }

        // Step 2 → 3: lock in the item + quantity to move.
        protected void lbtnConfirmOut_Click(object sender, EventArgs e)
        {
            if (Step != 2 || !ItemResolved) return;

            if (!decimal.TryParse((txtQty.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Enter a valid quantity.");
                return;
            }
            if (qty > Avail)
            {
                SetFeedback(false, $"&#9888; Only {Avail:0.##} on hand for that lot.");
                return;
            }

            MoveQty = qty;
            Step = 3;
            SetFeedback(true, $"&#10003; {qty:0.##} {ItemCode} &mdash; scan the destination.");
            RenderStep();
        }

        private void HandleDestScan(string code)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var s = ResolveStore(db, code);
                if (s == null) { SetFeedback(false, $"&#9888; '{code}' is not a valid location."); return; }
                if (s.StoreID == SrcId)
                {
                    SetFeedback(false, "&#9888; Destination must differ from the source.");
                    return;
                }
                DestCode = s.StoreCode;
                DestId   = s.StoreID;
            }
            Step = 4;
            InConfirmed = false;
            SetFeedback(true, $"&#10003; To <strong>{DestCode}</strong> &mdash; scan the item again to confirm.");
            RenderStep();
        }

        private void HandleItemInScan(string barcode)
        {
            GetOneItemFromBarcode_Result item;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, barcode).FirstOrDefault();

            InConfirmed = false;
            if (item == null) { SetFeedback(false, $"&#128683; Barcode not recognised: {barcode}"); return; }
            if (item.ID != ItemId)
            {
                SetFeedback(false, $"&#9888; Scanned <strong>{item.Code}</strong>, expected <strong>{ItemCode}</strong>.");
                return;
            }

            InConfirmed = true;
            SetFeedback(true, "&#10003; Confirmed &mdash; press Finish to post the move.");
            RenderStep();
        }

        // Step 4 → post: two ItemTransaction TRF legs, same pattern as put-away.
        protected void lbtnFinish_Click(object sender, EventArgs e)
        {
            if (Step != 4 || !InConfirmed) return;

            string toastMsg = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Re-check the source still holds the stock (availability was read without a lock).
                var row = SourceLines(db, ItemCode).FirstOrDefault(r => (r.LotNumber ?? "") == (Lot ?? ""));
                if (row == null || (row.QOH ?? 0) <= 0)
                {
                    SetFeedback(false, "&#9888; That stock is no longer in the source bin.");
                    ResetCycle(); RenderStep(); return;
                }
                if (MoveQty > (row.QOH ?? 0))
                {
                    SetFeedback(false, $"&#9888; Only {(row.QOH ?? 0):0.##} left in {SrcCode} now.");
                    Step = 2; InConfirmed = false; RenderStep(); return;
                }

                // OUT leg leaves at the source store's running weighted average (outs never
                // revalue a store); the IN leg arrives at that same cost and re-blends the
                // destination's running average.
                decimal srcAvg = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, ItemId, SrcId);
                decimal inLineVal;
                decimal destAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID, ItemId, DestId, MoveQty, srcAvg * MoveQty, out inLineVal);

                // IN to destination
                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = 0,
                    DocumentType              = 4,
                    TransactionType           = "TRF",
                    ItemID                    = ItemId,
                    ItemCode                  = row.ItemCode,
                    ItemDescription           = row.ItemDescription,
                    LotNumber                 = Lot,
                    Unit                      = row.Unit,
                    FromID                    = SrcId,
                    ToID                      = DestId,
                    Qty                       = MoveQty,
                    PriceExclusive            = srcAvg,
                    AdditionalCosts           = 0m,
                    TotalUnitPriceExclInclAdd = srcAvg,
                    TotalLineValExcl          = inLineVal,
                    StoreAvgCost              = destAvg,
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = row.ItemCode + " Quick Move " + MoveQty.ToString("0.##") + " to " + DestCode,
                    ExchRate                  = 1
                });
                EnsureStoreLink(db, ItemId, DestId);
                // Both transfer legs (IN to destination, OUT of source) commit in ONE SaveChanges below,
                // so a mid-operation failure can't leave phantom stock (destination up, source not down).

                // OUT of source (mirror)
                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = 0,
                    DocumentType              = 4,
                    TransactionType           = "TRF",
                    ItemID                    = ItemId,
                    ItemCode                  = row.ItemCode,
                    ItemDescription           = row.ItemDescription,
                    LotNumber                 = Lot,
                    Unit                      = row.Unit,
                    FromID                    = DestId,
                    ToID                      = SrcId,
                    Qty                       = MoveQty * -1,
                    PriceExclusive            = srcAvg,
                    AdditionalCosts           = 0m,
                    TotalUnitPriceExclInclAdd = srcAvg,
                    TotalLineValExcl          = srcAvg * (MoveQty * -1),
                    StoreAvgCost              = srcAvg,
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = row.ItemCode + " Quick Move " + MoveQty.ToString("0.##") + " from " + SrcCode,
                    ExchRate                  = 1
                });
                db.SaveChanges();

                RecordDone(row.ItemCode, MoveQty, SrcCode, DestCode);
                toastMsg = $"&#10003; {MoveQty:0.##} {row.ItemCode} &#8594; {DestCode}";
            }

            string movedItem = ItemCode; decimal movedQty = MoveQty; string fromC = SrcCode, toC = DestCode;
            ResetCycle();
            ResetTapInputs();
            SetFeedback(true, $"&#10003; Moved {movedQty:0.##} {movedItem} from {fromC} to {toC}.");
            RenderStep();
            BindDone();

            if (!string.IsNullOrEmpty(toastMsg))
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "moveToast",
                    $"showToast('{JsEscape(toastMsg)}');", true);
        }

        protected void lbtnRestart_Click(object sender, EventArgs e)
        {
            ResetCycle();
            ResetTapInputs();
            ClearFeedback();
            RenderStep();
        }

        // Tap mode only: clear the From/To dropdowns, search box and contents list for a fresh move.
        private void ResetTapInputs()
        {
            if (ScanMode) return;
            Search = ""; txtSearch.Text = "";
            if (ddFromStore.Items.Count > 0) ddFromStore.SelectedIndex = 0;
            if (ddToStore.Items.Count > 0) ddToStore.SelectedIndex = 0;
            rptContents.DataSource = null; rptContents.DataBind();
        }

        protected void lbtnClearScan_Click(object sender, EventArgs e)
        {
            txtScan.Text = string.Empty;
            ClearFeedback();
        }

        // ── Step rendering ──────────────────────────────────────────────────────
        private void RenderStep()
        {
            RenderSteps();

            pnlScanBar.Visible = ScanMode;
            pnlTapMode.Visible = !ScanMode;

            pnlOutDetail.Visible = (Step == 2 && ItemResolved);
            pnlSummary.Visible   = (Step >= 3);
            lbtnFinish.Visible   = (Step == 4 && InConfirmed);
            lbtnRestart.Visible  = (Step > 1);

            if (!ScanMode)
            {
                pnlContents.Visible = (SrcId != 0 && Step == 2);
                pnlToStore.Visible  = (Step == 3);
            }

            if (ScanMode)
            {
                switch (Step)
                {
                    case 1:
                        lblPrompt.Text = "Scan the source bin / location";
                        txtScan.Attributes["placeholder"] = "Scan source location...";
                        break;
                    case 2:
                        lblPrompt.Text = "Scan the item to move out of " + SrcCode;
                        txtScan.Attributes["placeholder"] = "Scan item...";
                        break;
                    case 3:
                        lblPrompt.Text = "Scan the destination bin / location";
                        txtScan.Attributes["placeholder"] = "Scan destination location...";
                        break;
                    case 4:
                        lblPrompt.Text = "Scan the item again at " + DestCode + " to confirm";
                        txtScan.Attributes["placeholder"] = "Scan item to confirm...";
                        break;
                }
            }
            else
            {
                switch (Step)
                {
                    case 1: lblPrompt.Text = "Choose the From location"; break;
                    case 2: lblPrompt.Text = "Tap the item to move out of " + SrcCode; break;
                    case 3: lblPrompt.Text = "Choose the To location"; break;
                    case 4: lblPrompt.Text = "Press Finish to post the move"; break;
                }
            }

            if (pnlOutDetail.Visible)
            {
                lblOutItem.Text  = ItemCode;
                lblOutDescr.Text = ItemDescr;
                lblOutUnit.Text  = ItemUnit;
                lblAvail.Text    = Avail.ToString("0.##");
                ddLot.Visible    = ddLot.Items.Count > 1;
                decimal def      = ScanMode ? (Avail >= 1 ? 1m : Avail) : Avail;
                txtQty.Text      = def.ToString("0.##");
            }

            if (pnlSummary.Visible)
            {
                lblSumFrom.Text = SrcCode;
                lblSumItem.Text = ItemCode + (string.IsNullOrEmpty(Lot) ? "" : "  (Lot " + Lot + ")");
                lblSumQty.Text  = MoveQty.ToString("0.##") + " " + (ItemUnit ?? "");
                lblSumDest.Text = (Step >= 4 && !string.IsNullOrEmpty(DestCode)) ? DestCode : "scan...";
            }
        }

        private void RenderSteps()
        {
            string[] labels = { "Source", "Item", "Dest", "Confirm" };
            var sb = new StringBuilder("<div class=\"qm-steps\">");
            for (int i = 0; i < labels.Length; i++)
            {
                string cls = "qm-step";
                if (i + 1 < Step) cls += " done";
                else if (i + 1 == Step) cls += " active";
                sb.Append($"<span class=\"{cls}\"><b>{i + 1}</b>{labels[i]}</span>");
            }
            sb.Append("</div>");
            lblSteps.Text = sb.ToString();
        }

        private void ResetCycle()
        {
            Step = 1;
            SrcCode = null; SrcId = 0;
            DestCode = null; DestId = 0;
            ItemId = 0; ItemCode = null; ItemDescr = null; ItemUnit = null; Lot = null;
            Avail = 0m; Cost = 0m; MoveQty = 0m;
            ItemResolved = false; InConfirmed = false;
            ddLot.Items.Clear();
        }

        // ── Lookups ───────────────────────────────────────────────────────────
        // Any active store/bin, excluding the reserved system stores.
        private Store ResolveStore(SBMSEntities db, string code)
        {
            return db.Stores.FirstOrDefault(s => s.CompanyID == CurrentUser.CoID
                && s.StoreCode == code
                && s.StoreActive == true
                && s.StoreCode != "CoR"
                && s.StoreCode != "CoD");
        }

        // On-hand lines for an item in the source bin, by lot, qty > 0.
        private List<GetOpeningBalancesAllStores_Result> SourceLines(SBMSEntities db, string itemCode)
        {
            return db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                .Where(r => r.StoreCode == SrcCode && r.ItemCode == itemCode && (r.QOH ?? 0) > 0)
                .OrderBy(r => r.LotNumber)
                .ToList();
        }

        private void EnsureStoreLink(SBMSEntities db, long itemId, long storeId)
        {
            bool exists = db.ItemStoreLinkMasters.Any(x => x.CompanyID == CurrentUser.CoID
                && x.StoreID == (int?)storeId && x.ItemID == itemId);
            if (!exists)
                db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                {
                    CompanyID = CurrentUser.CoID,
                    ItemID    = itemId,
                    StoreID   = (int?)storeId,
                    Active    = true
                });
        }

        // ── "Moved this session" running list (held in Session; clears on revisit) ──
        [Serializable]
        public class MoveDone
        {
            public string ItemCode { get; set; }
            public decimal Qty { get; set; }
            public string From { get; set; }
            public string Dest { get; set; }
            public string TimeText { get; set; }
        }

        private List<MoveDone> DoneList
        {
            get
            {
                var l = Session["QuickMoveSession"] as List<MoveDone>;
                if (l == null) { l = new List<MoveDone>(); Session["QuickMoveSession"] = l; }
                return l;
            }
        }

        private void RecordDone(string itemCode, decimal qty, string from, string dest)
        {
            DoneList.Insert(0, new MoveDone
            {
                ItemCode = itemCode,
                Qty      = qty,
                From     = from,
                Dest     = dest,
                TimeText = DateTime.Now.ToString("HH:mm")
            });
        }

        private void BindDone()
        {
            var l = DoneList;
            pnlDone.Visible    = l.Count > 0;
            lblDoneCount.Text  = l.Count.ToString();
            rptDone.DataSource = l;
            rptDone.DataBind();
        }

        private static string JsEscape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'")
                            .Replace("\r", "").Replace("\n", "");
        }

        // ── Navigation ───────────────────────────────────────────────────────
        protected void lbtnTopBack_Click(object sender, EventArgs e) { GoDashboard(); }
        protected void lbtnTopHome_Click(object sender, EventArgs e) { GoDashboard(); }

        private void GoDashboard()
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

        // ── Feedback ───────────────────────────────────────────────────────────
        private void SetFeedback(bool ok, string html)
        {
            lblFeedback.Text     = html;
            lblFeedback.CssClass = ok ? "mob-feedback found" : "mob-feedback notfound";
        }

        private void ClearFeedback()
        {
            lblFeedback.Text     = string.Empty;
            lblFeedback.CssClass = "mob-feedback";
        }
    }
}
