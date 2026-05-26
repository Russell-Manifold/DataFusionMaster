using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockEnquiry : BasePage
    {
        static bool useLots = true;
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
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                useLots = false;
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
                lbtnViewAnalysis_Click(sender, EventArgs.Empty);
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

        protected void lbtnViewAnalysis_Click(object sender, EventArgs e)
        {
            CollectData();
            StringBuilder sb = new StringBuilder();
            sb.Append("<script type=" + "\"text/javascript\">");
            sb.Append("var derivers = $.pivotUtilities.derivers;");
            sb.Append("var sortAs  = $.pivotUtilities.sortAs;");
            sb.Append("var renderers = $.extend($.pivotUtilities.renderers,");
            sb.Append("$.pivotUtilities.plotly_renderers,");
            sb.Append(" $.pivotUtilities.export_renderers);");

            sb.Append("function createPivotTable(abc){");
            sb.Append("$(function () {");
            sb.Append("$(\"#pivotoutput\").html(\"" + "<div style='text-align:center; background-color:#f9f9f9'><img src='images/tenorwait.gif' id='myAnimatedImage' align='absmiddle' class='funkygif'/></div>\");");
            sb.Append("Papa.parse(abc,{");
            sb.Append("download: true,");
            sb.Append("skipEmptyLines: true,");
            sb.Append("complete: function(parsed) {");

            sb.Append("$('#pivotoutput').pivotUI(");
            sb.Append("parsed.data,");
            sb.Append("{");

            sb.Append("rows: [");
            sb.Append("\"ItemCode\"");
            sb.Append(",");
            sb.Append("\"Description\"");
            sb.Append(",");
            sb.Append("\"LotNumber\"");
            sb.Append("],");
            sb.Append("cols: [");
            sb.Append("\"StoreCode\"");
            sb.Append("],");
            sb.Append("vals: [");
            sb.Append("\"Sum_Qty\"");
            sb.Append("],");
            sb.Append("aggregatorName: [");
            sb.Append("\"Sum\"");
            sb.Append("],");
            sb.Append("rendererName: [");
            sb.Append("\"Table\"");
            sb.Append("]");
            sb.Append("});");
            sb.Append("}");
            sb.Append("});");
            sb.Append("});");
            sb.Append("};");
            sb.Append("</script>");
            Literal1.Text = sb.ToString();
            string inputpath = "inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-StkEnq.csv";
            Page.ClientScript.RegisterStartupScript(this.GetType(), "CreatePivotTable", "createPivotTable('" + inputpath + "');", true);
        }

        protected string CollectData()
        {
            string filePath = Server.MapPath("~/inputcsv/"
                + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "")
                + "-StkEnq.csv");

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string searchTerm = txtfind.Text.Trim().Length > 0
                    ? txtfind.Text.Trim()
                    : null;

                int Coid = Convert.ToInt32(CurrentUser.CoID);
                var CurrStock = _db.GetAllStockLevels(Coid, searchTerm).ToList();

                WriteToCsv(CurrStock, filePath);
            }

            return "OK";
        }

        //protected string CollectData()
        //{
        //    string filePath = Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-StkEnq.csv");
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //       var CurrStock = _db.GetAllStockLevels(CurrentUser.CoID).ToList();
        //       long itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID).Select(x => x.ID).FirstOrDefault(); // need to fix here.
        //        if (txtfind.Text.Trim().Length > 0)
        //        {
        //            CurrStock = CurrStock.Where(x => x.ItemCode.ToLower().Contains(txtfind.Text.Trim().ToLower()) || (x.Description != null && x.Description.ToLower().Contains(txtfind.Text.Trim().ToLower()))).ToList();
        //        }
        //        WriteToCsv(CurrStock, filePath);
        //    }
        //    return "OK";
        //}

        public static void WriteToCsv<T>(List<T> items, string filePath, bool append = false)
        {
            StringBuilder csvContent = new StringBuilder();

            // Get all the properties
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Write the header if not appending
            if (!append)
            {
                if ((bool)useLots)
                {
                    csvContent.AppendLine(string.Join(",", props.Select(p => p.Name)));
                } else
                {
                    csvContent.AppendLine(string.Join(",", props.Select((p, index) => index != 5 ? p.Name : null).Where(name => name != null)));
                }
            }

            // Write the data rows
            foreach (var item in items)
            {
                if ((bool)useLots)
                {
                    var values = props.Select(p =>
                    {
                        var value = p.GetValue(item, null);
                        return value?.ToString().Replace(",", " ");
                    }).ToArray();
                    csvContent.AppendLine(string.Join(",", values));
                }
                else
                {
                    var values = props.Select((p, index) =>
                    {
                        if (index == 5) return null;
                        var value = p.GetValue(item, null);
                        return value?.ToString().Replace(",", " ");
                    })
                    .Where(v => v != null) // Exclude null values
                    .ToArray();
                    csvContent.AppendLine(string.Join(",", values));
                }
            }

            // Write to file
            if (append)
            {
                File.AppendAllText(filePath, csvContent.ToString());
            }
            else
            {
                File.WriteAllText(filePath, csvContent.ToString());
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
            lbtnViewAnalysis_Click(sender, EventArgs.Empty);
        }
    }
}