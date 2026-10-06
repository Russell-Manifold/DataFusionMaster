using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Job cards: hide the lines ADDED on the job card from the customer
    /// (Job Card -> "Print all additional lines", unticked).
    ///
    /// The lines that came from the customer's Sales Order always print. A line added on
    /// the job card (webbing, rivets, transport) is internal when the box is unticked:
    ///   - it stays on the job card and on Data Fusion's own Sales Order screen (marked);
    ///   - it is left off the Sage Sales Order, the invoice and both delivery notes;
    ///   - it is NEVER billed: it is a cost of the job only. A line added on the job card
    ///     picks up the item's list price automatically, and that price is ignored;
    ///   - if it is stock, it is adjusted out of Sage and journalled to Cost of Sales,
    ///     referenced to the job card.
    ///
    /// HOW A LINE IS RECOGNISED: job card close-off writes every job card line onto the
    /// Sales Order (DocLines). A line that came from Sage keeps its Sage line id; a line
    /// added on the job card has SBCALineID = 0. On a job card order that is the test.
    ///
    /// Everything here is raw SQL against columns/tables outside the EF model
    /// (SQL/Add_JobCardHideLines.sql). The read methods answer "print everything" when
    /// those are not there yet, so nothing changes until the script has been run.
    /// </summary>
    public static class JobCardHide
    {
        public class JobInfo
        {
            public int JCID { get; set; }
            public string JCNumber { get; set; }
            public string JCSummary { get; set; }
            public bool PrintAllLines { get; set; }
        }

        /// <summary>The job card of this Sales Order, or null (no job card, or the columns are absent).</summary>
        public static JobInfo ForOrder(SBMSEntities db, long coId, long docId)
        {
            try
            {
                return db.Database.SqlQuery<JobInfo>(
                    "SELECT j.JCID, j.JCNumber, j.JCSummary, j.PrintAllLines " +
                    "FROM dbo.DocHeader h JOIN dbo.JobCardsMaster j ON j.JCID = h.LinkedJCID AND j.CustomerID = h.CompanyID " +
                    "WHERE h.CompanyID = @p0 AND h.DocID = @p1", coId, docId).FirstOrDefault();
            }
            catch { return null; }
        }

        /// <summary>True when this order's job card says the added lines are internal.</summary>
        public static bool HidesAddedLines(SBMSEntities db, long coId, long docId)
        {
            JobInfo j = ForOrder(db, coId, docId);
            return j != null && !j.PrintAllLines;
        }

        /// <summary>The job card's own setting. True (print everything) when the column is absent.</summary>
        public static bool GetPrintAll(SBMSEntities db, long coId, int jcId)
        {
            try
            {
                return db.Database.SqlQuery<bool>(
                    "SELECT PrintAllLines FROM dbo.JobCardsMaster WHERE JCID = @p0 AND CustomerID = @p1", jcId, coId)
                    .DefaultIfEmpty(true).First();
            }
            catch { return true; }
        }

        public static void SetPrintAll(SBMSEntities db, long coId, int jcId, bool printAll)
        {
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.JobCardsMaster SET PrintAllLines = @p0 WHERE JCID = @p1 AND CustomerID = @p2",
                printAll, jcId, coId);
        }

        /// <summary>Cost of Sales account for job card materials (Configuration -> Company). 0 = not set.</summary>
        public static long CosAccountId(SBMSEntities db, long coId)
        {
            try
            {
                return db.Database.SqlQuery<long?>(
                    "SELECT JobCosAccountID FROM dbo.CompanyMaster WHERE SBCACoID = @p0", coId).FirstOrDefault() ?? 0;
            }
            catch { return 0; }
        }

        public static void SetCosAccountId(SBMSEntities db, long coId, long accountId)
        {
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.CompanyMaster SET JobCosAccountID = @p0 WHERE SBCACoID = @p1",
                accountId > 0 ? (object)accountId : DBNull.Value, coId);
        }

        // ── What has already gone to Sage for this order ────────────────────────────────
        /// <summary>Units of this item already adjusted out of Sage for this order.</summary>
        public static decimal AdjustedQty(SBMSEntities db, long coId, long docId, long itemId)
        {
            return db.Database.SqlQuery<decimal?>(
                "SELECT SUM(Qty) FROM dbo.JobCardSagePostings WHERE CompanyID = @p0 AND DocID = @p1 AND Kind = 'A' AND ItemID = @p2",
                coId, docId, itemId).FirstOrDefault() ?? 0;
        }

        /// <summary>Value of the adjustments ('A') or of the journals ('J') posted for this order.</summary>
        public static decimal PostedAmount(SBMSEntities db, long coId, long docId, string kind)
        {
            return db.Database.SqlQuery<decimal?>(
                "SELECT SUM(Amount) FROM dbo.JobCardSagePostings WHERE CompanyID = @p0 AND DocID = @p1 AND Kind = @p2",
                coId, docId, kind).FirstOrDefault() ?? 0;
        }

        public static void Record(SBMSEntities db, long coId, long docId, int jcId, string kind,
                                  long? itemId, decimal? qty, decimal amount, string reference)
        {
            db.Database.ExecuteSqlCommand(
                "INSERT INTO dbo.JobCardSagePostings (CompanyID, DocID, JCID, Kind, ItemID, Qty, Amount, Reference) " +
                "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)",
                coId, docId, jcId, kind,
                itemId.HasValue ? (object)itemId.Value : DBNull.Value,
                qty.HasValue ? (object)qty.Value : DBNull.Value,
                amount, (object)reference ?? DBNull.Value);
        }

        /// <summary>True once anything has been posted to Sage for this order's hidden lines.</summary>
        public static bool AnyPosted(SBMSEntities db, long coId, long docId)
        {
            try
            {
                return db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM dbo.JobCardSagePostings WHERE CompanyID = @p0 AND DocID = @p1", coId, docId)
                    .FirstOrDefault() > 0;
            }
            catch { return false; }
        }

        // ── Job costing ─────────────────────────────────────────────────────────────────
        private static readonly string[] JobTypes = { "JC", "JC-Dr", "JC-Mf", "JCC" };

        /// <summary>
        /// What the job cost: everything drawn from stock on the job card, printed or hidden,
        /// at the cost it left the store at. Reversals are the same types with the opposite
        /// sign, so they net off. Materials only.
        /// </summary>
        public static decimal JobCost(SBMSEntities db, long coId, long jcId)
        {
            string[] types = JobTypes;   // a local, so EF sees a plain list
            decimal sum = db.ItemTransactions
                .Where(x => x.CompanyID == coId && x.DocumentID == jcId && types.Contains(x.TransactionType))
                .Sum(x => (decimal?)x.TotalLineValExcl) ?? 0m;
            return Math.Round(sum * -1, 2);
        }
    }
}
