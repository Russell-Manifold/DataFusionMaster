using AjaxControlToolkit.HtmlEditor.ToolbarButtons;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Org.BouncyCastle.Asn1.Cmp;
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
                var StkCnts = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID).ToList();
                if (chkAll.Checked == true)
                {
                    StkCnts = StkCnts.Where(x => x.ClosedOff == true ).ToList();
                }else
                {
                    StkCnts = StkCnts.Where(x => x.ClosedOff == false || x.ClosedOff == null).ToList();
                }
                GridCntLines.DataSource = null;
                GridCntLines.DataBind();
                DDStckCount.DataSource = null;
                DDStckCount.DataBind();

                if (StkCnts.Count == 0)
                {
                    Response.Redirect($"~/StockCountCreate.aspx?user={CurrentUser.UserGuiD}&id=0", true);
                    return;
                }

                DDStckCount.DataSource = StkCnts;
                DDStckCount.DataTextField = "CtDescription";
                DDStckCount.DataValueField = "StCntID";
                DDStckCount.DataBind();

                if (StkCnts.Count >0)
                {
                    DDStckCount.Items.Insert(0, "-Select Count-");
                }
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
            GridCntLines.DataSource = null;
            GridCntLines.DataBind();
            ApplyFilterAndSort();
        }

        protected void ApplyFilterAndSort(string sortExpression = null, string sortDirection = "ASC")
        {
            int CntID = Convert.ToInt32(DDStckCount.SelectedValue);         
            string filterText = txtFilter.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var CntLines = _db.GetStckCountDetails(CurrentUser.CoID, CntID).ToList();

                if (!CntLines.Any())
                {
                    GridCntLines.DataSource = null;
                    GridCntLines.DataBind();
                    return;
                }

                lblRef.Text = CntLines[0].CtDescription?.ToString();
                lblDate.Text = CntLines[0].CtCreateDate?.ToString("dd MMM yyyy");
                lblCreatedBy.Text = CntLines[0].CreateBy?.ToString();
                lblStatus.Text = CntLines[0].ClosedOff == true ? "Closed" : "Open";
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

                var result = filtered.ToList();
                GridCntLines.DataSource = result.Any() ? result : null;
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

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var varianceVal = DataBinder.Eval(e.Row.DataItem, "Variance");
                decimal variance = (varianceVal != null && varianceVal != DBNull.Value)
                                   ? Convert.ToDecimal(varianceVal)
                                   : 0;

                if (variance < 0)
                {
                    e.Row.Cells[10].ForeColor = System.Drawing.Color.Red;
                    e.Row.Cells[10].Font.Bold = true;
                }
                else if (variance > 0)
                {
                    e.Row.Cells[10].ForeColor = System.Drawing.Color.Green; 
                    e.Row.Cells[10].Font.Bold = true;
                }
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
                else if (DDSelect.SelectedValue == "4")
                {
                    if (DDStckCount.SelectedIndex > 0)
                    {
                        Response.Redirect($"~/StockCountVariances.aspx?user={CurrentUser.UserGuiD}&id={Convert.ToInt32(DDStckCount.SelectedValue)}", true);
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Please select a Stock Count");
                        return;
                    }    
                }
                else if (DDSelect.SelectedValue == "5")
                {
                    if (DDStckCount.SelectedIndex > 0)
                    {
                        CloseOffStockCount(Convert.ToInt32(DDStckCount.SelectedValue));
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Please select a Stock Count");
                        DDSelect.SelectedIndex = 0;
                        return;
                    }
                }
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
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
                    // ── COVER PAGE — always first tab ─────────────────────────
                    var wsCover = wb.Worksheets.Add("Instructions");

                    // Title
                    wsCover.Cell("B2").Value = "Data Fusion — Stock Count Worksheet";
                    wsCover.Cell("B2").Style.Font.Bold = true;
                    wsCover.Cell("B2").Style.Font.FontSize = 18;
                    wsCover.Cell("B2").Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

                    // Count details block
                    wsCover.Cell("B4").Value = "Count Reference:";
                    wsCover.Cell("C4").Value = DDStckCount.SelectedItem.Text.ToString();
                    wsCover.Cell("B5").Value = "Count ID:";
                    wsCover.Cell("C5").Value = DDStckCount.SelectedValue.ToString();
                    wsCover.Cell("B6").Value = "Generated:";
                    wsCover.Cell("C6").Value = DateTime.Now.ToString("dd MMM yyyy HH:mm");
                    wsCover.Cell("B7").Value = "Generated By:";
                    wsCover.Cell("C7").Value = CurrentUser.UserName;

                    wsCover.Range("B4:B7").Style.Font.Bold = true;
                    wsCover.Range("B4:B7").Style.Font.FontSize = 11;
                    wsCover.Range("C4:C7").Style.Font.FontSize = 11;

                    // Divider
                    wsCover.Cell("B9").Value = "────────────────────────────────────────────────────────";
                    wsCover.Cell("B9").Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

                    // Section: What is this workbook
                    wsCover.Cell("B11").Value = "WHAT IS THIS WORKBOOK?";
                    wsCover.Cell("B11").Style.Font.Bold = true;
                    wsCover.Cell("B11").Style.Font.FontSize = 12;
                    wsCover.Cell("B11").Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

                    wsCover.Cell("B12").Value = "This workbook has been generated by Data Fusion for a blind stock count.";
                    wsCover.Cell("B13").Value = "Each store has its own tab. Count sheets must be printed per store and handed to the counter.";
                    wsCover.Cell("B14").Value = "Once counting is complete, quantities are captured back into this workbook and uploaded to Data Fusion.";
                    wsCover.Range("B12:B14").Style.Font.FontSize = 11;

                    // Section: Tabs
                    wsCover.Cell("B16").Value = "TABS IN THIS WORKBOOK";
                    wsCover.Cell("B16").Style.Font.Bold = true;
                    wsCover.Cell("B16").Style.Font.FontSize = 12;
                    wsCover.Cell("B16").Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);

                    var storeList = lines.Select(l => l.StoreCode).Distinct().OrderBy(x => x).ToList();
                    int tabRow = 17;
                    foreach (var store in storeList)
                    {
                        wsCover.Cell(tabRow, 2).Value = $"• {store}";
                        wsCover.Cell(tabRow, 3).Value = "— Count sheet for this store";
                        wsCover.Cell(tabRow, 2).Style.Font.FontSize = 11;
                        wsCover.Cell(tabRow, 3).Style.Font.FontSize = 11;
                        tabRow++;
                    }

                    // Section: Instructions
                    int instrRow = tabRow + 1;
                    wsCover.Cell(instrRow, 2).Value = "COUNTING INSTRUCTIONS";
                    wsCover.Cell(instrRow, 2).Style.Font.Bold = true;
                    wsCover.Cell(instrRow, 2).Style.Font.FontSize = 12;
                    wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);
                    instrRow++;

                    var instructions = new[]
                    {
                "1.  Print the tab for your store only. Do not share other store tabs with counters.",
                "2.  Count each item physically and write the quantity in the COUNT 1 column.",
                "3.  A second counter must independently recount and record quantities in the COUNT 2 column.",
                "4.  If COUNT 1 and COUNT 2 differ, recount and record the final agreed quantity in FINAL QTY.",
                "5.  Return the completed sheet to the data capturer.",
                "6.  The data capturer enters the counted quantities into the yellow cells on each store tab.",
                "7.  Once all stores are captured, upload this workbook to Data Fusion to generate variances."
            };

                    foreach (var instruction in instructions)
                    {
                        wsCover.Cell(instrRow, 2).Value = instruction;
                        wsCover.Cell(instrRow, 2).Style.Font.FontSize = 11;
                        instrRow++;
                    }

                    // Section: What you CAN do
                    instrRow++;
                    wsCover.Cell(instrRow, 2).Value = "WHAT YOU CAN DO";
                    wsCover.Cell(instrRow, 2).Style.Font.Bold = true;
                    wsCover.Cell(instrRow, 2).Style.Font.FontSize = 12;
                    wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.Green;
                    instrRow++;

                    var canDo = new[]
                    {
                "✓  Enter quantities in the yellow COUNT 1, COUNT 2, and FINAL QTY cells.",
                "✓  Print any store tab for your counters.",
                "✓  Save this workbook locally while capturing quantities.",
                "✓  Upload this workbook back to Data Fusion once all quantities are captured."
            };

                    foreach (var item in canDo)
                    {
                        wsCover.Cell(instrRow, 2).Value = item;
                        wsCover.Cell(instrRow, 2).Style.Font.FontSize = 11;
                        wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.FromArgb(0, 128, 0);
                        instrRow++;
                    }

                    // Section: What you CANNOT do
                    instrRow++;
                    wsCover.Cell(instrRow, 2).Value = "WHAT YOU CANNOT DO";
                    wsCover.Cell(instrRow, 2).Style.Font.Bold = true;
                    wsCover.Cell(instrRow, 2).Style.Font.FontSize = 12;
                    wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.Red;
                    instrRow++;

                    var cannotDo = new[]
                    {
                "✗  Do not add, delete, or move any rows or columns — the upload will fail.",
                "✗  Do not rename or delete any store tabs — the upload uses tab names to match stores.",
                "✗  Do not change item codes or descriptions — these are used to match counted quantities.",
                "✗  Do not edit cells outside the yellow input columns.",
                "✗  Do not share system quantity information with counters — this is a blind count.",
                "✗  Do not upload a modified or reformatted version of this workbook."
            };

                    foreach (var item in cannotDo)
                    {
                        wsCover.Cell(instrRow, 2).Value = item;
                        wsCover.Cell(instrRow, 2).Style.Font.FontSize = 11;
                        wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.Red;
                        instrRow++;
                    }

                    // Section: Upload instructions
                    instrRow++;
                    wsCover.Cell(instrRow, 2).Value = "HOW TO UPLOAD";
                    wsCover.Cell(instrRow, 2).Style.Font.Bold = true;
                    wsCover.Cell(instrRow, 2).Style.Font.FontSize = 12;
                    wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.FromArgb(68, 114, 196);
                    instrRow++;

                    var uploadSteps = new[]
                    {
                "1.  Ensure all store tabs have been captured.",
                "2.  Save this workbook without renaming it.",
                "3.  Log into Data Fusion and navigate to Stock Control → Stock Counts.",
                "4.  Select this count from the dropdown and choose 'Upload Counted Quantities'.",
                "5.  Drop or browse to this workbook file and click Upload.",
                "6.  Data Fusion will automatically read all store tabs and update counted quantities.",
                "7.  Review the variance report and approve adjustments."
            };

                    foreach (var step in uploadSteps)
                    {
                        wsCover.Cell(instrRow, 2).Value = step;
                        wsCover.Cell(instrRow, 2).Style.Font.FontSize = 11;
                        instrRow++;
                    }

                    // Warning box
                    instrRow++;
                    var warningRange = wsCover.Range(instrRow, 2, instrRow + 1, 5);
                    warningRange.Merge();
                    warningRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 235, 156);
                    warningRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    warningRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(255, 192, 0);
                    warningRange.Style.Alignment.WrapText = true;
                    warningRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    wsCover.Cell(instrRow, 2).Value =
                        "⚠  IMPORTANT: Stock movements (receipts, dispatches, transfers) should not occur " +
                        "between count creation and upload. Any movements during the count period will " +
                        "affect variance accuracy.";
                    wsCover.Cell(instrRow, 2).Style.Font.FontSize = 11;
                    wsCover.Cell(instrRow, 2).Style.Font.Bold = true;
                    wsCover.Cell(instrRow, 2).Style.Font.FontColor = XLColor.FromArgb(156, 87, 0);
                    wsCover.Row(instrRow).Height = 30;
                    wsCover.Row(instrRow + 1).Height = 20;

                    // Cover page column widths
                    wsCover.Column(1).Width = 4;
                    wsCover.Column(2).Width = 80;
                    wsCover.Column(3).Width = 40;

                    // Protect cover page — fully locked, no editing
                    wsCover.Protect("stockcount");

                    // ── One tab per store ─────────────────────────────────
                    var grouped = lines.GroupBy(l => l.StoreCode);

                    foreach (var storeGroup in grouped)
                    {
                        string tabName = storeGroup.First().StoreCode ?? storeGroup.Key.ToString();
                        tabName = tabName.Length > 31 ? tabName.Substring(0, 31) : tabName;

                        var ws = wb.Worksheets.Add(tabName);

                        // ── Header block ──────────────────────────────────────
                        ws.Cell("A1").Value = "Stock Count Sheet";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 14;
                        ws.Cell("A2").Value = "Count:";
                        ws.Cell("B1").Value = DDStckCount.SelectedValue.ToString();
                        ws.Cell("B2").Value = DDStckCount.SelectedItem.Text.ToString();
                        ws.Cell("A3").Value = "store:";
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
                        ws.Cell(headerRow, col++).Value = "Count 1";
                        ws.Cell(headerRow, col++).Value = "Count 2";
                        ws.Cell(headerRow, col++).Value = "Final Qty";

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
                            ws.Cell(dataRow, 1).Value = line.CategoryDescript ?? "";
                            ws.Cell(dataRow, 2).Value = line.ItemCode ?? "";
                            ws.Cell(dataRow, 3).Value = line.ItemDescription ?? "";
                            ws.Cell(dataRow, 4).Value = line.Count1Qty.HasValue
                                                        ? line.Count1Qty.Value : (decimal?)null;
                            ws.Cell(dataRow, 5).Value = line.Count2Qty.HasValue
                                                        ? line.Count2Qty.Value : (decimal?)null;
                            ws.Cell(dataRow, 6).Value = line.FinalQty.HasValue
                                                        ? line.FinalQty.Value : (decimal?)null;

                            ws.Range(dataRow, 4, dataRow, 6)
                              .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

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

                        // ── Input cell styling ────────────────────────────────
                        var inputRange = ws.Range(headerRow + 1, 4, dataRow - 1, 6);
                        inputRange.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 204);
                        inputRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                        inputRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(68, 114, 196);

                        // ── Column widths ─────────────────────────────────────
                        ws.Column(1).Width = 22;
                        ws.Column(2).Width = 14;
                        ws.Column(3).Width = 40;
                        ws.Column(4).Width = 14;
                        ws.Column(5).Width = 14;
                        ws.Column(6).Width = 14;

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

                        // ── Protect — input columns unlocked ─────────────────
                        ws.Protect("stockcount");
                        ws.Range(headerRow + 1, 4, dataRow - 1, 6)
                          .Style.Protection.Locked = false;
                    }

                    // ── Stream to browser ─────────────────────────────────────
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

        private void CloseOffStockCount(int countID)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var StCount = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StCntID == countID).FirstOrDefault();
                StCount.ClosedBy = CurrentUser.RoleID;
                StCount.ClosedDate = DateTime.Now;
                StCount.ClosedOff = true;   
                _db.SaveChanges();
                GridCntLines.DataSource = null;
                GridCntLines.DataBind();
                AlertHelper.ShowSweetAlert(this, "Stock Count closed off successfully");
                    LoadOpenCounts();
            }
         }

        protected void chkAll_CheckedChanged(object sender, EventArgs e)
        {
           if (DDStckCount.Items.Count > 0) DDStckCount.SelectedIndex = 0;
           if (DDSelect.Items.Count >0) DDSelect.SelectedIndex = 0;
           LoadOpenCounts();   
        }
    }
}