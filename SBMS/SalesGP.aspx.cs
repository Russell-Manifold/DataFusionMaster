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
    /// Stock Control -> Picking Slip GP Analysis (?view=orders) / Item Sales GP Analysis
    /// (?view=items). Invoiced Sales Orders only. Figures are defined in Classes/SalesGP.cs.
    /// </summary>
    public partial class SalesGPPage : BasePage
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
                if (Request.QueryString["view"] == "items") rblView.SelectedValue = "items";
                LoadReport();
            }
        }

        private bool ReadDates(out DateTime dateFrom, out DateTime dateTo)
        {
            dateTo = DateTime.Today;
            return DateTime.TryParseExact(txtFrom.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateFrom)
                && DateTime.TryParseExact(txtTo.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTo)
                && dateFrom <= dateTo;
        }

        private static bool Has(string s, string find)
        {
            return (s ?? "").IndexOf(find, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void LoadReport()
        {
            DateTime dateFrom, dateTo;
            if (!ReadDates(out dateFrom, out dateTo))
            {
                AlertHelper.ShowSweetAlert(this, "Enter a valid date range.", "error");
                return;
            }
            bool byItem = rblView.SelectedValue == "items";
            pnlOrders.Visible = !byItem;
            pnlItems.Visible = byItem;
            pnlOrderDetail.Visible = false;
            pnlItemDetail.Visible = false;
            string find = (txtSearch.Text ?? "").Trim();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (!byItem)
                {
                    var orders = SalesGP.Orders(_db, CurrentUser.CoID, dateFrom, dateTo);
                    if (find.Length > 0) orders = orders.Where(x => Has(x.SONumber, find) || Has(x.Customer, find)).ToList();
                    GridOrders.DataSource = orders;
                    GridOrders.DataBind();
                    if (orders.Count > 0 && GridOrders.FooterRow != null)
                    {
                        decimal rev = orders.Sum(x => x.Revenue), cost = orders.Sum(x => x.Cost);
                        GridOrders.FooterRow.Cells[0].Text = "Total";
                        GridOrders.FooterRow.Cells[4].Text = rev.ToString("N2");
                        GridOrders.FooterRow.Cells[5].Text = cost.ToString("N2");
                        GridOrders.FooterRow.Cells[6].Text = (rev - cost).ToString("N2");
                        GridOrders.FooterRow.Cells[7].Text = (rev != 0 ? (rev - cost) / rev : 0).ToString("P1");
                        GridOrders.FooterRow.Font.Bold = true;
                    }
                }
                else
                {
                    var items = SalesGP.Items(_db, CurrentUser.CoID, dateFrom, dateTo);
                    if (find.Length > 0) items = items.Where(x => Has(x.ItemCode, find) || Has(x.ItemDescription, find)).ToList();
                    GridItems.DataSource = items;
                    GridItems.DataBind();
                    if (items.Count > 0 && GridItems.FooterRow != null)
                    {
                        decimal rev = items.Sum(x => x.Revenue), cost = items.Sum(x => x.Cost);
                        GridItems.FooterRow.Cells[0].Text = "Total";
                        GridItems.FooterRow.Cells[3].Text = rev.ToString("N2");
                        GridItems.FooterRow.Cells[4].Text = cost.ToString("N2");
                        GridItems.FooterRow.Cells[5].Text = (rev - cost).ToString("N2");
                        GridItems.FooterRow.Cells[6].Text = (rev != 0 ? (rev - cost) / rev : 0).ToString("P1");
                        GridItems.FooterRow.Font.Bold = true;
                    }
                }
            }
        }

        protected void lbtnShow_Click(object sender, EventArgs e) { LoadReport(); }

        protected void rblView_SelectedIndexChanged(object sender, EventArgs e) { LoadReport(); }

        protected void GridOrders_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Detail") return;
            long docId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument), out docId)) return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long coId = CurrentUser.CoID;
                var h = _db.DocHeaders.Where(x => x.CompanyID == coId && x.DocID == docId)
                           .Select(x => new { x.DocumentNumber, x.CustSupName }).FirstOrDefault();
                if (h == null) return;   // not this company's order
                GridOrderLines.DataSource = SalesGP.OrderLines(_db, docId);
                GridOrderLines.DataBind();
                GridSlips.DataSource = SalesGP.OrderSlips(_db, coId, docId);
                GridSlips.DataBind();
                lblOrderTitle.Text = Server.HtmlEncode((h.DocumentNumber ?? "") + " - " + (h.CustSupName ?? ""));
                pnlOrderDetail.Visible = true;
            }
        }

        protected void GridItems_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Customers") return;
            long itemId;
            DateTime dateFrom, dateTo;
            if (!long.TryParse(Convert.ToString(e.CommandArgument), out itemId) || !ReadDates(out dateFrom, out dateTo)) return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long coId = CurrentUser.CoID;
                var itm = _db.ItemsMasters.Where(x => x.CompanyID == coId && x.ID == itemId)
                             .Select(x => new { x.Code, x.Description }).FirstOrDefault();
                GridItemCustomers.DataSource = SalesGP.ItemCustomers(_db, coId, dateFrom, dateTo, itemId);
                GridItemCustomers.DataBind();
                lblItemTitle.Text = "Customers for " + Server.HtmlEncode(itm != null ? (itm.Code + " - " + itm.Description) : ("item " + itemId));
                pnlItemDetail.Visible = true;
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
