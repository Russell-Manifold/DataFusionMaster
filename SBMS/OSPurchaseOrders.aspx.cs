using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class OSPurchaseOrders : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }


        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SessionValidator.ValidateUserSession(CurrentUser);
            
            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false);
                return;
            }

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
            if (!IsPostBack)
            {
                lblDir.Text = "ASC";
                showhidebuttons();
                ApiUrlCall api = new ApiUrlCall();
                JObject POResult = await api.LoadPurchaseOrders(CurrentUser);
                if (POResult != null && POResult["error"] != null)
                {
                    string message = POResult["error"].ToString();
                    string errMsg = $"CoID: {CurrentUser.CoID} + OS PurchaseOrder Error 56 - {message} ";
                    api.LogErrorToFile(errMsg);
                    AlertHelper.ShowSweetAlert(this, message, "error");
                }

                BindData();
                LoadDD();
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


        public List<GetListOfDocHeadersByType_Result> GetSortedDocHeaders(string sortExpression, string sortDirection)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString();
                var query = _db.GetListOfDocHeadersByType(CurrentUser.CoID, 1).AsQueryable();

                if (txtfind.Text.ToString().Trim().Length > 1)
                {
                    query = query.Where(x=>x.DocumentNumber.Contains(findstr) || x.CustSupName.Contains(findstr));
                } 
                else if (!chkCompl.Checked)
                {
                    query = query.Where(x => x.Complete == false);
                }
                if (DDPOStatus.SelectedIndex > 0)
                {
                    query = query.Where(x => x.Status == DDPOStatus.Text);
                } else
                {
                    query = query.Where(x => x.Status != "Cancelled");
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

        private void BindData()
        {
            string sortExpression = ViewState["SortExpression"] as string ?? "DueDelDate"; // Replace "DefaultColumn" with your default column
            string sortDirection = ViewState["SortDirection"] as string ?? "ASC";

            var sortedData = GetSortedDocHeaders(sortExpression, sortDirection);
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
        protected void GridPOs_SelectedIndexChanged(object sender, EventArgs e)
        {
            long id = Convert.ToInt64(GridPOs.SelectedRow.Cells[0].Text.ToString());
            CheckBox ckb = (CheckBox)(GridPOs.SelectedRow.FindControl("chkstarted"));
            Response.Redirect("~/SalesOrder.aspx?docid=" + id.ToString());
        }

        protected void GridPOs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.Header || e.Row.RowType == DataControlRowType.DataRow)
            {
                if (e.Row.Cells[4].Text.ToString().Length > 26)
                {
                    e.Row.Cells[4].Text = e.Row.Cells[4].Text.Substring(0, 26) + "...";
                }
                    if (e.Row.Cells[7].Text == "Invoiced")
                {
                    e.Row.Cells[7].BackColor = System.Drawing.Color.Red;
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

        protected void lbtnPO_Click(object sender, EventArgs e)
        {
            LinkButton lbtnPO = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnPO.NamingContainer;
            CheckBox ckb = (CheckBox)row.FindControl("chkstarted");

            string id = lbtnPO.CommandArgument;
            bool updt = false;
             if (ckb.Checked) updt = true;
            Response.Redirect("~/Receiving.aspx?docid=" + id.ToString() + "&updt=" + updt);
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

        protected void LoadDD()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var statuses = _db.DocHeaders
                                    .Where(d => d.DocType == 1 && d.CompanyID == CurrentUser.CoID)
                                    .OrderBy(d => d.Status)
                                    .Select(d => d.Status)
                                    .Distinct()
                                    .ToList();
                DDPOStatus.DataSource = statuses;
                DDPOStatus.DataBind();
                DDPOStatus.Items.Insert(0, "-Select-");
            }
        }

        protected void DDPOStatus_SelectedIndexChanged(object sender, EventArgs e)
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

       protected void lbtnDeletePO_Click(object sender, EventArgs e)
        {
            LinkButton lbtnPO = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnPO.NamingContainer;
            CheckBox ckb = (CheckBox)row.FindControl("chkstarted");
            string docguid = lbtnPO.CommandArgument;
            if (ckb.Checked)
            {
                string warnMsg = "You cannot delete a PO for which receiving has started. Please reset receiving before deleting.";
                AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                return;
            }
            // ✅ Success case (or result message from DeletePO)
            string resultMsg = DeletePO(docguid).ToString();
            AlertHelper.ShowSweetAlert(this, resultMsg, "success");
        }

        protected string DeletePO( string docguid)
        {
            string retStr = "";

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thispo = _db.GetOneDocHeaderFromDocID(CurrentUser.CoID, docguid).FirstOrDefault();
                if (thispo != null)
                {
                    var po = _db.DocHeaders.FirstOrDefault(p => p.DocID == thispo.DocID && p.CompanyID == CurrentUser.CoID);
                    _db.DocHeaders.Remove(po);

                    var poLines = _db.DocLines.Where(dl => dl.DocID == thispo.DocID && dl.CompanyID == CurrentUser.CoID).ToList();
                    _db.DocLines.RemoveRange(poLines);
                    _db.SaveChanges();
                    BindData();
                    retStr = "Successfully deleted";
                }
            }
            return retStr;
        }
    }
}