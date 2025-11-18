using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class OSSalesOrders : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected  async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SessionValidator.ValidateUserSession(CurrentUser);

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

            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
            if (!IsPostBack)
            {
                showhidebuttons();
                lblDir.Text = "ASC";
                ApiUrlCall api = new ApiUrlCall();
                JObject SOResult = await api.LoadSalesOrders(CurrentUser);
                if (SOResult != null && SOResult["error"] != null)
                {
                    string message = SOResult["error"].ToString();
                    string errMsg = $"CoID: {CurrentUser.CoID} + OS SalesOrder Error 54 - {message} ";
                    api.LogErrorToFile(errMsg);
                    AlertHelper.ShowSweetAlert(this, message, "error");
                }
                BindData();
                LoadDD();
            }
        }
        public List<GetListOfDocHeadersByType_Result> GetSortedDocHeaders(string sortExpression, string sortDirection)
        {
            UserDetails userDetails = CurrentUser;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString().ToLower();
                var query = _db.GetListOfDocHeadersByType(userDetails.CoID, 5).AsQueryable();
                
                if (txtfind.Text.ToString().Trim().Length > 1)
                {
                    query = query.Where(x => x.DocumentNumber.ToLower().Contains(findstr) || x.CustSupName.ToLower().Contains(findstr) || x.Reference.ToLower().Contains(findstr));
                } 
                if (!chkCompl.Checked) 
                {
                    query = query.Where(x => x.Active == true);
                }
                if (DDSOStatus.SelectedIndex > 0)
                {
                    query = query.Where(x => x.Status == DDSOStatus.Text);
                }
                if (DDStatus.SelectedIndex > 0)
                {
                    query = query.Where(x => x.LinkedJCStatus == DDStatus.Text || x.LinkedPSStatus == DDStatus.Text);
                }

                // Apply sorting using dynamic LINQ
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    var sortQuery = $"{sortExpression} {(sortDirection == "ASC" ? "ascending" : "descending")}";
                    query = query.OrderBy(sortQuery);
                }
                return query.ToList();
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
        private void BindData()
        {
            string sortExpression = ViewState["SortExpression"] as string ?? "DueDelDate"; // Replace "DefaultColumn" with your default column
            string sortDirection = ViewState["SortDirection"] as string ?? "ASC";

            var sortedData = GetSortedDocHeaders(sortExpression, sortDirection);   
            foreach (var dl in sortedData)
            {
                if (dl.LinkedJCStatus != null) dl.LinkedStatus = dl.LinkedJCStatus;
                if (dl.LinkedPSStatus != null) dl.LinkedStatus = dl.LinkedPSStatus;
            }
            GridPOs.DataSource = sortedData;
            GridPOs.DataBind();
            lblpoqty.Text = " (" + GridPOs.Rows.Count + ")";
        }

        protected void myDataGrid_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = ViewState["SortDirection"] as string == "ASC" ? "DESC" : "ASC";

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            BindData();
        }
        //protected void GridPOs_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    long id = Convert.ToInt64(GridPOs.SelectedRow.Cells[0].Text.ToString());
        //    bool updt = false;
        //    CheckBox ckb = (CheckBox)(GridPOs.SelectedRow.FindControl("chkstarted"));
        //    if (ckb.Checked) updt = true;
        //    Response.Redirect("~/Receiving.aspx?docid=" + id.ToString() + "&updt=" + updt);
        //}

        protected void GridPOs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            UserDetails userDetails = CurrentUser;
            e.Row.Cells[0].Visible = false;
          
            if (e.Row.RowType == DataControlRowType.Header || e.Row.RowType == DataControlRowType.DataRow)
            {
                if (userDetails.UseModule2 == false)
                {
                    e.Row.Cells[6].Visible = false;
                }  
                else if (userDetails.CanViewJobCards == false)
                {
                    e.Row.Cells[6].Visible = false;
                }
               
                if (e.Row.Cells[12].Text == "Invoiced")
                {
                    e.Row.Cells[12].BackColor = System.Drawing.Color.Red;
                    e.Row.Cells[12].ForeColor = System.Drawing.Color.White;
                }
                if (e.Row.Cells[7].Text == "Complete")
                {
                    e.Row.Cells[7].BackColor = System.Drawing.Color.Orange;
                    e.Row.Cells[7].ForeColor = System.Drawing.Color.White;
                }
            }
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            BindData();
        }

        protected void chkCompl_CheckedChanged(object sender, EventArgs e)
        {
            BindData();
        }

        protected void GridPOs_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = ViewState["SortDirection"] as string == "ASC" ? "DESC" : "ASC";

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            BindData();
        }

        protected void GridPOs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
          if (e.CommandName == "lbtnSO")
            {
                try
                {
                    string id = e.CommandArgument.ToString();
                    Response.Redirect("~/SalesOrder.aspx?docid=" + id.ToString(), true);
                }
                catch { }
            }
            else
           if (e.CommandName == "lbtnPS")
            {
                try
                {
                    string ComArg = e.CommandArgument.ToString();
                    if (ComArg.Split('|')[1].ToString().Length > 1)
                    {
                        string id = ComArg.Split('|')[0].ToString();
                        Response.Redirect("~/PickingSlip.aspx?docid=" + id.ToString(), true);
                    }
                }
                catch { }
            }
            else
                if (e.CommandName == "lbtnJC")
            {
                try
                {
                    string ComArg = e.CommandArgument.ToString();
                    if (ComArg.Split('|')[1].ToString().Length > 1)
                    {
                        string id = ComArg.Split('|')[0].ToString();
                        Response.Redirect("~/JobCard.aspx?docid=" + id.ToString(), true);
                    }
                }
                catch { }
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

        protected void lbtnRefresh_Click(object sender, EventArgs e)
        {
            //string DS = ApiUrlCall.LoadSalesOrders(CurrentUser);
            BindData();
        }

        protected void LoadDD()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var statuses = _db.DocHeaders
                                    .Where(d => d.DocType == 5 && d.CompanyID == CurrentUser.CoID)
                                    .OrderBy(d => d.Status)
                                    .Select(d => d.Status)
                                    .Distinct()
                                    .ToList();
                DDSOStatus.DataSource = statuses;
                DDSOStatus.DataBind();
                DDSOStatus.Items.Insert(0, "-Select-");

                var result = _db.DocHeaders
                    .Where(dh => dh.CompanyID == CurrentUser.CoID)  
                    .Select(dh => new
                    {
                        // LinkedStatus directly uses the null-coalescing operator to choose between JCStatus and PSStatus
                        LinkedStatus = (_db.JobCardsMasters
                                        .Where(jcm => jcm.JCID == dh.LinkedJCID && jcm.CustomerID == dh.CompanyID)
                                        .Select(jcm => jcm.JCStatus)
                                        .FirstOrDefault())
                                    ?? (_db.PickingSlipMasters
                                        .Where(psm => psm.PSID == dh.LinkedPSID && psm.CustomerID == dh.CompanyID)
                                        .Select(psm => psm.PSStatus)
                                        .FirstOrDefault())
                                    ?? ""  // Default to an empty string if both are null
                    })
                    .Distinct()
                    .ToList();

                DDStatus.DataSource = result;
                DDStatus.DataTextField = "LinkedStatus";
                DDStatus.DataBind();
                DDStatus.Items.Insert(0, "-Select-");
            }
        }

        protected void DDSOStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindData();
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

        protected void lbtnSO_Click(object sender, EventArgs e)
        {
             LinkButton lbtnPO = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnPO.NamingContainer;

            string id = lbtnPO.CommandArgument;
            Response.Redirect("~/SalesOrder.aspx?docid=" + id.ToString(), true);
        }

        protected void lbtnDashSales_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/DashboardSales.aspx?user=" + CurrentUser.UserGuiD, false);
        }
    }
}