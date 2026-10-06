using SBMS.Classes;
using SBMS.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Stock Control -> Job Card GP Analysis. Revenue, cost (order lines / lines added on the
    /// job card), GP and margin per completed job card, with the components drawn underneath.
    /// The figures and their definitions are in Classes/JobCardGP.cs.
    /// </summary>
    public partial class JobCardGPPage : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (CurrentUser.CanStockControl != true)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
                return;
            }
            if (!IsPostBack)
            {
                lblUsername.Text = $":.. {CurrentUser.UserName}..:  ";
                string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
                imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");
                txtFrom.Text = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
                txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
                LoadJobs();
            }
        }

        private bool ReadDates(out DateTime dateFrom, out DateTime dateTo)
        {
            dateTo = DateTime.Today;
            return DateTime.TryParseExact(txtFrom.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateFrom)
                && DateTime.TryParseExact(txtTo.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTo)
                && dateFrom <= dateTo;
        }

        private void LoadJobs()
        {
            DateTime dateFrom, dateTo;
            if (!ReadDates(out dateFrom, out dateTo))
            {
                AlertHelper.ShowSweetAlert(this, "Enter a valid date range.", "error");
                return;
            }
            pnlComp.Visible = false;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                try
                {
                    var jobs = JobCardGP.Jobs(_db, CurrentUser.CoID, dateFrom, dateTo);
                    // Search: any part of the job card number, sales order number or customer name.
                    string find = (txtSearch.Text ?? "").Trim();
                    if (find.Length > 0)
                    {
                        jobs = jobs.Where(x => (x.JCNumber ?? "").IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0
                                            || (x.SONumber ?? "").IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0
                                            || (x.Customer ?? "").IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                    }
                    GridJobs.DataSource = jobs;
                    GridJobs.DataBind();
                    if (jobs.Count > 0 && GridJobs.FooterRow != null)
                    {
                        decimal rev = jobs.Sum(x => x.Revenue), cost = jobs.Sum(x => x.TotalCost);
                        GridJobs.FooterRow.Cells[0].Text = "Total";
                        GridJobs.FooterRow.Cells[6].Text = rev.ToString("N2");
                        GridJobs.FooterRow.Cells[7].Text = jobs.Sum(x => x.OrderCost).ToString("N2");
                        GridJobs.FooterRow.Cells[8].Text = jobs.Sum(x => x.AddedCost).ToString("N2");
                        GridJobs.FooterRow.Cells[9].Text = (rev - cost).ToString("N2");
                        GridJobs.FooterRow.Cells[10].Text = (rev != 0 ? (rev - cost) / rev : 0).ToString("P1");
                        GridJobs.FooterRow.Font.Bold = true;
                    }
                }
                catch
                {
                    pnlNotReady.Visible = true;
                    pnlMain.Visible = false;
                }
            }
        }

        protected void lbtnShow_Click(object sender, EventArgs e) { LoadJobs(); }

        protected void GridJobs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Components") return;
            int jcId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out jcId)) return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long coId = CurrentUser.CoID;
                // Tenant check: the job card must belong to this company.
                var jc = _db.JobCardsMasters.Where(x => x.JCID == jcId && x.CustomerID == coId)
                            .Select(x => new { x.JCNumber }).FirstOrDefault();
                if (jc == null) return;
                GridComp.DataSource = JobCardGP.Components(_db, coId, jcId);
                GridComp.DataBind();
                lblCompTitle.Text = "Stock drawn on " + Server.HtmlEncode(jc.JCNumber ?? ("job card " + jcId));
                pnlComp.Visible = true;
            }
        }

        protected void lbtnDash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnStockControl_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
