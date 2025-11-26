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
    public partial class OSPurchaseOrdersM : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (GridPOs.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridPOs.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridPOs, "Select$" + row.RowIndex, true));
                    }
                }
            }
            base.Render(writer);
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SessionValidator.ValidateUserSession(CurrentUser);
            if (!IsPostBack)
            {
                lblDir.Text = "ASC";
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
                else
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
            string id = GridPOs.SelectedRow.Cells[0].Text.ToString();
            CheckBox ckb = (CheckBox)(GridPOs.SelectedRow.FindControl("chkstarted"));
            Response.Redirect("~/Receiving.aspx?docid=" + id.ToString());
        }

        protected void GridPOs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
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

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/SBMSMobile/Dashboard.aspx", true);
            }
        }
    }
}