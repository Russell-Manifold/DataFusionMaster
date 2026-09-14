using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Re-Order Report - one row per item: what is on hand, what is on order, what is
    /// committed out, and how much to buy. The flat list the demands pivot cannot give
    /// without the reader having to drive a pivot table.
    ///
    /// All of the arithmetic lives in dbo.GetReOrderReport - see SQL/Add_GetReOrderReport.sql
    /// for why On Hand is the item transaction ledger rather than the Sage figure, and why
    /// committed counts outstanding lines only. This page filters, sorts and exports.
    /// </summary>
    public partial class ReOrderReport : BasePage
    {
        /// <summary>Shape of one row of dbo.GetReOrderReport. Names must match the proc's columns.</summary>
        public class ReOrderRow
        {
            public string ItemCode { get; set; }
            public string Description { get; set; }
            public string CategoryDescript { get; set; }
            public string Unit { get; set; }
            public decimal QtyOnHand { get; set; }
            public decimal QtyOnOrder { get; set; }
            public decimal QtyCommitted { get; set; }
            public decimal QtyAvailable { get; set; }
            public decimal ReOrderLevel { get; set; }
            public decimal RecommendedQty { get; set; }
        }

        /// <summary>Shape of one row of dbo.GetReOrderItemDetail. Names must match its columns.</summary>
        public class ReOrderDetailRow
        {
            public int SortKey { get; set; }
            public string Source { get; set; }
            public string Direction { get; set; }
            public string DocNumber { get; set; }
            public string Reference { get; set; }
            public string Status { get; set; }
            public DateTime? DueDate { get; set; }
            public decimal Qty { get; set; }
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
                Response.Redirect("~/Dashboard.aspx?exp=true", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            imgCoImg.ImageUrl = File.Exists(Server.MapPath(imgPath))
                ? ResolveUrl(imgPath)
                : ResolveUrl("~/images/CoImages/0000.png");

            if (!IsPostBack)
            {
                // Seed the sort state to match what LoadData actually loads with (the proc
                // orders by ItemCode ascending). Without this the first click on Item Code
                // evaluates null == "ASC" as false, re-applies ASC, and the user has to click
                // twice before anything moves.
                ViewState["SortExpression"] = "ItemCode";
                ViewState["SortDirection"] = "ASC";
                LoadData();
            }
        }

        private List<ReOrderRow> FetchRows()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string searchText = txtfind.Text.Trim();

                return _db.Database.SqlQuery<ReOrderRow>(
                    "EXEC GetReOrderReport @CoID, @SearchText, @OnlyBelow, @IncludeForecasts, @HideDormant",
                    new System.Data.SqlClient.SqlParameter("@CoID", CurrentUser.CoID),
                    new System.Data.SqlClient.SqlParameter("@SearchText",
                        string.IsNullOrEmpty(searchText) ? (object)DBNull.Value : searchText),
                    new System.Data.SqlClient.SqlParameter("@OnlyBelow", chkOnlyBelow.Checked),
                    new System.Data.SqlClient.SqlParameter("@IncludeForecasts", chkForecasts.Checked),
                    new System.Data.SqlClient.SqlParameter("@HideDormant", chkHideDormant.Checked)
                ).ToList();
            }
        }

        private void LoadData()
        {
            try
            {
                var rows = FetchRows();

                foreach (var r in rows)
                {
                    r.QtyOnHand = ApiUrlCall.NumberToDecimal(r.QtyOnHand, CurrentUser.CompanyDecPlaces);
                    r.QtyOnOrder = ApiUrlCall.NumberToDecimal(r.QtyOnOrder, CurrentUser.CompanyDecPlaces);
                    r.QtyCommitted = ApiUrlCall.NumberToDecimal(r.QtyCommitted, CurrentUser.CompanyDecPlaces);
                    r.QtyAvailable = ApiUrlCall.NumberToDecimal(r.QtyAvailable, CurrentUser.CompanyDecPlaces);
                    r.ReOrderLevel = ApiUrlCall.NumberToDecimal(r.ReOrderLevel, CurrentUser.CompanyDecPlaces);
                    r.RecommendedQty = ApiUrlCall.NumberToDecimal(r.RecommendedQty, CurrentUser.CompanyDecPlaces);
                }

                rows = ApplySorting(rows, ViewState["SortExpression"] as string, ViewState["SortDirection"] as string);

                GridReOrder.DataSource = rows;
                GridReOrder.DataBind();
                lblReccount.Text = rows.Count.ToString("N0") + " item" + (rows.Count == 1 ? "" : "s");
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} ReOrderReport LoadData - {ex.Message}");
                AlertHelper.ShowSweetAlert(this, "The re-order report could not be loaded. " + ex.Message, "error");
            }
        }

        private List<ReOrderRow> ApplySorting(List<ReOrderRow> rows, string sortExpression, string sortDirection)
        {
            bool desc = sortDirection == "DESC";
            switch (sortExpression)
            {
                case "Description":
                    return desc ? rows.OrderByDescending(x => x.Description).ToList() : rows.OrderBy(x => x.Description).ToList();
                case "CategoryDescript":
                    return desc ? rows.OrderByDescending(x => x.CategoryDescript).ToList() : rows.OrderBy(x => x.CategoryDescript).ToList();
                case "QtyOnHand":
                    return desc ? rows.OrderByDescending(x => x.QtyOnHand).ToList() : rows.OrderBy(x => x.QtyOnHand).ToList();
                case "QtyOnOrder":
                    return desc ? rows.OrderByDescending(x => x.QtyOnOrder).ToList() : rows.OrderBy(x => x.QtyOnOrder).ToList();
                case "QtyCommitted":
                    return desc ? rows.OrderByDescending(x => x.QtyCommitted).ToList() : rows.OrderBy(x => x.QtyCommitted).ToList();
                case "QtyAvailable":
                    return desc ? rows.OrderByDescending(x => x.QtyAvailable).ToList() : rows.OrderBy(x => x.QtyAvailable).ToList();
                case "ReOrderLevel":
                    return desc ? rows.OrderByDescending(x => x.ReOrderLevel).ToList() : rows.OrderBy(x => x.ReOrderLevel).ToList();
                case "RecommendedQty":
                    return desc ? rows.OrderByDescending(x => x.RecommendedQty).ToList() : rows.OrderBy(x => x.RecommendedQty).ToList();
                default:
                    return desc ? rows.OrderByDescending(x => x.ItemCode).ToList() : rows.OrderBy(x => x.ItemCode).ToList();
            }
        }

        protected void GridReOrder_Sorting(object sender, GridViewSortEventArgs e)
        {
            string current = ViewState["SortExpression"] as string;
            string direction = ViewState["SortDirection"] as string;

            // A different column starts ascending; the same column flips. Inheriting the
            // previous column's direction makes the first click on a new column look wrong.
            if (e.SortExpression == current)
            {
                direction = direction == "ASC" ? "DESC" : "ASC";
            }
            else
            {
                direction = "ASC";
            }

            ViewState["SortExpression"] = e.SortExpression;
            ViewState["SortDirection"] = direction;
            LoadData();
        }

        /// <summary>
        /// Column hints. These live on the headings rather than as a line of small print above
        /// the grid, because a note above a grid is not read - state what each number counts
        /// where the reader is already looking.
        /// </summary>
        private static readonly string[] ColumnHints =
        {
            /* 0 Item Code      */ "Stock item code. Active, physical items only.",
            /* 1 Description    */ "Item description.",
            /* 2 Category       */ "Item category.",
            /* 3 Unit           */ "Unit of measure.",
            /* 4 On Hand        */ "Stock on hand across all stores, taken from the item transaction "
                                 + "ledger - the same total the Items screen and the works order components "
                                 + "screen show. Everything already picked, drawn or received is applied to it.",
            /* 5 On PO          */ "Still to arrive from suppliers: the outstanding balance of open "
                                 + "purchase order lines that have not been fully received.",
            /* 6 Committed      */ "Everything spoken for but not yet taken off stock:\n"
                                 + "  Picking slips - open slips, lines not yet picked\n"
                                 + "  Job cards - active cards, lines not yet picked\n"
                                 + "  Works orders - materials still to be drawn (ordered less used)\n"
                                 + "Picked and drawn quantities are excluded: they have already come off "
                                 + "On Hand.\n"
                                 + "Sales forecasts are counted only when that box is ticked.",
            /* 7 Available      */ "On Hand + On PO - Committed. This is what the re-order decision is "
                                 + "made on. A negative figure means the item is already oversold.",
            /* 8 Re-Order Level */ "The item's minimum level, from the item record. Items with no level "
                                 + "captured are treated as 1.",
            /* 9 Order Qty      */ "Recommended purchase: enough to bring Available back up to the "
                                 + "Re-Order Level, and never less than the item's minimum order quantity "
                                 + "where one is set."
        };

        protected void GridReOrder_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                for (int i = 0; i < e.Row.Cells.Count && i < ColumnHints.Length; i++)
                {
                    e.Row.Cells[i].ToolTip = ColumnHints[i];
                    // Signals there is something to hover - without it nobody discovers the hint.
                    e.Row.Cells[i].Attributes["style"] = "cursor:help";
                }
                return;
            }

            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var row = e.Row.DataItem as ReOrderRow;
            if (row == null) return;

            // Negative available means the item is already oversold - worth the eye more than
            // a routine top-up is.
            if (row.QtyAvailable < 0)
            {
                e.Row.Cells[7].BackColor = System.Drawing.Color.MistyRose;
                e.Row.Cells[7].ForeColor = System.Drawing.Color.Firebrick;
                e.Row.Cells[7].Font.Bold = true;
            }
            if (row.RecommendedQty > 0)
            {
                e.Row.Cells[9].Font.Bold = true;
            }
        }

        protected void lbtnFind_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            LoadData();
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            try
            {
                var rows = ApplySorting(FetchRows(), ViewState["SortExpression"] as string, ViewState["SortDirection"] as string);

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Re-Order Report");
                    var headers = new[] { "Item Code", "Description", "Category", "Unit", "On Hand",
                                          "On PO", "Committed", "Available", "Re-Order Level", "Order Qty" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    int r = 2;
                    foreach (var row in rows)
                    {
                        ws.Cell(r, 1).Value = row.ItemCode;
                        ws.Cell(r, 2).Value = row.Description;
                        ws.Cell(r, 3).Value = row.CategoryDescript;
                        ws.Cell(r, 4).Value = row.Unit;
                        ws.Cell(r, 5).Value = row.QtyOnHand;
                        ws.Cell(r, 6).Value = row.QtyOnOrder;
                        ws.Cell(r, 7).Value = row.QtyCommitted;
                        ws.Cell(r, 8).Value = row.QtyAvailable;
                        ws.Cell(r, 9).Value = row.ReOrderLevel;
                        ws.Cell(r, 10).Value = row.RecommendedQty;
                        if (r % 2 == 0) ws.Row(r).Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 250);
                        r++;
                    }
                    ws.Columns().AdjustToContents();

                    string filepath = Server.MapPath($"~/inputcsv/{CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "")}-ReOrder.xlsx");
                    workbook.SaveAs(filepath);
                    string file = $"ReOrderReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    Response.Clear();
                    Response.ContentType = "application/vnd.ms-excel";
                    Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    Response.TransmitFile(filepath);
                    Response.Flush();
                    System.IO.File.Delete(filepath);
                    Response.End();
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // Response.End() above - expected, not an error.
                throw;
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} ReOrderReport Download - {ex.Message}");
                AlertHelper.ShowSweetAlert(this, "The download could not be created. " + ex.Message, "error");
            }
        }

        /// <summary>
        /// Shows the documents behind one item's figures. GetReOrderItemDetail applies exactly
        /// the same filters as GetReOrderReport, so what is listed here adds up to the On PO
        /// and Committed figures on the row that was clicked - the summary line states both so
        /// the reader can see that for themselves.
        /// </summary>
        protected void lbtnDrill_Click(object sender, EventArgs e)
        {
            var lbtn = sender as LinkButton;
            if (lbtn == null) return;

            string itemCode = lbtn.CommandArgument;
            if (string.IsNullOrWhiteSpace(itemCode)) return;

            try
            {
                List<ReOrderDetailRow> detail;
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    detail = _db.Database.SqlQuery<ReOrderDetailRow>(
                        "EXEC GetReOrderItemDetail @CoID, @ItemCode, @IncludeForecasts",
                        new System.Data.SqlClient.SqlParameter("@CoID", CurrentUser.CoID),
                        new System.Data.SqlClient.SqlParameter("@ItemCode", itemCode),
                        new System.Data.SqlClient.SqlParameter("@IncludeForecasts", chkForecasts.Checked)
                    ).ToList();
                }

                foreach (var d in detail)
                {
                    d.Qty = ApiUrlCall.NumberToDecimal(d.Qty, CurrentUser.CompanyDecPlaces);
                }

                var row = (GridReOrder.DataSource as List<ReOrderRow>) == null
                    ? null
                    : (GridReOrder.DataSource as List<ReOrderRow>).FirstOrDefault(x => x.ItemCode == itemCode);

                decimal totalIn = detail.Where(x => x.Direction == "In").Sum(x => x.Qty);
                decimal totalOut = detail.Where(x => x.Direction == "Out").Sum(x => x.Qty);

                lblDetailItem.Text = Server.HtmlEncode(itemCode)
                    + (row != null && !string.IsNullOrEmpty(row.Description) ? " - " + Server.HtmlEncode(row.Description) : "");
                lblDetailSummary.Text = $"On PO {totalIn:N2} in, Committed {totalOut:N2} out, across {detail.Count:N0} document line"
                                        + (detail.Count == 1 ? "" : "s")
                                        + ". On Hand is a stock balance, not a document - use Stock/Lot Movement for the transactions behind it.";

                GridDetail.DataSource = detail;
                GridDetail.DataBind();
                lblDetailNone.Text = detail.Count == 0
                    ? "No open purchase orders, picking slips, job cards or works orders reference this item."
                    : "";

                mpeDetail.Show();
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} ReOrderReport Drill {itemCode} - {ex.Message}");
                AlertHelper.ShowSweetAlert(this, "The document detail could not be loaded. " + ex.Message, "error");
            }
        }

        protected void GridDetail_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var d = e.Row.DataItem as ReOrderDetailRow;
            if (d == null) return;

            // Incoming stock reads green, demand going out reads red - the direction of each
            // line matters more than its size when you are deciding what to buy.
            if (d.Direction == "In")
            {
                e.Row.Cells[5].ForeColor = System.Drawing.Color.SeaGreen;
            }
            else
            {
                e.Row.Cells[5].ForeColor = System.Drawing.Color.Firebrick;
                e.Row.Cells[5].Text = "-" + e.Row.Cells[5].Text;
            }
            e.Row.Cells[5].Font.Bold = true;
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
