using Newtonsoft.Json.Linq;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Consignment month end (Stock Control -> Consignment Stock). A consignment location is a
    /// store linked to a Sage customer (Stores.ConsignCustomerId). A run reads the ledger for that
    /// store, the user types the count, Used = on hand - counted, and "Create Sales Order" raises
    /// the Sage SO for the used quantities and books them out of the location. Raw SQL: the two
    /// tables are outside the EF model (SQL/Add_ConsignmentUsage.sql).
    /// </summary>
    public static class ConsignmentUsage
    {
        public class StoreRow
        {
            public int StoreID { get; set; }
            public string StoreCode { get; set; }
            public string StoreDescript { get; set; }
            public long ConsignCustomerId { get; set; }
            public string ConsignCustomer { get; set; }
            public string ConsignAddress { get; set; }
        }
        public class UsageHeader
        {
            public long UsageID { get; set; }
            public int StoreID { get; set; }
            public long CustomerID { get; set; }
            public string CustomerName { get; set; }
            public DateTime CountDate { get; set; }
            public DateTime? PeriodFrom { get; set; }
            public string Status { get; set; }
            public long? SageSODocID { get; set; }
            public string SageSONumber { get; set; }
            public decimal TotalExcl { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? OrderedDate { get; set; }
            public string StoreCode { get; set; }
            public string StoreDescript { get; set; }
            public string ConsignAddress { get; set; }
        }
        public class UsageLine
        {
            public long LineID { get; set; }
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public string Unit { get; set; }
            public decimal Opening { get; set; }
            public decimal Delivered { get; set; }
            public decimal OtherMoves { get; set; }
            public decimal? Counted { get; set; }
            public decimal Used { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineExcl { get; set; }
            public long? TaxTypeId { get; set; }
            public decimal TaxPerc { get; set; }
            /// <summary>What the ledger says the customer holds right now.</summary>
            public decimal OnHand { get { return Opening + Delivered + OtherMoves; } }
        }

        /// <summary>True when this Sage sales order was raised by a consignment month end: the stock
        /// is already at the customer and has been booked out, so it must not be picked.</summary>
        public static bool IsUsageOrder(SBMSEntities db, long coId, long docId)
        {
            try
            {
                return db.Database.SqlQuery<int>(
                    "SELECT CASE WHEN OBJECT_ID('dbo.ConsignmentUsage','U') IS NULL THEN 0 ELSE " +
                    "(SELECT COUNT(*) FROM dbo.ConsignmentUsage WHERE CompanyID = @p0 AND SageSODocID = @p1) END", coId, docId).First() > 0;
            }
            catch { return false; }
        }

        public static bool TablesExist(SBMSEntities db)
        {
            return db.Database.SqlQuery<int>("SELECT CASE WHEN OBJECT_ID('dbo.ConsignmentUsageLine','U') IS NULL THEN 0 ELSE 1 END").First() == 1;
        }

        /// <summary>Active stores linked to a Sage customer.</summary>
        public static List<StoreRow> Stores(SBMSEntities db, long coId)
        {
            return db.Database.SqlQuery<StoreRow>(
                "SELECT StoreID, StoreCode, StoreDescript, ConsignCustomerId, ConsignCustomer, ConsignAddress " +
                "FROM dbo.Stores WHERE CompanyID = @p0 AND ConsignCustomerId IS NOT NULL AND ISNULL(StoreActive,1) = 1 " +
                "ORDER BY ConsignCustomer, StoreDescript", coId).ToList();
        }

        private const string HeaderSelect =
            "SELECT u.UsageID, u.StoreID, u.CustomerID, u.CustomerName, u.CountDate, u.PeriodFrom, u.Status, u.SageSODocID, " +
            "u.SageSONumber, u.TotalExcl, u.CreatedDate, u.OrderedDate, s.StoreCode, s.StoreDescript, s.ConsignAddress " +
            "FROM dbo.ConsignmentUsage u JOIN dbo.Stores s ON s.StoreID = u.StoreID ";

        public static UsageHeader Header(SBMSEntities db, long coId, long usageId)
        {
            return db.Database.SqlQuery<UsageHeader>(HeaderSelect + "WHERE u.CompanyID = @p0 AND u.UsageID = @p1", coId, usageId).FirstOrDefault();
        }

        /// <summary>The unfinished run for a store, if there is one (only ever one).</summary>
        public static UsageHeader OpenDraft(SBMSEntities db, long coId, int storeId)
        {
            return db.Database.SqlQuery<UsageHeader>(HeaderSelect + "WHERE u.CompanyID = @p0 AND u.StoreID = @p1 AND u.Status = 'Draft'", coId, storeId).FirstOrDefault();
        }

        public static List<UsageHeader> History(SBMSEntities db, long coId, int take = 60)
        {
            return db.Database.SqlQuery<UsageHeader>(HeaderSelect.Replace("SELECT u.UsageID", "SELECT TOP " + take + " u.UsageID") +
                "WHERE u.CompanyID = @p0 ORDER BY u.CreatedDate DESC", coId).ToList();
        }

        /// <summary>End of the count date: the count is what the customer held at close of that day.</summary>
        public static DateTime EndOfDay(DateTime d) { return d.Date.AddDays(1).AddMilliseconds(-3); }

        /// <summary>Count date of the last completed run for the store (the next run's period starts after it).</summary>
        public static DateTime? LastCountDate(SBMSEntities db, long coId, int storeId)
        {
            return db.Database.SqlQuery<DateTime?>(
                "SELECT MAX(CountDate) FROM dbo.ConsignmentUsage WHERE CompanyID = @p0 AND StoreID = @p1 AND Status = 'Ordered'",
                coId, storeId).FirstOrDefault();
        }

        /// <summary>
        /// Starts a run: reads the ledger for the store from the end of the previous run's count date
        /// (PeriodFrom) up to the END of this count date. Movements after the count date (a delivery
        /// made after the customer counted) belong to the next month. Opening = balance at PeriodFrom;
        /// Delivered = transfers in during the period; OtherMoves = any other movement in the period.
        /// </summary>
        public static long StartDraft(SBMSEntities db, UserDetails user, StoreRow store, DateTime countDate)
        {
            long coId = user.CoID;
            DateTime? lastCount = LastCountDate(db, coId, store.StoreID);
            object from = lastCount.HasValue ? (object)EndOfDay(lastCount.Value) : DBNull.Value;
            DateTime upTo = EndOfDay(countDate);

            long usageId = db.Database.SqlQuery<long>(
                "INSERT INTO dbo.ConsignmentUsage (CompanyID, StoreID, CustomerID, CustomerName, CountDate, PeriodFrom, Status, CreatedBy) " +
                "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'Draft', @p6); SELECT CAST(SCOPE_IDENTITY() AS bigint)",
                coId, store.StoreID, store.ConsignCustomerId, store.ConsignCustomer, countDate.Date, from, user.RoleID).First();

            // One line per item that has ever moved through the store and still has a balance or
            // had a delivery this period. ISNULL(@from, 1900) makes the first run all "delivered".
            db.Database.ExecuteSqlCommand(
                "INSERT INTO dbo.ConsignmentUsageLine (UsageID, CompanyID, ItemID, ItemCode, ItemDescription, Unit, Opening, Delivered, OtherMoves, UnitPrice, TaxTypeId, TaxPerc) " +
                "SELECT @p0, @p1, t.ItemID, i.Code, i.Description, i.Unit, t.Opening, t.Delivered, t.OtherMoves, " +
                "       ISNULL(i.PriceExclusive, 0), i.TaxTypeIdSales, ISNULL(i.TaxTypeSalesPerc, 0) " +
                "FROM ( SELECT ItemID, " +
                "         SUM(CASE WHEN TransactionDate <= ISNULL(@p3, '19000101') THEN Qty ELSE 0 END) AS Opening, " +
                "         SUM(CASE WHEN TransactionDate > ISNULL(@p3, '19000101') AND TransactionType = 'TRF' AND Qty > 0 THEN Qty ELSE 0 END) AS Delivered, " +
                "         SUM(CASE WHEN TransactionDate > ISNULL(@p3, '19000101') AND NOT (TransactionType = 'TRF' AND Qty > 0) THEN Qty ELSE 0 END) AS OtherMoves " +
                "       FROM dbo.ItemTransaction WHERE CompanyID = @p1 AND ToID = @p2 AND TransactionDate <= @p4 GROUP BY ItemID " +
                "       HAVING SUM(Qty) <> 0 OR SUM(CASE WHEN TransactionDate > ISNULL(@p3, '19000101') AND TransactionType = 'TRF' AND Qty > 0 THEN Qty ELSE 0 END) <> 0 ) t " +
                "JOIN dbo.ItemsMaster i ON i.ID = t.ItemID AND i.CompanyID = @p1 " +
                "ORDER BY i.Code",
                usageId, coId, store.StoreID, from, upTo);
            return usageId;
        }

        public static List<UsageLine> Lines(SBMSEntities db, long coId, long usageId)
        {
            return db.Database.SqlQuery<UsageLine>(
                "SELECT LineID, ItemID, ItemCode, ItemDescription, Unit, Opening, Delivered, OtherMoves, Counted, Used, UnitPrice, LineExcl, TaxTypeId, TaxPerc " +
                "FROM dbo.ConsignmentUsageLine WHERE CompanyID = @p0 AND UsageID = @p1 ORDER BY ItemCode", coId, usageId).ToList();
        }

        /// <summary>Saves one line's count and price; Used and value are recalculated here, nowhere else.</summary>
        public static void SaveLine(SBMSEntities db, long coId, long lineId, decimal? counted, decimal price)
        {
            object cnt = counted.HasValue ? (object)counted.Value : DBNull.Value;
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ConsignmentUsageLine SET Counted = @p2, UnitPrice = @p3, " +
                "  Used = CASE WHEN @p2 IS NULL THEN 0 ELSE (Opening + Delivered + OtherMoves) - @p2 END, " +
                "  LineExcl = CASE WHEN @p2 IS NULL OR (Opening + Delivered + OtherMoves) - @p2 <= 0 THEN 0 " +
                "             ELSE ROUND(((Opening + Delivered + OtherMoves) - @p2) * @p3, 2) END " +
                "WHERE CompanyID = @p0 AND LineID = @p1", coId, lineId, cnt, price);
        }

        public static void RefreshTotal(SBMSEntities db, long coId, long usageId)
        {
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ConsignmentUsage SET TotalExcl = (SELECT ISNULL(SUM(LineExcl), 0) FROM dbo.ConsignmentUsageLine WHERE UsageID = @p1) " +
                "WHERE CompanyID = @p0 AND UsageID = @p1", coId, usageId);
        }

        public static void DeleteDraft(SBMSEntities db, long coId, long usageId)
        {
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.ConsignmentUsageLine WHERE CompanyID = @p0 AND UsageID = @p1 AND EXISTS (SELECT 1 FROM dbo.ConsignmentUsage WHERE UsageID = @p1 AND Status = 'Draft'); " +
                "DELETE FROM dbo.ConsignmentUsage WHERE CompanyID = @p0 AND UsageID = @p1 AND Status = 'Draft'", coId, usageId);
        }

        /// <summary>Records the Sage order on the draft the moment Sage returns it, BEFORE the stock is
        /// booked, so a failure afterwards can never lead to a second Sage order: the retry sees the
        /// claim and only finishes the booking.</summary>
        public static void ClaimOrder(SBMSEntities db, long coId, long usageId, long soId, string soNumber)
        {
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ConsignmentUsage SET SageSODocID = @p2, SageSONumber = @p3 " +
                "WHERE CompanyID = @p0 AND UsageID = @p1 AND Status = 'Draft'", coId, usageId, soId, (object)soNumber ?? DBNull.Value);
        }

        public static void MarkOrdered(SBMSEntities db, long coId, long usageId, DateTime orderedAt)
        {
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ConsignmentUsage SET Status = 'Ordered', OrderedDate = @p2 " +
                "WHERE CompanyID = @p0 AND UsageID = @p1 AND Status = 'Draft'", coId, usageId, orderedAt);
        }

        /// <summary>The Sage SalesOrder/Save payload for the used quantities (lines with Used > 0 only).</summary>
        public static string BuildSalesOrderJson(UsageHeader h, List<UsageLine> lines)
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var jo = new JObject
            {
                ["CustomerId"] = h.CustomerID,
                ["Date"] = today,
                ["DeliveryDate"] = today,
                ["DueDate"] = today,
                ["Inclusive"] = false,
                ["Reference"] = ("Consignment " + h.StoreCode + " " + h.CountDate.ToString("yyyy-MM-dd")).Trim(),
                ["Message"] = "Consignment stock used at " + h.StoreDescript + " - count " + h.CountDate.ToString("dd MMM yyyy") + ". Statement " + h.UsageID + "."
            };
            var arr = new JArray();
            foreach (var l in lines.Where(x => x.Used > 0))
            {
                decimal excl = Math.Round(l.Used * l.UnitPrice, 2);
                decimal tax = Math.Round(excl * l.TaxPerc / 100m, 2);
                var line = new JObject
                {
                    ["SelectionId"] = l.ItemID,
                    ["LineType"] = 0,
                    ["Description"] = l.ItemDescription ?? l.ItemCode,
                    ["Quantity"] = l.Used,
                    ["Unit"] = l.Unit ?? "",
                    ["UnitPriceExclusive"] = l.UnitPrice,
                    ["UnitPriceInclusive"] = Math.Round(l.UnitPrice * (1 + l.TaxPerc / 100m), 4),
                    ["TaxPercentage"] = l.TaxPerc,
                    ["DiscountPercentage"] = 0,
                    ["Exclusive"] = excl,
                    ["Discount"] = 0,
                    ["Tax"] = tax,
                    ["Total"] = excl + tax
                };
                if (l.TaxTypeId.HasValue && l.TaxTypeId.Value > 0) line["TaxTypeId"] = l.TaxTypeId.Value;
                arr.Add(line);
            }
            jo["Lines"] = arr;
            return jo.ToString();
        }

        /// <summary>
        /// Books the used quantities out of the consignment store: one ledger row per line, at the
        /// store's average cost (same rule as a picking-slip sale), dated the END of the count date so
        /// the next run's opening balance equals this count. Sage stock is reduced by the invoice, not
        /// here. DocumentType 10 = sale, as picking-slip issues use.
        /// </summary>
        public static void BookUsage(SBMSEntities db, UserDetails user, UsageHeader h, List<UsageLine> lines, long soId, string soNumber)
        {
            DateTime bookedAt = EndOfDay(h.CountDate);
            string reference = ("Consignment usage " + (soNumber ?? "") + " " + h.StoreCode).Trim();
            if (reference.Length > 100) reference = reference.Substring(0, 100);
            foreach (var l in lines.Where(x => x.Used > 0))
            {
                decimal storeAvg = StoreCosting.GetStoreAvgCost(db, user.CoID, l.ItemID, h.StoreID);
                if (storeAvg == 0)
                    storeAvg = db.ItemsMasters.Where(x => x.CompanyID == user.CoID && x.ID == l.ItemID).Select(x => x.AverageCost ?? 0).FirstOrDefault();
                var t = new ItemTransaction
                {
                    CompanyID = user.CoID,
                    DocumentID = soId,
                    DocumentType = 10,                // sale (same as picking-slip issues)
                    TransactionType = "CSU",          // consignment usage
                    ItemID = l.ItemID,
                    ItemCode = l.ItemCode ?? "",
                    ItemDescription = l.ItemDescription ?? "",
                    Unit = l.Unit,
                    FromID = 0,
                    ToID = h.StoreID,
                    Qty = l.Used * -1,
                    PriceExclusive = storeAvg,
                    TotalUnitPriceExclInclAdd = storeAvg,
                    AdditionalCosts = 0,
                    TotalLineValExcl = storeAvg * l.Used * -1,
                    StoreAvgCost = storeAvg,
                    TransactionDate = bookedAt,
                    ByRoleID = user.RoleID,
                    TransactionReference = reference,
                    LotNumber = string.Empty,
                    ExchRate = 1
                };
                db.ItemTransactions.Add(t);
            }
            db.SaveChanges();
        }
    }
}
