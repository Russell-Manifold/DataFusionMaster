using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockEnquiry : BasePage
    {
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
                lblUsername.Text = $":..  {CurrentUser.UserName} ..: ";
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

                // Lots only for companies that use them. Categories come from the stock itself.
                chkLots.Visible = CurrentUser.CompanyUseLotNumbers;
                var rows = LoadRows();
                ddlCategory.Items.Add(new ListItem("All", ""));
                foreach (string c in rows.Select(r => r.CategoryDescript ?? "").Where(c => c != "").Distinct().OrderBy(c => c))
                    ddlCategory.Items.Add(c);
                BindStock(rows);
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

        // One row per item (per item + lot when "Show lots" is ticked), one column per store, then a
        // Total (Classes/QtyMatrix). Built from GetAllStockLevels - the same data the old pivot used.
        private int firstQtyCol;

        private List<GetAllStockLevels_Result> LoadRows()
        {
            string find = txtfind.Text.Trim();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                return _db.GetAllStockLevels(Convert.ToInt32(CurrentUser.CoID), find.Length > 0 ? find : null).ToList();
        }

        private void BindStock(List<GetAllStockLevels_Result> rows)
        {
            if (rows == null) rows = LoadRows();
            string cat = ddlCategory.SelectedValue;
            if (cat != "") rows = rows.Where(r => (r.CategoryDescript ?? "") == cat).ToList();
            bool lots = CurrentUser.CompanyUseLotNumbers && chkLots.Checked;

            DataTable dt = QtyMatrix.Build(
                rows.Select(r => new QtyMatrix.Line { Code = r.ItemCode, Description = r.Description, Lot = r.LotNumber, Column = r.StoreCode, Qty = r.Sum_Qty ?? 0 }),
                lots, chkHideZero.Checked, false, null, null, "Total", out firstQtyCol);
            GridStock.DataSource = QtyMatrix.Sorted(dt, ViewState["SortCol"] as string, ViewState["SortDesc"] as bool? == true);
            GridStock.DataBind();
            lblInfo.Text = $"{dt.Rows.Count:N0} {(lots ? "item / lot row(s)" : "item(s)")} in {dt.Columns.Count - firstQtyCol - 1} store(s)";
        }

        protected void GridStock_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            QtyMatrix.FormatRow(e, firstQtyCol);
        }

        protected void GridStock_Sorting(object sender, GridViewSortEventArgs e)
        {
            // First click on a column sorts it ascending, the next click descending.
            bool desc = ViewState["SortCol"] as string == e.SortExpression && ViewState["SortDesc"] as bool? != true;
            ViewState["SortCol"] = e.SortExpression;
            ViewState["SortDesc"] = desc;
            GridStock.PageIndex = 0;
            BindStock(null);
        }

        protected void GridStock_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridStock.PageIndex = e.NewPageIndex;
            BindStock(null);
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            GridStock.PageIndex = 0;
            BindStock(null);
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

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {    
            XLWorkbook wb = new XLWorkbook();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string searchTerm = txtfind.Text.Trim().Length > 0
                    ? txtfind.Text.Trim()
                    : null;
                int Coid = Convert.ToInt32(CurrentUser.CoID);
                var stockLevels = _db.GetAllStockLevels(Coid, searchTerm).ToList();
                DataTable data = stockLevels.ToDataTable();
           
                /// // Add pivot table sheet
                var pivotSheet = wb.Worksheets.Add("Stock_Enquiry");
                ///// add source data
                var dataSheet = wb.Worksheets.Add("SourceData");
                dataSheet.Cell(1, 1).InsertTable(data);

                // Create pivot table
                var pivotTable = pivotSheet.PivotTables.Add("PivotTable", pivotSheet.Cell("A1"), dataSheet.RangeUsed());
                // Add values to pivot table
                var NameField = pivotTable.RowLabels.Add("ItemCode");
                var NameField2 = pivotTable.RowLabels.Add("Description");
                var NameField3 = pivotTable.RowLabels.Add("LotNumber");
                var MthYrField = pivotTable.ColumnLabels.Add("StoreCode");
                var amountField = pivotTable.Values.Add("Sum_Qty");

                amountField.SummaryFormula = XLPivotSummary.Sum;
                //dataSheet.Visibility = XLWorksheetVisibility.Hidden;

                wb.SaveAs(Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-StkEnq.xlsx"));
                string file = "Stock_Enquiry.xlsx";
                string filepath = (Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-StkEnq.xlsx"));
                Response.Clear();
                Response.ContentType = "application/vnd.ms-excel";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.TransmitFile(filepath);
                Response.Flush();
                Response.End();
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

        protected void lbtnSOH_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockEnquiry.aspx?user=" + CurrentUser.UserGuiD, false);
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

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            GridStock.PageIndex = 0;
            BindStock(null);
        }
    }
}