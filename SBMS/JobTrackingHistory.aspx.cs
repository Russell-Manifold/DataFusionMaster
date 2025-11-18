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
    public partial class JobTrackingHistory : BasePage
    {
        long CoID;
        long jobid = 0;
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
            jobid = Convert.ToInt64(Request.QueryString["jobid"]);
            if (!IsPostBack)
            {
                Uri referrer = Request.UrlReferrer;
                if (referrer != null)
                {
                    ViewState["ReferringUrl"] = referrer.ToString();
                }
                LoadHistory();
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

        private decimal totalRejectQty = 0;

        protected void GridHistLines_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GridViewSortDirection == "ASC" ? "DESC" : "ASC";

            GridViewSortDirection = sortDirection;
            GridViewSortExpression = sortExpression;

            LoadHistory();
        }

        protected void GridHistLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                decimal rejectQty = Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "RejectQty"));
                totalRejectQty += rejectQty;
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Cells[4].Text = totalRejectQty.ToString("N2");
            }
        }

        private void LoadHistory()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var HistLines = _db.GetJobMovementTransactions(CoID, jobid).ToList();
                if (HistLines != null)
                {
                    if (!string.IsNullOrEmpty(GridViewSortExpression))
                    {
                        if (GridViewSortDirection == "ASC")
                        {
                            HistLines = HistLines.OrderBy(h => h.GetType().GetProperty(GridViewSortExpression).GetValue(h)).ToList();
                        }
                        else
                        {
                            HistLines = HistLines.OrderByDescending(h => h.GetType().GetProperty(GridViewSortExpression).GetValue(h)).ToList();
                        }
                    }

                    if (HistLines.Count > 0)
                    {
                        GridHistLines.DataSource = HistLines;
                        GridHistLines.DataBind();
                        lblJobNumber.Text = HistLines[0].JCNumber;
                        lblCustomer.Text = HistLines[0].CustSupName;
                        lblCreatedDate.Text = Convert.ToDateTime(HistLines[0].JCCreatedDate).ToString("dd MMM yyyy");
                        lblSummary.Text = HistLines[0].JCSummary;
                        lblJCQty.Text = HistLines[0].JCQtyOfItems.ToString();
                    }
                }
            }
        }

        protected void LinkButton1_Click(object sender, EventArgs e)
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

        protected void lbtnViewJC_Click(object sender, EventArgs e)
        {
            if (ViewState["ReferringUrl"] != null)
            {
                Response.Redirect(ViewState["ReferringUrl"].ToString());
            }
            else
            {
                // Handle the case where there is no referring URL, e.g., redirect to a default page
                Response.Redirect("~/Dashboard.aspx");
            }
        }
    }
}