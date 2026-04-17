using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCounts : BasePage
    {

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

            if (!IsPostBack)
            {
                if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");
                lblUsername.Text = $":.. {CurrentUser.UserName}..: ";
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

                showhidebuttons();
                LoadOpenCounts();
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

        protected void LoadOpenCounts()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var StkCnts = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ClosedOff != true).ToList();
                DDStckCount.DataSource = StkCnts;
                DDStckCount.DataTextField = "CtDescription";
                DDStckCount.DataValueField = "StCntID";
                DDStckCount.DataBind();
                DDStckCount.Items.Insert(0, "-Select-");

                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD").OrderBy(x=>x.StoreDescript).ToList();
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

       protected void LoadCount()
        {
            ApplyFilterAndSort();
        }

        protected void ApplyFilterAndSort(string sortExpression = null, string sortDirection = "ASC")
        {
            int CntID = Convert.ToInt32(DDStckCount.SelectedValue);         
            string filterText = txtFilter.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var CntLines = _db.GetStckCountDetails(CurrentUser.CoID, CntID).ToList();

                if (!CntLines.Any()) return;

                lblRef.Text = CntLines[0].CtDescription?.ToString();
                lblDate.Text = CntLines[0].CtCreateDate?.ToString("dd MMM yyyy");

                // Apply filters
                var filtered = CntLines.AsQueryable();

                if (DDCateg.SelectedIndex > 0)
                {
                    string selectedCategory = DDCateg.SelectedValue;
                    filtered = filtered.Where(x => x.CategoryDescript == selectedCategory);
                }

                if (DDStore.SelectedIndex > 0)
                {
                    string selectedStore = DDStore.SelectedItem.Text;
                    filtered = filtered.Where(x => x.StoreCode == selectedStore);
                }

                if (!string.IsNullOrEmpty(filterText))
                {
                    filtered = filtered.Where(x => x.ItemCode.Contains(filterText)
                                                || x.ItemDescription.Contains(filterText));
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    filtered = filtered.OrderBy($"{sortExpression} {sortDirection}");
                }

                GridCntLines.DataSource = filtered.ToList();
                GridCntLines.DataBind();
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

        protected void DDStckCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDStckCount.SelectedIndex > 0)
            {
                DDSelect.SelectedIndex = 0;
                LoadCount();
            }
        }

        protected void GridCntLines_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
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
 
        protected void DDSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                if (DDSelect.SelectedValue == "0") // download count sheet
                {
                    Response.Redirect($"~/StockCountCreate.aspx?user={CurrentUser.UserGuiD}&id=0", true);
                }
                else if (DDSelect.SelectedValue == "1") // download count sheet
                {
                    if (DDStckCount.SelectedIndex > 0)
                    {
                        Response.Redirect($"~/StockCountCreate.aspx?user={CurrentUser.UserGuiD}&id={Convert.ToInt32(DDStckCount.SelectedValue)}", true);
                    } else
                    {
                        AlertHelper.ShowSweetAlert(this, "Please select a Stock Count");
                        return;
                    }
                } 
                else if (DDSelect.SelectedValue == "2") // download count sheet
                {
                    if (DDStckCount.SelectedIndex > 0)
                    {
                        DownloadStockCountSheet(Convert.ToInt32(DDStckCount.SelectedValue));
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Please select a Stock Count");
                        return;
                    }
                }
                else if (DDSelect.SelectedValue == "3")
                {
                    Response.Redirect("~/StockCountUpload.aspx?user=" + CurrentUser.UserGuiD, true);
                }
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        private void DownloadStockCountSheet(int countID)
        {
            // Pull all lines for this count from DB
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.GetStckCountDetails(CurrentUser.CoID,countID)
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
                        ws.Cell("B1").Value = DDStckCount.SelectedValue.ToString();
                        ws.Cell("B2").Value = DDStckCount.SelectedItem.Text.ToString(); ;
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

        protected void lbtnHome_Click1(object sender, EventArgs e)
        {

        }
    }
}