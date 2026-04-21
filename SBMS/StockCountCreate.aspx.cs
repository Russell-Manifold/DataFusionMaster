using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCountCreate : BasePage
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
                CleanUpEmptyCounts();
                LoadDDs();
                ApplyFilterAndSort();
                // create stock coundID
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (countid == 0)
                    {
                        var maxStCntID = _db.StockCountMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID)
                        .Max(x => (int?)x.StCntID) ?? 0;

                        long LastCnt = maxStCntID;
                        var NewCnt = new StockCountMaster();
                        NewCnt.CtDescription = $"Count {maxStCntID+1}-{DateTime.Today.ToString("dd-MM-yy")}";
                        NewCnt.CompanyID = CurrentUser.CoID;
                        NewCnt.CtCreateDate = DateTime.Now;
                        NewCnt.CreatedBy = CurrentUser.RoleID;
                        _db.StockCountMasters.Add(NewCnt);
                        _db.SaveChanges();
                        lblCountID.Text = NewCnt.StCntID.ToString();
                        countid = NewCnt.StCntID;
                        lblDate.Text = DateTime.Today.ToString("dd MMM yyyy");
                        txtRef.Text = NewCnt.CtDescription.ToString();
                        lblCountID.Visible = false;
                    }
                    else
                    {
                        lblCountID.Text = countid.ToString();
                        var CntID = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StCntID == countid).FirstOrDefault();
                        txtRef.Text = CntID.CtDescription.ToString();
                        ApplyFilterAndSort();
                        LoadCountList();
                    }  
                }
            }
        }

        private void CleanUpEmptyCounts()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var toDelete = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && (x.CtDescription == null || x.CtDescription.Trim() == "")).ToList();
                if (toDelete.Any())
                {
                    _db.StockCountMasters.RemoveRange(toDelete);
                    _db.SaveChanges();
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
               
                var StkLines = _db.GetStockCountList(CurrentUser.CoID).AsQueryable();

               // Apply filters
                if (DDCateg.SelectedIndex > 0)
                {
                    string selectedCategory = DDCateg.SelectedValue;
                    StkLines = StkLines.Where(x => x.CategoryDescript == selectedCategory);
                }

                if (DDStore.SelectedIndex > 0)
                {
                    string selectedStore = DDStore.SelectedItem.Text;
                    StkLines = StkLines.Where(x => x.StoreCode == selectedStore);
                }

                if (!string.IsNullOrEmpty(filterText))
                {
                    StkLines = StkLines.Where(x => x.Code.ToLower().Contains(filterText.ToLower()) || x.Description.ToLower().Contains(filterText.ToLower()));
                }

                // Apply sorting using dynamic LINQ
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    StkLines = StkLines.OrderBy($"{sortExpression} {sortDirection}");
                }

                GridItemsSelect.DataSource = StkLines.ToList();
                GridItemsSelect.DataBind();
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
        }

        protected void LoadDDs()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD").OrderBy(x => x.StoreDescript).ToList();
                DDStore.DataSource = Stores;
                DDStore.DataTextField = "StoreCode";
                DDStore.DataValueField = "StoreID";
                DDStore.DataBind();
                DDStore.Items.Insert(0, "-Store-");

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

        protected void lbtnAddSelected_Click(object sender, EventArgs e)
        {
            ProcessSelectedRows();
            LoadCountList();
        }

        protected void ProcessSelectedRows()
        {
            int previd = -1;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Pull all existing lines for this count once — outside the loop
                var existingLines = _db.StockCountLines.Where(l => l.CompanyID == CurrentUser.CoID && l.CountID == countid).Select(l => new { l.ItemID, l.StoreID }).ToList();

                foreach (GridViewRow row in GridItemsSelect.Rows)
                {
                    CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                    if (chkSelect != null && chkSelect.Checked)
                    {
                        if (!int.TryParse(row.Cells[0].Text.Trim(), out int itmid)) continue;

                        if (previd != itmid)
                        {
                            var storesList = _db.GetItemLinkedStores(CurrentUser.CoID, itmid).ToList();

                            foreach (var stor in storesList)
                            {
                                // Skip if line already exists — in memory check, no DB hit
                                bool lineExists = existingLines.Any(l =>l.ItemID == itmid && l.StoreID == stor.StoreID);

                                if (lineExists) continue;

                                StockCountLine newLine = new StockCountLine
                                {
                                    CompanyID = CurrentUser.CoID,
                                    CountID = Convert.ToInt32(lblCountID.Text),
                                    ItemID = itmid,
                                    StoreID = stor.StoreID,
                                    ItemCode = row.Cells[2].Text,
                                    ItemDescription = row.Cells[3].Text,
                                    StoreCode = stor.StoreCode,
                                    QtyOnHand = stor.QOH ?? 0
                                };
                                _db.StockCountLines.Add(newLine);
                            }

                            try
                            {
                                _db.SaveChanges();
                            }
                            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                            {
                                var errors = new System.Text.StringBuilder();
                                foreach (var eve in ex.EntityValidationErrors)
                                {
                                    foreach (var ve in eve.ValidationErrors)
                                    {
                                        errors.AppendLine($"Property: {ve.PropertyName} — Error: {ve.ErrorMessage}");
                                    }
                                }
                                string str = errors.ToString().Replace(Environment.NewLine, "<br/>");
                                return;
                            }
                            previd = itmid;
                        }
                    }
                }
            }
        }

        protected void LoadCountList()
        {
            long IntCnt = Convert.ToInt64(lblCountID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var countList = _db.StockCountLines.Where(x=>x.CompanyID == CurrentUser.CoID && x.CountID == IntCnt).ToList();
                GridCntLines.DataSource = countList;
                GridCntLines.DataBind();
            }
        }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            if (txtRef.Text.ToString().Length < 3)
            {
                AlertHelper.ShowSweetAlert(this, "Reference must be at least 3 characters.");
                return;
            }
            if (GridCntLines.Rows.Count == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Please capture at least 1 item to count.");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                countid = Convert.ToInt32(lblCountID.Text);
                var CntHead = _db.StockCountMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == countid);
                if (CntHead != null)
                {
                    CntHead.CtDescription = txtRef.Text;
                    CntHead.CtCreateDate = DateTime.Now;
                    _db.Entry(CntHead).State = EntityState.Modified;
                    _db.SaveChanges();
                    LinkButton btn = sender as LinkButton;
                    if (btn != null && btn.ID == "lbtnSave")
                    {
                        AlertHelper.ShowSweetAlert(this, "Stock Count saved successfully.", "success");
                        Response.Redirect("~/StockCounts.aspx?user=" + CurrentUser.UserGuiD, false);
                    }
                }
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
            lbtnSave_Click(sender, EventArgs.Empty);
            DownloadStockCountSheet(countid);
        }

        private void DownloadStockCountSheet(int countID)
        {
            // Pull all lines for this count from DB
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.GetStckCountDetails(CurrentUser.CoID, countID)
                                .OrderBy(l => l.ItemCode)
                                .ToList();

                if (!lines.Any()) return;

                using (var wb = new XLWorkbook())
                {
                    // Group lines by StoreID — one tab per warehouse
                    var grouped = lines.GroupBy(l => l.StoreCode);

                    foreach (var storeGroup in grouped)
                    {
                        // Get store code for tab name — use StoreID as fallback
                        string tabName = storeGroup.First().StoreCode
                                            ?? storeGroup.Key.ToString();

                        // Sanitise tab name — Excel tab names max 31 chars, no special chars
                        tabName = tabName.Length > 31
                                    ? tabName.Substring(0, 31)
                                    : tabName;

                        var ws = wb.Worksheets.Add(tabName);

                        // ── Header block ────────────────────────────────────────
                        ws.Cell("A1").Value = "Stock Count Sheet";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 14;

                        ws.Cell("A2").Value = "Count:";
                        ws.Cell("B1").Value = countID;
                        ws.Cell("B2").Value = lines[0].CtDescription.ToString(); ;
                        ws.Cell("A3").Value = "Warehouse:";
                        ws.Cell("B3").Value = tabName;
                        ws.Cell("A4").Value = "Date:";
                        ws.Cell("B4").Value = DateTime.Now.ToString("dd MMM yyyy");
                        ws.Cell("A5").Value = "Counted by:";
                        ws.Cell("B5").Value = ""; // blank — counter fills in

                        ws.Range("A2:A5").Style.Font.Bold = true;
                        ws.Range("A2:A5").Style.Font.FontSize = 10;
                        ws.Range("B2:B5").Style.Font.FontSize = 10;

                        // ── Column headers ───────────────────────────────────────
                        int headerRow = 7;

                        ws.Cell(headerRow, 1).Value = "Category";
                        ws.Cell(headerRow, 2).Value = "Code";
                        ws.Cell(headerRow, 3).Value = "Item Description";
                        ws.Cell(headerRow, 4).Value = "Count 1";
                        ws.Cell(headerRow, 5).Value = "Count 2";
                        ws.Cell(headerRow, 6).Value = "Final";

                        var headerRange = ws.Range(headerRow, 1, headerRow, 5);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontSize = 10;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

                        // ── Data rows ────────────────────────────────────────────
                        int dataRow = headerRow + 1;

                        foreach (var line in storeGroup)
                        {
                            ws.Cell(dataRow, 1).Value = line.CategoryDescript ?? "";
                            ws.Cell(dataRow, 2).Value = line.ItemCode ?? "";
                            ws.Cell(dataRow, 3).Value = line.ItemDescription ?? "";
                            ws.Cell(dataRow, 4).Value = line.Count1Qty.HasValue ? line.Count1Qty.Value.ToString("0.##") : ""; // Count 1 — blank if no value
                            ws.Cell(dataRow, 5).Value = line.Count2Qty.HasValue ? line.Count2Qty.Value.ToString("0.##") : ""; // Count 2 — blank if no value
                            ws.Cell(dataRow, 6).Value = line.FinalQty.HasValue ? line.FinalQty.Value.ToString("0.##") : ""; // Final — blank if no value

                            // Alternating row colour for readability
                            if (dataRow % 2 == 0)
                            {
                                ws.Range(dataRow, 1, dataRow, 6)
                                    .Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                            }

                            ws.Range(dataRow, 1, dataRow, 6)
                                .Style.Font.FontSize = 10;
                            ws.Range(dataRow, 1, dataRow, 6)
                                .Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            ws.Range(dataRow, 1, dataRow, 6)
                                .Style.Border.BottomBorderColor = XLColor.FromArgb(217, 217, 217);

                            dataRow++;
                        }

                        // ── Count columns — bordered input cells ─────────────────
                        // Make Count 1 and Count 2 visually distinct as input cells
                        var countRange = ws.Range(headerRow + 1, 4, dataRow - 1, 6);
                        countRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 204);
                        countRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        countRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(68, 114, 196);

                        // ── Column widths ─────────────────────────────────────────
                        ws.Column(1).Width = 22; // Category
                        ws.Column(2).Width = 14; // Code
                        ws.Column(3).Width = 40; // Description
                        ws.Column(4).Width = 14; // Count 1
                        ws.Column(5).Width = 14; // Count 2
                        ws.Column(6).Width = 14; // Final

                        // ── Freeze header rows ────────────────────────────────────
                        ws.SheetView.FreezeRows(headerRow);

                        // ── Print setup ───────────────────────────────────────────
                        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
                        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                        ws.PageSetup.FitToPages(1, 0); // Fit width to 1 page, height flows
                        ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
                        ws.PageSetup.ShowGridlines = true;

                        // Print header on every page
                        ws.PageSetup.Header.Left.AddText(
                            $"Stock Count — {tabName} — {DateTime.Now:dd MMM yyyy}");
                        ws.PageSetup.Footer.Right.AddText("Page ");
                        ws.PageSetup.Footer.Right.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
                        ws.PageSetup.Footer.Right.AddText(" of ");
                        ws.PageSetup.Footer.Right.AddText(XLHFPredefinedText.NumberOfPages, XLHFOccurrence.AllPages);

                        // ── Lock everything except Count 1 and Count 2 ────────────
                        ws.Protect("stockcount");
                        ws.Range(headerRow + 1, 4, dataRow - 1, 6).Style.Protection.Locked = false;
                    }

                    // ── Stream to browser ─────────────────────────────────────────
                    string fileName = $"StockCount_{countID}_{DateTime.Now:yyyyMMdd}.xlsx";

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