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
    public partial class ManufactureM : BasePage
    {
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

            IsProcessing = true;
            try
            {
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    long fromStoreId = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == fromStore).Select(x => (long)x.StoreID).FirstOrDefault();
                    long targetStoreId = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == targetStore).Select(x => (long)x.StoreID).FirstOrDefault();
                    var rms = db.WorksOrderRMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedWOLineID == FGLineID).ToList();

                    // Finished-good unit cost rolls up from the BOM (cost flows through).
                    decimal fgUnitCost = rms.Sum(r => PerUnit(r) * (r.UnitCost ?? 0));

                    // Backflush each raw material (DRAW / negative) from the From store.
                    foreach (var r in rms)
                    {
                        decimal useQty = PerUnit(r) * makeQty;
                        if (useQty <= 0) continue;
                        string res = PostAdjustment(db, r.SelectionId, r.ItemCode, r.ItemDescription, r.Unit, fromStoreId, useQty * -1m, r.UnitCost ?? 0);
                        if (res != "OK") { SetFeedback(false, "&#9888; Manufacture failed on " + r.ItemCode + ": " + res); return; }
                    }

                    // Produce the finished good (MANF / positive) into the target store.
                    string fres = PostAdjustment(db, OutItemId, OutCode, OutDescr, OutUnit, targetStoreId, makeQty, fgUnitCost);
                    if (fres != "OK") { SetFeedback(false, "&#9888; Manufacture failed on " + OutCode + ": " + fres); return; }

                    // Update the WO line: reduce the outstanding balance; close it when done.
                    var line = db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.LineID == FGLineID);
                    if (line != null)
                    {
                        line.OrderedQty = line.OrderedQty ?? Remaining;
                        line.Quantity = Remaining - makeQty;
                        if ((line.Quantity ?? 0) <= 0)
                        {
                            line.Quantity = 0;
                            line.Complete = true;
                        }
                        db.SaveChanges();
                    }
                }

                RecordDone(OutCode, makeQty, targetStore);
                decimal newRemaining = Remaining - makeQty;
                string toast = $"&#10003; Made {makeQty:0.##} {OutCode}";
                bool complete = newRemaining <= 0;
                ResetCycle();
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

        // Mirrors the desktop DoItemAdjustment: weighted-average cost, Sage post (UATMode-gated),
        // then the local MANF/DRAW ItemTransaction. Sage adjusts item-level QOH/cost; the store
        // is a local concept only.
        private string PostAdjustment(SBMSEntities db, long itmid, string code, string descr, string unit, long storeId, decimal qty, decimal unitcost)
        {
            try
            {
                ApiUrlCall api = new ApiUrlCall();
                api.LoadOneItemNA(itmid, CurrentUser);
                var itm = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid);
                if (itm == null) return "Item not found";

                if (unitcost == 0) unitcost = itm.AverageCost ?? 0;

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
            lblPrompt.Text = WOLoaded ? "Choose store and quantity, then Manufacture" : "Scan the Works Order barcode";
            if (WOLoaded)
            {
                lblWONum.Text = WONum;
                lblOutItem.Text = OutCode;
                lblOutDescr.Text = OutDescr;
                lblOutUnit.Text = OutUnit;
                lblRemaining.Text = Remaining.ToString("0.##") + " " + (OutUnit ?? "");
                if (string.IsNullOrEmpty(txtQty.Text)) txtQty.Text = Remaining.ToString("0.##");
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
