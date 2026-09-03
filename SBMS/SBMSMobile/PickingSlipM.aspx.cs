using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class PickingSlipM : MobileBasePage
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

        // Closed-off slips are immutable from mobile: close-off wrote the SO lines
        // and doc header, so resets after that must go through the desktop/admin.
        private bool SlipComplete
        {
            get { return ViewState["SlipComplete"] != null && (bool)ViewState["SlipComplete"]; }
            set { ViewState["SlipComplete"] = value; }
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

        // Keyed per slip: a second tab on another slip must not share (or wipe)
        // this slip's double-save guard.
        private string PickedLineIDsKey { get { return "PSPickedLineIDs_" + PSID; } }

        private HashSet<int> PickedLineIDs
        {
            get
            {
                if (Session[PickedLineIDsKey] == null)
                    Session[PickedLineIDsKey] = new HashSet<int>();
                return (HashSet<int>)Session[PickedLineIDsKey];
            }
        }

        // Active box (LPN): scanned from a pre-printed sticker; every pick is stamped
        // with it until the box is closed or the next sticker is scanned. Optional -
        // when empty, picks save with no LPN (pre-LPN behaviour).
        private string ActiveLPN
        {
            get { return ViewState["ActiveLPN"] as string ?? ""; }
            set { ViewState["ActiveLPN"] = value; }
        }

        // Company-level LPN workflow for mobile picking: "off" | "box" | "unit".
        // A process decision, not an operator one - set on ConfigCompany, read once
        // per login session from CompanyMaster (raw SQL - the column is outside the
        // EF model). ConfigCompany refreshes the session cache when saved; other
        // sessions pick a change up at next login.
        private string LPNMode
        {
            get
            {
                if (Session["PSLPNMode"] == null)
                {
                    string mode = "off";
                    try
                    {
                        using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                            mode = db.Database.SqlQuery<string>(
                                    "SELECT LPNPickMode FROM dbo.CompanyMaster WHERE SBCACoID = @p0",
                                    CurrentUser.CoID)
                                .FirstOrDefault() ?? "off";
                    }
                    catch (Exception ex)
                    {
                        // Falling back to 'off' silently disables the company's LPN
                        // enforcement for this whole login - log it so it is visible.
                        new ApiUrlCall().LogErrorToFile(
                            $"CoID:{CurrentUser.CoID} PickingSlipM LPNPickMode lookup failed - defaulting to off - {ex}");
                    }
                    Session["PSLPNMode"] = mode;
                }
                return (string)Session["PSLPNMode"];
            }
        }

        // Company-level "Pick by bin" flag: warehouses that use Stores AS bins pick
        // each line from the bin(s) that actually hold stock (splitting across bins),
        // instead of choosing one store for the whole slip. Session-cached like LPNMode;
        // off = today's behaviour (the store modal) unchanged.
        private bool PickByBin
        {
            get
            {
                if (Session["PSPickByBin"] == null)
                {
                    bool on = false;
                    try
                    {
                        using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                            on = db.Database.SqlQuery<bool>(
                                    "SELECT PickByBin FROM dbo.CompanyMaster WHERE SBCACoID = @p0",
                                    CurrentUser.CoID)
                                .FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        new ApiUrlCall().LogErrorToFile(
                            $"CoID:{CurrentUser.CoID} PickingSlipM PickByBin lookup failed - defaulting to off - {ex}");
                    }
                    Session["PSPickByBin"] = on;
                }
                return (bool)Session["PSPickByBin"];
            }
        }

        // Unit-mode pending sticker: scanned and waiting for its stock scan. The
        // floor rhythm is sticker -> stock, sticker -> stock; each pair adds one
        // unit and the LPN rows themselves are the picked-quantity counter.
        private string PendingLabel
        {
            get { return ViewState["PendingLabel"] as string ?? ""; }
            set { ViewState["PendingLabel"] = value; }
        }

        // Per-line bin-pick totals for this slip (LineID -> qty picked so far across
        // all bins), loaded once per bind. Raw SQL - PickSlipLinePicks is outside EF.
        private Dictionary<int, decimal> _linePickedQty;

        private class LinePickRow { public int LineID { get; set; } public decimal Qty { get; set; } }
        private class BinStockRow { public string StoreCode { get; set; } public decimal Qty { get; set; } public string LotNumber { get; set; } }
        private class BinPickResetRow { public long? ItemTransLineID { get; set; } public string StoreCode { get; set; } }

        // LineID -> label aggregate for this slip, loaded once per bind.
        private Dictionary<int, LineLPNAgg> _lineLPNs;

        private class LineLPNRow
        {
            public int LineID { get; set; }
            public string LPN { get; set; }
        }

        private class LineLPNAgg
        {
            public int Count;
            public string First;
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

            // Barcode-off companies pick by tapping the line (qty prefilled to full); hide the scan bar.
            pnlScanBar.Visible = CurrentUser.MobileModule == true;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                _activeLotNums = db.GetActiveLotNumbersLinkedToStores(CurrentUser.CoID)
                                   .Where(x => x.AllowPicking)
                                   .OrderBy(x => x.LotNumber)
                                   .ToList();
            }

            if (!IsPostBack)
            {
                // Pick-by-bin: no single store for the slip - the bin is chosen per line.
                if (!PickByBin)
                {
                    LoadStoresModal();
                    pnlStoreModal.Visible = true;
                }

                if (!int.TryParse(Request.QueryString["psid"], out int psid) || psid == 0)
                {
                    Response.Redirect("~/SBMSMobile/OSPickingSlipsM.aspx", false);
                    return;
                }

                PSID = psid;
                Session.Remove(PickedLineIDsKey);   // fresh guard for this slip
                LoadPSHeader();
                BindLines();
            }

            UpdateStoreIndicator();
        }

        // Single place that syncs the LPN bar to the company mode + pending state.
        // Runs after all event handlers so scans are already applied.
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            if (CurrentUser == null) return;

            bool unitMode = LPNMode == "unit";
            bool boxMode  = LPNMode == "box";

            // Company-level setting: no LPN UI at all when the mode is off, or in
            // Pick-by-bin mode (the two flows are separate for now).
            pnlLPNBar.Visible = CurrentUser.MobileModule == true && LPNMode != "off" && !PickByBin;

            // All scanning goes through the single item scan box; the placeholder
            // walks the picker through each step so it is never ambiguous.
            if (pnlScanBar.Visible)
            {
                string hint = "Scan or type barcode…";
                if (unitMode) hint = string.IsNullOrEmpty(PendingLabel) ? "Scan LPN" : "Scan Item";
                if (boxMode)  hint = string.IsNullOrEmpty(ActiveLPN) ? "Scan Box LPN" : "Scan Item";
                txtBarcode.Attributes["placeholder"] = hint;
            }

            pnlActiveLPN.Visible = boxMode && !string.IsNullOrEmpty(ActiveLPN);
            if (pnlActiveLPN.Visible) lblActiveLPN.Text = HttpUtility.HtmlEncode(ActiveLPN);

            pnlUnitLPN.Visible = unitMode && !string.IsNullOrEmpty(PendingLabel);
            if (pnlUnitLPN.Visible)
                lblUnitLPNInfo.Text =
                    $"Sticker <strong>{HttpUtility.HtmlEncode(PendingLabel)}</strong> ready " +
                    "&mdash; now scan the stock item";

            lbtnResetSlip.Visible = !SlipComplete;
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
                    lblPickMsg.Text    = HttpUtility.HtmlEncode(ps.PSPickMessage);
                    pnlPickMsg.Visible = true;
                }

                SlipComplete = ps.Complete == true;
                if (SlipComplete)
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

                // Barcode-off: prefill each unpicked line's qty to its full quantity so
                // the operator picks with a single tap. Barcode-on keeps the 0 default.
                bool prefillFull = CurrentUser.MobileModule != true;
                foreach (var l in lines)
                {
                    if (prefillFull && l.PickComplete != true)
                        l.PickQty = l.Quantity;
                    else if (l.PickQty == null)
                        l.PickQty = 0;
                }

                return lines;
            }
        }

        private void BindLines()
        {
            var lines = GetLines();
            LoadLineLPNs();
            LoadLinePickedQty();
            LoadLineSerials(lines);
            lblLineCount.Text   = lines.Count(l => l.PickComplete != true).ToString();
            lblEmpty.Visible    = lines.All(l => l.PickComplete == true);
            rptLines.DataSource = lines;
            rptLines.DataBind();
        }

        // Which items on this slip are serial tracked, and how many serials each line has
        // scanned so far. Loaded ONCE per bind - the card binder runs per line, and a query
        // per card on a handheld over a warehouse connection is felt immediately.
        private HashSet<long> _serialItems = new HashSet<long>();
        private Dictionary<int, int> _lineSerialCount = new Dictionary<int, int>();

        private void LoadLineSerials(List<PickSlipLine> lines)
        {
            _serialItems = new HashSet<long>();
            _lineSerialCount = new Dictionary<int, int>();
            if (lines == null || lines.Count == 0) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ids = lines.Select(l => l.SelectionId).Distinct().ToList();
                foreach (long id in db.ItemsMasters
                             .Where(x => x.CompanyID == CurrentUser.CoID && x.IsSerialTracked && ids.Contains(x.ID))
                             .Select(x => x.ID).ToList())
                    _serialItems.Add(id);

                if (_serialItems.Count == 0) return;   // nothing serial on this slip

                foreach (var l in lines.Where(x => _serialItems.Contains(x.SelectionId)))
                    _lineSerialCount[l.LineID] =
                        SerialPicking.GetLineSerials(db, CurrentUser.CoID, PSID, l.LineID).Count;
            }
        }

        /// <summary>True when this slip line is for a serial-tracked item.</summary>
        private bool IsSerialLine(PickSlipLine line)
        {
            return line != null && _serialItems.Contains(line.SelectionId);
        }

        // Per-line bin-pick totals (raw SQL - PickSlipLinePicks is outside EF).
        private void LoadLinePickedQty()
        {
            if (!PickByBin) return;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                _linePickedQty = db.Database.SqlQuery<LinePickRow>(
                        "SELECT LineID, SUM(Qty) AS Qty FROM dbo.PickSlipLinePicks WHERE CompanyID = @p0 AND PSID = @p1 GROUP BY LineID",
                        CurrentUser.CoID, PSID)
                    .ToList()
                    .ToDictionary(x => x.LineID, x => x.Qty);
            }
        }

        // Bins holding stock of a non-lot item (fullest first). Per-store running
        // balance from the ledger; only bins with a positive balance are returned.
        private List<BinStockRow> GetBinsWithStock(long selectionId)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                return db.Database.SqlQuery<BinStockRow>(
                        "SELECT s.StoreCode AS StoreCode, SUM(t.Qty) AS Qty, CAST(NULL AS varchar(50)) AS LotNumber " +
                        "FROM dbo.ItemTransaction t INNER JOIN dbo.Stores s ON s.StoreID = t.ToID AND s.CompanyID = t.CompanyID " +
                        "WHERE t.CompanyID = @p0 AND t.ItemID = @p1 " +
                        "GROUP BY s.StoreCode HAVING SUM(t.Qty) > 0 ORDER BY SUM(t.Qty) DESC",
                        CurrentUser.CoID, selectionId)
                    .ToList();
            }
        }

        // PickSlipLineLPNs is intentionally outside the EF model (no EDMX churn);
        // it is read and written with parameterised raw SQL only.
        private void LoadLineLPNs()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                _lineLPNs = db.Database.SqlQuery<LineLPNRow>(
                        "SELECT LineID, LPN FROM dbo.PickSlipLineLPNs WHERE CompanyID = @p0 AND PSID = @p1",
                        CurrentUser.CoID, PSID)
                    .ToList()
                    .GroupBy(x => x.LineID)
                    .ToDictionary(g => g.Key,
                                  g => new LineLPNAgg { Count = g.Count(), First = g.First().LPN });
            }
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

            LineLPNAgg agg = null;
            if (_lineLPNs != null) _lineLPNs.TryGetValue(line.LineID, out agg);

            var lpnBadge = (Label)e.Item.FindControl("lblLPNBadge");
            if (lpnBadge != null && agg != null)
            {
                lpnBadge.Text = "&#128230; " + (agg.Count == 1
                    ? HttpUtility.HtmlEncode(agg.First) : agg.Count + " labels");
                lpnBadge.Visible = true;
            }

            var pickBtn = (LinkButton)e.Item.FindControl("lbtnPickLine");
            if (pickBtn != null && isPicked) pickBtn.Enabled = false;

            var txtQty = (TextBox)e.Item.FindControl("txtPickQty");
            if (txtQty != null && isMatched &&
                (line.PickQty == null || line.PickQty == 0))
                txtQty.Text = line.Quantity.HasValue
                    ? line.Quantity.Value.ToString("0.##", CultureInfo.InvariantCulture) : "0";

            // Unit mode: the label scans ARE the count - the qty box is read-only
            // and mirrors the units boxed so far. A short line is committed with the
            // tick button at exactly that scanned count. Barcode-off companies have
            // no scan bar, so the qty stays editable there or picking would be
            // impossible in that configuration.
            if (txtQty != null && LPNMode == "unit" && CurrentUser.MobileModule == true)
            {
                txtQty.Enabled = false;
                if (!isPicked)
                    txtQty.Text = agg != null ? agg.Count.ToString() : "0";
            }

            // ── Pick-by-bin: bin/source selector + progress + qty prefill ──
            if (PickByBin)
            {
                decimal ordered  = line.Quantity ?? 0;
                decimal picked   = (_linePickedQty != null && _linePickedQty.TryGetValue(line.LineID, out decimal p)) ? p : 0m;
                bool binComplete = line.PickComplete == true || picked >= ordered - 0.0001m;
                decimal remaining = ordered - picked;

                var prog = (Label)e.Item.FindControl("lblBinProgress");
                if (prog != null && picked > 0)
                {
                    prog.Text = $"&#128230; {picked:0.##}/{ordered:0.##}";
                    prog.Visible = true;
                }

                // "Done short": finish a partly-picked line at what's been taken so far.
                var doneShort = (LinkButton)e.Item.FindControl("lbtnDoneShort");
                if (doneShort != null)
                    doneShort.Visible = picked > 0 && !binComplete && !SlipComplete;

                var resetB = (LinkButton)e.Item.FindControl("lbtnResetLine");
                if (resetB != null) resetB.Visible = (picked > 0 || binComplete) && !SlipComplete;

                var pickB = (LinkButton)e.Item.FindControl("lbtnPickLine");
                if (pickB != null) pickB.Enabled = !binComplete && !SlipComplete;

                var ddBin = (DropDownList)e.Item.FindControl("ddLotNum");
                var qtyB  = (TextBox)e.Item.FindControl("txtPickQty");
                var optQty = new Dictionary<string, decimal>();   // dropdown value -> that bin's available qty
                if (ddBin != null)
                {
                    ddBin.Items.Clear();
                    ddBin.Visible = !binComplete;
                    ddBin.Items.Add(new ListItem("— Bin —", ""));
                    if (CurrentUser.CompanyUseLotNumbers && line.IsLotTracked)
                    {
                        // Lot items: the lot carries its bin - list lot @ bin (qty).
                        foreach (var lot in _activeLotNums
                                    .Where(x => x.ItemId == line.SelectionId && x.QtyHandToStore > 0)
                                    .OrderByDescending(x => x.QtyHandToStore))
                        {
                            string val = lot.StoreCode + "|" + lot.LotNumber;
                            ddBin.Items.Add(new ListItem($"{lot.StoreCode} · {lot.LotNumber} ({lot.QtyHandToStore:0.##})", val));
                            optQty[val] = (decimal)lot.QtyHandToStore;
                        }
                    }
                    else
                    {
                        var bins = GetBinsWithStock(line.SelectionId);
                        if (bins.Count > 0)
                        {
                            foreach (var b in bins)
                            {
                                string val = b.StoreCode + "|";
                                ddBin.Items.Add(new ListItem($"{b.StoreCode} ({b.Qty:0.##})", val));
                                optQty[val] = b.Qty;
                            }
                        }
                        else
                        {
                            // No stock ledger. A NON-PHYSICAL (service) item has no bin - let
                            // it pick without one. A physical item with no stock has no bin to
                            // offer (the pick is blocked, correctly).
                            bool nonPhysical;
                            using (var pdb = new SBMSEntities(Config.GetConnectionString()))
                                nonPhysical = pdb.ItemsMasters.Any(x => x.CompanyID == CurrentUser.CoID && x.ID == line.SelectionId && x.Physical != true);
                            if (nonPhysical)
                            {
                                ddBin.Items.Add(new ListItem("— no bin (service item) —", "|"));
                                optQty["|"] = remaining;
                            }
                        }
                    }

                    // Pre-select: a bin the picker SCANNED for this line wins; else the fullest.
                    string scannedBin = ViewState["PBin_" + line.LineID] as string;
                    bool selected = false;
                    if (!string.IsNullOrEmpty(scannedBin))
                    {
                        foreach (ListItem li in ddBin.Items)
                            if (li.Value.Split(new[] { '|' }, 2)[0] == scannedBin) { ddBin.SelectedValue = li.Value; selected = true; break; }
                        if (!selected)
                        {
                            // Scanned a bin with no recorded stock for this item - offer it anyway;
                            // the pick's stock check will reject it if it is truly empty.
                            var scannedItem = new ListItem($"{scannedBin} (scanned)", scannedBin + "|");
                            ddBin.Items.Insert(1, scannedItem);
                            ddBin.SelectedValue = scannedItem.Value;
                            optQty[scannedItem.Value] = 0m;   // unknown recorded qty
                            selected = true;
                        }
                    }
                    if (!selected && ddBin.Items.Count > 1) ddBin.SelectedIndex = 1;   // fullest bin

                    // Prefill qty from the SELECTED bin's available qty (not the fullest),
                    // capped at the line's remaining balance.
                    if (qtyB != null && !binComplete)
                    {
                        decimal selQty = optQty.TryGetValue(ddBin.SelectedValue ?? "", out var sq) ? sq : remaining;
                        decimal suggest = Math.Min(remaining, selQty > 0 ? selQty : remaining);
                        if (suggest < 0) suggest = 0;
                        qtyB.Text = suggest.ToString("0.##", CultureInfo.InvariantCulture);
                        qtyB.Enabled = true;
                    }
                }
                return;   // bin mode fully handled this card
            }

            var resetBtn = (LinkButton)e.Item.FindControl("lbtnResetLine");
            if (resetBtn != null) resetBtn.Visible = isPicked && !SlipComplete;

            // ── Serial lines: scanned count, no lot list ──
            // A serial is deeper than a lot, so a lot number means nothing on this card, and
            // the quantity box means nothing either - the count IS the number of units scanned.
            if (IsSerialLine(line))
            {
                int have = _lineSerialCount.ContainsKey(line.LineID) ? _lineSerialCount[line.LineID] : 0;
                decimal need = line.Quantity ?? 0;

                var ddSer = (DropDownList)e.Item.FindControl("ddLotNum");
                if (ddSer != null) ddSer.Visible = false;

                var qtySer = (TextBox)e.Item.FindControl("txtPickQty");
                if (qtySer != null) qtySer.Visible = false;

                var progSer = (Label)e.Item.FindControl("lblBinProgress");
                if (progSer != null)
                {
                    progSer.Text = isPicked
                        ? $"&#128290; {have} serial(s) picked"
                        : $"&#128290; Serials {have}/{need:0.##} &mdash; scan each unit";
                    progSer.Visible = true;
                }

                // Finish short with what has been scanned: the balance goes on back order,
                // exactly as on the web.
                var shortSer = (LinkButton)e.Item.FindControl("lbtnDoneShort");
                if (shortSer != null)
                    shortSer.Visible = have > 0 && !isPicked && !SlipComplete;

                var pickSer = (LinkButton)e.Item.FindControl("lbtnPickLine");
                if (pickSer != null) pickSer.Enabled = have > 0 && !isPicked && !SlipComplete;

                return;   // serial card fully handled
            }

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

            // A serial number identifies the unit AND the line - a serial belongs to exactly
            // one item - so scanning the unit in your hand is the whole interaction. Tried
            // first: a serial is a lot number, and nothing else on this page looks one up.
            if (HandleSerialScan(raw))
            {
                txtBarcode.Text = string.Empty;
                BindLines();
                return;
            }

            // Pick-by-bin takes precedence over LPN scanning (the two flows are
            // separate); in bin mode scans go to normal item matching + the bin-scan
            // branch below, never to the LPN handlers.
            if (!PickByBin && LPNMode == "unit")
            {
                txtBarcode.Text = string.Empty;
                HandleUnitScan(raw);
                return;
            }

            if (!PickByBin && LPNMode == "box")
            {
                txtBarcode.Text = string.Empty;
                HandleBoxScan(raw);
                return;
            }

            GetOneItemFromBarcode_Result barcodeItem = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                barcodeItem = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            string scanCode = barcodeItem != null ? barcodeItem.Code
                            : (MatchesUnpickedLine(raw) ? raw : null);

            if (scanCode == null)
            {
                // Pick-by-bin: an unrecognised scan that is a valid bin, while a line
                // is matched, selects that bin for the line (scan item -> scan bin).
                if (PickByBin && MatchedLineID > 0)
                {
                    bool isBin;
                    using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                        isBin = db.Stores.Any(s => s.CompanyID == CurrentUser.CoID && s.StoreCode == raw);
                    if (isBin)
                    {
                        ViewState["PBin_" + MatchedLineID] = raw;
                        SetFeedback(true, $"&#128230; Bin <strong>{HttpUtility.HtmlEncode(raw)}</strong> selected &mdash; check the qty and tap &#10004;.");
                        txtBarcode.Text = string.Empty;
                        BindLines();
                        return;
                    }
                }
                SetFeedback(false, $"&#128683; Barcode not recognised: {HttpUtility.HtmlEncode(raw)}");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            PickSlipLine matchedLine = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                matchedLine = db.PickSlipLines.FirstOrDefault(l =>
                    l.PSID        == PSID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode  == scanCode
                    && l.LineType  == 0
                    && (l.PickComplete == null || l.PickComplete == false));

            if (matchedLine == null)
            {
                SetFeedback(false,
                    $"&#9888; <strong>{scanCode}</strong> " +
                    "is not on this slip or is already picked.");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            SetFeedback(true,
                $"&#10003; Found: <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> &mdash; " +
                HttpUtility.HtmlEncode(matchedLine.ItemDescription));
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

        protected void lbtnCloseBox_Click(object sender, EventArgs e)
        {
            ActiveLPN = "";
            SetFeedback(true, "&#128230; Box closed &mdash; scan the next box label to continue.");
        }

        protected void lbtnSkipLabels_Click(object sender, EventArgs e)
        {
            SetFeedback(false,
                $"&#9888; Sticker <strong>{HttpUtility.HtmlEncode(PendingLabel)}</strong> discarded.");
            PendingLabel = "";
        }

        // ── Box mode: one box, many items, single scan stream ─────────────────────
        // Anything that resolves to an item is a stock scan (matches a line, picked
        // with the tick button into the open box); anything else is a box label,
        // which opens - or switches to - that box. Picks require an open box.
        private void HandleBoxScan(string raw)
        {
            if (raw.Length > 50) raw = raw.Substring(0, 50);

            GetOneItemFromBarcode_Result item;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            string scanCode = item != null ? item.Code
                            : (MatchesUnpickedLine(raw) ? raw : null);

            if (scanCode == null)
            {
                // ── Box label ──
                if (raw == ActiveLPN)
                {
                    SetFeedback(true,
                        $"&#128230; Already packing into box <strong>{HttpUtility.HtmlEncode(raw)}</strong> " +
                        "&mdash; scan the items.");
                    return;
                }

                string previous = ActiveLPN;
                ActiveLPN = raw;
                SetFeedback(true, string.IsNullOrEmpty(previous)
                    ? $"&#128230; Box <strong>{HttpUtility.HtmlEncode(raw)}</strong> open &mdash; scan the items."
                    : $"&#128230; Switched to box <strong>{HttpUtility.HtmlEncode(raw)}</strong> &mdash; scan the items.");
                return;
            }

            // ── Stock scan ──
            if (string.IsNullOrEmpty(ActiveLPN))
            {
                SetFeedback(false,
                    $"&#9888; <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> scanned, but no box is open " +
                    "&mdash; scan a box LPN first, then the items.");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            PickSlipLine matchedLine;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                matchedLine = db.PickSlipLines.FirstOrDefault(l =>
                    l.PSID         == PSID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode  == scanCode
                    && l.LineType  == 0
                    && (l.PickComplete == null || l.PickComplete == false));

            if (matchedLine == null)
            {
                SetFeedback(false,
                    $"&#9888; <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> " +
                    "is not on this slip or is already picked.");
                MatchedLineID = 0;
                BindLines();
                return;
            }

            SetFeedback(true,
                $"&#10003; <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> &mdash; check the qty and tap " +
                $"&#10004; to pack into box <strong>{HttpUtility.HtmlEncode(ActiveLPN)}</strong>.");
            MatchedLineID = matchedLine.LineID;
            BindLines();
        }

        // ── Unit mode: sticker -> stock pair scanning ──────────────────────────────
        // One scan stream: anything that resolves to an item is a stock scan, anything
        // else is a sticker. A sticker arms PendingLabel; the following stock scan
        // writes one LPN row (qty 1) against the matching line. The count of LPN rows
        // IS the picked quantity - when it reaches the line's ordered qty, the pick
        // commits automatically (stock movement, PickComplete) with no qty typing.
        private void HandleUnitScan(string raw)
        {
            if (raw.Length > 50) raw = raw.Substring(0, 50);

            GetOneItemFromBarcode_Result item;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            string scanCode = item != null ? item.Code
                            : (MatchesUnpickedLine(raw) ? raw : null);

            if (scanCode == null) { HandleUnitSticker(raw); return; }

            // ── Stock scan ──
            if (string.IsNullOrEmpty(PendingLabel))
            {
                SetFeedback(false,
                    $"&#9888; <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> scanned, but no sticker " +
                    "is ready &mdash; scan the box sticker first, then the stock item.");
                return;
            }

            if (string.IsNullOrEmpty(SelectedStore))
            {
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            int lineId; decimal orderedQty; string itemCode; bool lotTracked;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.PickSlipLines.FirstOrDefault(l =>
                    l.PSID         == PSID
                    && l.CompanyID == CurrentUser.CoID
                    && l.ItemCode  == scanCode
                    && l.LineType  == 0
                    && (l.PickComplete == null || l.PickComplete == false));

                if (line == null)
                {
                    SetFeedback(false,
                        $"&#9888; <strong>{HttpUtility.HtmlEncode(scanCode)}</strong> is not on this slip " +
                        "or is already picked. Sticker is still ready.");
                    return;
                }

                lineId     = line.LineID;
                orderedQty = line.Quantity ?? 0;
                itemCode   = line.ItemCode ?? "";
                lotTracked = line.IsLotTracked && CurrentUser.CompanyUseLotNumbers;

                db.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.PickSlipLineLPNs (CompanyID, PSID, LineID, LPN, Qty, CreatedBy) " +
                    "VALUES (@p0, @p1, @p2, @p3, 1, @p4)",
                    CurrentUser.CoID, PSID, lineId, PendingLabel, CurrentUser.RoleID);
            }

            string usedLabel = PendingLabel;
            PendingLabel = "";

            int scanned = CountLineLabels(lineId);
            int required = Math.Max(1, (int)Math.Ceiling(orderedQty));

            if (scanned >= required)
            {
                // Full quantity reached - commit the pick. Never over-invoice: the
                // committed qty is the ordered qty even if an extra pair sneaks in.
                string lotNum = lotTracked ? GetCardLot(lineId) : "";
                if (lotTracked && string.IsNullOrEmpty(lotNum))
                {
                    SetFeedback(false,
                        $"&#9888; All {required} unit(s) labelled for <strong>{HttpUtility.HtmlEncode(itemCode)}</strong>, " +
                        "but it is Lot Tracked &mdash; select a lot number on the line and tap &#10004; to complete.");
                    BindLines();
                    return;
                }

                string error = TrySavePickLine(lineId, Math.Min(scanned, orderedQty), SelectedStore, lotNum);
                if (error != null)
                {
                    SetFeedback(false, "&#9888; " + error);
                    BindLines();
                    return;
                }

                PickedLineIDs.Add(lineId);
                MatchedLineID = 0;
                SetFeedback(true,
                    $"&#10003; <strong>{HttpUtility.HtmlEncode(itemCode)}</strong> complete &mdash; " +
                    $"{required}/{required} units boxed and picked.");
            }
            else
            {
                SetFeedback(true,
                    $"&#127991;&#65039; <strong>{HttpUtility.HtmlEncode(itemCode)}</strong> &mdash; unit " +
                    $"{scanned}/{required} boxed ({HttpUtility.HtmlEncode(usedLabel)}). Scan the next sticker.");
            }

            BindLines();
        }

        private void HandleUnitSticker(string lpn)
        {
            if (lpn == PendingLabel)
            {
                SetFeedback(true,
                    $"&#127991;&#65039; Sticker <strong>{HttpUtility.HtmlEncode(lpn)}</strong> is already ready " +
                    "&mdash; scan the stock item.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                int dup = db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM dbo.PickSlipLineLPNs WHERE CompanyID = @p0 AND PSID = @p1 AND LPN = @p2",
                        CurrentUser.CoID, PSID, lpn)
                    .First();
                if (dup > 0)
                {
                    SetFeedback(false,
                        $"&#9888; Sticker <strong>{HttpUtility.HtmlEncode(lpn)}</strong> was already used on this slip.");
                    return;
                }
            }

            string replaced = PendingLabel;
            PendingLabel = lpn;

            SetFeedback(true, string.IsNullOrEmpty(replaced)
                ? $"&#127991;&#65039; Sticker <strong>{HttpUtility.HtmlEncode(lpn)}</strong> ready &mdash; now scan the stock item."
                : $"&#127991;&#65039; Sticker <strong>{HttpUtility.HtmlEncode(lpn)}</strong> ready " +
                  $"(unused sticker {HttpUtility.HtmlEncode(replaced)} discarded) &mdash; now scan the stock item.");
        }

        // The slip's own lines are authoritative: a scan equal to ANY line's item
        // code on this slip is a stock scan even when the barcode/item-master
        // lookup fails (e.g. the master code was renamed after the slip was
        // created). Without this, such a scan would be misread as a box label.
        // Picked lines are included on purpose - the downstream match then gives
        // a proper "already picked" message instead of arming a bogus sticker.
        private bool MatchesUnpickedLine(string code)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                return db.PickSlipLines.Any(l =>
                    l.PSID         == PSID
                    && l.CompanyID == CurrentUser.CoID
                    && l.LineType  == 0
                    && l.ItemCode  == code);
        }

        private int CountLineLabels(int lineId)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                return db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM dbo.PickSlipLineLPNs WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                        CurrentUser.CoID, PSID, lineId)
                    .First();
        }

        // Reads the lot dropdown on the line's card, if rendered.
        private string GetCardLot(int lineId)
        {
            foreach (RepeaterItem item in rptLines.Items)
            {
                var hf = (HiddenField)item.FindControl("hfLineID");
                if (hf == null || hf.Value != lineId.ToString()) continue;
                var ddLot = (DropDownList)item.FindControl("ddLotNum");
                return ddLot != null && ddLot.Visible ? (ddLot.SelectedValue ?? "") : "";
            }
            return "";
        }

        // ── Per-line Pick ──────────────────────────────────────────────────────────
        protected void lbtnPickLine_Click(object sender, EventArgs e)
        {
            // ── Serial line: the pick quantity IS the number of units scanned, so the tick
            //    means "book what I have". Checked before the bin branch, because a serial
            //    line has no qty box and no bin list to read.
            var itemS = ((LinkButton)sender).NamingContainer as RepeaterItem;
            var hfS = (HiddenField)itemS?.FindControl("hfLineID");
            if (int.TryParse(hfS?.Value, out int serialLineId)
                && CommitSerialLineIfSerial(serialLineId)) return;

            // ── Pick-by-bin: take this qty from the selected bin; accumulate. ──
            if (PickByBin)
            {
                var itemB = ((LinkButton)sender).NamingContainer as RepeaterItem;
                if (itemB == null) return;
                var hfB  = (HiddenField)itemB.FindControl("hfLineID");
                var qtyB = (TextBox)itemB.FindControl("txtPickQty");
                var ddB  = (DropDownList)itemB.FindControl("ddLotNum");
                if (!int.TryParse(hfB?.Value, out int binLineId)) return;

                if (!decimal.TryParse((qtyB?.Text ?? "").Trim(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal binQty) || binQty <= 0)
                {
                    SetFeedback(false, "&#9888; Enter a valid quantity for this bin.");
                    return;
                }
                string binVal = ddB?.SelectedValue ?? "";
                if (string.IsNullOrEmpty(binVal))
                {
                    SetFeedback(false, "&#9888; Choose (or scan) the bin to pick from.");
                    return;
                }
                // Split on the FIRST '|' only, so a lot number containing '|' survives.
                var parts = binVal.Split(new[] { '|' }, 2);
                string binStore = parts[0];
                string binLot   = parts.Length > 1 ? parts[1] : "";

                string binErr = TrySaveBinPick(binLineId, binQty, binStore, binLot);
                if (binErr != null) { SetFeedback(false, "&#9888; " + binErr); return; }

                ViewState.Remove("PBin_" + binLineId);   // clear the scanned bin so the next pick re-suggests fresh
                MatchedLineID = 0;
                ClearFeedback();
                BindLines();
                return;
            }

            if (string.IsNullOrEmpty(SelectedStore))
            {
                LoadStoresModal();
                pnlStoreModal.Visible = true;
                return;
            }

            // Box mode means every pick goes into a box - require one to be open.
            if (LPNMode == "box" && string.IsNullOrEmpty(ActiveLPN))
            {
                SetFeedback(false, "&#9888; No box is open &mdash; scan a box LPN first, then pick the items into it.");
                return;
            }

            var btn  = (LinkButton)sender;
            var item = btn.NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf     = (HiddenField)item.FindControl("hfLineID");
            var txtQty = (TextBox)item.FindControl("txtPickQty");
            var ddLot  = (DropDownList)item.FindControl("ddLotNum");

            if (!int.TryParse(hf?.Value, out int lineId)) return;

            // Invariant parse: <input type="number"> posts dot-decimal regardless of
            // device locale; a current-culture parse on a comma-decimal server reads
            // "2.5" as 25 (dot taken as a group separator) - a 10x over-pick.
            if (!decimal.TryParse((txtQty?.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
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
            // Not valid in Pick-by-bin mode (each line is taken from specific bins).
            // The button is hidden in markup; this is defence in depth.
            if (PickByBin)
            {
                SetFeedback(false, "&#9888; Pick All is not available in bin mode &mdash; pick each line from its bin.");
                return;
            }

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

        // ── Reset (mobile re-start) ────────────────────────────────────────────────
        protected void lbtnResetLine_Click(object sender, EventArgs e)
        {
            if (SlipComplete)
            {
                SetFeedback(false, "&#9888; This slip is closed off &mdash; it can no longer be reset from mobile.");
                return;
            }

            var btn  = (LinkButton)sender;
            var item = btn.NamingContainer as RepeaterItem;
            var hf   = (HiddenField)item?.FindControl("hfLineID");
            if (!int.TryParse(hf?.Value, out int lineId)) return;

            string error = TryResetPickLine(lineId, out string itemCode);
            if (error != null)
            {
                SetFeedback(false, "&#9888; " + error);
                return;
            }

            ViewState.Remove("PBin_" + lineId);   // clear any scanned-bin memory for the line
            MatchedLineID = 0;
            SetFeedback(true,
                $"&#8634; <strong>{HttpUtility.HtmlEncode(itemCode)}</strong> reset &mdash; " +
                "stock returned, labels cleared. Pick it again when ready.");
            BindLines();
        }

        protected void lbtnResetSlip_Click(object sender, EventArgs e)
        {
            if (SlipComplete)
            {
                SetFeedback(false, "&#9888; This slip is closed off &mdash; it can no longer be reset from mobile.");
                return;
            }

            List<int> pickedIds;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                pickedIds = db.PickSlipLines
                    .Where(l => l.PSID == PSID && l.CompanyID == CurrentUser.CoID
                             && l.LineType == 0 && l.PickComplete == true)
                    .Select(l => l.LineID)
                    .ToList();

                // Pick-by-bin: also reset lines only PARTLY picked across bins
                // (PickComplete is still false but bin-pick rows exist).
                if (PickByBin)
                {
                    var partialIds = db.Database.SqlQuery<int>(
                            "SELECT DISTINCT LineID FROM dbo.PickSlipLinePicks WHERE CompanyID = @p0 AND PSID = @p1",
                            CurrentUser.CoID, PSID)
                        .ToList();
                    foreach (var pid in partialIds)
                        if (!pickedIds.Contains(pid)) pickedIds.Add(pid);
                }
            }

            int resetCount = 0;
            var failed = new List<string>();
            foreach (int id in pickedIds)
            {
                string error = TryResetPickLine(id, out string itemCode);
                if (error != null) failed.Add($"{itemCode} ({error})");
                else resetCount++;
            }

            ActiveLPN     = "";
            PendingLabel  = "";
            MatchedLineID = 0;

            if (failed.Any())
                SetFeedback(false,
                    $"&#9888; Slip reset: {resetCount} line(s) done, failed: {string.Join(", ", failed)}");
            else if (resetCount == 0)
                SetFeedback(true, "&#8634; Nothing to reset &mdash; no lines are picked.");
            else
                SetFeedback(true,
                    $"&#8634; Slip reset &mdash; {resetCount} line(s) returned to unpicked, " +
                    "stock reversed, labels cleared.");

            BindLines();
        }

        // Returns a picked line to unpicked: reverses its stock movement (same
        // reversal pattern a re-pick uses), clears pick fields and deletes its
        // LPN labels. Never touches SO lines - resets are blocked once the slip
        // is closed off.
        private string TryResetPickLine(int lineId, out string itemCode)
        {
            itemCode = "";
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Authoritative close-off check: the ViewState flag can be stale
                // (finalised in this session, or by another picker on another
                // device) - a closed slip must never be reset from mobile.
                bool closedOff = db.PickingSlipMasters.Any(x =>
                    x.CustomerID == CurrentUser.CoID && x.PSID == PSID && x.PSComplete == true);
                if (closedOff)
                {
                    SlipComplete = true;
                    return "This slip is closed off - it can no longer be reset from mobile.";
                }

                var line = db.PickSlipLines
                    .FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return "Line not found.";
                itemCode = line.ItemCode ?? "";

                // Only Pick-by-bin companies have bin-pick rows; skip the raw query when
                // the flag is off so a company that never ran the PickByBin migration can
                // still reset normally (the "off = unchanged" isolation guarantee).
                decimal binPickedForReset = PickByBin ? BinPickedQty(db, lineId) : 0m;

                // ── Serial line: it moved as one row PER UNIT and stamps no ItemTransLineID,
                //    so the checks and the reversal below would both find nothing and leave
                //    every unit issued. Put them all back and clear the scanned list.
                if (SerialGuard.IsSerialItem(db, CurrentUser.CoID, line.SelectionId))
                {
                    int resetStoreId = db.Stores
                        .Where(x => x.StoreCode == (line.StoreCodeFrom ?? SelectedStore) && x.CompanyID == CurrentUser.CoID)
                        .Select(x => x.StoreID).FirstOrDefault();

                    string serialRevErr = ReverseSerialMovements(db, line, resetStoreId);
                    if (serialRevErr != null) return serialRevErr;

                    SerialPicking.SetLineSerials(db, CurrentUser.CoID, PSID, lineId,
                                                 new List<string>(), CurrentUser.RoleID);
                    line.PickQty      = null;
                    line.PickComplete = false;
                    line.PickTime     = null;
                    line.LotNumber    = null;
                    db.SaveChanges();

                    PickedLineIDs.Remove(lineId);
                    RefreshPickStatus(PSID);
                    return null;
                }

                if (line.PickComplete != true && (line.ItemTransLineID ?? 0) == 0 && binPickedForReset <= 0)
                    return "Line is not picked.";

                if (line.ItemTransLineID != null && line.ItemTransLineID > 0)
                {
                    long prevId  = (long)line.ItemTransLineID;
                    var  prevTrn = db.ItemTransactions
                        .FirstOrDefault(t => t.CompanyID == CurrentUser.CoID && t.TrnID == prevId);

                    if (prevTrn != null)
                    {
                        // Put the stock back INTO the store it was picked from, at the
                        // pick's original cost; the inbound re-blends the store average.
                        int prevStoreId = Convert.ToInt32(prevTrn.ToID ?? 0);
                        decimal revQty = (decimal)prevTrn.Qty * -1;
                        decimal revVal = (decimal)(prevTrn.TotalUnitPriceExclInclAdd ?? 0m) * revQty;
                        decimal revLineVal;
                        decimal revAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID,
                            Convert.ToInt64(prevTrn.ItemID), prevStoreId, revQty, revVal, out revLineVal);

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
                            ToID                      = prevStoreId,
                            Qty                       = revQty,
                            DocumentType              = 10,
                            PriceExclusive            = prevTrn.PriceExclusive,
                            TotalUnitPriceExclInclAdd = prevTrn.TotalUnitPriceExclInclAdd,
                            AdditionalCosts           = 0,
                            TotalLineValExcl          = prevTrn.TotalUnitPriceExclInclAdd * prevTrn.Qty * -1,
                            StoreAvgCost              = revAvg,
                            TransactionDate           = DateTime.Now,
                            ByRoleID                  = CurrentUser.RoleID,
                            TransactionReference      = PSIntNumber + " Reset",
                            LotNumber                 = prevTrn.LotNumber,
                            ExchRate                  = 1
                        });
                    }
                }

                // Pick-by-bin: reverse EACH bin-pick's outbound movement (stock back
                // into the bin it came from, at that pick's cost) before clearing.
                // Gated on the flag so the reset path never touches PickSlipLinePicks
                // for a non-bin company (whose DB may not have the table).
                if (PickByBin)
                {
                var binResetRows = db.Database.SqlQuery<BinPickResetRow>(
                        "SELECT ItemTransLineID, StoreCode FROM dbo.PickSlipLinePicks " +
                        "WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2 AND ItemTransLineID IS NOT NULL",
                        CurrentUser.CoID, PSID, lineId)
                    .ToList();
                int missingTrn = 0;
                foreach (var bp in binResetRows)
                {
                    var pTrn = db.ItemTransactions.FirstOrDefault(t => t.CompanyID == CurrentUser.CoID && t.TrnID == bp.ItemTransLineID);
                    if (pTrn == null) { missingTrn++; continue; }   // transaction edited out-of-band; logged below
                    int pStoreId = Convert.ToInt32(pTrn.ToID ?? 0);
                    decimal rQty = (decimal)pTrn.Qty * -1;
                    decimal rVal = (decimal)(pTrn.TotalUnitPriceExclInclAdd ?? 0m) * rQty;
                    decimal rLineVal;
                    decimal rAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID,
                        Convert.ToInt64(pTrn.ItemID), pStoreId, rQty, rVal, out rLineVal);
                    db.ItemTransactions.Add(new ItemTransaction
                    {
                        CompanyID                 = CurrentUser.CoID,
                        DocumentID                = pTrn.DocumentID,
                        TransactionType           = "PS",
                        ItemID                    = pTrn.ItemID,
                        ItemCode                  = pTrn.ItemCode ?? "",
                        ItemDescription           = pTrn.ItemDescription ?? "",
                        Unit                      = pTrn.Unit,
                        FromID                    = 0,
                        ToID                      = pStoreId,
                        Qty                       = rQty,
                        DocumentType              = 10,
                        PriceExclusive            = pTrn.PriceExclusive,
                        TotalUnitPriceExclInclAdd = pTrn.TotalUnitPriceExclInclAdd,
                        AdditionalCosts           = 0,
                        TotalLineValExcl          = pTrn.TotalUnitPriceExclInclAdd * pTrn.Qty * -1,
                        StoreAvgCost              = rAvg,
                        TransactionDate           = DateTime.Now,
                        ByRoleID                  = CurrentUser.RoleID,
                        TransactionReference      = PSIntNumber + " Reset",
                        LotNumber                 = pTrn.LotNumber,
                        ExchRate                  = 1
                    });
                }
                if (missingTrn > 0)
                    new ApiUrlCall().LogErrorToFile(
                        $"CoID:{CurrentUser.CoID} PickingSlipM reset PSID:{PSID} Line:{lineId} - {missingTrn} bin-pick(s) had no matching ItemTransaction to reverse (edited out-of-band); rows cleared without reversal - verify bin stock.");
                }

                line.ItemTransLineID = null;
                line.PickQty         = 0;
                line.PickComplete    = false;
                line.PickTime        = null;
                line.StoreCodeFrom   = null;
                line.LotNumber       = null;
                db.SaveChanges();

                db.Database.ExecuteSqlCommand(
                    "DELETE FROM dbo.PickSlipLineLPNs WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                    CurrentUser.CoID, PSID, lineId);
                if (PickByBin)
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.PickSlipLinePicks WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                        CurrentUser.CoID, PSID, lineId);
            }

            PickedLineIDs.Remove(lineId);
            RefreshPickStatus(PSID);
            return null;
        }

        // Finish a partly-picked bin line SHORT, at whatever has been picked so far,
        // so the slip can be closed off (like a short pick in the normal flow).
        protected void lbtnDoneShort_Click(object sender, EventArgs e)
        {
            if (SlipComplete) { SetFeedback(false, "&#9888; This slip is closed off."); return; }
            var item = ((LinkButton)sender).NamingContainer as RepeaterItem;
            var hf = (HiddenField)item?.FindControl("hfLineID");
            if (!int.TryParse(hf?.Value, out int lineId)) return;

            if (CommitSerialLineIfSerial(lineId)) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Authoritative close-off check (matches Reset / Finalise): don't touch
                // a line on a slip that another device has already closed off.
                if (db.PickingSlipMasters.Any(x => x.CustomerID == CurrentUser.CoID && x.PSID == PSID && x.PSComplete == true))
                {
                    SlipComplete = true;
                    SetFeedback(false, "&#9888; This slip is closed off.");
                    return;
                }

                var line = db.PickSlipLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return;
                decimal picked = BinPickedQty(db, lineId);
                if (picked <= 0) { SetFeedback(false, "&#9888; Nothing picked on this line yet."); return; }
                line.PickQty      = picked;
                line.PickComplete = true;
                line.PickTime     = DateTime.Now;
                db.SaveChanges();
            }
            PickedLineIDs.Add(lineId);
            MatchedLineID = 0;
            RefreshPickStatus(PSID);
            SetFeedback(true, "&#10003; Line finished short at what was picked.");
            BindLines();
        }

        // ── Pick-by-bin: one bin-pick of a line (additive) ─────────────────────────
        // Takes `qty` of the line's item OUT of `storeCode` (a bin), records it in
        // PickSlipLinePicks, and completes the line once its bin-picks sum to the
        // ordered qty. Each bin-pick is its own outbound ItemTransaction; corrections
        // go through Reset (which reverses them all). No re-pick reversal here.
        private decimal BinPickedQty(SBMSEntities db, int lineId)
        {
            return db.Database.SqlQuery<decimal>(
                    "SELECT ISNULL(SUM(Qty),0) FROM dbo.PickSlipLinePicks WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                    CurrentUser.CoID, PSID, lineId)
                .FirstOrDefault();
        }

        private string TrySaveBinPick(int lineId, decimal qty, string storeCode, string lotNum)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Authoritative close-off check (the ViewState/UI guard can be stale):
                // a slip closed on another device must never accept a bin pick, which
                // would move stock that is never re-invoiced.
                bool closedOff = db.PickingSlipMasters.Any(x =>
                    x.CustomerID == CurrentUser.CoID && x.PSID == PSID && x.PSComplete == true);
                if (closedOff) { SlipComplete = true; return "This slip is closed off - it can no longer be picked."; }

                var line = db.PickSlipLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return "Line not found.";
                if (line.PickComplete == true) return $"{line.ItemCode} is already fully picked.";

                var itm = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == line.SelectionId);
                bool isPhysical = itm?.Physical == true;

                // Serial capture is web-only for now. This page books one lumped movement per
                // line, which on a serial line would be -20 against a lot holding one unit:
                // stock corrupted and nineteen units stranded as still on hand. Refuse it
                // rather than write it.
                if (itm != null && itm.IsSerialTracked)
                {
                    return itm.Code + " is serial tracked - pick it on the web screen so each "
                         + "unit's serial number can be recorded.";
                }

                decimal ordered   = line.Quantity ?? 0;
                decimal already   = BinPickedQty(db, lineId);
                decimal remaining = ordered - already;
                if (remaining <= 0.0001m) { line.PickComplete = true; db.SaveChanges(); RefreshPickStatus(PSID); return null; }
                if (qty > remaining) qty = remaining;   // never over-pick the line

                // Resolve the bin. Non-physical (service) items have no stock ledger and
                // no bin - they pick without one.
                int storeId = 0;
                if (!string.IsNullOrEmpty(storeCode))
                    storeId = db.Stores.Where(x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID)
                                       .Select(x => x.StoreID).FirstOrDefault();

                if (isPhysical)
                {
                    if (storeId == 0)
                        return string.IsNullOrEmpty(storeCode) ? "Choose (or scan) the bin to pick from." : $"'{storeCode}' is not a valid bin.";
                    if (line.IsLotTracked && CurrentUser.CompanyUseLotNumbers && string.IsNullOrEmpty(lotNum))
                        return $"{line.ItemCode} is Lot Tracked — choose a lot/bin before picking.";

                    var trnQuery = db.ItemTransactions.Where(t => t.CompanyID == CurrentUser.CoID
                        && t.ItemID == line.SelectionId && t.ToID == storeId);
                    if (!string.IsNullOrEmpty(lotNum)) trnQuery = trnQuery.Where(t => t.LotNumber == lotNum);
                    decimal qoh = trnQuery.Select(t => (decimal?)t.Qty).DefaultIfEmpty(0).Sum() ?? 0m;
                    if (qoh < qty)
                        return $"Insufficient stock for {line.ItemCode} in {storeCode}: only {qoh:0.##} available.";
                }

                decimal price = (isPhysical && storeId > 0)
                    ? StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, Convert.ToInt64(line.SelectionId), storeId)
                    : 0m;
                if (price == 0m && itm != null) price = (decimal)(itm.AverageCost ?? 0m);

                // Atomic: the stock movement, its counter row, and the line update land
                // together or not at all - so a failure can never orphan a movement that
                // reset would be unable to reverse.
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        var newTrn = new ItemTransaction
                        {
                            CompanyID                 = CurrentUser.CoID,
                            DocumentID                = PSID,
                            TransactionType           = "PS",
                            ItemID                    = line.SelectionId,
                            ItemCode                  = line.ItemCode ?? "",
                            ItemDescription           = line.ItemDescription ?? "",
                            Unit                      = line.Unit,
                            FromID                    = 0,
                            ToID                      = storeId,
                            Qty                       = qty * -1,
                            DocumentType              = 10,
                            PriceExclusive            = price,
                            TotalUnitPriceExclInclAdd = price,
                            AdditionalCosts           = 0,
                            TotalLineValExcl          = price * qty * -1,
                            StoreAvgCost              = price,
                            TransactionDate           = DateTime.Now,
                            ByRoleID                  = CurrentUser.RoleID,
                            TransactionReference      = PSIntNumber,
                            LotNumber                 = lotNum,
                            ExchRate                  = 1
                        };
                        db.ItemTransactions.Add(newTrn);
                        db.SaveChanges();

                        db.Database.ExecuteSqlCommand(
                            "INSERT INTO dbo.PickSlipLinePicks (CompanyID, PSID, LineID, StoreCode, LotNumber, Qty, ItemTransLineID, CreatedBy) " +
                            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)",
                            CurrentUser.CoID, PSID, lineId, storeCode ?? "",
                            (object)(string.IsNullOrEmpty(lotNum) ? null : lotNum) ?? DBNull.Value,
                            qty, newTrn.TrnID, CurrentUser.RoleID);

                        decimal total = already + qty;
                        line.PickQty       = total;
                        line.PickTime      = DateTime.Now;
                        line.StoreCodeFrom = storeCode;   // last bin used; the per-bin detail is in PickSlipLinePicks
                        if (!string.IsNullOrEmpty(lotNum)) line.LotNumber = lotNum;
                        if (total >= ordered - 0.0001m) line.PickComplete = true;
                        db.SaveChanges();
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} PickingSlipM TrySaveBinPick failed (rolled back) PSID:{PSID} Line:{lineId} - {ex}");
                        return "Pick failed - nothing was moved. Please try again.";
                    }
                }
            }
            RefreshPickStatus(PSID);
            return null;
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

                // Never pick above the ordered quantity - close-off invoices
                // ReceiveQty, so an over-pick becomes an over-invoice. (Matches
                // the desktop clamp; short picks remain allowed.)
                if (line.Quantity.HasValue && qty > line.Quantity.Value)
                    qty = line.Quantity.Value;

                int storeId = db.Stores
                    .Where(x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID)
                    .Select(x => x.StoreID)
                    .FirstOrDefault();

                if (line.IsLotTracked && CurrentUser.CompanyUseLotNumbers &&
                    string.IsNullOrEmpty(lotNum))
                    return $"{line.ItemCode} is Lot Tracked — select a lot number before picking.";

                // Serial lines never come through here. This method books ONE movement for the
                // whole quantity, which against a lot holding a single unit is -20 on a lot of
                // one: stock corrupted, nineteen units stranded as on hand. They are picked by
                // scanning each unit, which routes to TrySaveSerialPick instead.
                if (SerialGuard.IsSerialItem(db, CurrentUser.CoID, line.SelectionId))
                    return $"{line.ItemCode} is serial tracked — scan each unit's serial number "
                         + "instead of entering a quantity.";

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

                // Box mode: a (re-)pick replaces the line's stamp with the active box
                // for the full pick qty. Unit mode: the accumulated per-unit rows ARE
                // the pick (they were written pair-by-pair) - leave them untouched.
                if (LPNMode != "unit")
                {
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.PickSlipLineLPNs WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                        CurrentUser.CoID, PSID, lineId);
                    if (!string.IsNullOrEmpty(ActiveLPN))
                        db.Database.ExecuteSqlCommand(
                            "INSERT INTO dbo.PickSlipLineLPNs (CompanyID, PSID, LineID, LPN, Qty, CreatedBy) " +
                            "VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                            CurrentUser.CoID, PSID, lineId, ActiveLPN, qty, CurrentUser.RoleID);
                }
            }

            RefreshPickStatus(PSID);
            return null;
        }

        // PSStatus lifecycle when Pick-Slip Tracking is off: Captured -> Started -> Picked.
        // (Complete is set at close-off; when tracking is on, station names drive PSStatus.)
        private void RefreshPickStatus(int psid)
        {
            if (CurrentUser.UsePickSlipTracking == true) return;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var psm = db.PickingSlipMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.PSID == psid);
                if (psm == null || psm.PSComplete == true) return;
                var lines = db.PickSlipLines.Where(l => l.PSID == psid && l.CompanyID == CurrentUser.CoID && l.LineType == 0).ToList();
                if (lines.Count == 0) return;
                string newStatus = lines.All(l => l.PickComplete == true) ? "Picked"
                                 : lines.Any(l => l.PickComplete == true) ? "Started"
                                 : "Captured";
                if (psm.PSStatus != newStatus) { psm.PSStatus = newStatus; db.SaveChanges(); }
            }
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

            if (LPNMode == "unit" && !string.IsNullOrEmpty(PendingLabel))
            {
                SetFeedback(false,
                    $"&#9888; Sticker <strong>{HttpUtility.HtmlEncode(PendingLabel)}</strong> is ready but no " +
                    "stock item was scanned for it &mdash; scan the item or tap Discard first.");
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

            // Authoritative double-finalise guard: a second tap, or a second picker
            // on another device, must not close off the slip twice (duplicate
            // PickSlipTransactions, notifications, and re-written SO lines).
            using (SBMSEntities dbChk = new SBMSEntities(Config.GetConnectionString()))
            {
                bool alreadyClosed = dbChk.PickingSlipMasters.Any(x =>
                    x.CustomerID == CurrentUser.CoID && x.PSID == PSID && x.PSComplete == true);
                if (alreadyClosed)
                {
                    SlipComplete         = true;
                    pnlFinalise.Visible  = false;
                    lbtnFinalise.Enabled = false;
                    SetFeedback(false, "&#9888; This slip has already been closed off.");
                    BindLines();
                    return;
                }
            }

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
                        .Where(x => x.PSID == slipId && x.CompanyID == CurrentUser.CoID)
                        .OrderBy(x => x.LineID)
                        .ToList();

                    foreach (var psl in psLines)
                    {
                        decimal pickQty = psl.PickQty ?? psl.Quantity ?? 0;

                        if (psl.SBCALineID != null && psl.SBCALineID != 0)
                        {
                            var soLine = db.DocLines
                                .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.SBCALineID == psl.SBCALineID);

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
                                // VAT is charged on the discounted-NET amount, not the gross line value.
                                soLine.Tax             = (soLine.Exclusive - soLine.Discount)
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
                        psm.PSStatus       = (CurrentUser.UsePickSlipTracking == true) ? finalStat.PSName : "Complete";
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
                SlipComplete         = true;   // hides + blocks Reset from here on
                Session.Remove(PickedLineIDsKey);

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

        /// <summary>
        /// Books a serial line at whatever has been scanned so far - a part pick. Returns true
        /// when the line WAS a serial line and has been dealt with, so the caller stops.
        /// Shared by the tick button and "Done short": on a serial line they mean the same
        /// thing, because the count of units scanned is the pick quantity.
        /// </summary>
        private bool CommitSerialLineIfSerial(int lineId)
        {
            List<string> serials;
            string itemCode;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.PickSlipLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return false;
                if (!SerialGuard.IsSerialItem(db, CurrentUser.CoID, line.SelectionId)) return false;

                itemCode = line.ItemCode ?? "";
                serials = SerialPicking.GetLineSerials(db, CurrentUser.CoID, PSID, lineId);
            }

            if (serials.Count == 0)
            {
                SetFeedback(false, "&#9888; Scan at least one serial number for " + HttpUtility.HtmlEncode(itemCode) + " first.");
                BindLines();
                return true;
            }

            string err = TrySaveSerialPick(lineId, serials, SelectedStore);
            if (err != null) { SetFeedback(false, "&#9888; " + err); BindLines(); return true; }

            PickedLineIDs.Add(lineId);
            MatchedLineID = 0;
            SetFeedback(true, $"&#10003; <strong>{HttpUtility.HtmlEncode(itemCode)}</strong> picked with "
                + $"{serials.Count} serial(s). Any balance goes on back order at close-off.");
            BindLines();
            return true;
        }

        // ══ Serial picking ══════════════════════════════════════════════════════════
        //
        // A serial IS a lot holding one unit, so picking one is "choose these exact lots".
        // On a scanner that is simply: scan the unit. The serial names the item, so the line
        // is found for you - no selecting a line first, no reading numbers off a screen and
        // matching them to boxes, which is what the web chooser makes you do.
        //
        // Scanned serials go straight into PickSlipLineSerials (the same table the web uses)
        // rather than into ViewState, so a dropped connection or a reload loses nothing and
        // the "already picked on another line" check works across devices.

        /// <summary>
        /// True when the scan was a serial number and this method dealt with it - including
        /// when it was rejected. False means "not a serial", so the normal scan flow runs.
        /// </summary>
        private bool HandleSerialScan(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 50) return false;
            string serial = raw.Trim().ToUpperInvariant();

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lot = db.LotTrackingMasters.FirstOrDefault(
                    x => x.CompanyID == CurrentUser.CoID && x.LotNumber == serial);
                if (lot == null) return false;

                long itemId = lot.ItemId ?? 0;
                if (!SerialGuard.IsSerialItem(db, CurrentUser.CoID, itemId)) return false;

                // From here the scan IS a serial: every exit returns true.
                if (SlipComplete) { SetFeedback(false, "&#9888; This slip is closed off."); return true; }

                if (string.IsNullOrEmpty(SelectedStore))
                {
                    LoadStoresModal();
                    pnlStoreModal.Visible = true;
                    return true;
                }

                // Pick-by-bin and the LPN box/unit flows book stock their own way; serials are
                // not wired into either, so say so rather than half-handle it.
                if (PickByBin || LPNMode == "box" || LPNMode == "unit")
                {
                    SetFeedback(false, "&#9888; Serial items are picked in the standard flow &mdash; "
                        + "box labels and pick-by-bin do not carry serial numbers.");
                    return true;
                }

                // The line this unit satisfies: first unpicked line for the item that still has
                // room. A 50-unit order is split into lines of 20/20/10, so they fill in order.
                var lines = db.PickSlipLines
                    .Where(l => l.PSID == PSID && l.CompanyID == CurrentUser.CoID
                             && l.LineType == 0 && l.SelectionId == itemId
                             && (l.PickComplete == null || l.PickComplete == false))
                    .OrderBy(l => l.LineID).ToList();
                if (lines.Count == 0)
                {
                    SetFeedback(false, $"&#9888; <strong>{HttpUtility.HtmlEncode(serial)}</strong> is "
                        + $"{HttpUtility.HtmlEncode(lot.ItemCode ?? "")}, which is not on this slip or is already picked.");
                    return true;
                }

                PickSlipLine target = null;
                List<string> already = null;
                foreach (var l in lines)
                {
                    var have = SerialPicking.GetLineSerials(db, CurrentUser.CoID, l.PSID, l.LineID);
                    if (have.Count < (l.Quantity ?? 0)) { target = l; already = have; break; }
                }
                if (target == null)
                {
                    SetFeedback(false, $"&#9888; Every line for {HttpUtility.HtmlEncode(lot.ItemCode ?? "")} "
                        + "already has its full count of serials.");
                    return true;
                }

                if (already.Contains(serial, StringComparer.OrdinalIgnoreCase))
                {
                    SetFeedback(false, $"&#9888; <strong>{HttpUtility.HtmlEncode(serial)}</strong> is already "
                        + "scanned on this line.");
                    return true;
                }

                // The same physical unit must never go out on two lines of one slip.
                var elsewhere = SerialPicking.PickedElsewhere(db, CurrentUser.CoID, PSID,
                                                              target.LineID, new List<string> { serial });
                if (elsewhere.Count > 0)
                {
                    SetFeedback(false, $"&#9888; <strong>{HttpUtility.HtmlEncode(serial)}</strong> is already "
                        + "picked on another line of this slip.");
                    return true;
                }

                int storeId = db.Stores
                    .Where(x => x.StoreCode == SelectedStore && x.CompanyID == CurrentUser.CoID)
                    .Select(x => x.StoreID).FirstOrDefault();

                // On hand HERE. A serial already issued nets to zero, so this also catches a
                // unit that has left the building.
                decimal onHand = db.ItemTransactions
                    .Where(t => t.CompanyID == CurrentUser.CoID && t.LotNumber == serial && t.ToID == storeId)
                    .Select(t => (decimal?)t.Qty).DefaultIfEmpty(0).Sum() ?? 0m;
                if (onHand <= 0)
                {
                    SetFeedback(false, $"&#9888; <strong>{HttpUtility.HtmlEncode(serial)}</strong> is not on hand "
                        + $"in {HttpUtility.HtmlEncode(SelectedStore)} &mdash; it may already be issued, or be in another store.");
                    return true;
                }

                var serials = new List<string>(already) { serial };
                SerialPicking.SetLineSerials(db, CurrentUser.CoID, PSID, target.LineID, serials, CurrentUser.RoleID);

                // ── Advisories. Neither blocks: the picker may have a good reason, but must
                //    not do it unknowingly. Same rule as the web.
                string warn = "";
                if (lot.UseByDate.HasValue && lot.UseByDate.Value.Date < DateTime.Today)
                    warn = " &#9888; this unit EXPIRED " + lot.UseByDate.Value.ToString("dd MMM yyyy") + ".";
                else
                {
                    // FEFO is advisory on a scanner - the picker takes what they physically
                    // reach - but an earlier-dated unit sitting in the same store is worth saying.
                    DateTime? mine = lot.UseByDate;
                    var earlier = db.LotTrackingMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.ItemId == itemId
                                 && x.UseByDate.HasValue
                                 && (!mine.HasValue || x.UseByDate.Value < mine.Value))
                        .Select(x => new { x.LotNumber, x.UseByDate })
                        .OrderBy(x => x.UseByDate).FirstOrDefault();
                    if (earlier != null)
                    {
                        decimal earlierOnHand = db.ItemTransactions
                            .Where(t => t.CompanyID == CurrentUser.CoID && t.LotNumber == earlier.LotNumber && t.ToID == storeId)
                            .Select(t => (decimal?)t.Qty).DefaultIfEmpty(0).Sum() ?? 0m;
                        if (earlierOnHand > 0)
                            warn = " &#9888; " + HttpUtility.HtmlEncode(earlier.LotNumber) + " expires earlier ("
                                 + earlier.UseByDate.Value.ToString("dd MMM yyyy") + ") and is still here.";
                    }
                }

                decimal need = target.Quantity ?? 0;
                if (serials.Count >= need)
                {
                    string err = TrySaveSerialPick(target.LineID, serials, SelectedStore);
                    if (err != null) { SetFeedback(false, "&#9888; " + err); return true; }

                    PickedLineIDs.Add(target.LineID);
                    MatchedLineID = 0;
                    SetFeedback(true, $"&#10003; <strong>{HttpUtility.HtmlEncode(target.ItemCode ?? "")}</strong> "
                        + $"complete &mdash; {serials.Count:0.##}/{need:0.##} serials picked." + warn);
                }
                else
                {
                    MatchedLineID = target.LineID;
                    SetFeedback(true, $"&#128290; <strong>{HttpUtility.HtmlEncode(serial)}</strong> &mdash; "
                        + $"{serials.Count}/{need:0.##} for {HttpUtility.HtmlEncode(target.ItemCode ?? "")}. "
                        + "Scan the next unit, or tap &#10004; to finish short." + warn);
                }
                return true;
            }
        }

        /// <summary>
        /// Books a serial line: one movement of -1 per serial, never one lumped row.
        /// Mirrors the web (PickingSlip.aspx.cs) exactly - including reversing what ACTUALLY
        /// moved rather than what is currently selected, so changing the selection cannot
        /// strand the units issued under the old one.
        /// </summary>
        private string TrySaveSerialPick(int lineId, List<string> serials, string storeCode)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.PickSlipLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return "Line not found.";
                if (serials == null || serials.Count == 0) return "No serials scanned for this line.";

                int storeId = db.Stores
                    .Where(x => x.StoreCode == storeCode && x.CompanyID == CurrentUser.CoID)
                    .Select(x => x.StoreID).FirstOrDefault();

                string revErr = ReverseSerialMovements(db, line, storeId);
                if (revErr != null) return revErr;

                // Outbound leaves at the pick store's running weighted average, and an OUT never
                // revalues a store - so the average is read ONCE and stamped on every unit. Do
                // NOT run ComputeMovement here: that is the re-blend an inbound does, and using
                // it on the way out would drift the store average on every pick.
                // (Same rule as the web and as the lumped mobile pick above it.)
                decimal unitCost = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, line.SelectionId, storeId);
                if (unitCost == 0m)
                {
                    var itmCost = db.ItemsMasters.FirstOrDefault(
                        x => x.CompanyID == CurrentUser.CoID && x.ID == line.SelectionId);
                    if (itmCost != null) unitCost = (decimal)(itmCost.AverageCost ?? 0m);
                }

                foreach (string serial in serials)
                {
                    db.ItemTransactions.Add(new ItemTransaction
                    {
                        CompanyID                 = CurrentUser.CoID,
                        DocumentID                = PSID,
                        TransactionType           = "PS",
                        ItemID                    = line.SelectionId,
                        ItemCode                  = line.ItemCode ?? "",
                        ItemDescription           = line.ItemDescription ?? "",
                        Unit                      = line.Unit,
                        FromID                    = 0,
                        ToID                      = storeId,
                        Qty                       = -1m,
                        DocumentType              = 10,
                        PriceExclusive            = unitCost,
                        TotalUnitPriceExclInclAdd = unitCost,
                        AdditionalCosts           = 0,
                        TotalLineValExcl          = -unitCost,
                        StoreAvgCost              = unitCost,
                        TransactionDate           = DateTime.Now,
                        ByRoleID                  = CurrentUser.RoleID,
                        TransactionReference      = PSIntNumber,
                        LotNumber                 = serial,
                        ExchRate                  = 1
                    });
                    // Saved per unit so the next one's running-average read sees this row -
                    // ComputeMovement queries the database and cannot see rows only Added.
                    db.SaveChanges();
                }

                line.PickQty       = serials.Count;
                line.PickComplete  = true;
                line.PickTime      = DateTime.Now;
                line.StoreCodeFrom = storeCode;
                // The line carries its serials in the child table; LotNumber holds the first so
                // anything still reading a single lot has something sensible to show.
                line.LotNumber        = serials[0];
                // No single row to point at - the line moved as many.
                line.ItemTransLineID  = null;
                db.SaveChanges();
            }

            RefreshPickStatus(PSID);
            return null;
        }

        /// <summary>
        /// Puts back every unit of this line's item still out on this slip, except the units
        /// belonging to the slip's OTHER lines. Netted, so a unit already returned sums to zero
        /// and is left alone - un-picking twice cannot return the same unit twice.
        /// Returns null on success.
        /// </summary>
        private string ReverseSerialMovements(SBMSEntities db, PickSlipLine line, int fallbackStoreId)
        {
            var otherLineUnits = SerialPicking.SerialsOnOtherLines(db, CurrentUser.CoID, line.PSID, line.LineID);

            long revItemId = line.SelectionId;
            var toReverse = db.ItemTransactions
                .Where(x => x.CompanyID == CurrentUser.CoID
                         && x.DocumentID == line.PSID
                         && x.ItemID == revItemId
                         && x.TransactionType == "PS"
                         && x.LotNumber != null && x.LotNumber != "")
                .GroupBy(x => x.LotNumber)
                .Select(g => new { Serial = g.Key, Net = g.Sum(x => x.Qty) ?? 0 })
                .Where(x => x.Net < 0)
                .Select(x => x.Serial)
                .ToList()
                .Where(x => !otherLineUnits.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (string serial in toReverse)
            {
                string thisSerial = serial;
                var outTrn = db.ItemTransactions
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.DocumentID == line.PSID
                             && x.ItemID == revItemId && x.LotNumber == thisSerial && x.Qty < 0)
                    .OrderByDescending(x => x.TrnID).FirstOrDefault();
                if (outTrn == null) continue;

                int backStore = Convert.ToInt32(outTrn.ToID ?? fallbackStoreId);
                decimal backVal;
                decimal backAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID, revItemId, backStore,
                                      1m, outTrn.TotalUnitPriceExclInclAdd ?? 0m, out backVal);
                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = outTrn.DocumentID,
                    TransactionType           = "PS",
                    ItemID                    = outTrn.ItemID,
                    ItemCode                  = outTrn.ItemCode ?? "",
                    ItemDescription           = outTrn.ItemDescription ?? "",
                    Unit                      = outTrn.Unit,
                    FromID                    = 0,
                    ToID                      = backStore,
                    Qty                       = 1m,
                    DocumentType              = 10,
                    PriceExclusive            = outTrn.PriceExclusive,
                    TotalUnitPriceExclInclAdd = outTrn.TotalUnitPriceExclInclAdd,
                    TotalLineValExcl          = outTrn.TotalUnitPriceExclInclAdd,
                    StoreAvgCost              = backAvg,
                    AdditionalCosts           = 0,
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = PSIntNumber + " Reversal",
                    LotNumber                 = thisSerial,
                    ExchRate                  = 1
                });
                db.SaveChanges();
            }
            return null;
        }

        /// <summary>Serials scanned so far for a line - for the card badge.</summary>
        private int SerialCount(SBMSEntities db, int lineId)
        {
            return SerialPicking.GetLineSerials(db, CurrentUser.CoID, PSID, lineId).Count;
        }

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
