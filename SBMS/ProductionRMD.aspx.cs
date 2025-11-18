using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ProductionRMD : BasePage
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
        public class ItemsList
        {
            public long ItemID { get; set; }
            public string descript { get; set; }
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
            sb.Append("],");
            sb.Append("cols: [");
            sb.Append("\"Year_Mth_Week\"");
            sb.Append("],");
            sb.Append("vals: [");
            sb.Append("\"Qty\"");
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
            string inputpath = "inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "") + "-RMDemands.csv";
            Page.ClientScript.RegisterStartupScript(this.GetType(), "CreatePivotTable", "createPivotTable('" + inputpath + "');", true);
        }

        protected string CollectData()
        {
            string filePath = Server.MapPath("~/inputcsv/"+ CurrentUser.UserGuiD.ToString().Replace(" ","") + "-RMDemands.csv");
            if (File.Exists(filePath)) File.Delete(filePath);

           using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ProdDemands = _db.GetProdRMDemands(CurrentUser.CoID).ToList();
                DateTime earliestDueDate = DateTime.Today;
                try
                {
                    earliestDueDate = (DateTime)ProdDemands.Min(d => d.Due_Date);
                }
                catch { }
                WriteToCsv(ProdDemands.OrderBy(x=>x.Due_Date).ToList(), filePath, earliestDueDate.AddDays(-7));
            }
                return "OK";
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            DataTable planLinesDataTable = GetDemands();
            planLinesDataTable.Columns.Remove("Year_Mth_Week");
            ExcelHelper.ExportToExcel(planLinesDataTable, "RawMaterialDemands", "RawMaterialDemands");
        }

        public DataTable GetDemands()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ProdDemands = _db.GetProdRMDemands(CurrentUser.CoID).ToList();
                DateTime earliestDueDate = (DateTime)ProdDemands.Min(d => d.Due_Date);
                return DataTableHelper.ConvertToDataTable(ProdDemands);
            }    
        }
        public static void WriteToCsv<T>(List<T> items, string filePath, DateTime StDate, bool append = false)
        {
            StringBuilder csvContent = new StringBuilder();

            // Get all the properties
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Write the header if not appending
            if (!append)
            {
                csvContent.AppendLine(string.Join(",", props.Select(p => p.Name)));
            }

            // Write the data rows
            foreach (var item in items)
            {
                var values = props.Select(p =>
                {
                    var value = p.GetValue(item, null);     
                    if (p.Name == "Year_Mth_Week")
                    {
                        DateTime dateValue = StDate;
                        try
                        {
                            dateValue = Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch { }

                        if (dateValue == StDate)
                        {
                           return "0n Hand";
                        }
                        else
                        {
                            // Custom date format "yyyy-MM" + "weeknumber"
                            var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                dateValue,
                                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday);

                            return dateValue.ToString($"yyyy-MM") + $" Week{weekNumber}";
                        }
                    } else
                    if (value is DateTime dateValue2)
                    {
                        return dateValue2.ToString($"dd MMM yyyy");
                    }
                  return value?.ToString().Replace(",", " ");
                }).ToArray();

                if (values[10] == "WO")
                {
                    values[7] = "-" + values[7].ToString();
                }
                csvContent.AppendLine(string.Join(",", values));
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

        protected async void lbtnRefresh_Click(object sender, EventArgs e)
        {
            ApiUrlCall api = new ApiUrlCall();
            var errors = await api.LoadItems(CurrentUser);
            lbtnViewAnalysis_Click(sender, EventArgs.Empty);
        }

        protected void lbtnGenerate_Click(object sender, EventArgs e)
        {

        }

        public class RMDemandLines
        {
            //public long ItemID { get; set; }
            public string FGItemCode { get; set; }
            public string FGGDescription { get; set; }
            public string FGCategory { get; set; }
            public decimal FGPlanQty { get; set; }
            public DateTime Year_Mth_Week { get; set; }
           public DateTime Due_Date { get; set; }
            public string RMItemCode { get; set; }
            public string RMItemdescription { get; set; }
            public decimal DemandQty { get; set; }
            public decimal BalanceQty { get; set; }
            public decimal QtyOnPO { get; set; }

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
    }
}