using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Job Card GP report (Stock Control -> Job Card GP Analysis).
    ///
    ///   Revenue = the selling value of the job card's lines (excl VAT, after discount).
    ///             When the job card keeps its added lines internal, those lines are never
    ///             billed, so only the lines from the customer's order count as revenue.
    ///   Cost    = everything drawn from stock on the job card, at the cost it left the
    ///             store at - whether the customer saw the line or not. Materials only.
    ///   GP      = Revenue - Cost.          Margin = GP / Revenue.
    ///
    /// Cost is split into "order lines" (items the customer ordered) and "added lines"
    /// (items added on the job card - the ones kept internal when the job card has
    /// "Print all additional lines" unticked).
    ///
    /// Raw SQL: JobCardsMaster.PrintAllLines is outside the EF model
    /// (SQL/Add_JobCardHideLines.sql).
    /// </summary>
    public static class JobCardGP
    {
        public class JobRow
        {
            public int JCID { get; set; }
            public string JCNumber { get; set; }
            public string SONumber { get; set; }
            public string Customer { get; set; }
            public DateTime? CompleteDate { get; set; }
            public string JCSummary { get; set; }
            public bool PrintAllLines { get; set; }
            public decimal Revenue { get; set; }
            public decimal TotalCost { get; set; }
            public decimal AddedCost { get; set; }
            public decimal OrderCost { get { return TotalCost - AddedCost; } }
            public decimal GP { get { return Revenue - TotalCost; } }
            public decimal Margin { get { return Revenue != 0 ? (Revenue - TotalCost) / Revenue : 0; } }
            public string AddedLines { get { return PrintAllLines ? "Printed" : "Internal"; } }
        }

        public class ComponentRow
        {
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Qty { get; set; }
            public decimal Cost { get; set; }
            public int IsOrderLine { get; set; }
            public string Source { get { return IsOrderLine == 1 ? "Customer order" : "Added on job card"; } }
        }

        // A stock movement belongs to an "order line" when the job card has that item on a line
        // that came from the Sales Order (it has a Sage line id); otherwise it was added on the job card.
        private const string JobTypes = "'JC','JC-Dr','JC-Mf','JCC'";

        public static List<JobRow> Jobs(SBMSEntities db, long coId, DateTime dateFrom, DateTime dateTo)
        {
            return db.Database.SqlQuery<JobRow>(
                "SELECT j.JCID, j.JCNumber, h.DocumentNumber AS SONumber, h.CustSupName AS Customer, " +
                "       CAST(h.CompleteDate AS DATETIME) AS CompleteDate, j.JCSummary, j.PrintAllLines, " +
                "       ISNULL(r.Revenue, 0) AS Revenue, ISNULL(c.TotalCost, 0) AS TotalCost, ISNULL(c.AddedCost, 0) AS AddedCost " +
                "FROM dbo.JobCardsMaster j " +
                "JOIN dbo.DocHeader h ON h.LinkedJCID = j.JCID AND h.CompanyID = j.CustomerID " +   // the order points at the job card; JobCardsMaster.LinkedSOrdID is never filled
                "OUTER APPLY (SELECT SUM(ISNULL(l.Exclusive, 0) - ISNULL(l.Discount, 0)) AS Revenue " +
                "             FROM dbo.JobCardLines l WHERE l.JCID = j.JCID AND ISNULL(l.isKitLine, 0) = 0 " +
                "               AND (j.PrintAllLines = 1 OR ISNULL(l.SBCALineID, 0) <> 0)) r " +
                "OUTER APPLY (SELECT -SUM(x.V) AS TotalCost, -SUM(CASE WHEN x.IsOrder = 0 THEN x.V ELSE 0 END) AS AddedCost " +
                "             FROM (SELECT ISNULL(t.TotalLineValExcl, 0) AS V, " +
                "                          CASE WHEN EXISTS (SELECT 1 FROM dbo.JobCardLines o WHERE o.JCID = j.JCID " +
                "                                            AND o.SelectionId = t.ItemID AND ISNULL(o.SBCALineID, 0) <> 0) THEN 1 ELSE 0 END AS IsOrder " +
                "                   FROM dbo.ItemTransaction t " +
                "                   WHERE t.CompanyID = j.CustomerID AND t.DocumentID = j.JCID AND t.TransactionType IN (" + JobTypes + ")) x) c " +
                "WHERE j.CustomerID = @p0 AND h.Complete = 1 AND h.CompleteDate >= @p1 AND h.CompleteDate < @p2 " +
                "ORDER BY h.CompleteDate DESC, j.JCID DESC",
                coId, dateFrom.Date, dateTo.Date.AddDays(1)).ToList();
        }

        /// <summary>Everything drawn from stock on one job card, by item.</summary>
        public static List<ComponentRow> Components(SBMSEntities db, long coId, int jcId)
        {
            return db.Database.SqlQuery<ComponentRow>(
                "SELECT MAX(t.ItemCode) AS ItemCode, MAX(t.ItemDescription) AS ItemDescription, " +
                "       -SUM(ISNULL(t.Qty, 0)) AS Qty, -SUM(ISNULL(t.TotalLineValExcl, 0)) AS Cost, " +
                "       MAX(CASE WHEN o.JCID IS NULL THEN 0 ELSE 1 END) AS IsOrderLine " +
                "FROM dbo.ItemTransaction t " +
                "OUTER APPLY (SELECT TOP 1 o.JCID FROM dbo.JobCardLines o WHERE o.JCID = @p1 " +
                "             AND o.SelectionId = t.ItemID AND ISNULL(o.SBCALineID, 0) <> 0) o " +
                "WHERE t.CompanyID = @p0 AND t.DocumentID = @p1 AND t.TransactionType IN (" + JobTypes + ") " +
                "GROUP BY t.ItemID " +
                "HAVING SUM(ISNULL(t.Qty, 0)) <> 0 OR SUM(ISNULL(t.TotalLineValExcl, 0)) <> 0 " +
                "ORDER BY IsOrderLine DESC, ItemCode", coId, jcId).ToList();
        }
    }
}
