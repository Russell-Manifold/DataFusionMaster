using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Returnable items (Stock Control -> Returnables): pallets, crates, drums, cylinders -
    /// anything that goes out with a delivery and is expected back.
    ///
    /// What went OUT is read from the completed picking slips, never stored a second time,
    /// so it cannot disagree with what was actually delivered. Returns and opening balances
    /// are the only things recorded (dbo.ReturnableMovements).
    ///
    ///     Outstanding = opening balances + delivered since the item's date - returned
    ///
    /// Both tables are outside the EF model (SQL/Add_Returnables.sql) and are read and
    /// written with raw SQL here. Callers that only DISPLAY a balance (the delivery note)
    /// use the Try methods, which return nothing when the tables are not there yet.
    /// </summary>
    public static class Returnables
    {
        public class ItemRow
        {
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public DateTime Since { get; set; }
        }

        public class BalanceRow
        {
            public long CustomerID { get; set; }
            public string CustomerName { get; set; }
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Opening { get; set; }
            public decimal Delivered { get; set; }
            public decimal Returned { get; set; }
            public DateTime? LastDelivered { get; set; }
            public DateTime? LastReturned { get; set; }
            public decimal Outstanding { get { return Opening + Delivered - Returned; } }
        }

        public class MoveRow
        {
            public int RmID { get; set; }
            public DateTime MoveDate { get; set; }
            public string MoveType { get; set; }
            public string CustomerName { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Qty { get; set; }
            public string Reference { get; set; }
        }

        public class DeliveredRow
        {
            public long CustomerID { get; set; }
            public string CustomerName { get; set; }
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Delivered { get; set; }
            public DateTime? LastDelivered { get; set; }
        }

        public class MovedRow
        {
            public long CustomerID { get; set; }
            public string CustomerName { get; set; }
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal Opening { get; set; }
            public decimal Returned { get; set; }
            public DateTime? LastReturned { get; set; }
        }

        // ── Which items are returnable ──────────────────────────────────────────────────
        public static List<ItemRow> Items(SBMSEntities db, long coId)
        {
            return db.Database.SqlQuery<ItemRow>(
                "SELECT r.ItemID, i.Code AS ItemCode, i.Description AS ItemDescription, CAST(r.Since AS DATETIME) AS Since " +
                "FROM dbo.ReturnableItems r " +
                "LEFT JOIN dbo.ItemsMaster i ON i.CompanyID = r.CompanyID AND i.ID = r.ItemID " +
                "WHERE r.CompanyID = @p0 ORDER BY i.Code", coId).ToList();
        }

        public static void AddItem(SBMSEntities db, long coId, long itemId)
        {
            db.Database.ExecuteSqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM dbo.ReturnableItems WHERE CompanyID = @p0 AND ItemID = @p1) " +
                "INSERT INTO dbo.ReturnableItems (CompanyID, ItemID, Since) VALUES (@p0, @p1, CAST(GETDATE() AS DATE))",
                coId, itemId);
        }

        public static void RemoveItem(SBMSEntities db, long coId, long itemId)
        {
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.ReturnableItems WHERE CompanyID = @p0 AND ItemID = @p1", coId, itemId);
        }

        // ── Balances ────────────────────────────────────────────────────────────────────
        /// <summary>
        /// One row per customer and returnable item. customerId null = every customer.
        /// Delivered = picked quantities on completed picking slips, from the item's Since
        /// date. PickingSlipMaster.CustomerID holds the COMPANY id; the customer is on the
        /// Sales Order (DocHeader.CustSuppID).
        /// </summary>
        public static List<BalanceRow> Balances(SBMSEntities db, long coId, long? customerId = null)
        {
            object cust = customerId.HasValue ? (object)customerId.Value : DBNull.Value;

            var delivered = db.Database.SqlQuery<DeliveredRow>(
                "SELECT h.CustSuppID AS CustomerID, MAX(h.CustSupName) AS CustomerName, l.SelectionId AS ItemID, " +
                "       MAX(l.ItemCode) AS ItemCode, MAX(l.ItemDescription) AS ItemDescription, " +
                "       SUM(ISNULL(l.PickQty, ISNULL(l.Quantity, 0))) AS Delivered, MAX(p.PSCompleteDate) AS LastDelivered " +
                "FROM dbo.PickSlipLines l " +
                "JOIN dbo.PickingSlipMaster p ON p.PSID = l.PSID " +
                "JOIN dbo.DocHeader h ON h.DocID = p.LinkedSOrdID AND h.CompanyID = p.CustomerID " +
                "JOIN dbo.ReturnableItems r ON r.CompanyID = p.CustomerID AND r.ItemID = l.SelectionId " +
                "WHERE p.CustomerID = @p0 AND p.PSComplete = 1 AND l.PickComplete = 1 " +
                "  AND h.CustSuppID IS NOT NULL AND CAST(p.PSCompleteDate AS DATE) >= r.Since " +
                "  AND (@p1 IS NULL OR h.CustSuppID = @p1) " +
                "GROUP BY h.CustSuppID, l.SelectionId", coId, cust).ToList();

            var moved = db.Database.SqlQuery<MovedRow>(
                "SELECT m.CustomerID, MAX(m.CustomerName) AS CustomerName, m.ItemID, " +
                "       MAX(m.ItemCode) AS ItemCode, MAX(m.ItemDescription) AS ItemDescription, " +
                "       SUM(CASE WHEN m.MoveType = 'O' THEN m.Qty ELSE 0 END) AS Opening, " +
                "       SUM(CASE WHEN m.MoveType = 'R' THEN m.Qty ELSE 0 END) AS Returned, " +
                "       MAX(CASE WHEN m.MoveType = 'R' THEN CAST(m.MoveDate AS DATETIME) END) AS LastReturned " +
                "FROM dbo.ReturnableMovements m " +
                "JOIN dbo.ReturnableItems r ON r.CompanyID = m.CompanyID AND r.ItemID = m.ItemID " +
                "WHERE m.CompanyID = @p0 AND (@p1 IS NULL OR m.CustomerID = @p1) " +
                "GROUP BY m.CustomerID, m.ItemID", coId, cust).ToList();

            var rows = new Dictionary<string, BalanceRow>();
            foreach (var d in delivered)
            {
                rows[d.CustomerID + "|" + d.ItemID] = new BalanceRow
                {
                    CustomerID = d.CustomerID, CustomerName = d.CustomerName,
                    ItemID = d.ItemID, ItemCode = d.ItemCode, ItemDescription = d.ItemDescription,
                    Delivered = d.Delivered, LastDelivered = d.LastDelivered
                };
            }
            foreach (var m in moved)
            {
                BalanceRow b;
                string key = m.CustomerID + "|" + m.ItemID;
                if (!rows.TryGetValue(key, out b))
                {
                    b = new BalanceRow
                    {
                        CustomerID = m.CustomerID, CustomerName = m.CustomerName,
                        ItemID = m.ItemID, ItemCode = m.ItemCode, ItemDescription = m.ItemDescription
                    };
                    rows[key] = b;
                }
                b.Opening = m.Opening;
                b.Returned = m.Returned;
                b.LastReturned = m.LastReturned;
            }
            return rows.Values.OrderBy(x => x.CustomerName).ThenBy(x => x.ItemCode).ToList();
        }

        /// <summary>For display only (delivery note): nothing when the tables are not there yet.</summary>
        public static List<BalanceRow> TryBalances(SBMSEntities db, long coId, long? customerId)
        {
            try { return Balances(db, coId, customerId); }
            catch { return new List<BalanceRow>(); }
        }

        // ── Movements ───────────────────────────────────────────────────────────────────
        public static void AddMovement(SBMSEntities db, long coId, long customerId, string customerName,
                                       long itemId, string itemCode, string itemDescription, decimal qty,
                                       string moveType, DateTime moveDate, int? storeId, string reference,
                                       int roleId, long? trnId)
        {
            db.Database.ExecuteSqlCommand(
                "INSERT INTO dbo.ReturnableMovements (CompanyID, CustomerID, CustomerName, ItemID, ItemCode, ItemDescription, " +
                "Qty, MoveType, MoveDate, StoreID, Reference, ByRoleID, TrnID) " +
                "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)",
                coId, customerId, (object)customerName ?? DBNull.Value, itemId,
                (object)itemCode ?? DBNull.Value, (object)itemDescription ?? DBNull.Value,
                qty, moveType, moveDate.Date,
                storeId.HasValue ? (object)storeId.Value : DBNull.Value,
                (object)reference ?? DBNull.Value, roleId,
                trnId.HasValue ? (object)trnId.Value : DBNull.Value);
        }

        public static List<MoveRow> Recent(SBMSEntities db, long coId, int take)
        {
            return db.Database.SqlQuery<MoveRow>(
                "SELECT TOP (@p1) RmID, CAST(MoveDate AS DATETIME) AS MoveDate, " +
                "       CASE WHEN MoveType = 'O' THEN 'Opening balance' ELSE 'Return' END AS MoveType, " +
                "       CustomerName, ItemCode, ItemDescription, Qty, Reference " +
                "FROM dbo.ReturnableMovements WHERE CompanyID = @p0 ORDER BY RmID DESC", coId, take).ToList();
        }
    }
}
