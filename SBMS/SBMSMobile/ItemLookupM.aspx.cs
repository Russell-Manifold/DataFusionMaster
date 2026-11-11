using SBMS.Classes;
using SBMS.Models;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Item Lookup: scan a barcode or type a code / description and see the stock on hand per
    /// store / bin (and lot). Read-only - nothing is changed.
    /// </summary>
    public partial class ItemLookupM : MobileBasePage
    {
        private new UserDetails CurrentUser => Session["UserDetails"] as UserDetails;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
        }

        protected void lbtnFind_Click(object sender, EventArgs e)
        {
            string find = txtFind.Text.Trim();
            ClearResults();
            if (find == "") return;

            long co = CurrentUser.CoID;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Barcode first, then an exact code, then code / description containing the text.
                var byBarcode = db.GetOneItemFromBarcode(co, find).FirstOrDefault();
                if (byBarcode != null) { ShowItem(db, byBarcode.Code); return; }

                var exact = db.ItemsMasters.Where(x => x.CompanyID == co && x.Code == find).Select(x => x.Code).FirstOrDefault();
                if (exact != null) { ShowItem(db, exact); return; }

                var matches = db.ItemsMasters
                    .Where(x => x.CompanyID == co && (x.Code.Contains(find) || x.Description.Contains(find)))
                    .OrderBy(x => x.Code)
                    .Select(x => new { x.Code, x.Description })
                    .Take(31).ToList();

                if (matches.Count == 0) { SetFeedback(false, "&#128683; No item found for: " + Server.HtmlEncode(find)); return; }
                if (matches.Count == 1) { ShowItem(db, matches[0].Code); return; }

                rptMatches.DataSource = matches.Take(30);
                rptMatches.DataBind();
                SetFeedback(true, matches.Count > 30 ? "More than 30 items match - showing the first 30. Type more to narrow it down."
                                                     : matches.Count + " items match - tap one.");
            }
        }

        protected void lbtnPick_Click(object sender, EventArgs e)
        {
            string code = ((LinkButton)sender).CommandArgument;
            ClearResults();
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                ShowItem(db, code);
        }

        private void ShowItem(SBMSEntities db, string code)
        {
            long co = CurrentUser.CoID;
            var item = db.ItemsMasters.Where(x => x.CompanyID == co && x.Code == code)
                .Select(x => new { x.Code, x.Description, x.Unit }).FirstOrDefault();
            if (item == null) { SetFeedback(false, "&#128683; Item not found."); return; }

            bool lots = CurrentUser.CompanyUseLotNumbers;
            // One row per store (per store + lot when the company uses lots): the ledger keeps
            // lot rows even when lots are off, which showed the same store twice.
            var stock = db.GetOpeningBalancesAllStores(co)
                .Where(r => r.ItemCode == code)
                .GroupBy(r => r.StoreCode + (lots && !string.IsNullOrEmpty(r.LotNumber) ? "  -  lot " + r.LotNumber : ""))
                .Select(g => new { Where = g.Key, Qty = g.Sum(r => r.QOH ?? 0) })
                .Where(s => s.Qty != 0)
                .OrderBy(s => s.Where)
                .Select(s => new { s.Where, s.Qty, QtyText = FormatQty(s.Qty) })
                .ToList();

            lblCode.Text = Server.HtmlEncode(item.Code);
            lblDescr.Text = Server.HtmlEncode(item.Description ?? "");
            lblTotal.Text = FormatQty(stock.Sum(s => s.Qty)) + " " + Server.HtmlEncode(item.Unit ?? "");
            rptStock.DataSource = stock;
            rptStock.DataBind();
            lblNoStock.Visible = stock.Count == 0;
            pnlItem.Visible = true;
            txtFind.Text = "";
        }

        private void ClearResults()
        {
            lblFeedback.Text = "";
            lblFeedback.CssClass = "mob-feedback";
            rptMatches.DataSource = null;
            rptMatches.DataBind();
            pnlItem.Visible = false;
        }

        private void SetFeedback(bool ok, string html)
        {
            lblFeedback.Text = html;
            lblFeedback.CssClass = ok ? "mob-feedback found" : "mob-feedback notfound";
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
