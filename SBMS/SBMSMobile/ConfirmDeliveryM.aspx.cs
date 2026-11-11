using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Confirm Delivery: after picking is closed off, whoever hands the goods over records who
    /// received them and a comment. Stored on the slip (PickingSlipMaster.PODReceivedBy /
    /// PODConfirmedDate / PODConfirmedBy / PODComment - raw SQL, outside the EF model,
    /// SQL\Add_PODComment.sql) and shown on the web Deliveries page.
    /// </summary>
    public partial class ConfirmDeliveryM : MobileBasePage
    {
        private new UserDetails CurrentUser => Session["UserDetails"] as UserDetails;

        public class DeliveryCard
        {
            public int PSID { get; set; }
            public string PSIntNumber { get; set; }
            public DateTime? Delivered { get; set; }
            public string SalesOrder { get; set; }
            public string Customer { get; set; }
            public string POD { get; set; }
        }

        public class PodRow
        {
            public int PSID { get; set; }
            public string PODNumber { get; set; }
            public string PODComment { get; set; }
            public DateTime? PODConfirmedDate { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (!IsPostBack) BindList();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            StampActionToken(hfActionToken);
        }

        // ── List ────────────────────────────────────────────────────────────────
        private void BindList()
        {
            long co = CurrentUser.CoID;
            string find = txtFind.Text.Trim().ToLower();
            DateTime from = DateTime.Today.AddDays(-60);
            var cards = new List<DeliveryCard>();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var slips = _db.PickingSlipMasters
                    .Where(m => m.CustomerID == co && m.PSComplete == true && m.LinkedSOrdID != null
                             && (m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate) >= from)
                    .Where(m => _db.PickSlipLines.Any(l => l.PSID == m.PSID && l.PickComplete == true))
                    .Select(m => new { m.PSID, m.PSIntNumber, m.LinkedSOrdID, Delivered = m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate })
                    .ToList();

                if (slips.Count > 0)
                {
                    var orderIds = slips.Select(m => m.LinkedSOrdID.Value).Distinct().ToList();
                    var orders = _db.DocHeaders.Where(x => x.CompanyID == co && orderIds.Contains(x.DocID))
                        .Select(x => new { x.DocID, x.DocumentNumber, x.CustSupName }).ToList()
                        .GroupBy(x => x.DocID).ToDictionary(g => g.Key, g => g.First());

                    string ids = string.Join(",", slips.Select(m => m.PSID));
                    var pods = _db.Database.SqlQuery<PodRow>(
                            "SELECT PSID, PODNumber, PODComment, PODConfirmedDate FROM dbo.PickingSlipMaster " +
                            "WHERE CustomerID = @p0 AND PSID IN (" + ids + ")", co)
                        .ToList().ToDictionary(x => x.PSID);

                    foreach (var m in slips)
                    {
                        pods.TryGetValue(m.PSID, out PodRow pod);
                        if (pod?.PODConfirmedDate != null) continue;   // already confirmed
                        orders.TryGetValue(m.LinkedSOrdID.Value, out var so);
                        cards.Add(new DeliveryCard
                        {
                            PSID = m.PSID,
                            PSIntNumber = m.PSIntNumber,
                            Delivered = m.Delivered,
                            SalesOrder = so?.DocumentNumber ?? "",
                            Customer = so?.CustSupName ?? "",
                            POD = string.IsNullOrEmpty(pod?.PODNumber) ? "" : "POD " + pod.PODNumber
                        });
                    }
                }
            }

            if (find.Length > 1)
                cards = cards.Where(c => (c.PSIntNumber ?? "").ToLower().Contains(find)
                                      || c.Customer.ToLower().Contains(find)
                                      || c.SalesOrder.ToLower().Contains(find)).ToList();

            rptDeliveries.DataSource = cards.OrderByDescending(c => c.Delivered).ThenBy(c => c.PSIntNumber).ToList();
            rptDeliveries.DataBind();
            lblEmpty.Visible = cards.Count == 0;
            pnlList.Visible = true;
            pnlDetail.Visible = false;
        }

        protected void lbtnFind_Click(object sender, EventArgs e) => BindList();

        // ── One delivery ────────────────────────────────────────────────────────
        protected void lbtnOpen_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(((LinkButton)sender).CommandArgument, out int psid)) return;
            ShowDetail(psid);
        }

        private void ShowDetail(int psid)
        {
            long co = CurrentUser.CoID;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var slip = _db.PickingSlipMasters.Where(m => m.CustomerID == co && m.PSID == psid)
                    .Select(m => new { m.PSIntNumber, m.LinkedSOrdID, Delivered = m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate })
                    .FirstOrDefault();
                if (slip == null) { BindList(); return; }

                var so = _db.DocHeaders.Where(x => x.CompanyID == co && x.DocID == slip.LinkedSOrdID)
                    .Select(x => new { x.DocumentNumber, x.CustSupName }).FirstOrDefault();
                var pod = _db.Database.SqlQuery<PodRow>(
                    "SELECT PSID, PODNumber, PODComment, PODConfirmedDate FROM dbo.PickingSlipMaster WHERE CustomerID = @p0 AND PSID = @p1",
                    co, psid).FirstOrDefault();

                long lpsid = psid;
                var lines = _db.PickSlipLines.Where(l => l.PSID == lpsid && l.PickComplete == true)
                    .OrderBy(l => l.LineID).ToList()
                    .Select(l => new { l.ItemCode, Description = l.ItemDescription, Qty = FormatQty(l.PickQty ?? l.Quantity ?? 0) + " " + (l.Unit ?? "") })
                    .ToList();

                hfPSID.Value = psid.ToString();
                lblSlip.Text = Server.HtmlEncode(slip.PSIntNumber);
                litMeta.Text = Server.HtmlEncode(so?.CustSupName ?? "") + "<br />SO: " + Server.HtmlEncode(so?.DocumentNumber ?? "")
                    + (string.IsNullOrEmpty(pod?.PODNumber) ? "" : " &nbsp; POD: " + Server.HtmlEncode(pod.PODNumber))
                    + "<br />Delivered: " + (slip.Delivered?.ToString("dd MMM yyyy") ?? "");
                rptLines.DataSource = lines;
                rptLines.DataBind();
                txtReceivedBy.Text = "";
                txtComment.Text = pod?.PODComment ?? "";
                lblError.Visible = false;
            }
            pnlList.Visible = false;
            pnlDetail.Visible = true;
        }

        protected void lbtnConfirm_Click(object sender, EventArgs e)
        {
            if (!TryConsumeActionToken(hfActionToken)) return;
            if (!int.TryParse(hfPSID.Value, out int psid)) { BindList(); return; }

            string receivedBy = txtReceivedBy.Text.Trim();
            string comment = txtComment.Text.Trim();
            if (receivedBy == "") { ShowError("Enter the name of the person who received the goods."); return; }
            if (receivedBy.Length > 100 || comment.Length > 500) { ShowError("Name: 100 characters at most; comment: 500."); return; }

            long co = CurrentUser.CoID;
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.PickingSlipMaster SET PODReceivedBy = @by, PODComment = @comment, PODConfirmedDate = GETDATE(), " +
                        "PODConfirmedBy = @user WHERE PSID = @psid AND CustomerID = @co",
                        new SqlParameter("@by", receivedBy),
                        new SqlParameter("@comment", comment == "" ? (object)DBNull.Value : comment),
                        new SqlParameter("@user", (object)(CurrentUser.UserName ?? CurrentUser.UserEmail) ?? DBNull.Value),
                        new SqlParameter("@psid", psid),
                        new SqlParameter("@co", co));
                }
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{co} ConfirmDelivery save PSID {psid} failed: {ex.Message}");
                ShowError("Not saved: " + ex.Message);
                return;
            }

            string slipNo = lblSlip.Text;
            BindList();
            lblFeedback.Text = "&#10004; " + slipNo + " confirmed.";
            lblFeedback.CssClass = "mob-feedback found";
        }

        private void ShowError(string msg)
        {
            lblError.Text = Server.HtmlEncode(msg);
            lblError.Visible = true;
            pnlList.Visible = false;
            pnlDetail.Visible = true;
        }

        protected void lbtnCancel_Click(object sender, EventArgs e) => BindList();

        // ── Navigation ──────────────────────────────────────────────────────────
        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            if (pnlDetail.Visible) { BindList(); return; }
            lbtnHome_Click(sender, e);
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
