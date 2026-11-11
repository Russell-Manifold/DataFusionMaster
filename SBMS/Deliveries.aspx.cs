using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Deliveries made: every picked line of every closed-off picking slip linked to a Sales Order,
    /// with the date it was closed off. Opened from the Sales Orders list (date range / search) or
    /// from one Sales Order (?docid= - all deliveries of that order, any date). Read-only.
    /// </summary>
    public partial class Deliveries : BasePage
    {
        public class DeliveryRow
        {
            public int PSID { get; set; }
            public DateTime? Delivered { get; set; }
            public string SalesOrder { get; set; }
            public string SalesOrderGuid { get; set; }
            public string Customer { get; set; }
            public string PickingSlip { get; set; }
            public string POD { get; set; }
            public string Comment { get; set; }
            public string ReceivedBy { get; set; }
            public DateTime? Confirmed { get; set; }
            public string ItemCode { get; set; }
            public string Description { get; set; }
            public string Lot { get; set; }
            public string Store { get; set; }
            public decimal OnSlip { get; set; }
            public decimal DeliveredQty { get; set; }
            public string Unit { get; set; }
        }

        public class PodRow
        {
            public int PSID { get; set; }
            public string PODNumber { get; set; }
            public string PODComment { get; set; }
            public string PODReceivedBy { get; set; }
            public DateTime? PODConfirmedDate { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false);
                return;
            }
            if (!IsPostBack)
            {
                string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
                imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");

                // From one Sales Order: exactly that order, no date limit (its number shown in the search box).
                if (long.TryParse(Request.QueryString["docid"], out long docId))
                {
                    ViewState["DocID"] = docId;
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        long co = CurrentUser.CoID;
                        txtSearch.Text = _db.DocHeaders.Where(x => x.CompanyID == co && x.DocID == docId)
                                            .Select(x => x.DocumentNumber).FirstOrDefault() ?? "";
                    }
                }
                else
                {
                    txtFrom.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
                    txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
                }
                BindGrid();
            }
        }

        private List<DeliveryRow> LoadRows()
        {
            long co = CurrentUser.CoID;
            long? oneOrder = ViewState["DocID"] as long?;
            string search = txtSearch.Text.Trim();
            bool hasFrom = DateTime.TryParseExact(txtFrom.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime from);
            bool hasTo = DateTime.TryParseExact(txtTo.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime to)
                         && to < DateTime.MaxValue.Date;
            if (!hasFrom && !hasTo && search == "" && oneOrder == null)
            {
                // Nothing to narrow it down - keep it to the last 30 days.
                from = DateTime.Today.AddDays(-30); hasFrom = true;
                txtFrom.Text = from.ToString("yyyy-MM-dd");
            }
            DateTime toEnd = hasTo ? to.AddDays(1) : DateTime.MaxValue;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var slips = _db.PickingSlipMasters.Where(m => m.CustomerID == co && m.PSComplete == true && m.LinkedSOrdID != null);
                if (hasFrom) slips = slips.Where(m => (m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate) >= from);
                if (hasTo) slips = slips.Where(m => (m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate) < toEnd);
                if (oneOrder != null)
                    slips = slips.Where(m => m.LinkedSOrdID == oneOrder);
                else if (search != "")
                {
                    var docIds = _db.DocHeaders
                        .Where(x => x.CompanyID == co && (x.DocumentNumber.Contains(search) || x.CustSupName.Contains(search)))
                        .Select(x => (long?)x.DocID);
                    slips = slips.Where(m => docIds.Contains(m.LinkedSOrdID));
                }
                var slipList = slips.Select(m => new
                {
                    m.PSID,
                    m.PSIntNumber,
                    m.LinkedSOrdID,
                    Delivered = m.PSCompleteDate ?? m.PSClosedOffDate ?? m.PSCreatedDate
                }).ToList();
                if (slipList.Count == 0) return new List<DeliveryRow>();

                var orderIds = slipList.Select(m => m.LinkedSOrdID.Value).Distinct().ToList();
                var orders = _db.DocHeaders.Where(x => x.CompanyID == co && orderIds.Contains(x.DocID))
                    .Select(x => new { x.DocID, x.DocumentNumber, x.CustSupName, x.DocGUID }).ToList()
                    .GroupBy(x => x.DocID).ToDictionary(g => g.Key, g => g.First());

                var slipIds = slipList.Select(m => (long)m.PSID).ToList();
                var lines = _db.PickSlipLines.Where(l => slipIds.Contains(l.PSID) && l.PickComplete == true)
                    .OrderBy(l => l.LineID).ToList().ToLookup(l => l.PSID);

                // POD number + comment live outside the EF model (SQL\Add_DeliveryNoteLayout.sql,
                // SQL\Add_PODComment.sql). The number is given on first print of the detailed delivery
                // note, or typed in here.
                var pods = new Dictionary<int, PodRow>();
                int podNext = 0;
                try
                {
                    string ids = string.Join(",", slipList.Select(m => m.PSID));
                    pods = _db.Database.SqlQuery<PodRow>(
                            "SELECT PSID, PODNumber, PODComment, PODReceivedBy, PODConfirmedDate FROM dbo.PickingSlipMaster WHERE CustomerID = @p0 " +
                            "AND (PODNumber IS NOT NULL OR PODComment IS NOT NULL OR PODConfirmedDate IS NOT NULL) AND PSID IN (" + ids + ")", co)
                        .ToList().ToDictionary(x => x.PSID);
                    podNext = _db.Database.SqlQuery<int?>(
                            "SELECT PODNextNumber FROM dbo.CompanyMaster WHERE SBCACoID = @p0", co).FirstOrDefault() ?? 0;
                }
                catch (Exception ex) { new ApiUrlCall().LogErrorToFile($"CoID:{co} Deliveries POD read failed: {ex.Message}"); }

                // No running POD number: the delivery note prints the order number, plus "-n" (the slip's
                // place among the order's slips) when the order has more than one - same as DnNumber().
                var slipsByOrder = podNext > 0 ? null : _db.PickingSlipMasters
                    .Where(x => x.CustomerID == co && orderIds.Contains(x.LinkedSOrdID.Value))
                    .Select(x => new { x.PSID, x.LinkedSOrdID }).ToList()
                    .GroupBy(x => x.LinkedSOrdID.Value)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.PSID).OrderBy(x => x).ToList());

                var rows = new List<DeliveryRow>();
                foreach (var m in slipList)
                {
                    pods.TryGetValue(m.PSID, out PodRow podRow);
                    string pod = podRow?.PODNumber;
                    orders.TryGetValue(m.LinkedSOrdID.Value, out var so);
                    if (pod == null && slipsByOrder != null && so != null
                        && slipsByOrder.TryGetValue(m.LinkedSOrdID.Value, out var orderSlips))
                    {
                        pod = (so.DocumentNumber ?? "").Replace("SO", "");
                        if (orderSlips.Count > 1) pod += "-" + (orderSlips.IndexOf(m.PSID) + 1);
                    }
                    foreach (var l in lines[m.PSID])
                    {
                        rows.Add(new DeliveryRow
                        {
                            PSID = m.PSID,
                            Comment = podRow?.PODComment ?? "",
                            ReceivedBy = podRow?.PODReceivedBy ?? "",
                            Confirmed = podRow?.PODConfirmedDate,
                            Delivered = m.Delivered,
                            SalesOrder = so?.DocumentNumber ?? "",
                            SalesOrderGuid = so?.DocGUID?.ToString() ?? "",
                            Customer = so?.CustSupName ?? "",
                            PickingSlip = m.PSIntNumber,
                            POD = pod ?? "",
                            ItemCode = l.ItemCode,
                            Description = l.ItemDescription,
                            Lot = l.IsLotTracked ? (l.LotNumber ?? "") : "",
                            Store = l.StoreCodeFrom,
                            OnSlip = l.Quantity ?? 0,
                            DeliveredQty = l.PickQty ?? l.Quantity ?? 0,
                            Unit = l.Unit
                        });
                    }
                }
                return rows.OrderByDescending(r => r.Delivered).ThenBy(r => r.SalesOrder).ThenBy(r => r.PickingSlip).ToList();
            }
        }

        private void BindGrid()
        {
            var rows = LoadRows();
            lblInfo.Text = rows.Count == 0 ? "" :
                $"{rows.Select(r => r.PickingSlip).Distinct().Count()} delivery(ies), {rows.Count} line(s)";
            GridDeliveries.DataSource = rows;
            GridDeliveries.DataBind();
        }

        protected void lbtnFind_Click(object sender, EventArgs e)
        {
            ViewState["DocID"] = null;   // own search from here on, not the one order it was opened for
            GridDeliveries.PageIndex = 0;
            BindGrid();
        }

        protected void GridDeliveries_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridDeliveries.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void lbtnExcel_Click(object sender, EventArgs e)
        {
            var dt = DataTableHelper.ConvertToDataTable(LoadRows());
            dt.Columns.Remove("PSID");
            dt.Columns.Remove("SalesOrderGuid");
            ExcelHelper.ExportToExcel(dt, "Deliveries", "Deliveries");
        }

        protected void GridDeliveries_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "OpenSO")
            {
                if (Guid.TryParse(Convert.ToString(e.CommandArgument), out Guid soGuid))
                    Response.Redirect("~/SalesOrder.aspx?docid=" + soGuid, false);
                return;
            }
            if (e.CommandName != "OpenPOD" || !int.TryParse(Convert.ToString(e.CommandArgument), out int psid)) return;
            lblPODMsg.Text = "";
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    long co = CurrentUser.CoID;
                    var slip = _db.PickingSlipMasters.Where(m => m.CustomerID == co && m.PSID == psid)
                                  .Select(m => new { m.PSIntNumber, m.LinkedSOrdID }).FirstOrDefault();
                    if (slip == null) return;
                    var so = _db.DocHeaders.Where(x => x.CompanyID == co && x.DocID == slip.LinkedSOrdID)
                                .Select(x => new { x.DocumentNumber, x.CustSupName }).FirstOrDefault();
                    var pod = _db.Database.SqlQuery<PodRow>(
                        "SELECT PSID, PODNumber, PODComment FROM dbo.PickingSlipMaster WHERE CustomerID = @p0 AND PSID = @p1", co, psid).FirstOrDefault();

                    hfPSID.Value = psid.ToString();
                    lblPODSlip.Text = Server.HtmlEncode($"{slip.PSIntNumber} - {so?.DocumentNumber} - {so?.CustSupName}");
                    txtPOD.Text = pod?.PODNumber ?? "";
                    txtPODComment.Text = pod?.PODComment ?? "";
                    pnlPOD.Visible = true;
                }
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Deliveries open POD {psid} failed: {ex.Message}");
                pnlPOD.Visible = true;
                lblPODMsg.Text = "Could not open this delivery: " + Server.HtmlEncode(ex.Message);
            }
        }

        protected void lbtnPODSave_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(hfPSID.Value, out int psid)) return;
            string pod = txtPOD.Text.Trim();
            string comment = txtPODComment.Text.Trim();
            if (pod.Length > 30) { lblPODMsg.Text = "POD number: 30 characters at most."; return; }
            if (comment.Length > 500) { lblPODMsg.Text = "Comment: 500 characters at most."; return; }
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.PickingSlipMaster SET PODNumber = @pod, PODComment = @comment WHERE PSID = @psid AND CustomerID = @co",
                        new SqlParameter("@pod", pod == "" ? (object)DBNull.Value : pod),
                        new SqlParameter("@comment", comment == "" ? (object)DBNull.Value : comment),
                        new SqlParameter("@psid", psid),
                        new SqlParameter("@co", CurrentUser.CoID));
                }
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Deliveries save POD {psid} failed: {ex.Message}");
                lblPODMsg.Text = "Not saved: " + Server.HtmlEncode(ex.Message);
                return;
            }
            pnlPOD.Visible = false;
            BindGrid();
        }

        protected void lbtnPODCancel_Click(object sender, EventArgs e)
        {
            pnlPOD.Visible = false;
        }

        protected void lbtnSOs_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
