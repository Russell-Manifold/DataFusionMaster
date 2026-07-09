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
    // Mobile STOCK MOVE — bulk, multi-item transfer via a GIT (goods-in-transit) store.
    // The transfer is created up-front on the desktop (Transfer.aspx) with its lines and planned
    // TrfOutQty. Mobile just executes it in two role-stamped legs, scanning the transfer barcode
    // (TransferID) to load it:
    //   • SEND OUT  : source store -> GIT     (status -> In Transit)
    //   • RECEIVE IN: GIT -> destination store (per line, with count checks; completes when all in)
    // Store moves are local-only in SBMS (Sage tracks item-level QOH), so each leg is two local
    // TRF ItemTransactions. The GIT store is a reserved store (code "GIT"), auto-created if missing.
    public partial class StockMoveM : BasePage
    {
        private const string GitCode = "GIT";

        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private string Mode { get { return ViewState["Mode"] as string ?? "out"; } set { ViewState["Mode"] = value; } }
        private int TrfID { get { return ViewState["TrfID"] != null ? (int)ViewState["TrfID"] : 0; } set { ViewState["TrfID"] = value; } }
        private string TrfNum { get { return ViewState["TrfNum"] as string; } set { ViewState["TrfNum"] = value; } }
        private string FromStore { get { return ViewState["FromStore"] as string; } set { ViewState["FromStore"] = value; } }
        private string ToStore { get { return ViewState["ToStore"] as string; } set { ViewState["ToStore"] = value; } }
        private long FromStoreId { get { return ViewState["FromStoreId"] != null ? (long)ViewState["FromStoreId"] : 0; } set { ViewState["FromStoreId"] = value; } }
        private long ToStoreId { get { return ViewState["ToStoreId"] != null ? (long)ViewState["ToStoreId"] : 0; } set { ViewState["ToStoreId"] = value; } }
        private bool IsProcessing { get { return ViewState["IsProcessing"] != null && (bool)ViewState["IsProcessing"]; } set { ViewState["IsProcessing"] = value; } }

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
                ResetCycle();
                RenderMode();
            }
        }

        protected void lbtnModeOut_Click(object sender, EventArgs e) { Mode = "out"; ResetCycle(); RenderMode(); }
        protected void lbtnModeIn_Click(object sender, EventArgs e) { Mode = "in"; ResetCycle(); RenderMode(); }

        // Scan the transfer barcode (TransferID).
        protected void txtScan_TextChanged(object sender, EventArgs e)
        {
            string raw = (txtScan.Text ?? "").Trim();
            txtScan.Text = string.Empty;
            if (string.IsNullOrEmpty(raw)) return;

            long tnum;
            string digits = new string(raw.Where(char.IsDigit).ToArray());
            if (!long.TryParse(digits, out tnum) || tnum == 0)
            {
                SetFeedback(false, $"&#9888; '{raw}' is not a valid Transfer number.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var hdr = db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TransferID == tnum);
                if (hdr == null) { SetFeedback(false, $"&#9888; Transfer {tnum} not found."); return; }

                bool inTransit = hdr.TrfStatus == "In Transit";
                bool done = hdr.TrfComplete || hdr.TrfStatus == "Complete" || hdr.TrfStatus == "Deleted";
                if (done) { SetFeedback(false, $"&#9888; Transfer {tnum} is {hdr.TrfStatus}."); return; }

                if (Mode == "out" && inTransit)
                {
                    SetFeedback(false, $"&#9888; Transfer {tnum} is already In Transit — switch to Receive."); return;
                }
                if (Mode == "in" && !inTransit)
                {
                    SetFeedback(false, $"&#9888; Transfer {tnum} has not been sent out yet — switch to Send Out."); return;
                }

                TrfID = hdr.TrfID;
                TrfNum = (hdr.TransferID ?? 0).ToString().PadLeft(8, '0');
                FromStoreId = hdr.TrfFromID ?? 0;
                ToStoreId = hdr.TrfToID ?? 0;
                FromStore = StoreCode(db, FromStoreId);
                ToStore = StoreCode(db, ToStoreId);
            }

            BindLines();
            SetFeedback(true, $"&#10003; Transfer {TrfNum}: <strong>{FromStore} &#8594; {ToStore}</strong>.");
            RenderMode();
        }

        // ── SEND OUT: move every line source -> GIT ──
        protected void lbtnSendOut_Click(object sender, EventArgs e)
        {
            if (IsProcessing || TrfID == 0 || Mode != "out") return;
            IsProcessing = true;
            try
            {
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var hdr = db.ItemTransferHeaders.FirstOrDefault(x => x.TrfID == TrfID && x.CompanyID == CurrentUser.CoID);
                    if (hdr == null || hdr.TrfStatus == "In Transit" || hdr.TrfComplete)
                    {
                        SetFeedback(false, "&#9888; Transfer is no longer open to send."); BindLines(); RenderMode(); return;
                    }
                    long gitId = GitStoreId(db);
                    var lines = db.ItemTransferLines.Where(x => x.TrfID == TrfID && x.CompanyID == CurrentUser.CoID && (x.TrfOutQty ?? 0) > 0).ToList();

                    string trfref = "TRF" + TrfNum;
                    foreach (var line in lines)
                        MoveStock(db, line, FromStoreId, gitId, line.TrfOutQty ?? 0, trfref);

                    hdr.TrfStatus = "In Transit";
                    hdr.TrfStarted = true;
                    hdr.TrfActive = true;
                    hdr.TrfBy = CurrentUser.UserName;
                    db.SaveChanges();
                }

                string num = TrfNum;
                ResetCycle();
                SetFeedback(true, $"&#10003; Transfer {num} sent to transit. Receive it at the destination.");
                RenderMode();
            }
            finally { IsProcessing = false; }
        }

        // ── RECEIVE IN: per line, GIT -> destination ──
        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "receive" || IsProcessing || Mode != "in") return;
            IsProcessing = true;
            try
            {
                long trfLid = Convert.ToInt64(e.CommandArgument);
                TextBox txtRecv = (e.Item.FindControl("txtRecv") as TextBox);
                if (!decimal.TryParse((txtRecv?.Text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal recvQty) || recvQty <= 0)
                {
                    SetFeedback(false, "&#9888; Enter a valid received quantity."); return;
                }

                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var line = db.ItemTransferLines.FirstOrDefault(x => x.TrfLID == trfLid && x.CompanyID == CurrentUser.CoID);
                    if (line == null) { SetFeedback(false, "&#9888; Line not found."); BindLines(); RenderMode(); return; }

                    decimal sent = line.TrfOutQty ?? 0;
                    decimal already = line.TrfInQty ?? 0;
                    decimal remaining = sent - already;
                    if (recvQty > remaining)
                    {
                        SetFeedback(false, $"&#9888; Only {remaining:0.##} of {line.ItemCode} still in transit.");
                        BindLines(); RenderMode(); return;
                    }

                    long gitId = GitStoreId(db);
                    MoveStock(db, line, gitId, ToStoreId, recvQty, "TRF" + TrfNum);
                    line.TrfInQty = already + recvQty;
                    db.SaveChanges();

                    // Complete the transfer once every line is fully received.
                    var open = db.ItemTransferLines.Any(x => x.TrfID == TrfID && x.CompanyID == CurrentUser.CoID
                                                             && (x.TrfInQty ?? 0) < (x.TrfOutQty ?? 0));
                    if (!open)
                    {
                        var hdr = db.ItemTransferHeaders.FirstOrDefault(x => x.TrfID == TrfID && x.CompanyID == CurrentUser.CoID);
                        if (hdr != null)
                        {
                            hdr.TrfStatus = "Complete";
                            hdr.TrfComplete = true;
                            hdr.TrfActive = false;
                            hdr.TrfCompleteDate = DateTime.Now;
                            db.SaveChanges();
                        }
                    }
                }

                BindLines();
                SetFeedback(true, $"&#10003; Received {recvQty:0.##} into {ToStore}.");
                RenderMode();
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "smToast",
                    $"showToast('{JsEscape($"&#10003; Received {recvQty:0.##}")}');", true);
            }
            finally { IsProcessing = false; }
        }

        private void BindLines()
        {
            if (TrfID == 0) { pnlLines.Visible = false; return; }
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = db.ItemTransferLines.Where(x => x.TrfID == TrfID && x.CompanyID == CurrentUser.CoID && (x.TrfOutQty ?? 0) > 0)
                    .OrderBy(x => x.TrfLID).ToList();
                var disp = lines.Select(l => new
                {
                    l.TrfLID,
                    l.ItemCode,
                    l.ItemDescription,
                    Sent = (l.TrfOutQty ?? 0).ToString("0.##") + " " + (l.Unit ?? ""),
                    Remaining = ((l.TrfOutQty ?? 0) - (l.TrfInQty ?? 0)),
                    RemainingText = ((l.TrfOutQty ?? 0) - (l.TrfInQty ?? 0)).ToString("0.##")
                }).ToList();
                rptLines.DataSource = disp;
                rptLines.DataBind();
                pnlLines.Visible = disp.Count > 0;
            }
        }

        // ── Local TRF move: qty out of fromStore, into toStore (two ItemTransactions). ──
        private void MoveStock(SBMSEntities db, ItemTransferLine line, long fromStoreId, long toStoreId, decimal qty, string reference)
        {
            if (qty <= 0) return;
            long itemId = line.ItemSelectionId ?? 0;

            // OUT leg leaves at the source store's running weighted average (outs never revalue
            // a store); the IN leg arrives at that same cost and re-blends the destination's
            // running average. No freight on mobile moves.
            decimal srcAvg = StoreCosting.GetStoreAvgCost(db, CurrentUser.CoID, itemId, fromStoreId);
            decimal inLineVal;
            decimal destAvg = StoreCosting.ComputeMovement(db, CurrentUser.CoID, itemId, toStoreId, qty, srcAvg * qty, out inLineVal);

            db.ItemTransactions.Add(new ItemTransaction
            {
                CompanyID = CurrentUser.CoID,
                DocumentID = 0,
                TransactionType = "TRF",
                ItemID = itemId,
                ItemCode = line.ItemCode,
                ItemDescription = line.ItemDescription,
                LotNumber = line.LotNumber,
                Unit = line.Unit,
                FromID = toStoreId,
                ToID = fromStoreId,
                Qty = qty * -1m,               // out of source
                DocumentType = 4,
                TransactionDate = DateTime.Now,
                ByRoleID = CurrentUser.RoleID,
                PriceExclusive = srcAvg,
                AdditionalCosts = 0,
                TotalUnitPriceExclInclAdd = srcAvg,
                TotalLineValExcl = srcAvg * (qty * -1m),
                StoreAvgCost = srcAvg,
                ExchRate = 1,
                TransactionReference = line.ItemCode + " " + reference + " out"
            });

            db.ItemTransactions.Add(new ItemTransaction
            {
                CompanyID = CurrentUser.CoID,
                DocumentID = 0,
                TransactionType = "TRF",
                ItemID = itemId,
                ItemCode = line.ItemCode,
                ItemDescription = line.ItemDescription,
                LotNumber = line.LotNumber,
                Unit = line.Unit,
                FromID = fromStoreId,
                ToID = toStoreId,
                Qty = qty,                     // into destination
                DocumentType = 4,
                TransactionDate = DateTime.Now,
                ByRoleID = CurrentUser.RoleID,
                PriceExclusive = srcAvg,
                AdditionalCosts = 0,
                TotalUnitPriceExclInclAdd = srcAvg,
                TotalLineValExcl = inLineVal,
                StoreAvgCost = destAvg,
                ExchRate = 1,
                TransactionReference = line.ItemCode + " " + reference + " in"
            });

            // Ensure the destination store is linked to the item.
            var link = db.ItemStoreLinkMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreID == (int)toStoreId && x.ItemID == itemId);
            if (link == null)
            {
                db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                {
                    CompanyID = CurrentUser.CoID,
                    ItemID = itemId,
                    StoreID = (int)toStoreId,
                    Active = true
                });
            }

            // Save per move so the next line's ComputeMovement sees this row in the ledger
            // (Send Out posts several lines in one loop — same item twice must re-blend correctly).
            db.SaveChanges();
        }

        // Reserved GIT (goods-in-transit) store; created on first use like CoR/CoD/Scr.
        private long GitStoreId(SBMSEntities db)
        {
            var git = db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == GitCode);
            if (git == null)
            {
                git = new Store
                {
                    CompanyID = CurrentUser.CoID,
                    StoreCode = GitCode,
                    StoreDescript = "Goods In Transit",
                    StoreActive = true,
                    AllowPicking = false,
                    AllowReceiving = false
                };
                db.Stores.Add(git);
                db.SaveChanges();
            }
            return git.StoreID;
        }

        private string StoreCode(SBMSEntities db, long id)
        {
            int sid = (int)id;
            return db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == sid).Select(x => x.StoreCode).FirstOrDefault();
        }

        // ── Render ──
        private void RenderMode()
        {
            bool outMode = Mode == "out";
            lbtnModeOut.CssClass = "sm-toggle " + (outMode ? "on" : "off");
            lbtnModeIn.CssClass = "sm-toggle " + (outMode ? "off" : "on");
            lblModeTitle.Text = outMode ? "Send out to transit" : "Receive from transit";
            lblPrompt.Text = TrfID == 0
                ? "Scan the Transfer barcode"
                : (outMode ? "Confirm and send the whole transfer to transit" : "Receive each line into the destination");

            pnlHdr.Visible = TrfID != 0;
            pnlSendOut.Visible = TrfID != 0 && outMode;
            lbtnRestart.Visible = TrfID != 0;

            // The receive button per row is only active in Receive mode (handled in the template).
            rptLines.Visible = TrfID != 0;

            if (TrfID != 0)
            {
                lblTrfNum.Text = TrfNum;
                lblRoute.Text = FromStore + " → " + ToStore;
            }
        }

        private void ResetCycle()
        {
            TrfID = 0; TrfNum = null;
            FromStore = null; ToStore = null; FromStoreId = 0; ToStoreId = 0;
            pnlLines.Visible = false; pnlHdr.Visible = false; pnlSendOut.Visible = false;
            rptLines.DataSource = null; rptLines.DataBind();
        }

        protected void lbtnRestart_Click(object sender, EventArgs e) { ResetCycle(); ClearFeedback(); RenderMode(); }
        protected void lbtnClearScan_Click(object sender, EventArgs e) { txtScan.Text = string.Empty; ClearFeedback(); }

        // rptLines needs to know the current mode for the template (receive button visibility).
        protected bool InMode { get { return Mode == "in"; } }

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
