using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemsOnHandAnalysis : BasePage
    {
        long CoID;

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

            CoID = CurrentUser.CoID;
            if (!IsPostBack)
            {
                LoadItems();
            }
        }

        private string GridViewSortDirection
        {
            get { return ViewState["SortDirection"] as string ?? "ASC"; }
            set { ViewState["SortDirection"] = value; }
        }

        private string GridViewSortExpression
        {
            get { return ViewState["SortExpression"] as string ?? string.Empty; }
            set { ViewState["SortExpression"] = value; }
        }

        private decimal totalQOH = 0;

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            GridItems.PageIndex = 0;
            LoadItems();
        }

        protected void GridItems_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GridViewSortDirection == "ASC" ? "DESC" : "ASC";

            GridViewSortDirection = sortDirection;
            GridViewSortExpression = sortExpression;

            LoadItems();
        }

        protected void GridItems_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridItems.PageIndex = e.NewPageIndex;
            LoadItems();
        }

        protected void GridItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                decimal qoh = Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "QOH"));
                totalQOH += qoh;
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Cells[4].Text = totalQOH.ToString("N2");
            }
        }

        private void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Items = _db.GetOpeningBalancesAllStores(CoID).ToList();

                // Filtering with case insensitivity
                if (!string.IsNullOrEmpty(txtStoreCode.Text))
                {
                    Items = Items.Where(i => i.StoreCode.IndexOf(txtStoreCode.Text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                if (!string.IsNullOrEmpty(txtItemCode.Text))
                {
                    Items = Items.Where(i => i.ItemCode.IndexOf(txtItemCode.Text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                // Sorting
                if (!string.IsNullOrEmpty(GridViewSortExpression))
                {
                    if (GridViewSortDirection == "ASC")
                    {
                        Items = Items.OrderBy(i => i.GetType().GetProperty(GridViewSortExpression).GetValue(i)).ToList();
                    }
                    else
                    {
                        Items = Items.OrderByDescending(i => i.GetType().GetProperty(GridViewSortExpression).GetValue(i)).ToList();
                    }
                }

                // Paging and Binding
                GridItems.DataSource = Items;
                GridItems.DataBind();
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
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}