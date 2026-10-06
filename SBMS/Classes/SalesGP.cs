using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Sales GP reports (Stock Control -> "Picking Slip GP Analysis" / "Item Sales GP Analysis",
    /// one page: SalesGP.aspx).
    ///
    ///   Orders  = Sales Orders that are complete AND invoiced (DocHeader.Status = 'Invoiced'),
    ///             by completion date.
    ///   Revenue = the order's lines, excl VAT, after discount (DocLines).
    ///   Cost    = the stock ledger: what every picking slip of the order drew ('PS'
    ///             movements), plus what its job card drew ('JC' movements), at the cost it
    ///             left the store at. Materials only.
    ///   GP      = Revenue - Cost.         Margin = GP / Revenue.
    ///
    /// Same basis as the Job Card GP report, so the three GP reports agree. Deliberately
    /// nothing is read from DITransactionsTbl (redundant, to be removed).
    /// </summary>
    public static class SalesGP
    {
        public class OrderRow
        {
            public long DocID { get; set; }
            public string SONumber { get; set; }
            public string Customer { get; set; }
            public DateTime? CompleteDate { get; set; }
            public string Route { get; set; }
            public decimal Revenue { get; set; }
            public decimal Cost { get; set; }
            public decimal GP { get { return Revenue - Cost; } }
            public decimal Margin { get { return Revenue != 0 ? (Revenue - Cost) / Revenue : 0; } }
        }

        public class OrderLineRow
        {
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Qty { get; set; }
            public decimal Revenue { get; set; }
        }

        public class SlipRow
        {
            public string Slip { get; set; }
            public DateTime? CompleteDate { get; set; }
            public decimal Cost { get; set; }
        }

        public class ItemRow
        {
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Qty { get; set; }
            public decimal Revenue { get; set; }
            public decimal Cost { get; set; }
            public decimal GP { get { return Revenue - Cost; } }
            public decimal Margin { get { return Revenue != 0 ? (Revenue - Cost) / Revenue : 0; } }
        }

        public class ItemCustomerRow
        {
            public string Customer { get; set; }
            public int Orders { get; set; }
            public decimal Qty { get; set; }
            public decimal Revenue { get; set; }
        }

        private const string JobTypes = "'JC','JC-Dr','JC-Mf','JCC'";
        private const string InvoicedOrders =
            "h.CompanyID = @p0 AND h.DocType = 5 AND h.Complete = 1 AND h.Status = 'Invoiced' " +
            "AND h.CompleteDate >= @p1 AND h.CompleteDate < @p2";

        public static List<OrderRow> Orders(SBMSEntities db, long coId, DateTime dateFrom, DateTime dateTo)
        {
            return db.Database.SqlQuery<OrderRow>(
                "SELECT h.DocID, h.DocumentNumber AS SONumber, h.CustSupName AS Customer, CAST(h.CompleteDate AS DATETIME) AS CompleteDate, " +
                "       CASE WHEN h.LinkedJCID IS NOT NULL THEN 'Job card' ELSE 'Picking slip' END AS Route, " +
                "       ISNULL(r.Revenue, 0) AS Revenue, ISNULL(ps.Cost, 0) + ISNULL(jc.Cost, 0) AS Cost " +
                "FROM dbo.DocHeader h " +
                "OUTER APPLY (SELECT SUM(ISNULL(l.Exclusive, 0) - ISNULL(l.Discount, 0)) AS Revenue FROM dbo.DocLines l WHERE l.DocID = h.DocID) r " +
                "OUTER APPLY (SELECT -SUM(ISNULL(t.TotalLineValExcl, 0)) AS Cost FROM dbo.ItemTransaction t " +
                "             WHERE t.CompanyID = h.CompanyID AND t.TransactionType = 'PS' " +
                "               AND t.DocumentID IN (SELECT p.PSID FROM dbo.PickingSlipMaster p WHERE p.CustomerID = h.CompanyID AND p.LinkedSOrdID = h.DocID)) ps " +
                "OUTER APPLY (SELECT -SUM(ISNULL(t.TotalLineValExcl, 0)) AS Cost FROM dbo.ItemTransaction t " +
                "             WHERE t.CompanyID = h.CompanyID AND t.DocumentID = h.LinkedJCID AND t.TransactionType IN (" + JobTypes + ")) jc " +
                "WHERE " + InvoicedOrders + " " +
                "ORDER BY h.CompleteDate DESC, h.DocumentNumber DESC",
                coId, dateFrom.Date, dateTo.Date.AddDays(1)).ToList();
        }

        public static List<OrderLineRow> OrderLines(SBMSEntities db, long docId)
        {
            return db.Database.SqlQuery<OrderLineRow>(
                "SELECT l.ItemCode, l.ItemDescription, ISNULL(l.Quantity, 0) AS Qty, ISNULL(l.Exclusive, 0) - ISNULL(l.Discount, 0) AS Revenue " +
                "FROM dbo.DocLines l WHERE l.DocID = @p0 ORDER BY l.LineID", docId).ToList();
        }

        /// <summary>The order's picking slips with what each drew; a job card order shows its job card instead.</summary>
        public static List<SlipRow> OrderSlips(SBMSEntities db, long coId, long docId)
        {
            return db.Database.SqlQuery<SlipRow>(
                "SELECT p.PSIntNumber AS Slip, CAST(p.PSCompleteDate AS DATETIME) AS CompleteDate, -SUM(ISNULL(t.TotalLineValExcl, 0)) AS Cost " +
                "FROM dbo.PickingSlipMaster p " +
                "LEFT JOIN dbo.ItemTransaction t ON t.CompanyID = p.CustomerID AND t.DocumentID = p.PSID AND t.TransactionType = 'PS' " +
                "WHERE p.CustomerID = @p0 AND p.LinkedSOrdID = @p1 " +
                "GROUP BY p.PSID, p.PSIntNumber, p.PSCompleteDate " +
                "UNION ALL " +
                "SELECT j.JCNumber, CAST(j.JCCompleteDate AS DATETIME), -SUM(ISNULL(t.TotalLineValExcl, 0)) " +
                "FROM dbo.DocHeader h JOIN dbo.JobCardsMaster j ON j.JCID = h.LinkedJCID AND j.CustomerID = h.CompanyID " +
                "LEFT JOIN dbo.ItemTransaction t ON t.CompanyID = h.CompanyID AND t.DocumentID = j.JCID AND t.TransactionType IN (" + JobTypes + ") " +
                "WHERE h.CompanyID = @p0 AND h.DocID = @p1 " +
                "GROUP BY j.JCID, j.JCNumber, j.JCCompleteDate " +
                "ORDER BY 2", coId, docId).ToList();
        }

        /// <summary>
        /// Per item over the period. Items with cost but no revenue (internal job card
        /// materials) are included at zero revenue, so the item view adds up to the order view.
        /// </summary>
        public static List<ItemRow> Items(SBMSEntities db, long coId, DateTime dateFrom, DateTime dateTo)
        {
            return db.Database.SqlQuery<ItemRow>(
                "WITH inv AS (SELECT h.DocID, h.CompanyID, h.LinkedJCID FROM dbo.DocHeader h WHERE " + InvoicedOrders + "), " +
                "rev AS (SELECT l.SelectionId AS ItemID, MAX(l.ItemCode) AS ItemCode, MAX(l.ItemDescription) AS ItemDescription, " +
                "               SUM(ISNULL(l.Quantity, 0)) AS Qty, SUM(ISNULL(l.Exclusive, 0) - ISNULL(l.Discount, 0)) AS Revenue " +
                "        FROM dbo.DocLines l JOIN inv ON inv.DocID = l.DocID GROUP BY l.SelectionId), " +
                "cost AS (SELECT t.ItemID, -SUM(ISNULL(t.TotalLineValExcl, 0)) AS Cost FROM dbo.ItemTransaction t " +
                "         WHERE t.CompanyID = @p0 AND ( " +
                "               (t.TransactionType = 'PS' AND t.DocumentID IN (SELECT p.PSID FROM dbo.PickingSlipMaster p JOIN inv ON inv.DocID = p.LinkedSOrdID WHERE p.CustomerID = @p0)) " +
                "            OR (t.TransactionType IN (" + JobTypes + ") AND t.DocumentID IN (SELECT inv.LinkedJCID FROM inv WHERE inv.LinkedJCID IS NOT NULL))) " +
                "         GROUP BY t.ItemID) " +
                "SELECT ISNULL(rev.ItemID, cost.ItemID) AS ItemID, ISNULL(rev.ItemCode, im.Code) AS ItemCode, ISNULL(rev.ItemDescription, im.Description) AS ItemDescription, " +
                "       ISNULL(rev.Qty, 0) AS Qty, ISNULL(rev.Revenue, 0) AS Revenue, ISNULL(cost.Cost, 0) AS Cost " +
                "FROM rev FULL OUTER JOIN cost ON cost.ItemID = rev.ItemID " +
                "LEFT JOIN dbo.ItemsMaster im ON im.CompanyID = @p0 AND im.ID = ISNULL(rev.ItemID, cost.ItemID) " +
                "ORDER BY Revenue DESC, ItemCode",
                coId, dateFrom.Date, dateTo.Date.AddDays(1)).ToList();
        }

        public static List<ItemCustomerRow> ItemCustomers(SBMSEntities db, long coId, DateTime dateFrom, DateTime dateTo, long itemId)
        {
            return db.Database.SqlQuery<ItemCustomerRow>(
                "SELECT h.CustSupName AS Customer, COUNT(DISTINCT h.DocID) AS Orders, SUM(ISNULL(l.Quantity, 0)) AS Qty, " +
                "       SUM(ISNULL(l.Exclusive, 0) - ISNULL(l.Discount, 0)) AS Revenue " +
                "FROM dbo.DocLines l JOIN dbo.DocHeader h ON h.DocID = l.DocID " +
                "WHERE " + InvoicedOrders + " AND l.SelectionId = @p3 " +
                "GROUP BY h.CustSupName ORDER BY Revenue DESC",
                coId, dateFrom.Date, dateTo.Date.AddDays(1), itemId).ToList();
        }
    }
}
