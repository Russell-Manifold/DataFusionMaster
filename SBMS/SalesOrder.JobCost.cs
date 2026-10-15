using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Job card costing on the Sales Order screen (for the person closing off and invoicing -
    /// factory staff never see prices). Shown once the job card has closed off: the materials
    /// drawn on the ADDED lines with their cost and selling rate, the job price on the order's
    /// service line (editable until the order is posted to Sage), and the margin at that price.
    /// </summary>
    public partial class SalesOrder
    {
        /// <summary>
        /// Job cards are raised from a ONE-line sales order whose line is a Service item: that
        /// line is the job itself (header only), materials are added on the job card. Returns
        /// the message to show when the order does not qualify, or null when it does.
        /// </summary>
        private string JobCardRuleError(SBMSEntities _db, long docId)
        {
            const string how = " Set the job up as a Service item in Sage, put it on the order as the only line, and add the materials on the job card itself.";
            var lines = _db.DocLines.Where(x => x.DocID == docId).OrderBy(x => x.LineID).ToList();
            if (lines.Count != 1)
                return "Job cards are raised from a sales order with ONE line. This order has " + lines.Count + "." + how;
            var ln = lines[0];
            var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == ln.SelectionId);
            if (ln.LineType != 0 || itm == null)
                return "The order line must be a Service item, not a GL account line." + how;
            if (itm.Physical == true)
                return "The order line '" + (ln.ItemCode ?? "") + "' is a stock item. A job card's line is the job itself, a Service item. To sell stock create a picking slip; to make stock create a works order." + how;
            return null;
        }

        public class JcCostRow
        {
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Qty { get; set; }
            public decimal UnitCost { get { return Qty != 0 ? Cost / Qty : 0; } }
            public decimal Cost { get; set; }
            public decimal SellRate { get; set; }
        }

        /// <summary>The order's primary (service) line: the first line that came from Sage.</summary>
        private static DocLine PrimaryLine(SBMSEntities _db, long docId)
        {
            return _db.DocLines.Where(x => x.DocID == docId && x.SBCALineID != 0).OrderBy(x => x.LineID).FirstOrDefault();
        }

        private void LoadJobCostBlock(SBMSEntities _db, long docId, long? jcId, bool complete, bool active)
        {
            PnlJobCost.Visible = jcId != null && complete;
            if (!PnlJobCost.Visible) return;

            var job = _db.JobCardsMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.JCID == jcId);
            lblJcCostNum.Text = job != null ? job.JCNumber : "";

            // Materials = what the ADDED lines drew (the service line draws nothing). Selling rate
            // from the job card line; shown for information - internal lines are never billed.
            var sell = _db.JobCardLines.Where(x => x.JCID == jcId && (x.SBCALineID ?? 0) == 0 && x.ItemCode != null)
                          .GroupBy(x => x.ItemCode).ToDictionary(g => g.Key, g => g.Max(x => x.UnitPriceExclusive ?? 0));
            var rows = JobCardGP.Components(_db, CurrentUser.CoID, (int)jcId)
                          .Where(c => c.IsOrderLine == 0)
                          .Select(c => new JcCostRow
                          {
                              ItemCode = c.ItemCode, ItemDescription = c.ItemDescription, Qty = c.Qty, Cost = c.Cost,
                              SellRate = c.ItemCode != null && sell.ContainsKey(c.ItemCode) ? sell[c.ItemCode] : 0
                          }).ToList();
            GridJcCost.DataSource = rows;
            GridJcCost.DataBind();
            decimal matCost = rows.Sum(r => r.Cost);
            lblJcMatCost.Text = matCost.ToString("N2");

            var primary = PrimaryLine(_db, docId);
            decimal qty = primary != null ? (primary.Quantity ?? 1) : 1;
            decimal price = primary != null ? (primary.Exclusive ?? 0) - (primary.Discount ?? 0) : 0;
            lblJcQty.Text = qty.ToString("N2");
            txtJobPrice.Text = price.ToString("N2");
            lblJcMargin.Text = price != 0 ? ((price - matCost) / price).ToString("P1") : "-";
            lblJcPriceNote.Text = price == 0 ? "This job's order line has no price yet. Enter the job price and save it before updating Sage." : "";

            bool canEdit = active && primary != null;
            txtJobPrice.Enabled = canEdit;
            lbtnJobPriceSave.Visible = canEdit;
        }

        /// <summary>
        /// Saves the job price onto the order's service line. Unit price = job price / quantity;
        /// the line's values are rebuilt the way the picking slip does it. Sent to Sage by the
        /// existing "Update Sage SO" (it saves every line). Refused once the order is posted.
        /// </summary>
        protected async void lbtnJobPriceSave_Click(object sender, EventArgs e)
        {
            decimal price;
            if (!decimal.TryParse((txtJobPrice.Text ?? "").Replace(" ", "").Replace(",", ""), System.Globalization.NumberStyles.Any,
                                  System.Globalization.CultureInfo.InvariantCulture, out price) || price < 0)
            {
                AlertHelper.ShowSweetAlert(this, "Enter a valid job price (excl VAT).", "error");
                return;
            }
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DH = _db.DocHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid);
                if (DH == null || DH.Active != true)
                {
                    AlertHelper.ShowSweetAlert(this, "This order has already been updated to Sage; the job price can no longer be changed here.", "warning");
                    return;
                }
                var primary = PrimaryLine(_db, docid);
                if (primary == null)
                {
                    AlertHelper.ShowSweetAlert(this, "The order's service line was not found.", "error");
                    return;
                }
                decimal qty = primary.Quantity ?? 1; if (qty == 0) qty = 1;
                decimal discPct = primary.DiscountPercentage ?? 0;
                decimal taxPct = primary.TaxPercentage ?? 0;
                // "price" is the job price after discount; back out the discount to get the gross line value.
                decimal gross = discPct < 1 ? price / (1 - discPct) : price;
                decimal unit = Math.Round(gross / qty, 4);
                primary.UnitPriceExclusive = unit;
                primary.UnitPriceInclusive = Math.Round(unit * (1 + taxPct), 4);
                primary.Exclusive = Math.Round(unit * qty, 2);
                primary.Discount = Math.Round((primary.Exclusive ?? 0) * discPct, 2);
                primary.Tax = Math.Round(((primary.Exclusive ?? 0) - (primary.Discount ?? 0)) * taxPct, 2);
                primary.Total = (primary.Exclusive ?? 0) - (primary.Discount ?? 0) + (primary.Tax ?? 0);

                // Saved GP follows the new price (cost is unchanged). Sum the lines with the primary's new values.
                decimal revenue = 0;
                foreach (var l in _db.DocLines.Where(x => x.DocID == docid).ToList())
                    revenue += (l.Exclusive ?? 0) - (l.Discount ?? 0);
                decimal cost = DH.DocCost ?? 0;
                DH.DocGP = revenue != 0 ? (revenue - cost) / revenue : 0;
                _db.SaveChanges();

                // The Sales Order screen re-pulls line prices from Sage every time it loads, so a
                // price kept only locally would be overwritten before Update Sage SO ever ran. Send
                // it to Sage now: full order from Sage, the one line re-priced, saved back.
                if (CurrentUser.UATMode == false)
                {
                    ApiUrlCall api = new ApiUrlCall();
                    JObject soJson = await api.GetOneSOFull(CurrentUser, docid);
                    JArray soLines = soJson?["Lines"] as JArray;
                    JObject sageLine = soLines?.OfType<JObject>().FirstOrDefault(l => (long?)l["ID"] == primary.SBCALineID);
                    if (sageLine == null)
                    {
                        AlertHelper.ShowSweetAlert(this, "Price saved locally, but the line could not be found on the Sage order. Reload the order and try again.", "warning");
                        return;
                    }
                    sageLine["UnitPriceExclusive"] = primary.UnitPriceExclusive;
                    sageLine["UnitPriceInclusive"] = primary.UnitPriceInclusive;
                    sageLine["Exclusive"] = primary.Exclusive;
                    sageLine["Discount"] = primary.Discount;
                    sageLine["Tax"] = primary.Tax;
                    sageLine["Total"] = primary.Total;
                    string res = await SendSalesOrder(soJson.ToString());
                    long sageId;
                    if (!long.TryParse((res ?? "").Split('|')[0], out sageId))
                    {
                        AlertHelper.ShowSweetAlert(this, "Sage did not accept the new price: " + res, "error");
                        return;
                    }
                }
            }
            // Reload so the grid, totals and the block all show the new figures.
            Response.Redirect(Request.RawUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
