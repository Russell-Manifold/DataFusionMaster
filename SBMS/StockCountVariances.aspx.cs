using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCountVariances : BasePage
    {
        int countid;
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            countid = Convert.ToInt32(Request.QueryString["id"].ToString());
            if (countid== null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
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
                LoadDDs();
                ApplyFilterAndSort();
                // create stock coundID
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {

                }
            }
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

       protected void LoadCount()
        {
            ApplyFilterAndSort();
        }

        protected void ApplyFilterAndSort(string sortExpression = null, string sortDirection = "ASC")
        {
            string filterText = txtFilter.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var allLines = _db.Database.SqlQuery<GetStckCountVariances_Result>(
                        "EXEC GetStckCountVariances @CoID, @CountID",
                        new System.Data.SqlClient.SqlParameter("@CoID", CurrentUser.CoID),
                        new System.Data.SqlClient.SqlParameter("@CountID", countid)
                    ).ToList();

                if (!allLines.Any()) return;

                // ── Load count reference ──────────────────────────────────
                var cnt = _db.StockCountMasters
                             .Where(x => x.CompanyID == CurrentUser.CoID
                                      && x.StCntID == countid).FirstOrDefault();

                if (cnt != null)
                {
                    txtRef.Text = cnt.CtDescription.ToString();
                    lblDate.Text = cnt.CtCreateDate.HasValue
                                    ? cnt.CtCreateDate.Value.ToString("dd MMM yyyy")
                                    : "";
                    lblCreatedBy.Text = cnt.CreatedBy.HasValue
                                    ? _db.RolesMasters.Where(u => u.RoleID == cnt.CreatedBy.Value)
                                               .Select(u => u.RoleName).FirstOrDefault()
                                    : "";
                }

                // ── Apply filters in memory ───────────────────────────────
                var filtered = allLines.AsEnumerable();

                if (DDCateg.SelectedIndex > 0)
                {
                    string selectedCategory = DDCateg.SelectedValue;
                    filtered = filtered.Where(x => x.CategoryDescript == selectedCategory);
                }

                if (!string.IsNullOrEmpty(filterText))
                {
                    string search = filterText.ToLower();
                    filtered = filtered.Where(x =>
                        (x.ItemCode ?? "").ToLower().Contains(search) ||
                        (x.ItemDescription ?? "").ToLower().Contains(search));
                }

                // ── Apply sorting ─────────────────────────────────────────
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    bool asc = sortDirection == "ASC";

                    if (sortExpression == "ItemCode")
                        filtered = asc ? filtered.OrderBy(x => x.ItemCode)
                                       : filtered.OrderByDescending(x => x.ItemCode);

                    else if (sortExpression == "ItemDescription")
                        filtered = asc ? filtered.OrderBy(x => x.ItemDescription)
                                       : filtered.OrderByDescending(x => x.ItemDescription);

                    else if (sortExpression == "CategoryDescript")
                        filtered = asc ? filtered.OrderBy(x => x.CategoryDescript)
                                       : filtered.OrderByDescending(x => x.CategoryDescript);

                    else if (sortExpression == "SystemQOH")
                        filtered = asc ? filtered.OrderBy(x => x.SystemQOH)
                                       : filtered.OrderByDescending(x => x.SystemQOH);

                    else if (sortExpression == "TotalCountedQty")
                        filtered = asc ? filtered.OrderBy(x => x.TotalCountedQty)
                                       : filtered.OrderByDescending(x => x.TotalCountedQty);

                    else if (sortExpression == "Variance")
                        filtered = asc ? filtered.OrderBy(x => x.Variance)
                                       : filtered.OrderByDescending(x => x.Variance);

                    else
                        filtered = filtered.OrderBy(x => x.ItemCode);
                }

                GridItems.DataSource = filtered.ToList();
                GridItems.DataBind();
            }
        }

        protected void GridCntLines_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = e.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";

            ApplyFilterAndSort(sortExpression, sortDirection);
        }

        protected void DDCateg_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCount();
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCount();
        }

        protected void lbtnSearch_Click(object sender, EventArgs e)
        {
            LoadCount();
        }

       protected void GridCntLines_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var varianceVal = DataBinder.Eval(e.Row.DataItem, "Variance");
                decimal variance = (varianceVal != null && varianceVal != DBNull.Value)
                                   ? Convert.ToDecimal(varianceVal)
                                   : 0;

                if (variance < 0)
                {
                    e.Row.Cells[5].ForeColor = System.Drawing.Color.Red;
                    e.Row.Cells[5].Font.Bold = true;
                }
                else if (variance > 0)
                {
                    e.Row.Cells[5].ForeColor = System.Drawing.Color.Green;
                    e.Row.Cells[5].Font.Bold = true;
                }
            }
        }

        protected void LoadDDs()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Categs = _db.ItemsMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true)
                        .Select(x => x.CategoryDescript)
                        .Distinct()
                        .OrderBy(x => x) // Order by CategoryDescript
                        .ToList();
                DDCateg.DataSource = Categs;
                DDCateg.DataBind();
                DDCateg.Items.Insert(0, "-Category-");
            }
        }

       protected void lbtnBack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockCounts.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            //lbtnSave_Click(sender, EventArgs.Empty);
            DownloadStockCountSheet(countid);
        }

        private void DownloadStockCountSheet(int countID)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.GetStckCountDetails(CurrentUser.CoID, countID)
                               .OrderBy(l => l.ItemCode)
                               .ToList();

                if (!lines.Any()) return;

                using (var wb = new XLWorkbook())
                {
                    // ── One tab per warehouse ─────────────────────────────────
                    var grouped = lines.GroupBy(l => l.StoreCode);

                    foreach (var storeGroup in grouped)
                    {
                        string tabName = storeGroup.First().StoreCode ?? storeGroup.Key.ToString();
                        tabName = tabName.Length > 31 ? tabName.Substring(0, 31) : tabName;

                        var ws = wb.Worksheets.Add(tabName);

                        // ── Header block ──────────────────────────────────────
                        ws.Cell("A1").Value = "Variance Report";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 14;

                        ws.Cell("A2").Value = "Count:";
                        ws.Cell("B1").Value = countID;
                        ws.Cell("B2").Value = lines[0].CtDescription?.ToString();
                        ws.Cell("A3").Value = "Store:";
                        ws.Cell("B3").Value = tabName;
                        ws.Cell("A4").Value = "Date:";
                        ws.Cell("B4").Value = DateTime.Now.ToString("dd MMM yyyy");
                        ws.Cell("A5").Value = "Counted by:";
                        ws.Cell("B5").Value = "";

                        ws.Range("A2:A5").Style.Font.Bold = true;
                        ws.Range("A2:A5").Style.Font.FontSize = 10;
                        ws.Range("B2:B5").Style.Font.FontSize = 10;

                        // ── Column headers ────────────────────────────────────
                        int headerRow = 7;
                        int col = 1;

                        ws.Cell(headerRow, col++).Value = "Category";
                        ws.Cell(headerRow, col++).Value = "Code";
                        ws.Cell(headerRow, col++).Value = "Item Description";
                        ws.Cell(headerRow, col++).Value = "System QOH";
                        ws.Cell(headerRow, col++).Value = "Count 1";
                        ws.Cell(headerRow, col++).Value = "Count 2";
                        ws.Cell(headerRow, col++).Value = "Final Qty";
                        ws.Cell(headerRow, col++).Value = "Variance";

                        int totalCols = col - 1;

                        var headerRange = ws.Range(headerRow, 1, headerRow, totalCols);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontSize = 10;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

                        // ── Data rows ─────────────────────────────────────────
                        int dataRow = headerRow + 1;

                        foreach (var line in storeGroup)
                        {
                            decimal? variance = (line.FinalQty.HasValue && line.QtyOnHand.HasValue)
                                                ? line.FinalQty.Value - line.QtyOnHand.Value
                                                : (decimal?)null;

                            ws.Cell(dataRow, 1).Value = line.CategoryDescript ?? "";
                            ws.Cell(dataRow, 2).Value = line.ItemCode ?? "";
                            ws.Cell(dataRow, 3).Value = line.ItemDescription ?? "";

                            // System QOH — always populated
                            ws.Cell(dataRow, 4).Value = line.QtyOnHand.HasValue
                                                        ? line.QtyOnHand.Value
                                                        : 0;
                            ws.Cell(dataRow, 4).Style.Alignment.Horizontal =
                                                        XLAlignmentHorizontalValues.Right;

                            // Count 1 — input cell
                            ws.Cell(dataRow, 5).Value = line.Count1Qty.HasValue
                                                        ? line.Count1Qty.Value : (decimal?)null;

                            // Count 2 — input cell
                            ws.Cell(dataRow, 6).Value = line.Count2Qty.HasValue
                                                        ? line.Count2Qty.Value : (decimal?)null;

                            // Final Qty
                            ws.Cell(dataRow, 7).Value = line.FinalQty.HasValue
                                                        ? line.FinalQty.Value : (decimal?)null;

                            // Variance — colour coded
                            if (variance.HasValue)
                            {
                                ws.Cell(dataRow, 8).Value = variance.Value;
                                if (variance.Value < 0)
                                    ws.Cell(dataRow, 8).Style.Font.FontColor = XLColor.Red;
                                else if (variance.Value > 0)
                                    ws.Cell(dataRow, 8).Style.Font.FontColor = XLColor.Green;
                            }
                            else
                            {
                                ws.Cell(dataRow, 8).Value = "";
                            }

                            // Right align numeric columns
                            ws.Range(dataRow, 4, dataRow, 8)
                              .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                            // Alternating row colour
                            if (dataRow % 2 == 0)
                                ws.Range(dataRow, 1, dataRow, totalCols)
                                  .Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);

                            ws.Range(dataRow, 1, dataRow, totalCols).Style.Font.FontSize = 10;
                            ws.Range(dataRow, 1, dataRow, totalCols)
                              .Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            ws.Range(dataRow, 1, dataRow, totalCols)
                              .Style.Border.BottomBorderColor = XLColor.FromArgb(217, 217, 217);

                            dataRow++;
                        }

                        // ── Input cell styling — Count 1, Count 2, Final Qty ──
                        var inputRange = ws.Range(headerRow + 1, 5, dataRow - 1, 7);
                        inputRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 204);
                        inputRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        inputRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(68, 114, 196);

                        // ── Column widths ─────────────────────────────────────
                        ws.Column(1).Width = 22; // Category
                        ws.Column(2).Width = 14; // Code
                        ws.Column(3).Width = 40; // Description
                        ws.Column(4).Width = 14; // System QOH
                        ws.Column(5).Width = 14; // Count 1
                        ws.Column(6).Width = 14; // Count 2
                        ws.Column(7).Width = 14; // Final Qty
                        ws.Column(8).Width = 14; // Variance

                        // ── Freeze header rows ────────────────────────────────
                        ws.SheetView.FreezeRows(headerRow);

                        // ── Print setup ───────────────────────────────────────
                        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                        ws.PageSetup.FitToPages(1, 0);
                        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
                        ws.PageSetup.ShowGridlines = true;
                        ws.PageSetup.Header.Left.AddText(
                            $"Stock Count — {tabName} — {DateTime.Now:dd MMM yyyy}");
                        ws.PageSetup.Footer.Right.AddText("Page ");
                        ws.PageSetup.Footer.Right.AddText(
                            XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
                        ws.PageSetup.Footer.Right.AddText(" of ");
                        ws.PageSetup.Footer.Right.AddText(
                            XLHFPredefinedText.NumberOfPages, XLHFOccurrence.AllPages);

                        // ── Protect — lock everything except input columns ────
                        ws.Protect("stockcount");
                        ws.Range(headerRow + 1, 5, dataRow - 1, 7)
                          .Style.Protection.Locked = false;
                    }

                    // ── Collated summary tab ──────────────────────────────────
                    var wsSummary = wb.Worksheets.Add("Collated");

                    // Header block
                    wsSummary.Cell("A1").Value = "Collated Summary - Variance Report";
                    wsSummary.Cell("A1").Style.Font.Bold = true;
                    wsSummary.Cell("A1").Style.Font.FontSize = 14;
                    wsSummary.Cell("A2").Value = "Count:";
                    wsSummary.Cell("B2").Value = lines[0].CtDescription?.ToString();
                    wsSummary.Cell("A3").Value = "Date:";
                    wsSummary.Cell("B3").Value = DateTime.Now.ToString("dd MMM yyyy");
                    wsSummary.Range("A2:A3").Style.Font.Bold = true;

                    // Column headers
                    int sumHeaderRow = 5;
                    wsSummary.Cell(sumHeaderRow, 1).Value = "Category";
                    wsSummary.Cell(sumHeaderRow, 2).Value = "Code";
                    wsSummary.Cell(sumHeaderRow, 3).Value = "Item Description";
                    wsSummary.Cell(sumHeaderRow, 4).Value = "System QOH";
                    wsSummary.Cell(sumHeaderRow, 5).Value = "Counted Qty";
                    wsSummary.Cell(sumHeaderRow, 6).Value = "Variance";
                    wsSummary.Cell(sumHeaderRow, 7).Value = "Status";

                    var sumHeaderRange = wsSummary.Range(sumHeaderRow, 1, sumHeaderRow, 7);
                    sumHeaderRange.Style.Font.Bold = true;
                    sumHeaderRange.Style.Font.FontSize = 10;
                    sumHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                    sumHeaderRange.Style.Font.FontColor = XLColor.White;
                    sumHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    sumHeaderRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

                    // Collate by ItemCode across all warehouses
                    var collated = lines
                        .GroupBy(l => new { l.ItemCode, l.ItemDescription, l.CategoryDescript })
                        .Select(g => new
                        {
                            g.Key.CategoryDescript,
                            g.Key.ItemCode,
                            g.Key.ItemDescription,
                            SystemQOH = g.Sum(x => x.QtyOnHand ?? 0),
                            CountedQty = g.Sum(x => x.FinalQty ?? 0),
                            Variance = g.Sum(x => x.FinalQty ?? 0) - g.Sum(x => x.QtyOnHand ?? 0),
                            Status = g.Any(x => !x.FinalQty.HasValue) ? "⚠ Incomplete" : "✓ Complete"
                        })
                        .OrderBy(x => x.ItemCode)
                        .ToList();

                    int sumDataRow = sumHeaderRow + 1;

                    foreach (var item in collated)
                    {
                        wsSummary.Cell(sumDataRow, 1).Value = item.CategoryDescript ?? "";
                        wsSummary.Cell(sumDataRow, 2).Value = item.ItemCode ?? "";
                        wsSummary.Cell(sumDataRow, 3).Value = item.ItemDescription ?? "";
                        wsSummary.Cell(sumDataRow, 4).Value = item.SystemQOH;
                        wsSummary.Cell(sumDataRow, 5).Value = item.CountedQty;
                        wsSummary.Cell(sumDataRow, 6).Value = item.Variance;
                        wsSummary.Cell(sumDataRow, 7).Value = item.Status;

                        // Colour variance
                        if (item.Variance < 0)
                            wsSummary.Cell(sumDataRow, 6).Style.Font.FontColor = XLColor.Red;
                        else if (item.Variance > 0)
                            wsSummary.Cell(sumDataRow, 6).Style.Font.FontColor = XLColor.Green;

                        // Colour status
                        if (item.Status.Contains("Incomplete"))
                            wsSummary.Cell(sumDataRow, 7).Style.Font.FontColor = XLColor.Orange;
                        else
                            wsSummary.Cell(sumDataRow, 7).Style.Font.FontColor = XLColor.Green;

                        // Right align numeric columns
                        wsSummary.Range(sumDataRow, 4, sumDataRow, 6)
                                 .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        // Alternating row colour
                        if (sumDataRow % 2 == 0)
                            wsSummary.Range(sumDataRow, 1, sumDataRow, 7)
                                     .Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);

                        wsSummary.Range(sumDataRow, 1, sumDataRow, 7).Style.Font.FontSize = 10;
                        wsSummary.Range(sumDataRow, 1, sumDataRow, 7)
                                 .Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        wsSummary.Range(sumDataRow, 1, sumDataRow, 7)
                                 .Style.Border.BottomBorderColor = XLColor.FromArgb(217, 217, 217);

                        sumDataRow++;
                    }

                    // Totals row
                    wsSummary.Cell(sumDataRow, 3).Value = "TOTAL";
                    wsSummary.Cell(sumDataRow, 3).Style.Font.Bold = true;
                    wsSummary.Cell(sumDataRow, 4).Value = collated.Sum(x => x.SystemQOH);
                    wsSummary.Cell(sumDataRow, 5).Value = collated.Sum(x => x.CountedQty);
                    wsSummary.Cell(sumDataRow, 6).Value = collated.Sum(x => x.Variance);
                    wsSummary.Range(sumDataRow, 3, sumDataRow, 6).Style.Font.Bold = true;
                    wsSummary.Range(sumDataRow, 4, sumDataRow, 6)
                             .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    wsSummary.Range(sumDataRow, 1, sumDataRow, 7)
                             .Style.Border.TopBorder = XLBorderStyleValues.Medium;

                    // Column widths
                    wsSummary.Column(1).Width = 22;
                    wsSummary.Column(2).Width = 14;
                    wsSummary.Column(3).Width = 40;
                    wsSummary.Column(4).Width = 14;
                    wsSummary.Column(5).Width = 14;
                    wsSummary.Column(6).Width = 14;
                    wsSummary.Column(7).Width = 14;

                    wsSummary.SheetView.FreezeRows(sumHeaderRow);
                    wsSummary.PageSetup.PaperSize = XLPaperSize.A4Paper;
                    wsSummary.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    wsSummary.PageSetup.FitToPages(1, 0);

                    // ── Stream to browser ─────────────────────────────────────
                    string fileName = $"Variances - Count_{countID}_{DateTime.Now:yyyyMMdd}.xlsx";
                    Response.Clear();
                    Response.ContentType =
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader(
                        "content-disposition", $"attachment;filename={fileName}");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        wb.SaveAs(ms);
                        ms.WriteTo(Response.OutputStream);
                    }

                    Response.Flush();
                    Response.End();
                }
            }
        }
    }
}