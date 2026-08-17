using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    // Mobile SIMPLE auto-manufacture (backflush) — NO lot numbers.
    // Scan a Works Order -> loads the finished good + BOM -> pick a store -> enter qty ->
    // backflush the raw materials (DRAW) and produce the finished good (MANF), posted to Sage
    // exactly like the desktop Works Order Manufacture (DoItemAdjustment). Supports PART
    // MANUFACTURE: making less than the remaining qty leaves the balance open on the WO line.
    // Lot-tracked companies are blocked here and must use the full desktop version.
    public partial class ManufactureM : MobileBasePage
    {
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            StampActionToken(hfActionToken);   // fresh one-shot token per render (double-tap guard)
        }

        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private bool WOLoaded  { get { return ViewState["WOLoaded"] != null && (bool)ViewState["WOLoaded"]; } set { ViewState["WOLoaded"] = value; } }
        private long WOID       { get { return ViewState["WOID"] != null ? (long)ViewState["WOID"] : 0; } set { ViewState["WOID"] = value; } }
        private string WONum     { get { return ViewState["WONum"] as string; } set { ViewState["WONum"] = value; } }
        private int FGLineID    { get { return ViewState["FGLineID"] != null ? (int)ViewState["FGLineID"] : 0; } set { ViewState["FGLineID"] = value; } }
        private long OutItemId  { get { return ViewState["OutItemId"] != null ? (long)ViewState["OutItemId"] : 0; } set { ViewState["OutItemId"] = value; } }
        private string OutCode   { get { return ViewState["OutCode"] as string; } set { ViewState["OutCode"] = value; } }
        private string OutDescr  { get { return ViewState["OutDescr"] as string; } set { ViewState["OutDescr"] = value; } }
        private string OutUnit   { get { return ViewState["OutUnit"] as string; } set { ViewState["OutUnit"] = value; } }
        private decimal Remaining { get { return ViewState["Remaining"] != null ? (decimal)ViewState["Remaining"] : 0m; } set { ViewState["Remaining"] = value; } }
        private bool IsProcessing { get { return ViewState["IsProcessing"] != null && (bool)ViewState["IsProcessing"]; } set { ViewState["IsProcessing"] = value; } }

        private bool LotBlocked { get { return CurrentUser != null && CurrentUser.CompanyUseLotNumbers; } }

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

            // Barcode-off: capture the WO number by typing + Load button (scanner auto-submits on scan).
            bool scan = CurrentUser.MobileModule == true;
            txtScan.AutoPostBack = scan;
            lbtnLoadDoc.Visible = !scan;
            if (!scan) txtScan.Attributes["placeholder"] = "Enter WO number";

            if (!IsPostBack)
            {
                Session["ManfSession"] = new List<ManfDone>();
                if (!LotBlocked) { LoadStores(ddlStore); LoadStores(ddlToStore); }
                ResetCycle();
                RenderForm();
                BindDone();
            }
        }

        private void LoadStores(DropDownList ddl)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true
                        && x.StoreCode != "CoR" && x.StoreCode != "CoD")
                    .OrderBy(x => x.StoreCode).ToList();
                ddl.Items.Clear();
                ddl.Items.Add(new ListItem("- select store -", ""));
                foreach (var s in stores)
                {
                    string text = string.IsNullOrEmpty(s.StoreDescript) ? s.StoreCode : s.StoreCode + " - " + s.StoreDescript;
                    ddl.Items.Add(new ListItem(text, s.StoreCode));
                }
            }
        }

        // Scan box = Works Order only.
        protected void txtScan_TextChanged(object sender, EventArgs e)
        {
            string raw = (txtScan.Text ?? "").Trim();
            txtScan.Text = string.Empty;
            if (string.IsNullOrEmpty(raw) || LotBlocked) return;
            HandleWOScan(raw);
        }

        private void HandleWOScan(string code)
        {
            long won;
            string digits = new string(code.Where(char.IsDigit).ToArray());
            if (!long.TryParse(digits, out won) || won == 0)
            {
                SetFeedback(false, $"&#9888; '{code}' is not a valid Works Order number.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var wo = db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.WONum == won);
                if (wo == null) { SetFeedback(false, $"&#9888; Works Order {won} not found."); return; }

                // First open finished-good line (part-manufacture balances stay open here too).
                var fg = db.WorksOrderLines
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.WOID == wo.ID
                                && x.Active == true && x.Complete != true && (x.Quantity ?? 0) > 0)
                    .OrderBy(x => x.LineID).FirstOrDefault();
                if (fg == null) { SetFeedback(false, $"&#9888; WO {won} has no open line to manufacture."); return; }

                if (fg.IsLotTracked == true)
                {
                    SetFeedback(false, "&#9888; This item is lot-tracked — use the full Works Order Manufacture screen.");
                    return;
                }

                var master = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == fg.SelectionId);

                WOID = wo.ID;
                WONum = wo.WONum.ToString();
                FGLineID = fg.LineID;
                OutItemId = fg.SelectionId;
                OutCode = fg.ItemCode;
                OutDescr = fg.ItemDescription;
                OutUnit = master?.Unit ?? "Each";
                Remaining = fg.Quantity ?? 0;

                var rms = db.WorksOrderRMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedWOLineID == fg.LineID).ToList();
                var bom = rms.Select(r => new
                {
                    Code = r.ItemCode,
                    PerUnit = PerUnit(r).ToString("0.###") + " " + (r.Unit ?? "")
                }).ToList();
                rptBom.DataSource = bom;
                rptBom.DataBind();
            }

            WOLoaded = true;
            SetFeedback(true, $"&#10003; WO {WONum} &mdash; making <strong>{OutCode}</strong> ({Remaining:0.##} outstanding).");
            RenderForm();
        }

        // Make & keep: produce the finished good into the From store (multi-phase / stays in WIP).
        protected void lbtnMakeHere_Click(object sender, EventArgs e)
        {
            DoManufacture(ddlStore.SelectedValue, ddlStore.SelectedValue);
        }

        // Make & move: produce the finished good into a different destination store (e.g. Finished Goods).
        protected void lbtnMakeMove_Click(object sender, EventArgs e)
        {
            string from = ddlStore.SelectedValue, to = ddlToStore.SelectedValue;
            if (string.IsNullOrEmpty(to)) { SetFeedback(false, "&#9888; Select the destination store."); return; }
            if (to == from) { SetFeedback(false, "&#9888; Destination must differ from the From store."); return; }
            DoManufacture(from, to);
        }

        // Backflush RM from fromStore and produce the FG into targetStore. Making less than the
        // remaining leaves the balance open (part manufacture).
        private void DoManufacture(string fromStore, string targetStore)
        {
            if (!TryConsumeActionToken(hfActionToken))
            {
                SetFeedback(false, "&#9888; Already processed &mdash; that manufacture ran once.");
                return;
            }
            if (IsProcessing || LotBlocked) return;
            if (!WOLoaded) { SetFeedback(false, "&#9888; Scan a Works Order first."); return; }
            if (string.IsNullOrEmpty(fromStore)) { SetFeedback(false, "&#9888; Select the From store."); return; }
            if (!decimal.TryParse((txtQty.Text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal makeQty) || makeQty <= 0)
            {
                SetFeedback(false, "&#9888; Enter a valid quantity to make.");
                return;
            }
            if (makeQty > Remaining)
            {
                SetFeedback(false, $"&#9888; Only {Remaining:0.##} outstanding on this order.");
                return;
            }

            // Double-tap / second-device guard: 'Remaining' comes from ViewState and is stale across
            // postbacks, so a fast second tap would re-manufacture the order. Session postbacks
            // serialise, so re-read the live WO line: if it is already complete (or the live balance is
            // now below what was requested), bail instead of producing the order a second time.
            using (SBMSEntities dbChk = new SBMSEntities(Config.GetConnectionString()))
            {
                var lineChk = dbChk.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.LineID == FGLineID);
                if (lineChk == null || lineChk.Complete == true || makeQty > (lineChk.Quantity ?? 0))
                {
                    SetFeedback(false, "&#9888; This Works Order has already been manufactured (or its outstanding quantity has changed). Please re-scan.");
                    return;
                }
            }

            IsProcessing = true;
            // Declared out here so the result message below the context can still read them.
            decimal bomAddTotal = 0;
            string journalWarning = null;   // set if Sage refuses the add-cost journal
            try
            {
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    long fromStoreId = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == fromStore).Select(x => (long)x.StoreID).FirstOrDefault();
                    long targetStoreId = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == targetStore).Select(x => (long)x.StoreID).FirstOrDefault();
                    if (fromStoreId == 0 || targetStoreId == 0)
                    {
                        SetFeedback(false, "&#9888; Store not found - cannot manufacture.");
                        return;
                    }
                    var rms = db.WorksOrderRMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedWOLineID == FGLineID).ToList();

                    // ── Pre-validate every leg before anything is written ──
                    foreach (var r in rms)
                    {
                        if (PerUnit(r) * makeQty <= 0) continue;
                        bool exists = db.ItemsMasters.Any(x => x.CompanyID == CurrentUser.CoID && x.ID == r.SelectionId);
                        if (!exists)
                        {
                            SetFeedback(false, $"&#9888; Component {r.ItemCode} not found - nothing was manufactured.");
                            AlertHelper.ShowSweetAlert(this, $"Component {r.ItemCode} not found - nothing was manufactured.", "error");
                            return;
                        }
                    }
                    if (!db.ItemsMasters.Any(x => x.CompanyID == CurrentUser.CoID && x.ID == OutItemId))
                    {
                        SetFeedback(false, $"&#9888; Finished good {OutCode} not found - nothing was manufactured.");
                        return;
                    }

                    // Finished-good unit cost rolls up from the BOM (cost flows through).
                    //
                    // Same precedence the DRAW uses in PostAdjustment: the draw store's running
                    // weighted average wins, and the works-order line's snapshot is only a
                    // fallback for a component with no costed history in that store. Rolling up
                    // from the snapshot alone valued the finished good at what the components
                    // cost when the works order was RAISED, not what was actually consumed - so
                    // the same works order could cost differently on mobile and desktop.
                    decimal fgUnitCost = 0;
                    foreach (var r in rms)
                    {
                        decimal rmCost = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, r.SelectionId, fromStoreId);
                        if (rmCost <= 0) rmCost = r.UnitCost ?? 0;
                        fgUnitCost += PerUnit(r) * rmCost;
                    }

                    // BOM additional costs, captured PER UNIT on the BOM header, so they add
                    // straight onto the unit cost. Mirrors WorksOrdersManf - if that changes,
                    // change this too or the desktop and mobile will cost differently.
                    var bomHdrM = db.BOMHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.FGID == OutItemId);
                    long jDebitAcc = 0, jCreditAcc = 0;
                    if (bomHdrM != null)
                    {
                        decimal addPerUnit = (bomHdrM.AddCost01 ?? 0) + (bomHdrM.AddCost02 ?? 0) + (bomHdrM.AddCost03 ?? 0);
                        if (addPerUnit > 0)
                        {
                            fgUnitCost += addPerUnit;
                            bomAddTotal = addPerUnit * makeQty;
                        }
                    }

                    // ACCOUNTS MUST BE CONFIGURED BEFORE ANYTHING IS POSTED.
                    // The additional cost raises stock value in Sage and needs a matching credit.
                    // If we cannot raise that journal, stop here - nothing posted, nothing written.
                    if (bomAddTotal > 0 && CurrentUser.UATMode == false)
                    {
                        jDebitAcc = db.AccountsMasters
                            .Where(x => x.CompanyID == CurrentUser.CoID && x.AccountAddCostsContra == true)
                            .Select(x => x.AccountID ?? 0).FirstOrDefault();
                        jCreditAcc = bomHdrM?.AddCostAccountID ?? 0;

                        if (jDebitAcc > 0 && jDebitAcc == jCreditAcc)
                        {
                            SetFeedback(false, "&#9888; This BOM posts its additional costs to the same "
                                + "account as the Stock Adjustment Account, so nothing would reach Sage. "
                                + "Nothing was manufactured. Edit the BOM and choose a different account.");
                            return;
                        }
                        if (jDebitAcc <= 0 || jCreditAcc <= 0)
                        {
                            SetFeedback(false, "&#9888; This BOM carries additional costs of "
                                + bomAddTotal.ToString("N2") + ", but the accounts to post them to are not set. "
                                + "Nothing was manufactured. Ask an administrator to set the Stock Adjustment "
                                + "Account and the BOM's additional-costs account.");
                            return;
                        }
                    }

                    // ── All-or-nothing: local rows in ONE transaction; Sage posts are
                    // tracked so a failure can post compensating reversals. Nothing is
                    // left half-done on either side.
                    var sagePosted = new List<SagePostedAdjustment>();
                    using (var tx = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Backflush each raw material (DRAW / negative) from the From store.
                            foreach (var r in rms)
                            {
                                decimal useQty = PerUnit(r) * makeQty;
                                if (useQty <= 0) continue;
                                string res = PostAdjustment(db, r.SelectionId, r.ItemCode, r.ItemDescription, r.Unit, fromStoreId, useQty * -1m, r.UnitCost ?? 0, sagePosted);
                                if (res != "OK") throw new ApplicationException("Manufacture failed on " + r.ItemCode + ": " + res);
                            }

                            // Produce the finished good (MANF / positive) into the target store.
                            string fres = PostAdjustment(db, OutItemId, OutCode, OutDescr, OutUnit, targetStoreId, makeQty, fgUnitCost, sagePosted);
                            if (fres != "OK") throw new ApplicationException("Manufacture failed on " + OutCode + ": " + fres);

                            // Clear the additional cost off Sage's stock adjustment account:
                            // debit it, credit the account chosen on the BOM. Deliberately NOT
                            // thrown - the stock is correctly made and posted, and unwinding all
                            // of that over a journal would be worse. Mirrors the desktop: warn,
                            // log, and let it be posted by hand.
                            if (bomAddTotal > 0 && CurrentUser.UATMode == false)
                            {
                                string jRes = SendAddCostJournal(jDebitAcc, jCreditAcc, bomAddTotal,
                                    "WO" + WONum, "BOM additional costs - WO" + WONum);
                                if (jRes != "Success")
                                {
                                    journalWarning = jRes;
                                    try { new ApiUrlCall().LogErrorToFile(
                                        "BOM ADD-COST JOURNAL FAILED (mobile) - WO" + WONum +
                                        " amount " + bomAddTotal + " Dr " + jDebitAcc + " Cr " + jCreditAcc +
                                        " - " + jRes); } catch { }
                                }
                            }

                            // Update the WO line: reduce the outstanding balance; close it when done.
                            var line = db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.LineID == FGLineID);
                            if (line != null)
                            {
                                line.OrderedQty = line.OrderedQty ?? Remaining;
                                line.Quantity = (line.Quantity ?? 0) - makeQty;   // decrement the LIVE balance, not the (stale-across-postbacks) ViewState Remaining
                                if ((line.Quantity ?? 0) <= 0)
                                {
                                    line.Quantity = 0;
                                    line.Complete = true;
                                }
                                db.SaveChanges();
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            string compErr = CompensateSage(sagePosted);
                            string msg = ex.Message + (compErr == null
                                ? " Nothing was manufactured - no stock moved."
                                : " ATTENTION: " + compErr);
                            new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} ManufactureM rollback - {msg}");
                            SetFeedback(false, "&#9888; " + msg);
                            AlertHelper.ShowSweetAlert(this, msg, "error");
                            return;
                        }
                    }
                }

                RecordDone(OutCode, makeQty, targetStore);
                decimal newRemaining = Remaining - makeQty;
                string toast = $"&#10003; Made {makeQty:0.##} {OutCode}";
                bool complete = newRemaining <= 0;
                ResetCycle();
                if (journalWarning != null)
                {
                    // Stock is made and posted correctly; only the additional-costs journal failed.
                    SetFeedback(false, "&#9888; Made " + makeQty.ToString("0.##") + " " + OutCode
                        + ", but the additional-costs journal to Sage failed: " + journalWarning
                        + " — it must be posted manually.");
                    AlertHelper.ShowSweetAlert(this,
                        "The stock was manufactured and posted, but the additional-costs journal to Sage "
                        + "failed: " + journalWarning + "\\n\\nPost it manually: Debit the stock adjustment "
                        + "account, Credit the BOM's additional costs account, "
                        + bomAddTotal.ToString("N2") + ".", "warning");
                }
                else
                SetFeedback(true, complete
                    ? $"&#10003; Made {makeQty:0.##} {OutCode} into {targetStore} — order complete."
                    : $"&#10003; Made {makeQty:0.##} {OutCode} into {targetStore} — {newRemaining:0.##} still outstanding. Scan the WO again for the balance.");
                RenderForm();
                BindDone();
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "manfToast", $"showToast('{JsEscape(toast)}');", true);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // A Sage adjustment that has already been posted this manufacture - kept so a
        // later leg's failure can post the compensating reversal (Sage has no rollback).
        private class SagePostedAdjustment
        {
            public long ItemID;
            public string ItemCode;
            public decimal Qty;
            public decimal AvCost;
        }

        // Reverses any Sage adjustments already posted by a failed manufacture.
        // Returns null when clean (or nothing to reverse); otherwise a message
        // describing what must be corrected in Sage manually.
        private string CompensateSage(List<SagePostedAdjustment> posted)
        {
            if (posted == null || posted.Count == 0) return null;
            var failures = new List<string>();
            foreach (var p in posted)
            {
                try
                {
                    var iAdj = new ItemAdjustment
                    {
                        Date = DateTime.Now,
                        ItemID = p.ItemID,
                        AverageCost = p.AvCost,
                        Quantity = p.Qty * -1m,
                        Reason = "Reversal: WO" + WONum + " manufacture failed - " + DateTime.Now.ToString(),
                        Created = DateTime.Now
                    };
                    string res = SendItemAdjustment(JsonConvert.SerializeObject(iAdj, Formatting.Indented));
                    if (res != "Success") failures.Add($"{p.ItemCode} qty {p.Qty * -1m:0.##} ({res})");
                }
                catch (Exception ex)
                {
                    failures.Add($"{p.ItemCode} qty {p.Qty * -1m:0.##} ({ex.Message})");
                }
            }
            return failures.Count == 0
                ? null
                : "Sage reversal failed for: " + string.Join("; ", failures) +
                  ". Correct these item adjustments in Sage manually.";
        }

        // Mirrors the desktop DoItemAdjustment: weighted-average cost, Sage post (UATMode-gated),
        // then the local MANF/DRAW ItemTransaction. Sage adjusts item-level QOH/cost; the store
        // is a local concept only. Successful Sage posts are recorded in sagePosted so the
        // caller can compensate if a later leg fails.
        private string PostAdjustment(SBMSEntities db, long itmid, string code, string descr, string unit, long storeId, decimal qty, decimal unitcost, List<SagePostedAdjustment> sagePosted)
        {
            try
            {
                ApiUrlCall api = new ApiUrlCall();
                bool refreshed = api.LoadOneItemNA(itmid, CurrentUser);
                var itm = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid);
                if (itm == null) return "Item not found";

                // GUARD B - never compute an average cost from stale data (mirrors the desktop).
                // The average below is derived from QuantityOnHand / AverageCost and pushed to Sage,
                // which SETS the average. A failed refresh means those inputs are old.
                if (!refreshed)
                    return $"Could not refresh {code} from Sage - nothing posted. Check the connection and try again.";

                if (unitcost == 0) unitcost = itm.AverageCost ?? 0;

                // Draws leave at the draw store's running weighted average (outs never
                // revalue a store). The same number then drives the Sage adjustment and
                // the local row below — one cost, every side (mirrors desktop DoItemAdjustment).
                if (qty < 0)
                {
                    decimal drawAvg = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, itmid, storeId);
                    if (drawAvg > 0) unitcost = drawAvg;
                }

                decimal currentQOH = itm.QuantityOnHand ?? 0;
                decimal sageAvCost = itm.AverageCost ?? 0;
                decimal thisValue = qty * unitcost;
                decimal newAvCost;

                if (currentQOH < 0)
                {
                    newAvCost = unitcost;
                }
                else
                {
                    decimal sageValue = currentQOH > 0 ? currentQOH * sageAvCost : 0;
                    if (sageValue > 0)
                    {
                        decimal newQty = currentQOH + qty;
                        newAvCost = newQty > 0 ? (thisValue + sageValue) / newQty : unitcost;
                    }
                    else
                    {
                        newAvCost = unitcost;
                    }
                }

                if (CurrentUser.UATMode == false)
                {
                    var iAdj = new ItemAdjustment
                    {
                        Date = DateTime.Now,
                        ItemID = itmid,
                        AverageCost = newAvCost,
                        Quantity = qty,
                        Reason = (qty > 0 ? "Manf" : "Draw") + ": WO" + WONum + " - " + DateTime.Now.ToString(),
                        Created = DateTime.Now
                    };
                    string res = SendItemAdjustment(JsonConvert.SerializeObject(iAdj, Formatting.Indented));
                    if (res != "Success") return res;
                    sagePosted?.Add(new SagePostedAdjustment
                    {
                        ItemID = itmid, ItemCode = code, Qty = qty, AvCost = newAvCost
                    });
                }

                // Stamp the store's running average after this movement: a DRAW leaves it
                // unchanged (unitcost already IS the store average); MANF re-blends it.
                // Computed BEFORE the Add so the new row is not yet in the ledger.
                decimal storeAvgAfter;
                if (qty < 0)
                {
                    storeAvgAfter = unitcost;
                }
                else
                {
                    decimal manfVal;
                    storeAvgAfter = StoreCosting.ComputeMovement(db, CurrentUser.CoID, itmid, storeId, qty, unitcost * qty, out manfVal);
                }

                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID = CurrentUser.CoID,
                    DocumentID = 0,
                    TransactionType = qty > 0 ? "MANF" : "DRAW",
                    ItemID = itmid,
                    ItemCode = code,
                    ItemDescription = descr,
                    Unit = unit,
                    FromID = 0,
                    ToID = (int)storeId,
                    Qty = qty,
                    DocumentType = 1,
                    TransactionDate = DateTime.Now,
                    ByRoleID = CurrentUser.RoleID,
                    PriceExclusive = unitcost,
                    AdditionalCosts = 0,
                    TotalUnitPriceExclInclAdd = unitcost,
                    TotalLineValExcl = unitcost * qty,
                    StoreAvgCost = storeAvgAfter,
                    ExchRate = 1,
                    TransactionReference = (qty > 0 ? "MANF" : "DRAW") + ": WO" + WONum
                });
                db.SaveChanges();
                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string SendItemAdjustment(string item)
        {
            try
            {
                ApiUrlCall api = new ApiUrlCall();
                JObject parsed = api.APIPostDocumentNA("ItemAdjustment", item, CurrentUser);
                if (parsed == null) return "Null response from API";
                if (parsed["error"] != null)
                {
                    JObject err = (JObject)parsed["error"];
                    string msg = err["message"]?.ToString();
                    string ex = err["exception"]?.ToString();
                    string reason = err["reason"]?.ToString();
                    return !string.IsNullOrEmpty(msg) ? msg : !string.IsNullOrEmpty(ex) ? ex : !string.IsNullOrEmpty(reason) ? reason : "Unknown API error";
                }
                return "Success";
            }
            catch (Exception ex)
            {
                return "SendItemAdjustment exception: " + ex.Message;
            }
        }

        /// <summary>
        /// Posts the BOM additional-costs journal to Sage. Returns "Success" or the reason.
        /// Mirrors WorksOrdersManf.SendAddCostJournal - keep the two in step.
        ///
        /// Producing the finished good raises stock value by the additional cost and credits
        /// Sage's own stock adjustment account. Nothing debits it, so this journal clears it:
        /// DEBIT the stock adjustment account, CREDIT the account chosen on the BOM.
        /// JournalEntry/Save carries both sides in one call, so it balances by construction.
        /// No VAT - internal absorption of cost already expensed elsewhere, not a supply.
        /// </summary>
        private string SendAddCostJournal(long debitAccountId, long creditAccountId,
                                          decimal amount, string reference, string description)
        {
            if (amount <= 0) return "Success";
            if (debitAccountId <= 0 || creditAccountId <= 0) return "No accounts configured";

            try
            {
                // Sage rejects a journal with no tax type ("Tax Type is Required"). This entry
                // carries no VAT, so use the company's zero-rated type, falling back to the
                // default tax type on the stock adjustment account.
                long taxTypeId = 0;
                using (SBMSEntities dbT = new SBMSEntities(Config.GetConnectionString()))
                {
                    taxTypeId = dbT.TaxTypesMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && (x.TaxPerc ?? 0) == 0
                                    && (x.TaxTypeID ?? 0) > 0)
                        .OrderBy(x => x.TaxTypeID)
                        .Select(x => x.TaxTypeID ?? 0).FirstOrDefault();
                    if (taxTypeId <= 0)
                    {
                        taxTypeId = dbT.AccountsMasters
                            .Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == debitAccountId)
                            .Select(x => x.AcctDefTaxTypeID ?? 0).FirstOrDefault();
                    }
                }
                if (taxTypeId <= 0) return "No zero-rated tax type found for this company";

                var journal = new
                {
                    Date = DateTime.Now,
                    Effect = 1,                            // 1 = Debit (AccountId is debited)
                    AccountId = debitAccountId,            // stock adjustment account
                    ContraAccountId = creditAccountId,     // account chosen on the BOM
                    TaxTypeId = taxTypeId,                 // required by Sage, zero-rated
                    Reference = reference,
                    Description = description,
                    Exclusive = amount,
                    Tax = 0m,
                    Total = amount,
                    Debit = amount,
                    Credit = 0m
                };

                JObject parsed = new ApiUrlCall().APIPostDocumentNA(
                    "JournalEntry", JsonConvert.SerializeObject(journal, Formatting.Indented), CurrentUser);

                if (parsed == null) return "Null response from API";
                if (parsed["error"] != null)
                {
                    JObject err = (JObject)parsed["error"];
                    return err["message"]?.ToString()
                        ?? err["reason"]?.ToString()
                        ?? "Unknown API error posting journal";
                }
                // Sage can answer 200 with a validation payload and save nothing. A real save
                // always comes back with the new journal Id, so treat a missing Id as a failure.
                if (parsed["ID"] == null && parsed["Id"] == null)
                    return "Sage did not return a journal Id: " + parsed.ToString(Formatting.None);
                return "Success";
            }
            catch (Exception ex)
            {
                return "SendAddCostJournal exception: " + ex.Message;
            }
        }

        private static decimal PerUnit(WorksOrderRMLine r)
        {
            decimal fgQty = r.LinkedFGQty ?? 0;
            decimal q = r.Quantity ?? 0;
            return fgQty > 0 ? q / fgQty : q;
        }

        // ── Render ──
        private void RenderForm()
        {
            if (LotBlocked)
            {
                pnlScan.Visible = false;
                pnlWO.Visible = false;
                pnlMake.Visible = false;
                lbtnRestart.Visible = false;
                lblPrompt.Text = "";
                SetFeedback(false, "&#9888; This company uses lot numbers. Please use the full Works Order Manufacture screen.");
                return;
            }

            pnlWO.Visible = WOLoaded;
            pnlMake.Visible = WOLoaded;
            lblPrompt.Text = WOLoaded ? "Choose store and quantity, then Manufacture"
                : (CurrentUser.MobileModule == true ? "Scan the Works Order barcode" : "Enter the Works Order number");
            if (WOLoaded)
            {
                lblWONum.Text = WONum;
                lblOutItem.Text = OutCode;
                lblOutDescr.Text = OutDescr;
                lblOutUnit.Text = OutUnit;
                lblRemaining.Text = Remaining.ToString("0.##") + " " + (OutUnit ?? "");
                if (string.IsNullOrEmpty(txtQty.Text)) txtQty.Text = Remaining.ToString("0.##", CultureInfo.InvariantCulture);
            }
            lbtnRestart.Visible = WOLoaded;
        }

        private void ResetCycle()
        {
            WOLoaded = false;
            WOID = 0; WONum = null; FGLineID = 0;
            OutItemId = 0; OutCode = null; OutDescr = null; OutUnit = null; Remaining = 0m;
            txtQty.Text = string.Empty;
            if (ddlStore.Items.Count > 0) ddlStore.SelectedIndex = 0;
            if (ddlToStore.Items.Count > 0) ddlToStore.SelectedIndex = 0;
            rptBom.DataSource = null; rptBom.DataBind();
        }

        protected void lbtnRestart_Click(object sender, EventArgs e) { ResetCycle(); ClearFeedback(); RenderForm(); }
        protected void lbtnClearScan_Click(object sender, EventArgs e) { txtScan.Text = string.Empty; ClearFeedback(); }

        // Barcode-off: Load button submits the typed WO number (same path as a scan).
        protected void lbtnLoadDoc_Click(object sender, EventArgs e)
        {
            string raw = (txtScan.Text ?? "").Trim();
            txtScan.Text = string.Empty;
            if (string.IsNullOrEmpty(raw) || LotBlocked) return;
            HandleWOScan(raw);
        }

        // ── "Made this session" list ──
        [Serializable]
        public class ManfDone
        {
            public string ItemCode { get; set; }
            public decimal Qty { get; set; }
            public string Store { get; set; }
            public string TimeText { get; set; }
        }

        private List<ManfDone> DoneList
        {
            get
            {
                var l = Session["ManfSession"] as List<ManfDone>;
                if (l == null) { l = new List<ManfDone>(); Session["ManfSession"] = l; }
                return l;
            }
        }

        private void RecordDone(string code, decimal qty, string store)
        {
            DoneList.Insert(0, new ManfDone { ItemCode = code, Qty = qty, Store = store, TimeText = DateTime.Now.ToString("HH:mm") });
        }

        private void BindDone()
        {
            var l = DoneList;
            pnlDone.Visible = l.Count > 0;
            lblDoneCount.Text = l.Count.ToString();
            rptDone.DataSource = l;
            rptDone.DataBind();
        }

        private static string JsEscape(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");

        // ── Nav ──
        protected void lbtnTopBack_Click(object sender, EventArgs e) { GoDashboard(); }
        protected void lbtnTopHome_Click(object sender, EventArgs e) { GoDashboard(); }
        private void GoDashboard()
        {
            Response.Redirect(CurrentUser != null ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD : "~/SBMSMobile/DashboardM.aspx", false);
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

        private void SetFeedback(bool ok, string html) { lblFeedback.Text = html; lblFeedback.CssClass = ok ? "mob-feedback found" : "mob-feedback notfound"; }
        private void ClearFeedback() { lblFeedback.Text = string.Empty; lblFeedback.CssClass = "mob-feedback"; }
    }
}
