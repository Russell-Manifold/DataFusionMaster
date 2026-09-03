using System;
using System.Collections.Generic;
using System.Linq;
using SBMS.Models;

namespace SBMS.Classes
{
    /// <summary>
    /// A movement row that can be rolled up from serial level to batch level.
    /// Implemented by the TransLine class on each movement screen.
    /// </summary>
    public interface ISerialRollupRow
    {
        string LotNumber { get; set; }
        string Document { get; }
        string TransactionType { get; }
        string Store { get; }
        string TransactionReference { get; }
        decimal Qty { get; set; }
        decimal TotalLineValExcl { get; set; }
    }

    /// <summary>
    /// Rolls serial-level movement rows up to the supplier batch they belong to.
    ///
    /// A serial is stored as a lot of quantity 1, so receiving 20 units writes 20 ledger rows.
    /// That is correct, but it is not what a movement screen is for: the deepest those screens
    /// go is a LOT - one row per batch per document, quantities and values summed. Unit-by-unit
    /// detail lives on Traceability, which exists for exactly that question.
    ///
    /// Shared so the two movement screens cannot drift apart: one listing units while the other
    /// lists batches would be worse than either choice on its own.
    /// </summary>
    public static class SerialRollup
    {
        public static List<T> ToBatchLevel<T>(List<T> rows, long companyId) where T : ISerialRollupRow
        {
            if (rows == null || rows.Count == 0) return rows;

            Dictionary<string, string> batchOf;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lots = rows.Where(x => !string.IsNullOrEmpty(x.LotNumber))
                               .Select(x => x.LotNumber).Distinct().ToList();
                if (lots.Count == 0) return rows;

                // Only serials have a parent batch; ordinary lots come back empty and are left alone.
                // Keyed on the ITEM being serial tracked, not on the supplier batch. The batch
                // is optional at receiving, so keying on it meant the roll-up did nothing at all
                // on any receipt where nobody happened to type one - which is most of them - and
                // the movement screens filled with one row per unit, the very thing this exists
                // to prevent. Units with no batch group under their document instead.
                batchOf = db.LotTrackingMasters
                    .Join(db.ItemsMasters.Where(i => i.CompanyID == companyId && i.IsSerialTracked),
                          l => l.ItemId, i => (long?)i.ID, (l, i) => l)
                    .Where(x => x.CompanyID == companyId && lots.Contains(x.LotNumber))
                    .Select(x => new { x.LotNumber, x.ParentLotNumber })
                    .ToList()
                    .GroupBy(x => x.LotNumber)
                    .ToDictionary(g => g.Key, g => g.First().ParentLotNumber ?? "",
                                  StringComparer.OrdinalIgnoreCase);
            }
            if (batchOf.Count == 0) return rows;

            var result = new List<T>();
            var merged = new Dictionary<string, T>();

            foreach (var r in rows)
            {
                string batch;
                if (r.LotNumber == null || !batchOf.TryGetValue(r.LotNumber, out batch))
                {
                    result.Add(r);          // ordinary lot - untouched
                    continue;
                }

                // One row per batch, per document, per movement type and store. With no batch
                // the document does the grouping, so a batchless receipt still collapses to one
                // row instead of one per unit.
                string groupOn = batch.Length > 0 ? batch : "(no batch)";
                string key = string.Join("|", groupOn, r.Document ?? "", r.TransactionType ?? "",
                                             r.Store ?? "", r.TransactionReference ?? "");
                T hit;
                if (!merged.TryGetValue(key, out hit))
                {
                    // Show the batch where there is one; otherwise say how the row was grouped
                    // rather than naming one arbitrary unit out of the set.
                    r.LotNumber = batch.Length > 0 ? batch : "(serials)";
                    merged[key] = r;
                    result.Add(r);
                }
                else
                {
                    hit.Qty += r.Qty;
                    hit.TotalLineValExcl += r.TotalLineValExcl;
                }
            }
            return result;
        }
    }
}
