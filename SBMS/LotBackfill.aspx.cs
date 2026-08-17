using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace SBMS
{
    // ONE-OFF ADMIN TOOL - runs across ALL companies.
    //
    // Repairs two historic gaps that leave stock stranded:
    //   1. Items on a lot-tracking company that were never flagged IsLotTracked.
    //      LoadOneItem / LoadOneItemNA used to create items with the flag defaulted
    //      to false, so receiving never asked for a lot number on them.
    //   2. Stock already sitting in a store with NO lot number. The availability
    //      proc (GetActiveLotNumbersLinkedToStores) excludes LotNumber IS NULL, so
    //      that stock cannot be picked, transferred or manufactured with.
    //
    // History is NOT rewritten. Existing rows are left exactly as they are; the
    // repair is posted as a dated re-lot movement - qty OUT with no lot, the same
    // qty back IN under a new lot, at the store's current average cost. Net qty and
    // value are unchanged, so nothing is posted to Sage (lot and store are local-only
    // concepts; Sage tracks item-level QOH and value, neither of which moves here).
    //
    // Generated lots are prefixed SYS- so an auditor can tell at a glance that they
    // were assigned by this tool and not captured from a supplier document.
    //
    // DELETE THIS PAGE once the backfill has been run and verified.
    public partial class LotBackfill : BasePage
    {
        private const string LotPrefix = "SYS-";
        private const string TrnType   = "LOT";   // re-lot movement; net-zero qty and value

        // CurrentUser comes from BasePage, which also handles the not-logged-in redirect.

        // One planned re-lot: this much of this item, in this store, gets this new lot.
        // Public because GridView binds to it by reflection.
        public class PlanRow
        {
            public long    CompanyID   { get; set; }
            public string  CompanyName { get; set; }
            public long    ItemID      { get; set; }
            public string  ItemCode    { get; set; }
            public string  ItemDescription { get; set; }
            public string  Unit        { get; set; }
            public long    StoreID     { get; set; }
            public string  StoreCode   { get; set; }
            public decimal Qty         { get; set; }
            public decimal UnitCost    { get; set; }
            public string  OldLot      { get; set; }   // NULL, "" or "0" - the value being reversed out
            public string  NewLot      { get; set; }
            public string  Note        { get; set; }   // "" = will run, otherwise why it is skipped
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null) return;   // BasePage.OnInit has already redirected
            if (!IsPostBack)
            {
                lblStatus.Text = "Click Preview. Nothing is written until you type CONFIRM and click Apply.";
                btnApply.Enabled = false;
            }
        }

        // ── PREVIEW ────────────────────────────────────────────────────────────
        protected void btnPreview_Click(object sender, EventArgs e)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                int flagCount;
                var plan = BuildPlan(db, out flagCount);

                gvPlan.DataSource = plan;
                gvPlan.DataBind();

                int willRun = plan.Count(p => p.Note == "");
                int skipped = plan.Count - willRun;

                lblStatus.Text =
                    "PREVIEW ONLY - nothing written.<br/>"
                    + "Items to flag as lot tracked: <b>" + flagCount + "</b><br/>"
                    + "Stock balances to re-lot: <b>" + willRun + "</b><br/>"
                    + "Skipped (see Note column): <b>" + skipped + "</b>";

                btnApply.Enabled = willRun > 0 || flagCount > 0;
            }
        }

        // ── APPLY ──────────────────────────────────────────────────────────────
        protected void btnApply_Click(object sender, EventArgs e)
        {
            if ((txtConfirm.Text ?? "").Trim().ToUpperInvariant() != "CONFIRM")
            {
                lblStatus.Text = "Type CONFIRM in the box to apply. Nothing was written.";
                return;
            }

            int flagged = 0, relotted = 0;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Flag the items first - the re-lot plan below only covers
                    //    lot-tracked items, so this must happen before it is built.
                    var lotCompanies = db.CompanyMasters
                        .Where(c => c.UseLotTracking == true && c.SBCACoID != null)
                        .Select(c => c.SBCACoID.Value).ToList();

                    foreach (long coId in lotCompanies)
                    {
                        var toFlag = db.ItemsMasters
                            .Where(i => i.CompanyID == coId && i.Physical == true && i.IsLotTracked == false)
                            .ToList();
                        foreach (var itm in toFlag) { itm.IsLotTracked = true; flagged++; }
                    }
                    db.SaveChanges();

                    // 2. Re-lot the stranded balances.
                    int dummy;
                    var plan = BuildPlan(db, out dummy).Where(p => p.Note == "").ToList();

                    foreach (var p in plan)
                    {
                        db.LotTrackingMasters.Add(new LotTrackingMaster
                        {
                            CompanyID       = p.CompanyID,
                            LotNumber       = p.NewLot,
                            CreatedDate     = DateTime.Now,
                            LotActive       = true,
                            ItemId          = p.ItemID,
                            ItemCode        = p.ItemCode,
                            LotTotUnitPrice = p.UnitCost,
                            LotQuantity     = p.Qty,
                            LotUserDefined  = "Assigned by lot backfill"
                        });

                        // OUT: cancels the balance under the SAME lot value it currently sits on
                        // (NULL, "" or "0"), otherwise a "0" balance would survive and double-count.
                        db.ItemTransactions.Add(NewTrn(p, -p.Qty, p.OldLot));
                        // IN: same quantity back, now carrying the lot. Net movement zero.
                        db.ItemTransactions.Add(NewTrn(p, p.Qty, p.NewLot));
                        relotted++;
                    }

                    db.SaveChanges();
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    lblStatus.Text = "FAILED - everything was rolled back, nothing changed.<br/>" + ex.Message;
                    return;
                }
            }

            gvPlan.DataSource = null;
            gvPlan.DataBind();
            btnApply.Enabled = false;
            txtConfirm.Text = "";
            lblStatus.Text = "Done. Items flagged lot tracked: <b>" + flagged
                           + "</b>. Stock balances re-lotted: <b>" + relotted + "</b>.";
        }

        // Both legs sit in the SAME store (ToID), so the store balance nets to zero.
        // StoreAvgCost is stamped with the unchanged average so GetStoreAvgCost - which
        // reads the most recent stamped value - keeps returning the right figure.
        private ItemTransaction NewTrn(PlanRow p, decimal signedQty, string lot)
        {
            return new ItemTransaction
            {
                CompanyID                 = p.CompanyID,
                DocumentID                = 0,
                DocumentType              = 4,
                TransactionType           = TrnType,
                ItemID                    = p.ItemID,
                ItemCode                  = p.ItemCode,
                // MUST be populated. These rows carry the highest TrnID, so
                // GetOpeningBalancesAllStores treats them as the latest row for the
                // item/store/lot and returns THEIR ItemDescription and Unit. Leaving
                // them null crashed Transfer.aspx, which calls
                // x.ItemDescription.ToLower() on the result with no null check.
                ItemDescription           = p.ItemDescription,
                Unit                      = p.Unit,
                LotNumber                 = lot,
                FromID                    = p.StoreID,
                ToID                      = p.StoreID,
                Qty                       = signedQty,
                TransactionDate           = DateTime.Now,
                ByRoleID                  = CurrentUser.RoleID,
                PriceExclusive            = p.UnitCost,
                AdditionalCosts           = 0,
                TotalUnitPriceExclInclAdd = p.UnitCost,
                TotalLineValExcl          = p.UnitCost * signedQty,
                StoreAvgCost              = p.UnitCost,
                ExchRate                  = 1,
                TransactionReference      = (signedQty < 0
                                                ? "Lot backfill - out (lot " + (string.IsNullOrWhiteSpace(lot) ? "none" : lot) + ")"
                                                : "Lot backfill - in " + lot)
            };
        }

        // ── PLAN ───────────────────────────────────────────────────────────────
        private List<PlanRow> BuildPlan(SBMSEntities db, out int itemsToFlag)
        {
            var plan = new List<PlanRow>();
            itemsToFlag = 0;

            var companies = db.CompanyMasters
                .Where(c => c.UseLotTracking == true && c.SBCACoID != null)
                .Select(c => new { CoID = c.SBCACoID.Value, c.CompanyName })
                .ToList();

            // Lot numbers already in use anywhere - the generated ones must not collide.
            var usedLots = new HashSet<string>(
                db.LotTrackingMasters.Where(l => l.LotNumber != null).Select(l => l.LotNumber).ToList(),
                StringComparer.OrdinalIgnoreCase);

            string stamp = DateTime.Today.ToString("ddMMyyyy");

            foreach (var co in companies)
            {
                long coId = co.CoID;

                itemsToFlag += db.ItemsMasters
                    .Count(i => i.CompanyID == coId && i.Physical == true && i.IsLotTracked == false);

                // An item counts as lot tracked if it already is, OR if step 1 is about
                // to flag it - otherwise the preview would understate the work.
                var trackedItems = db.ItemsMasters
                    .Where(i => i.CompanyID == coId && (i.IsLotTracked == true || i.Physical == true))
                    .Select(i => new { i.ID, i.Code, i.Description, i.Unit, i.AverageCost })
                    .ToList()
                    .ToDictionary(i => i.ID, i => i);

                if (trackedItems.Count == 0) continue;

                // Positive stock sitting under a junk lot value, per item + store + LOT VALUE.
                //
                // "0" is treated as junk alongside NULL and blank. It is NOT equivalent though:
                // GetActiveLotNumbersLinkedToStores filters LotNumber IS NOT NULL AND <> '', so
                // "0" PASSES and that stock is already visible as a lot literally called "0".
                //
                // Hence the grouping by LotNumber. The reversal leg has to carry the SAME value
                // it is cancelling - reverse a "0" balance under a NULL lot and the "0" lot keeps
                // its quantity while the new SYS- lot adds more, double-counting the stock.
                var lotless = db.ItemTransactions
                    .Where(x => x.CompanyID == coId
                             && (x.LotNumber == null || x.LotNumber.Trim() == "" || x.LotNumber.Trim() == "0")
                             && x.ItemID != null && x.ToID != null)
                    .GroupBy(x => new { x.ItemID, x.ToID, x.LotNumber })
                    .Select(g => new { g.Key.ItemID, g.Key.ToID, g.Key.LotNumber, Qty = g.Sum(y => y.Qty) })
                    .ToList()
                    .Where(r => (r.Qty ?? 0) > 0)
                    .ToList();

                if (lotless.Count == 0) continue;

                var stores = db.Stores.Where(s => s.CompanyID == coId)
                    .Select(s => new { s.StoreID, s.StoreCode }).ToList()
                    .ToDictionary(s => (long)s.StoreID, s => s.StoreCode);

                int seq = 1;
                foreach (var r in lotless)
                {
                    long itemId  = r.ItemID.Value;
                    long storeId = r.ToID.Value;
                    if (!trackedItems.ContainsKey(itemId)) continue;   // not a lot-tracked item

                    var itm = trackedItems[itemId];
                    string storeCode = stores.ContainsKey(storeId) ? stores[storeId] : null;

                    var row = new PlanRow
                    {
                        CompanyID   = coId,
                        CompanyName = co.CompanyName,
                        ItemID          = itemId,
                        ItemCode        = itm.Code,
                        ItemDescription = itm.Description,
                        Unit            = itm.Unit,
                        StoreID     = storeId,
                        StoreCode   = storeCode ?? ("#" + storeId),
                        Qty         = r.Qty ?? 0,
                        OldLot      = r.LotNumber,
                        Note        = ""
                    };

                    if (storeCode == null)
                    {
                        row.Note = "Store not found - skipped";
                        plan.Add(row);
                        continue;
                    }

                    // Cost: the store's running average, falling back to the item's Sage average.
                    // Zero is a legitimate answer - consignment stock genuinely costs nothing, and
                    // a zero Sage average is exactly how that presents. The lot is still created;
                    // DoItemAdjustment allows a zero-cost draw when the Sage average is also zero
                    // (there is no value to re-weight), so a zero-cost lot is usable, not stranded.
                    decimal cost = StoreCosting.GetStoreAvgCost(db, coId, itemId, storeId);
                    if (cost <= 0) cost = itm.AverageCost ?? 0m;
                    if (cost < 0) cost = 0m;
                    row.UnitCost = cost;

                    string lot;
                    do
                    {
                        lot = LotPrefix + stamp + "-" + storeCode + "-" + seq.ToString();
                        seq++;
                    } while (usedLots.Contains(lot) || lot.Length > 50);

                    usedLots.Add(lot);
                    row.NewLot = lot;
                    plan.Add(row);
                }
            }

            return plan.OrderBy(p => p.CompanyName).ThenBy(p => p.ItemCode).ThenBy(p => p.StoreCode).ToList();
        }
    }
}
