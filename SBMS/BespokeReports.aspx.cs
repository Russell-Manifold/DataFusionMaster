using SBMS.Classes;
using System;
using System.Data;
using System.IO;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Irony;

namespace SBMS
{
    public partial class BespokeReports : BasePage
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
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
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

            if (!IsPostBack)
            {
                showhidebuttons();
                LoadAvailableReports();
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
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

        private void LoadAvailableReports()
        {
            long companyId = CurrentUser.CoID;
            gvReports.DataSource = GetAvailableReports(companyId);
            gvReports.DataBind();
        }

        private DataSet GetAvailableReports(long companyId)
        {
            DataSet dt = new DataSet();
            dt = ApiUrlCall.GetSQLDataFromString("SELECT ReportID, ReportName FROM DynamicReports WHERE CompanyID = " + companyId);
           return dt;
        }

        protected void gvReports_SelectedIndexChanged(object sender, EventArgs e)
        {
            int reportId = Convert.ToInt32(gvReports.SelectedDataKey.Value);
            Session["SelectedReportId"] = reportId;
            LoadReportData(reportId);
        }

        private void LoadReportData(int reportId)
        {
            string storedProc = GetStoredProcName(reportId);
            if (storedProc != null)
            {
                DataTable dt = RunStoredProc(storedProc); 
                if (dt != null)
                {
                    gvReportData.DataSource = dt;
                    gvReportData.DataBind();
                    gvReportData.HeaderRow.TableSection = TableRowSection.TableHeader;
                    ViewState["ReportData"] = dt; // Save for paging/sorting
                    lbtndwnload.Visible = true;
                    
                }
                else
                {
                    gvReportData.DataSource = null;
                    gvReportData.DataBind();
                    gvReportData.HeaderRow.TableSection = TableRowSection.TableHeader;
                    lbtndwnload.Visible = false;
                }
            }
        }

        // Helper: Fetch the stored procedure name
        private string GetStoredProcName(int reportId)
        {
            string storedProcName = null;
            storedProcName = ApiUrlCall.GetSQLDataFromString("SELECT StoredProcName FROM DynamicReports WHERE ReportID = " + reportId).Tables[0].Rows[0][0].ToString();
            return storedProcName;
        }

        // Helper: Run stored procedure and return DataTable
        private DataTable RunStoredProc(string storedProcName)
        {
            var parameters = new Dictionary<string, object>
                {
                    { "@CoID", CurrentUser.CoID }
                };

            DataSet ds = new DataSet();
            ds = ApiUrlCall.GetSQLDataFromStoredProc(storedProcName, parameters);
            DataTable dt = ds.Tables[0];
            return dt;
        }

        protected void btnDownloadExcel_Click(object sender, EventArgs e)
        {
           if (gvReportData.Rows.Count > 0)
            {
                ExportReportToExcel();
            }
            else
            {
                lblerr.Text = "No data available for export.";
            }
        }

        protected void ExportReportToExcel()
        {
            if (Session["SelectedReportId"] != null)
            {
                int reportId = Convert.ToInt32(Session["SelectedReportId"]);
                string storedProc = GetStoredProcName(reportId);

                DataTable dt = RunStoredProc(storedProc);

                if (dt != null && dt.Rows.Count > 0)
                {
                    string fileName = $"Report_{storedProc.Replace("dbo.","")}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    string wsName = "ReportData";
                    ExcelHelper.ExportToExcel(dt, fileName, wsName);
                }
                else
                {
                    lblerr.Text = "No data available for export.";
                }
            }
            else
            {
                // Session has expired or reportId is not available, handle accordingly
                lblerr.Text = "Session has expired or report was not selected. Please select a report again.";
                // Optionally, redirect the user to the reports page or another page
                Response.Redirect("ReportsPage.aspx"); // Change this to your reports page URL
            }
        }

        protected void gvReportData_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvReportData.PageIndex = e.NewPageIndex;

            // Reload from ViewState (no SQL call needed)
            if (ViewState["ReportData"] != null)
            {
                DataTable dt = ViewState["ReportData"] as DataTable;
                gvReportData.DataSource = dt;
                gvReportData.DataBind();
            }
        }

        protected void gvReportData_Sorting(object sender, GridViewSortEventArgs e)
        {
            DataTable dt = ViewState["ReportData"] as DataTable;
            if (dt != null)
            {
                string sortExpression = e.SortExpression;
                string sortDirection = GetSortDirection(sortExpression);

                dt.DefaultView.Sort = sortExpression + " " + sortDirection;
                gvReportData.DataSource = dt.DefaultView;
                gvReportData.DataBind();
            }
        }

        private string GetSortDirection(string column)
        {
            string sortDirection = "ASC";

            string sortExpression = ViewState["SortExpression"] as string;
            if (sortExpression != null)
            {
                if (sortExpression == column)
                {
                    string lastDirection = ViewState["SortDirection"] as string;
                    if ((lastDirection != null) && (lastDirection == "ASC"))
                    {
                        sortDirection = "DESC";
                    }
                }
            }

            ViewState["SortDirection"] = sortDirection;
            ViewState["SortExpression"] = column;

            return sortDirection;
        }

        protected void gvReports_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[1].Visible = false; // Hide the first column (ReportID)
        }
     
    }
}