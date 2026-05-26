using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class OSPurchaseOrdersPartial : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");
            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            if (File.Exists(Server.MapPath(imgPath)))
            {
                imgCoImg.ImageUrl = ResolveUrl(imgPath);
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            lblUsername.Text = $":.. {CurrentUser.UserName}..: ";
            if (!IsPostBack)
            {
                lblDir.Text = "ASC";
                showhidebuttons();
                BindData();
            }
        }

        private void showhidebuttons()
        {
            if (CurrentUser.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (CurrentUser.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (CurrentUser.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (CurrentUser.UseModule2 == true)
            {
                if (CurrentUser.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (CurrentUser.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (CurrentUser.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                ibtnFCasts.Style.Add("display", "none");
                ibtnmrp.Style.Add("display", "none");
                ibtnJobTrack.Style.Add("display", "none");
            }
            if (CurrentUser.UseModule3 == true)
            {
                if (CurrentUser.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (CurrentUser.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (CurrentUser.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
            }
            else
            {
                ibtmWorksOrders.Style.Add("display", "none");
                ibtmWOrdMgment.Style.Add("display", "none");
                ibtnRMD.Style.Add("display", "none");
            }
        }


        public List<ReceivingOutstanding> GetSortedDocHeaders(string sortExpression, string sortDirection)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString();
                
                // Auto-archive any fully-received line groups.
                //
                // Modern rows are grouped by (PODocID, SBCALineID) - each PO line is its
                // own group, even if two lines share an ItemCode. Legacy rows (SBCALineID
                // NULL because they predate the column) fall back to (PODocID, ItemCode).
                var modernGroups = _db.ReceivingOutstandings
                       .Where(x => x.CompanyID == CurrentUser.CoID
                                && x.Archive == false
                                && x.SBCALineID != null)
                       .GroupBy(x => new { x.PODocID, x.SBCALineID })
                       .Where(g => g.Sum(x => (decimal?)x.RecQty ?? 0) >= g.Max(x => x.OrigQty ?? 0))
                       .Select(g => new { g.Key.PODocID, g.Key.SBCALineID })
                       .ToList();

                foreach (var group in modernGroups)
                {
                    var itemsToUpdate = _db.ReceivingOutstandings
                        .Where(x => x.PODocID == group.PODocID
                                 && x.SBCALineID == group.SBCALineID
                                 && x.CompanyID == CurrentUser.CoID);

                    foreach (var item in itemsToUpdate)
                    {
                        item.Archive = true;
                        item.ArchiveBy = 0;
                        item.ArchiveDate = DateTime.Now;
                    }
                }

                var legacyGroups = _db.ReceivingOutstandings
                       .Where(x => x.CompanyID == CurrentUser.CoID
                                && x.Archive == false
                                && x.SBCALineID == null)
                       .GroupBy(x => new { x.PODocID, x.ItemCode })
                       .Where(g => g.Sum(x => (decimal?)x.RecQty ?? 0) >= g.Max(x => x.OrigQty ?? 0))
                       .Select(g => new { g.Key.PODocID, g.Key.ItemCode })
                       .ToList();

                foreach (var group in legacyGroups)
                {
                    var itemsToUpdate = _db.ReceivingOutstandings
                        .Where(x => x.PODocID == group.PODocID
                                 && x.ItemCode == group.ItemCode
                                 && x.SBCALineID == null
                                 && x.CompanyID == CurrentUser.CoID);

                    foreach (var item in itemsToUpdate)
                    {
                        item.Archive = true;
                        item.ArchiveBy = 0;
                        item.ArchiveDate = DateTime.Now;
                    }
                }

                // Step 3: Commit updates to DB
                _db.SaveChanges();

                var query = _db.ReceivingOutstandings.Where(x => x.CompanyID == CurrentUser.CoID).AsQueryable();

                if (txtfind.Text.ToString().Trim().Length > 1)
                {
                    query = query.Where(x=>x.PONumber.Contains(findstr) || x.Supplier.Contains(findstr));
                }
                if (!chkArchived.Checked)
                {
                    query = query.Where(x => x.Archive == false);
                } else
                if (chkArchived.Checked)
                {
                    query = query.Where(x => x.Archive == true);
                }

                // Apply sorting using dynamic LINQ
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    try
                    {
                        var sortQuery = $"{sortExpression} {(sortDirection == "ASC" ? "ascending" : "descending")}";
                        query = query.OrderBy(sortQuery);
                    }
                    catch { }
                }
                return query.ToList();
            }
        }

        public class ReceivingOutstandingSummary
        {
            public string PONumber { get; set; }
            public string Supplier { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal TotalReceivedQty { get; set; }
        }

        private void BindData()
        {
            string sortExpression = ViewState["SortExpression"] as string ?? "CreatedDate";
            string sortDirection = ViewState["SortDirection"] as string ?? "ASC";

            var sortedData = GetSortedDocHeaders(sortExpression, sortDirection);
            GridPOs.DataSource = sortedData;
            GridPOs.DataBind();
            lblpoqty.Text = " (" + GridPOs.Rows.Count + ")";
            if (GridPOs.Rows.Count == 0)
            {
                lbtnArchive.Visible = false;
            }
            else
            {
               lbtnArchive.Visible = true;
            }
        }

        protected void myDataGrid_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = ViewState["SortDirection"] as string == "ASC" ? "DESC" : "ASC";

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            BindData();
        }
        protected void GridPOs_SelectedIndexChanged(object sender, EventArgs e)
        {
            long id = Convert.ToInt64(GridPOs.SelectedRow.Cells[0].Text.ToString());
            CheckBox ckb = (CheckBox)(GridPOs.SelectedRow.FindControl("chkstarted"));
            Response.Redirect("~/SalesOrder.aspx?docid=" + id.ToString());
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            BindData();
        }

        protected void chkCompl_CheckedChanged(object sender, EventArgs e)
        {
            BindData();
        }

        protected void GridPOs_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = ViewState["SortDirection"] as string == "ASC" ? "DESC" : "ASC";

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            BindData();
        }

        protected void GridPOs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridPOs.PageIndex = e.NewPageIndex;
            BindData(); // Rebind data to the GridV
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void DDPOStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindData();
        }

        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgbTrf_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Transfer.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/FGDemands.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/JobTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ProductionRMD.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgdash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {

        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            var data = GetSortedDocHeaders(string.Empty, "ASC"); // or pass current sort state
            ExportToExcel(data);
        }

        private void ExportToExcel(List<ReceivingOutstanding> data)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Receiving Outstanding");

                // --- Header row ---
                var headers = new[]
                {
            "PO Number", "PO Doc ID", "Item Code", "Supplier",
            "Orig Qty", "Rec Qty", "Archive", "Archive By", "Archive Date"
            // Adjust these to match your actual ReceivingOutstanding properties
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192); // blue header
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // --- Data rows ---
                int row = 2;
                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.PONumber;
                    ws.Cell(row, 2).Value = item.PODocID;
                    ws.Cell(row, 3).Value = item.ItemCode;
                    ws.Cell(row, 4).Value = item.Supplier;
                    ws.Cell(row, 5).Value = item.OrigQty ?? 0;
                    ws.Cell(row, 6).Value = item.RecQty ?? 0;
                    ws.Cell(row, 7).Value = (bool)item.Archive ? "Yes" : "No";
                    ws.Cell(row, 8).Value = item.ArchiveBy;
                    ws.Cell(row, 9).Value = item.ArchiveDate.HasValue
                        ? item.ArchiveDate.Value.ToString("yyyy-MM-dd")
                        : string.Empty;

                    // Zebra striping
                    if (row % 2 == 0)
                    {
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 250);
                    }

                    row++;
                }

                // --- Auto-fit columns ---
                ws.Columns().AdjustToContents();

                workbook.SaveAs(Server.MapPath($"~/inputcsv/{CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "")}-RO.xlsx"));
                string file = $"ReceivingOutstanding_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                string filepath = (Server.MapPath($"~/inputcsv/{CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "")}-RO.xlsx"));

                Response.Clear();
                Response.ContentType = "application/vnd.ms-excel";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.TransmitFile(filepath);
                Response.Flush();
                Response.End();
                System.IO.File.Delete(filepath);
            }
        }

        protected void lbtnArchive_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                bool anyArchived = false;
                
                foreach (GridViewRow row in GridPOs.Rows)
                {
                    CheckBox chkArchive = (CheckBox)row.FindControl("chkArchive");
                    if (chkArchive != null && chkArchive.Checked)
                    {
                        int id = Convert.ToInt32(row.Cells[0].Text); // Assuming ID is in first column
                        var record = _db.ReceivingOutstandings.FirstOrDefault(x => x.id == id);       
                        if (record != null)
                        {
                            record.Archive = true;
                            record.ArchiveBy = CurrentUser.RoleID;
                            record.ArchiveDate = DateTime.Now;
                            anyArchived = true;
                        }
                    }
                    else
                    {
                        // If the checkbox is not checked, ensure the record is not archived
                        int id = Convert.ToInt32(row.Cells[0].Text); 
                        var record = _db.ReceivingOutstandings.FirstOrDefault(x => x.id == id);  
                        if (record != null)
                        {
                            record.Archive = false;
                            record.ArchiveBy = null;
                            record.ArchiveDate = null;
                            anyArchived = true;
                        }
                    }
                }

                if (anyArchived)
                {
                    try
                    {
                        _db.SaveChanges();
                        // Refresh the grid
                        BindData();
                        ShowMessage(sender, e, "Selected records have been archived successfully.");
                    }
                    catch (Exception ex)
                    {
                        // Handle or log any errors
                        System.Diagnostics.Debug.WriteLine($"Error archiving records: {ex.Message}");
                    }
                }
            }     
        }

        protected void GridPOs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }


        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }
    }

}