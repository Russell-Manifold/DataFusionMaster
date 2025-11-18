using ClosedXML.Excel;
using SBMS.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class DashboardSales : BasePage
    {
        string userid = "";
        byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
        byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };
        Boolean errfirstuser = false;

        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (customerSalesGridView.Rows.Count > 0)
            {
                foreach (GridViewRow row in customerSalesGridView.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(customerSalesGridView, "Select$" + row.RowIndex, true));
                    }
                }
            }
            base.Render(writer);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int dashid = 0;
            userid = Request.QueryString["user"].ToString();
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            MaintainScrollPositionOnPostBack = true;
            lblUserName.Text = $":.. {CurrentUser.UserName} ..:";
            if (!IsPostBack)
            {
                string CoID = "";
                string cacheKey = $"DashSales_{CoID}";
                if (ApiUrlCall.CheckForInternetConnection())
                {
                    try
                    {
                        if (ApiUrlCall.constr.ToLower().Contains("demo"))
                        {
                            lblUserName.Text = CapitalizeFirstLetter(Session["user"].ToString().Split('@')[0].ToString()).Substring(0, 2) + "...";
                        }
                        else
                        {
                            lblUserName.Text = CapitalizeFirstLetter(Session["user"].ToString().Split('@')[0].ToString());
                        }
                    }
                    catch { }

                    if (Cache[cacheKey] == null)
                    {
                        LoadInvoices();
                        // If not, fetch data and cache it
                        List<DashSalesModel> dashSales = GetDashSalesData();
                        LoadCharts(dashSales);
                    }
                    else
                    {
                        // If data is already cached, retrieve it
                        List<DashSalesModel> dashSales = Cache[cacheKey] as List<DashSalesModel>;
                        SalesQty(dashSales);
                        SalesRev(dashSales);
                        SalesCust(dashSales);
                    }
                        
                }
                else
                {
                    ShowMessage(sender, EventArgs.Empty, "No internet connection, Unable to continue");
                }
            }
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

        private async Task LoadInvoices()
        {
            //string dbpath = Server.MapPath("~/DataInsightsAPI/App_Data/" + Request.QueryString["userid"].ToString());
            await ApiUrlCall.LoadCustAdjustments(CurrentUser);
            await ApiUrlCall.LoadInvoices(CurrentUser);
            await ApiUrlCall.LoadSalesCreditNotes(CurrentUser);
        }

       protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.SetSQLDataFromString("Update dbo.Users SET IsLoggedIn = 0, LoggedInSessionID = '" + string.Empty + "' WHERE userguid = '" + userid + "'");
            Response.Redirect("~/SageCloudDIIndex.aspx", true);
        }

        protected void BtnAdvPivot_Click(object sender, EventArgs e)
        {
            
        }

        protected void ImageBtnPurchases_Click(object sender, ImageClickEventArgs e)
        {

        }

        protected void ImgBtnItem_Click(object sender, ImageClickEventArgs e)
        {
            
        }

        protected void ImgAnalysis_Click(object sender, ImageClickEventArgs e)
        {
            
        }

        protected void ImgBtnCust_Click(object sender, ImageClickEventArgs e)
        {
            
        }

        protected void ImgBtnConvert_Click(object sender, ImageClickEventArgs e)
        {
           
        }

        protected void lbtnConfig_Click(object sender, EventArgs e)
        {

        }

        protected void ImgBtnPendSO_Click(object sender, ImageClickEventArgs e)
        {
           
            
        }

        protected void ImgInvDemands_Click(object sender, ImageClickEventArgs e)
        {
          
        }


       protected void imgBtnAi_Click(object sender, ImageClickEventArgs e)
        {
            
        }


        public static string CapitalizeFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }

        protected void BtnSales_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SageCloudDashSales.aspx?userid=" + Request.QueryString["userid"].ToString() + "", false);// build redirect url here with querystring for criteria
        }
        protected void imgBtnMPack_Click(object sender, ImageClickEventArgs e)
        {
           
        }

       

        protected void imgBtnPowerBI_Click(object sender, ImageClickEventArgs e)
        {
            
        }

        protected void imgBtnCustom_Click(object sender, ImageClickEventArgs e)
        {

        }

        protected async void imgPOs_Click(object sender, ImageClickEventArgs e)
        {
          

           
                   // await ApiUrlCall.LoadSuppPurchaseOrders(dbpath, CoID, CoName, Session["user"].ToString());
               
        }

        private string FiltString()
        {
            string filtstr = "", CoFilt = string.Empty, RepFilt = string.Empty, custfilt = string.Empty, categfilt = string.Empty;
             CoFilt += " (TransactionsTbl.CoID = '" + CurrentUser.CoID + "'";

            if (ddCust.SelectedIndex > 0) custfilt = $" AND (TransactionsTbl.CustomerID = '{ddCust.SelectedItem.Value}')";
            if (ddRep.SelectedIndex > 0) RepFilt = $" AND (TransactionsTbl.Sales_Rep = '{ddRep.SelectedItem.Text}')";
            if (ddcustCateg.SelectedIndex > 0) categfilt = $" AND (CustomerTbl.Category = '{ddcustCateg.SelectedItem.Text}')";

            filtstr = CoFilt;
            return filtstr + RepFilt + custfilt + categfilt;
        }
        private List<DashSalesModel> GetDashSalesData()
        {
            //string dbpath = Server.MapPath("~/DataInsightsAPI/App_Data/" + Request.QueryString["userid"].ToString());
            string query = "SELECT TransactionsTbl.CustomerID, TransactionsTbl.Customer_Name, TransactionsTbl.Sales_Rep, TransactionsTbl.Number, TransactionsTbl.Type, Sum(TransactionsTbl.Nett_Line_Total) AS SumOfNett_Line_Total, Sum(TransactionsTbl.Line_Cost) AS SumOfLine_Cost, TransactionsTbl.[Date], TransactionsTbl.Year_Month, TransactionsTbl.[Year], TransactionsTbl.Year_Financial, TransactionsTbl.CoID, CustomerTbl.Category" +
                        " FROM TransactionsTbl INNER JOIN CustomerTbl ON TransactionsTbl.CustomerID = CustomerTbl.CustomerID" +
                        " GROUP BY TransactionsTbl.CustomerID, TransactionsTbl.Customer_Name, TransactionsTbl.Sales_Rep, TransactionsTbl.Number, TransactionsTbl.Year_Month, TransactionsTbl.[Year], TransactionsTbl.Year_Financial, TransactionsTbl.CoID, TransactionsTbl.[Type], TransactionsTbl.Date, CustomerTbl.Category" +
                        $" HAVING (((TransactionsTbl.[Type])='Tax_Invoice' Or (TransactionsTbl.[Type])='Credit_Note') AND {FiltString()});";

            List<DashSalesModel> dashSales = new List<DashSalesModel>(); ;
            //List<DashSalesModel> dashSales = ApiUrlCall.GetdashSalesData(query, dbpath);
            // Cache the data for 30 minutes (adjust as needed)
            string cacheKey = $"DashSales_{userid}";
            Cache.Insert(cacheKey, dashSales, null, DateTime.Now.AddMinutes(30), System.Web.Caching.Cache.NoSlidingExpiration);
            return dashSales;
        }

        private void LoadCharts(List<DashSalesModel> dashSales)
        {
            SalesQty(dashSales);
            SalesRev(dashSales);
            SalesCust(dashSales);
            Loaddropdowns(dashSales);
            LoadRepsChart(dashSales);
            LoadTopSales(dashSales);
            LoadTopSalesByRep(dashSales);
            LoadCategoryChart(dashSales);

            if (dashSales != null && dashSales.Any())
            {
                var chartDataM = GetApexChartData(dashSales, Convert.ToInt16(dMths.SelectedItem.Text)); // Get the chart data
                lblRepMonths.InnerHtml = "Last " + Convert.ToInt16(dMths.SelectedItem.Text) + " Months";
                string jsonChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartDataM); // Serialize to JSON
                chartDataLiteral.Text = $"<script>window.chartData = {jsonChartData}; console.log('Sales chart data loaded');</script>";
            }
        }

        private void LoadRepsChart(List<DashSalesModel> dashSales)
        {
            var totalSales = dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1)).Sum(x => x.Sum_Nett_Total);

            var salesRepData = dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
                .GroupBy(x => x.Sales_Rep)
                .Select(g => new
                {
                    SalesRep = g.Key,
                    PercentOfTotal = totalSales == 0 ? 0 : (g.Sum(x => x.Sum_Nett_Total) / totalSales) * 100
                })
                .OrderByDescending(x => x.PercentOfTotal)
                .ToList();

            // Prepare salesRepData for JavaScript
            var chartData = new
            {
                SalesRepData = salesRepData, // This will be used for both chart and table
                Labels = salesRepData.Select(x => x.SalesRep).ToArray(),
                Percentages = salesRepData.Select(x => x.PercentOfTotal).ToArray()
            };

            // Serialize to JSON safely and output as a JavaScript variable
            string repChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData, Newtonsoft.Json.Formatting.None);
            repChartDataLiteral.Text = $"<script>window.repChartData = {repChartData};</script>";
        }

        private void SalesQty(List<DashSalesModel> dashSales)
        {
            var monthsales = dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1)).AsQueryable();
            float thisales = monthsales.Count();
            lblSalesQty.InnerHtml = thisales.ToString("N2");

            var prevmonthsales = dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1).AddYears(-1) && x.Date < DateTime.Now.AddYears(-1)).AsQueryable();
            float prevsales = prevmonthsales.Count();
            lblSalesPrevQty.InnerHtml = prevsales.ToString("N2");

            if (thisales > 0 && prevsales > 0)
            {
                double salesDiff = 1 - (thisales / prevsales);
                lblSalesDiff.InnerHtml = salesDiff.ToString("P2");
                if (salesDiff > 0) lblSalesDiff.Style.Add("color", "green");
            }
        }

        private void SalesRev(List<DashSalesModel> dashSales)
        {
            double salesRev = Convert.ToDouble(dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1)).Sum(x => x.Sum_Nett_Total));
            double prevsalesRev = Convert.ToDouble(dashSales.Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1).AddYears(-1) && x.Date < DateTime.Now.AddYears(-1)).Sum(x => x.Sum_Nett_Total));
            lblSalesRev.InnerHtml = salesRev.ToString("N2");
            if (salesRev > 0 && prevsalesRev > 0)
            {
                double salesDiff = 1 - (salesRev / prevsalesRev);
                lblSalesPrevRev.InnerHtml = salesDiff.ToString("P2");
                if (salesDiff > 0) lblSalesPrevQty.Style.Add("color", "green");
            }
        }

        private void SalesCust(List<DashSalesModel> dashSales)
        {
            float thisMthCustCount = dashSales.Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1)).Select(x => x.Customer_Name).Distinct().Count();
            lblCustCount.InnerHtml = thisMthCustCount.ToString("N2");

            float lastMthCustCount = dashSales.Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1).AddYears(-1) && x.Date < DateTime.Now.AddYears(-1)).Select(x => x.Customer_Name).Distinct().Count();
            if (thisMthCustCount > 0 && lastMthCustCount > 0)
            {
                double custdiff = 1 - (thisMthCustCount / thisMthCustCount);
                lblcustCountdiff.InnerHtml = custdiff.ToString("P2");
                if (custdiff > 0) lblcustCountdiff.Style.Add("color", "green");
            }
        }

        protected void LoadTopSales(List<DashSalesModel> dashSales, string sortExpression = "TotalOrderValue", string sortDirection = "DESC", int pageIndex = 0, string searchTerm = "")
        {
            if (dashSales == null || !dashSales.Any())
                return;

            // Apply date filter (assuming dMths is a dropdown for months selection)
            var filteredData = dashSales
                .Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1));

            // Apply search filter if a search term is provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredData = filteredData.Where(x => x.Customer_Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Apply grouping
            var customerSalesAnalysis = filteredData
                 .GroupBy(s => new { s.Customer_ID, s.Customer_Name })
                .Select(g => new
                {
                    Customer_ID = g.Key.Customer_ID,
                    CustomerName = g.Key.Customer_Name,
                    NumberOfOrders = g.Count(),
                    TotalOrderValue = g.Sum(s => s.Sum_Nett_Total)
                })
                .AsQueryable(); // Convert to IQueryable for dynamic sorting

            // Apply sorting dynamically
            if (!string.IsNullOrEmpty(sortExpression))
            {
                customerSalesAnalysis = customerSalesAnalysis.OrderBy($"{sortExpression} {sortDirection}");
            }

            // Convert to list after sorting
            var sortedData = customerSalesAnalysis.ToList();

            // Bind data to GridView
            customerSalesGridView.DataSource = sortedData;
            customerSalesGridView.PageIndex = pageIndex; // Maintain paging
            customerSalesGridView.DataBind();
        }

        private void Loaddropdowns(List<DashSalesModel> dashSales)
        {
            if (ddCust.SelectedIndex == 0)
            {
                var custlist = dashSales.Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
              .OrderBy(x => x.Customer_Name)
              .Select(x => new { x.Customer_ID, x.Customer_Name })
              .Distinct().ToList();
                ddCust.DataSource = custlist;
                ddCust.DataTextField = "Customer_Name";
                ddCust.DataValueField = "Customer_ID";
                ddCust.DataBind();
                ddCust.Items.Insert(0, "- Select -");
            }

            if (ddRep.SelectedIndex == 0)
            {
                var Replist = dashSales.Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
                   .OrderBy(x => x.Sales_Rep)
                   .Select(x => new { x.Sales_Rep })
                   .Distinct().ToList();
                ddRep.DataSource = Replist;
                ddRep.DataTextField = "Sales_Rep";
                ddRep.DataValueField = "Sales_Rep";
                ddRep.DataBind();
                ddRep.Items.Insert(0, "- Select -");
            }

            if (ddcustCateg.SelectedIndex == 0)
            {
                var Categlist = dashSales.Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
                   .OrderBy(x => x.Category)
                   .Select(x => new { x.Category })
                   .Distinct().ToList();
                ddcustCateg.DataSource = Categlist;
                ddcustCateg.DataTextField = "Category";
                ddcustCateg.DataValueField = "Category";
                ddcustCateg.DataBind();
                ddcustCateg.Items.Insert(0, "- Select -");
            }
        }

        private object GetApexChartData(List<DashSalesModel> dashSales, int mths)
        {
            var chartData = dashSales
                .Where(x => x.Date > DateTime.Today.AddMonths(-mths))
                .GroupBy(x => x.Year_Month)
                .Select(g => new
                {
                    Month = g.Key, // Use the Year_Month as the label
                    TotalSales = g.Count(), // Count of sales in this group
                    TotalRevenue = g.Sum(x => x.Sum_Nett_Total), // Sum of revenue in this group
                    CustomerCount = g.Select(x => x.Customer_Name).Distinct().Count() // Count of unique customers
                })
                .OrderBy(x => x.Month) // Ensure data is ordered by Month
                .ToList();

            // Prepare data for ApexCharts
            var labels = chartData.Select(x => x.Month.ToString()).ToArray();
            var salesData = chartData.Select(x => x.TotalSales).ToArray();
            var revenueData = chartData.Select(x => x.TotalRevenue).ToArray();
            var customerData = chartData.Select(x => x.CustomerCount).ToArray();

            return new
            {
                Labels = labels,
                Sales = salesData,
                Revenue = revenueData,
                Customers = customerData
            };
        }

        protected void dMths_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<DashSalesModel> dashSales = GetDashSalesData();
            LoadCharts(dashSales);
        }

        protected void customerSalesGridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            customerSalesGridView.PageIndex = e.NewPageIndex;
            List<DashSalesModel> dashSales = GetDashSalesData();
            string searchTerm = txtFind.Text.Trim(); // Get search term
            LoadTopSales(dashSales, ViewState["SortExpression"]?.ToString() ?? "TotalOrderValue", ViewState["SortDirection"]?.ToString() ?? "DESC", e.NewPageIndex, searchTerm);
        }

        protected void customerSalesGridView_Sorting(object sender, GridViewSortEventArgs e)
        {
            List<DashSalesModel> dashSales = GetDashSalesData();

            if (dashSales != null)
            {
                string sortExpression = e.SortExpression;
                string currentSortDirection = ViewState["SortDirection"] as string ?? "ASC";
                string newSortDirection = (currentSortDirection == "ASC") ? "DESC" : "ASC";

                // Store new sort direction in ViewState
                ViewState["SortExpression"] = sortExpression;
                ViewState["SortDirection"] = newSortDirection;

                // Get search term from the textbox
                string searchTerm = txtFind.Text.Trim();

                // Load and bind sorted data
                LoadTopSales(dashSales, sortExpression, newSortDirection, customerSalesGridView.PageIndex, searchTerm);
            }
        }

        protected void lbtnSearch_Click(object sender, EventArgs e)
        {
            List<DashSalesModel> dashSales = GetDashSalesData();
            string searchTerm = txtFind.Text.Trim();
            LoadTopSales(dashSales, ViewState["SortExpression"]?.ToString() ?? "TotalOrderValue", ViewState["SortDirection"]?.ToString() ?? "DESC", 0, searchTerm);
        }

        protected void customerSalesGridView_SelectedIndexChanged(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)customerSalesGridView.SelectedRow.FindControl("lbtnCust");
            string custid = lbtn.CommandArgument.ToString();
            if (ddCust.Items.FindByValue(custid) != null)
            {
                ddCust.SelectedValue = custid;
            }
            List<DashSalesModel> dashSales = GetDashSalesData();
            LoadCharts(dashSales);
        }

        protected void lbtnClear_Click(object sender, EventArgs e)
        {
            txtFind.Text = string.Empty;
            if (ddCust.Items.Count > 0)
            {
                ddCust.SelectedIndex = -1;
            }
            List<DashSalesModel> dashSales = GetDashSalesData();
            LoadCharts(dashSales);
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            DataSet MyDS = new DataSet();
            string dbpath = Server.MapPath("~/DataInsightsAPI/App_Data/" + Request.QueryString["userid"].ToString());
            string query = "SELECT TransactionsTbl.CustomerID, TransactionsTbl.Customer_Name, TransactionsTbl.Sales_Rep, TransactionsTbl.Number, TransactionsTbl.Type, Sum(TransactionsTbl.Nett_Line_Total) AS SumOfNett_Line_Total, Sum(TransactionsTbl.Line_Cost) AS SumOfLine_Cost, TransactionsTbl.[Date], TransactionsTbl.Year_Month, TransactionsTbl.[Year], TransactionsTbl.Year_Financial, TransactionsTbl.CoID, CustomerTbl.Category" +
                        " FROM TransactionsTbl INNER JOIN CustomerTbl ON TransactionsTbl.CustomerID = CustomerTbl.CustomerID" +
                        " GROUP BY TransactionsTbl.CustomerID, TransactionsTbl.Customer_Name, TransactionsTbl.Sales_Rep, TransactionsTbl.Number, TransactionsTbl.Year_Month, TransactionsTbl.[Year], TransactionsTbl.Year_Financial, TransactionsTbl.CoID, TransactionsTbl.[Type], TransactionsTbl.Date, CustomerTbl.Category" +
                        $" HAVING (((TransactionsTbl.[Type])='Tax_Invoice' Or (TransactionsTbl.[Type])='Credit_Note') AND {FiltString()});";
           // MyDS = ApiUrlCall.GetSQLiteData(query, dbpath);

            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(MyDS.Tables[0], "Sales Analysis");
            wb.SaveAs(Server.MapPath("~/Charts/SalesData" + userid + DateTime.Today.ToString("dd_MMM_yyyy") + ".xlsx"));

            MyDS.Tables.Clear();
            MyDS.Dispose();

            string file = "SalesAnalysis.xlsx";
            string filepath = (Server.MapPath("~/Charts/SalesData" + userid + DateTime.Today.ToString("dd_MMM_yyyy") + ".xlsx"));
            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.TransmitFile(filepath);
            Response.Flush();
            Response.End();
        }

        protected void GridReps_Sorting(object sender, GridViewSortEventArgs e)
        {
            List<DashSalesModel> dashSales = GetDashSalesData();

            if (dashSales != null)
            {
                string sortExpression = e.SortExpression;
                string currentSortDirection = ViewState["SortDirection"] as string ?? "ASC";
                string newSortDirection = (currentSortDirection == "ASC") ? "DESC" : "ASC";

                // Store new sort direction in ViewState
                ViewState["SortExpression"] = sortExpression;
                ViewState["SortDirection"] = newSortDirection;

                // Get search term from the textbox
                string searchTerm = txtFind.Text.Trim();

                // Load and bind sorted data
                LoadTopSalesByRep(dashSales, sortExpression, newSortDirection);
            }
        }

        protected void GridReps_SelectedIndexChanged(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)customerSalesGridView.SelectedRow.FindControl("lbSalesRep");
            string repname = lbtn.CommandArgument.ToString();
            if (ddRep.Items.FindByValue(repname) != null)
            {
                ddRep.SelectedValue = repname;
            }
            List<DashSalesModel> dashSales = GetDashSalesData();
            LoadTopSalesByRep(dashSales);
        }

        protected void LoadTopSalesByRep(List<DashSalesModel> dashSales, string sortExpression = "TotalOrderValue", string sortDirection = "DESC", int pageIndex = 0)
        {
            var salesRepAnalysis = dashSales
                 .Where(x => x.Date > DateTime.Today.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
                .GroupBy(s => s.Sales_Rep)
                .Select(g => new
                {
                    SalesRep = g.Key,
                    NumberOfOrders = g.Count(),
                    TotalOrderValue = g.Sum(s => s.Sum_Nett_Total)
                })
                .AsQueryable(); // Convert to IQueryable for dynamic sorting

            // **Apply Sorting**
            salesRepAnalysis = salesRepAnalysis.OrderBy($"{sortExpression} {sortDirection}");

            GridReps.DataSource = salesRepAnalysis.ToList();
            GridReps.DataBind();
        }

        protected void GridReps_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            customerSalesGridView.PageIndex = e.NewPageIndex;
            List<DashSalesModel> dashSales = GetDashSalesData();
            LoadTopSalesByRep(dashSales, ViewState["SortExpression"]?.ToString() ?? "TotalOrderValue", ViewState["SortDirection"]?.ToString() ?? "DESC", e.NewPageIndex);
        }

        /////////////////////////////////////////////////////
        #region LoadCategory Data
        private void LoadCategoryChart(List<DashSalesModel> dashSales)
        {
            var categoryData = dashSales
                .Where(x => x.Date > DateTime.Now.AddMonths(Convert.ToInt16(dMths.SelectedItem.Text) * -1))
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalSales = g.Sum(x => x.Sum_Nett_Total)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();

            // Prepare data for JavaScript
            var chartData = new
            {
                Categories = categoryData.Select(x => x.Category).ToArray(),
                TotalSales = categoryData.Select(x => x.TotalSales).ToArray()
            };

            // Serialize to JSON and embed in the page
            string categoryChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData, Newtonsoft.Json.Formatting.None);
            categoryChartDataLiteral.Text = $"<script>window.categoryChartData = {categoryChartData};</script>";
        }
        #endregion
    }
}