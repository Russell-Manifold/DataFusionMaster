using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Expiry control.
    ///
    /// Everything still on hand that carries a Use By date, soonest first, so short-dated
    /// stock gets used or returned before it becomes a write-off. Quantities come from the
    /// stock ledger, not from LotTrackingMaster.LotQuantity - the ledger is what every other
    /// on-hand figure in the app is built from, so this can never disagree with them.
    ///
    /// Serials appear here as themselves (a lot of quantity 1); ordinary lots appear as one
    /// row with their remaining quantity. Both are useful, so neither is filtered out.
    /// </summary>
    public partial class ExpiryControl : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            imgCoImg.ImageUrl = File.Exists(Server.MapPath(imgPath))
                ? ResolveUrl(imgPath)
                : ResolveUrl("~/images/CoImages/0000.png");

            if (!IsPostBack)
            {
                LoadStores();
                BindGrid();
            }
        }

        private void LoadStores()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = db.Stores
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true
                             && x.StoreCode != "CoR" && x.StoreCode != "CoD")
                    .OrderBy(x => x.StoreCode).ToList();

                ddlStore.Items.Clear();
                ddlStore.Items.Add(new ListItem("all stores", "0"));
                foreach (var s in stores)
                {
                    ddlStore.Items.Add(new ListItem(s.StoreCode, s.StoreID.ToString()));
                }
            }
        }

        // ── Data ─────────────────────────────────────────────────────────────────────────

        /// <summary>Expiry and batch for one lot, read in blocks to keep the IN lists small.</summary>
        private class LotFacts
        {
            public Nullable<DateTime> UseByDate { get; set; }
            public string ParentLotNumber { get; set; }
        }

        /// <summary>One row per lot / serial still on hand.</summary>
        public class ExpiryRow
        {
            public string LotNumber { get; set; }
            public string Batch { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public string StoreCode { get; set; }
            public decimal OnHand { get; set; }
            public Nullable<DateTime> UseByDate { get; set; }
            public string DaysLeft { get; set; }
        }

        private List<ExpiryRow> BuildRows()
        {
            int shortDays;
            if (!int.TryParse(txtDays.Text, out shortDays) || shortDays < 0) shortDays = 30;

            long storeFilter;
            long.TryParse(ddlStore.SelectedValue, out storeFilter);

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID)
                                      .Select(x => new { x.StoreID, x.StoreCode }).ToList()
                                      .ToDictionary(x => (long)x.StoreID, x => x.StoreCode);

                // On hand per lot and store, straight off the ledger. Only lots that still have
                // something left are of any interest - a lot fully supplied cannot expire.
                // The store filter goes INTO the query. Applied afterwards, SQL still aggregated
                // every lot in every store across all history and the result was thrown away in
                // memory - and with serial tracking there is one group per physical unit ever
                // received, so that grows without limit.
                var ledger = db.ItemTransactions
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.LotNumber != null && x.LotNumber != "");
                if (storeFilter > 0) ledger = ledger.Where(x => x.ToID == storeFilter);

                var balances = ledger
                    .GroupBy(x => new { x.LotNumber, x.ToID })
                    .Select(g => new
                    {
                        g.Key.LotNumber,
                        g.Key.ToID,
                        Qty = g.Sum(x => x.Qty) ?? 0,
                        ItemCode = g.Max(x => x.ItemCode),
                        ItemDescription = g.Max(x => x.ItemDescription)
                    })
                    .Where(x => x.Qty > 0)
                    .ToList();

                if (balances.Count == 0) return new List<ExpiryRow>();

                // Expiry and batch come off the lot master.
                // Read in blocks: passing every lot number back as an IN list breaks past SQL
                // Server's 2100-parameter limit once a company has a year of serialised receipts.
                var lotNums = balances.Select(x => x.LotNumber).Distinct().ToList();
                var lots = new Dictionary<string, LotFacts>(StringComparer.OrdinalIgnoreCase);
                const int block = 500;
                for (int i = 0; i < lotNums.Count; i += block)
                {
                    var slice = lotNums.Skip(i).Take(block).ToList();
                    foreach (var l in db.LotTrackingMasters
                                        .Where(x => x.CompanyID == CurrentUser.CoID && slice.Contains(x.LotNumber))
                                        .Select(x => new { x.LotNumber, x.UseByDate, x.ParentLotNumber })
                                        .ToList())
                    {
                        if (l.LotNumber != null && !lots.ContainsKey(l.LotNumber))
                            lots[l.LotNumber] = new LotFacts { UseByDate = l.UseByDate, ParentLotNumber = l.ParentLotNumber };
                    }
                }

                var rows = balances.Select(b =>
                {
                    DateTime? useBy = null;
                    string batch = "";
                    if (b.LotNumber != null && lots.ContainsKey(b.LotNumber))
                    {
                        useBy = lots[b.LotNumber].UseByDate;
                        batch = lots[b.LotNumber].ParentLotNumber ?? "";
                    }

                    return new ExpiryRow
                    {
                        LotNumber = b.LotNumber,
                        Batch = batch,
                        ItemCode = b.ItemCode ?? "",
                        ItemDescription = b.ItemDescription ?? "",
                        StoreCode = (b.ToID.HasValue && stores.ContainsKey(b.ToID.Value)) ? stores[b.ToID.Value] : "",
                        OnHand = b.Qty,
                        UseByDate = useBy,
                        DaysLeft = useBy.HasValue
                            ? ((int)(useBy.Value.Date - DateTime.Today).TotalDays).ToString()
                            : ""
                    };
                }).ToList();

                // Tiles always describe the whole picture, whatever filter is applied below.
                lblExpired.Text = rows.Count(x => x.UseByDate.HasValue && x.UseByDate.Value.Date < DateTime.Today).ToString();
                lblShort.Text   = rows.Count(x => x.UseByDate.HasValue && x.UseByDate.Value.Date >= DateTime.Today
                                                  && x.UseByDate.Value.Date <= DateTime.Today.AddDays(shortDays)).ToString();
                lblOk.Text      = rows.Count(x => x.UseByDate.HasValue && x.UseByDate.Value.Date > DateTime.Today.AddDays(shortDays)).ToString();
                lblNoDate.Text  = rows.Count(x => !x.UseByDate.HasValue).ToString();

                switch (ddlView.SelectedValue)
                {
                    case "expired":
                        rows = rows.Where(x => x.UseByDate.HasValue && x.UseByDate.Value.Date < DateTime.Today).ToList();
                        break;
                    case "short":
                        rows = rows.Where(x => x.UseByDate.HasValue
                                            && x.UseByDate.Value.Date <= DateTime.Today.AddDays(shortDays)).ToList();
                        break;
                    case "nodate":
                        rows = rows.Where(x => !x.UseByDate.HasValue).ToList();
                        break;
                }

                // Soonest first; undated last, since there is nothing to act on.
                return rows
                    .OrderBy(x => x.UseByDate.HasValue ? 0 : 1)
                    .ThenBy(x => x.UseByDate ?? DateTime.MaxValue)
                    .ThenBy(x => x.ItemCode)
                    .ToList();
            }
        }

        private void BindGrid()
        {
            lblErr.Text = "";
            var rows = BuildRows();
            GridExpiry.DataSource = rows;
            GridExpiry.DataBind();
            ViewState["RowCount"] = rows.Count;
        }

        /// <summary>Red for expired, amber for short dated - readable at a glance from a distance.</summary>
        protected void GridExpiry_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var row = e.Row.DataItem as ExpiryRow;
            if (row == null || !row.UseByDate.HasValue) return;

            int shortDays;
            if (!int.TryParse(txtDays.Text, out shortDays) || shortDays < 0) shortDays = 30;

            if (row.UseByDate.Value.Date < DateTime.Today)
            {
                e.Row.BackColor = System.Drawing.Color.MistyRose;
                e.Row.ForeColor = System.Drawing.Color.Firebrick;
                e.Row.Font.Bold = true;
            }
            else if (row.UseByDate.Value.Date <= DateTime.Today.AddDays(shortDays))
            {
                e.Row.BackColor = System.Drawing.Color.LightGoldenrodYellow;
            }
        }

        // ── Events ───────────────────────────────────────────────────────────────────────

        protected void ddlView_SelectedIndexChanged(object sender, EventArgs e) { GridExpiry.PageIndex = 0; BindGrid(); }
        protected void ddlStore_SelectedIndexChanged(object sender, EventArgs e) { GridExpiry.PageIndex = 0; BindGrid(); }
        protected void txtDays_TextChanged(object sender, EventArgs e) { GridExpiry.PageIndex = 0; BindGrid(); }

        protected void GridExpiry_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridExpiry.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void lbtnExcel_Click(object sender, EventArgs e)
        {
            var rows = BuildRows();
            if (rows.Count == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Nothing to export for this filter.", "warning");
                return;
            }

            DataTable dt = new DataTable();
            dt.Columns.Add("Serial / Lot");
            dt.Columns.Add("Batch");
            dt.Columns.Add("Item");
            dt.Columns.Add("Description");
            dt.Columns.Add("Store");
            dt.Columns.Add("On Hand", typeof(decimal));
            dt.Columns.Add("Expires");
            dt.Columns.Add("Days");

            foreach (var r in rows)
            {
                dt.Rows.Add(r.LotNumber, r.Batch, r.ItemCode, r.ItemDescription, r.StoreCode,
                            r.OnHand,
                            r.UseByDate.HasValue ? r.UseByDate.Value.ToString("dd MMM yyyy") : "",
                            r.DaysLeft);
            }

            ExcelHelper.ExportToExcel(dt, "Expiry_" + DateTime.Today.ToString("yyyyMMdd"), "Expiry");
        }

        // ── Navigation ───────────────────────────────────────────────────────────────────

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
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
