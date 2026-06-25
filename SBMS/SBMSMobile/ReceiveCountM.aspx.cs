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
    // Mode 1 — COUNT on the scanner, RECEIVE on the web.
    // The operator scans each PO line and captures an Accept and/or Reject quantity (per-line
    // Accept/Reject toggle, default Accept), then taps Mark Ready (RecStatus = 1). No Sage and
    // no stock movement here — the web Receiving screen posts the GRN, booking accepted qty into
    // the default receiving warehouse and rejected qty into the reject warehouse.
    public partial class ReceiveCountM : BasePage
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
                        lbtnMarkReady.Enabled = false;
                }

                BindLines();
            }
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private List<DocLine> GetCountLines()
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
            var lines = GetCountLines();
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

            decimal ordered  = line.Quantity ?? 0;
            decimal accepted = line.ReceiveQty ?? 0;
            decimal rejected = line.RejectQty;
            decimal remaining = ordered - accepted - rejected;

            var hf = (HiddenField)e.Item.FindControl("hfLineID");
            if (hf != null && isMatched) hf.Value = "matched:" + line.LineID;

            var pnlCard = (Panel)e.Item.FindControl("pnlCard");
            if (pnlCard != null && isMatched) pnlCard.CssClass = "mob-linecard matched";

            var lblAcc = (Label)e.Item.FindControl("lblAccepted");
            if (lblAcc != null) lblAcc.Text = accepted.ToString("0.##");
            var lblRej = (Label)e.Item.FindControl("lblRejected");
            if (lblRej != null) lblRej.Text = rejected.ToString("0.##");
            var lblRem = (Label)e.Item.FindControl("lblRemaining");
            if (lblRem != null) lblRem.Text = remaining.ToString("0.##");

            var txtQty = (TextBox)e.Item.FindControl("txtQty");
            if (txtQty != null && isMatched)
                txtQty.Text = remaining > 0 ? remaining.ToString("0.##") : "";

            var ddMode = (DropDownList)e.Item.FindControl("ddMode");
            if (ddMode != null && ddMode.Items.Count == 0)
            {
                ddMode.Items.Add(new ListItem("Accept", "A"));
                ddMode.Items.Add(new ListItem("Reject", "R"));
            }

            var badge = (Label)e.Item.FindControl("lblSavedBadge");
            if (badge != null) badge.Visible = (accepted + rejected) > 0;

            // Fully counted → grey out (and disable) the save button.
            var saveBtn = (LinkButton)e.Item.FindControl("lbtnSaveLine");
            if (saveBtn != null && remaining <= 0)
            {
                saveBtn.Enabled = false;
                saveBtn.Attributes["style"] = "background:#cccccc;border-color:#cccccc;color:#777;";
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
                    && (l.ReceiveComplete == null || l.ReceiveComplete == false));
            }

            if (matched == null)
            {
                SetFeedback(false, $"&#9888; <strong>{item.Code}</strong> ({item.Description}) is not on this PO.");
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

        // ── Capture a count (Accept or Reject) ──────────────────────────────────

        protected void lbtnSaveLine_Click(object sender, EventArgs e)
        {
            var item = ((LinkButton)sender).NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf     = (HiddenField)item.FindControl("hfLineID");
            var txtQty = (TextBox)item.FindControl("txtQty");
            var ddMode = (DropDownList)item.FindControl("ddMode");
            if (hf == null) return;

            string rawId = hf.Value.Replace("matched:", "").Trim();
            if (!int.TryParse(rawId, out int lineId)) return;

            if (!decimal.TryParse((txtQty?.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Enter a valid quantity.");
                return;
            }

            bool reject = (ddMode?.SelectedValue == "R");

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.DocLines.FirstOrDefault(l => l.LineID == lineId && l.CompanyID == CurrentUser.CoID);
                if (line == null) return;

                if (reject) line.RejectQty  = line.RejectQty + qty;
                else        line.ReceiveQty = (line.ReceiveQty ?? 0) + qty;
                line.ToReceive = true;

                // Stamp the default receiving store on the line, as if captured on the web.
                var recvStore = db.Stores.FirstOrDefault(s => s.CompanyID == CurrentUser.CoID
                    && s.AllowReceiving == true && s.StoreActive == true);
                if (recvStore != null) line.StoreCode = recvStore.StoreCode;

                // Auto lot number — same convention as the web (ddMMyyyy + store + sequence).
                // Generated once per line (kept on re-capture) and the LotTrackingMaster row is
                // written here so the sequence stays unique. The web uses this lot, not a new one.
                if (CurrentUser.CompanyUseLotNumbers == true
                    && string.IsNullOrEmpty(line.LotNumber) && recvStore != null)
                {
                    var itm = db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.Code == line.ItemCode);
                    if (itm != null && itm.IsLotTracked == true)
                    {
                        int recnum = GetLotNum(CurrentUser.CoID);
                        string lotNum = DateTime.Today.ToString("ddMMyyyy") + recvStore.StoreCode + recnum.ToString();
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
            SetFeedback(true, $"&#10003; {qty:0.##} {(reject ? "rejected" : "accepted")}.");
            BindLines();
        }

        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e) { }

        // Next lot sequence for today (same as the web): count today's lot records + 1.
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

        // ── Mark Ready: hand the counted PO to the web for the GRN ──

        protected void lbtnMarkReady_Click(object sender, EventArgs e)
        {
            bool anyCounted;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                anyCounted = db.DocLines.Any(l => l.DocID == DocID && l.CompanyID == CurrentUser.CoID
                    && ((l.ReceiveQty ?? 0) > 0 || l.RejectQty > 0));
            }
            if (!anyCounted)
            {
                SetFeedback(false, "&#9888; Nothing counted yet. Capture quantities first.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var hdr = db.DocHeaders.FirstOrDefault(h => h.DocID == DocID && h.CompanyID == CurrentUser.CoID);
                if (hdr != null)
                {
                    hdr.Started   = true;
                    hdr.RecStatus = 1;   // Ready — the web finalises the GRN
                }
                db.SaveChanges();
            }

            SetFeedback(true, "&#10003; Marked ready. The receiving desk can now process this PO.");
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
