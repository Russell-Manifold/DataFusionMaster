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


        public List<ReceivingOutstandingSummary> GetSortedDocHeaders(string sortExpression, string sortDirection)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString().Trim();

                // Source of truth is the PO itself: DocLine.Quantity = ordered,
                // DocLine.QtyLeft = outstanding (maintained by every receipt finalize).
                // One row per PO line by construction - no receipt-event grouping.
                var query = from h in _db.DocHeaders
                            join l in _db.DocLines on h.DocID equals l.DocID
                            where h.CompanyID == CurrentUser.CoID
                               && h.DocType == 1
                               && h.Complete != true
                               && h.Status != "Cancelled"
                               && (l.QtyLeft ?? 0) > 0
                               && (l.QtyLeft ?? 0) < (l.Quantity ?? 0)
                            select new ReceivingOutstandingSummary
                            {
                                PODocID = h.DocID,
                                PONumber = h.DocumentNumber,
                                CreatedDate = h.DocDate,
                                Supplier = h.CustSupName,
                                ItemCode = l.ItemCode,
                                ItemDescription = l.ItemDescription,
                                OrigQty = l.Quantity ?? 0,
                                RecQty = (l.Quantity ?? 0) - (l.QtyLeft ?? 0),
                                QtyLeft = l.QtyLeft ?? 0
                            };

                if (findstr.Length > 1)
                {
                    query = query.Where(x => x.PONumber.Contains(findstr) || x.Supplier.Contains(findstr));
                }

                string finditem = txtfindItem.Text.Trim();
                if (finditem.Length > 1)
                {
                    query = query.Where(x => x.ItemCode.Contains(finditem) || x.ItemDescription.Contains(finditem));
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

        // One row per part-received PO line, read straight off DocHeader/DocLine.
        public class ReceivingOutstandingSummary
        {
            public long PODocID { get; set; }
            public string PONumber { get; set; }
            public Nullable<System.DateTime> CreatedDate { get; set; }
            public string Supplier { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public Nullable<decimal> OrigQty { get; set; }
            public Nullable<decimal> RecQty { get; set; }
            public Nullable<decimal> QtyLeft { get; set; }
        }

        private void BindData()
        {
            string sortExpression = ViewState["SortExpression"] as string ?? "CreatedDate";
            string sortDirection = ViewState["SortDirection"] as string ?? "ASC";

            var sortedData = GetSortedDocHeaders(sortExpression, sortDirection);
            GridPOs.DataSource = sortedData;
            GridPOs.DataBind();
            lblpoqty.Text = " (" + GridPOs.Rows.Count + ")";
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
            Response.Redirect("~/Receiving.aspx?docid=" + id.ToString());
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            GridPOs.PageIndex = 0;
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

        private void ExportToExcel(List<ReceivingOutstandingSummary> data)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Receiving Outstanding");

                // --- Header row ---
                var headers = new[]
                {
            "PO Number", "Date", "Supplier", "Item Code", "Description",
            "Order Qty", "Received", "Balance"
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
                    ws.Cell(row, 2).Value = item.CreatedDate.HasValue
                        ? item.CreatedDate.Value.ToString("yyyy-MM-dd")
                        : string.Empty;
                    ws.Cell(row, 3).Value = item.Supplier;
                    ws.Cell(row, 4).Value = item.ItemCode;
                    ws.Cell(row, 5).Value = item.ItemDescription;
                    ws.Cell(row, 6).Value = item.OrigQty ?? 0;
                    ws.Cell(row, 7).Value = item.RecQty ?? 0;
                    ws.Cell(row, 8).Value = item.QtyLeft ?? 0;

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
                    int id = Convert.ToInt32(row.Cells[0].Text); // representative row id for the line group
                    var record = _db.ReceivingOutstandings.FirstOrDefault(x => x.id == id);
                    if (record == null) continue;

                    // A grid row is one PO line (grouped) - apply the archive state to
                    // every receipt event in the group so the sums stay consistent.
                    var groupRows = _db.ReceivingOutstandings
                        .Where(x => x.CompanyID == CurrentUser.CoID
                                 && x.PODocID == record.PODocID
                                 && (record.SBCALineID != null
                                        ? x.SBCALineID == record.SBCALineID
                                        : x.SBCALineID == null && x.ItemCode == record.ItemCode))
                        .ToList();

                    bool archive = chkArchive != null && chkArchive.Checked;
                    foreach (var r in groupRows)
                    {
                        r.Archive = archive;
                        r.ArchiveBy = archive ? CurrentUser.RoleID : (long?)null;
                        r.ArchiveDate = archive ? DateTime.Now : (DateTime?)null;
                        anyArchived = true;
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