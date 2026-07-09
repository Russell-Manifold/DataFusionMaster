using SBMS.Models;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Per-store moving weighted-average costing on the ItemTransaction ledger.
    /// Each store keeps its OWN running average (ItemTransaction.StoreAvgCost), stamped on every
    /// movement row so the latest row for a store IS that store's current average cost:
    ///   - inbound  (Qty &gt; 0): average re-weighted by the incoming value
    ///   - outbound (Qty &lt; 0): average unchanged; stock leaves at the current store average
    /// A store is identified by ItemTransaction.ToID; on-hand is the signed sum of Qty for that store.
    /// Sage remains item-wide (one average per item) — per-store cost/GP is an SBMS-internal concept.
    /// </summary>
    public static class StoreCosting
    {
        /// <summary>Current on-hand qty of an item in a store (signed sum of ledger Qty).</summary>
        public static decimal GetStoreQty(SBMSEntities db, long companyId, long itemId, long storeId)
        {
            return db.ItemTransactions
                .Where(x => x.CompanyID == companyId && x.ItemID == itemId && x.ToID == storeId)
                .Sum(x => (decimal?)x.Qty) ?? 0m;
        }

        /// <summary>Current running weighted-average cost of an item in a store (latest stamped value).</summary>
        public static decimal GetStoreAvgCost(SBMSEntities db, long companyId, long itemId, long storeId)
        {
            return db.ItemTransactions
                .Where(x => x.CompanyID == companyId && x.ItemID == itemId && x.ToID == storeId && x.StoreAvgCost != null)
                .OrderByDescending(x => x.TrnID)
                .Select(x => x.StoreAvgCost)
                .FirstOrDefault() ?? 0m;
        }

        /// <summary>
        /// New store average after an inbound movement of <paramref name="inQty"/> units carrying
        /// <paramref name="inValue"/> total value. Outbound movements never change the average.
        /// </summary>
        public static decimal NewAvgOnInbound(decimal prevQty, decimal prevAvg, decimal inQty, decimal inValue)
        {
            if (inQty <= 0) return prevAvg;
            if (prevQty <= 0) return inValue / inQty;      // no meaningful existing layer to blend
            decimal newQty = prevQty + inQty;
            decimal newVal = (prevQty * prevAvg) + inValue;
            return newVal / newQty;
        }

        /// <summary>
        /// Compute the store's new running average for a signed movement and the line value to book.
        /// Call BEFORE the new row is saved (the current row must not be in the ledger yet).
        /// For inbound pass the actual incoming value; for outbound the value is derived at the
        /// current store average and returned via <paramref name="lineValue"/>.
        /// </summary>
        public static decimal ComputeMovement(SBMSEntities db, long companyId, long itemId, long storeId,
                                              decimal signedQty, decimal inValue, out decimal lineValue)
        {
            decimal prevQty = GetStoreQty(db, companyId, itemId, storeId);
            decimal prevAvg = GetStoreAvgCost(db, companyId, itemId, storeId);

            if (signedQty >= 0)
            {
                lineValue = inValue;
                return NewAvgOnInbound(prevQty, prevAvg, signedQty, inValue);
            }

            // outbound: leave at current average, book the value at that average
            lineValue = signedQty * prevAvg;   // signedQty is negative -> negative line value
            return prevAvg;
        }
    }
}
