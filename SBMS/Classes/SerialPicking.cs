using System;
using System.Collections.Generic;
using System.Linq;
using SBMS.Models;

namespace SBMS.Classes
{
    /// <summary>
    /// Serial-number handling for picking slips.
    ///
    /// A serial item is picked in whole units, and the customer has to be able to count the
    /// serials on an invoice line against that line's quantity. So a serialised order line is
    /// split when the picking slip is created: 50 becomes 20 + 20 + 10. Each of those lines is
    /// then picked and given its own serials, and each becomes one invoice line.
    ///
    /// The serials themselves live in dbo.PickSlipLineSerials, deliberately outside the EF
    /// model (raw SQL, like PickSlipLineLPNs) so nothing here can put the EDMX out of step
    /// with the database.
    /// </summary>
    public static class SerialPicking
    {
        /// <summary>Units per picking-slip line for a serial-tracked item.</summary>
        public const int SerialsPerLine = 20;

        /// <summary>
        /// Adds a picking-slip line, splitting it into lines of at most <see cref="SerialsPerLine"/>
        /// when the item is serial tracked. A non-serial item is added exactly as before.
        /// </summary>
        public static void AddPickSlipLines(SBMSEntities db, PickSlipLine line, bool isSerialTracked)
        {
            decimal qty = line.Quantity ?? 0;

            if (!isSerialTracked || qty <= SerialsPerLine)
            {
                db.PickSlipLines.Add(line);
                return;
            }

            decimal remaining = qty;
            bool first = true;
            while (remaining > 0)
            {
                decimal take = Math.Min(SerialsPerLine, remaining);
                remaining -= take;

                if (first)
                {
                    // Reuse the line we were handed for the first batch, so anything set on it
                    // by the caller that is not copied below still applies.
                    line.Quantity = take;
                    db.PickSlipLines.Add(line);
                    first = false;
                    continue;
                }

                db.PickSlipLines.Add(new PickSlipLine
                {
                    PSID            = line.PSID,
                    // 0, NOT the parent's id. Close-off matches the FIRST line back to the
                    // order line by SBCALineID and creates a fresh document line for every
                    // other one. Copying the id here would make all three batches overwrite
                    // the same order line, and only the last would be invoiced.
                    SBCALineID      = 0,
                    SelectionId     = line.SelectionId,
                    ItemCode        = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    BarCode         = line.BarCode,
                    LineType        = line.LineType,
                    Quantity        = take,
                    Unit            = line.Unit,
                    Comments        = line.Comments,
                    StoreCodeFrom   = line.StoreCodeFrom,
                    PickComplete    = false,
                    CompanyID       = line.CompanyID,
                    IsLotTracked    = line.IsLotTracked
                });
            }
        }

        /// <summary>
        /// A "Serial No: ..." note that fits the document comment column (varchar 400).
        ///
        /// Twenty serials of any length will overrun it, and EF throws on save - losing the
        /// receipt or the close-off entirely. Better to list what fits and say how many more
        /// there are; the full list is always on the Traceability screen.
        /// </summary>
        public static string SerialNote(IEnumerable<string> serials, int limit = 300)
        {
            var list = serials.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (list.Count == 0) return "";

            var sb = new System.Text.StringBuilder("Serial No: ");
            int used = 0;
            for (int i = 0; i < list.Count; i++)
            {
                string next = (used == 0 ? "" : ", ") + list[i];
                if (sb.Length + next.Length > limit)
                {
                    sb.Append(" (+").Append(list.Count - used).Append(" more)");
                    break;
                }
                sb.Append(next);
                used++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// A serial note joined to whatever the line already said, guaranteed to fit the
        /// document comment column (varchar 400).
        ///
        /// Capping only the note was not enough - appending the existing comment could still
        /// overrun the column, and re-saving a line kept prepending a fresh note onto the last
        /// one. Any previous note is stripped first, so a re-save replaces rather than stacks.
        /// </summary>
        /// <param name="max">
        /// Deliberately short of the 400-character column. The Sage payload prepends
        /// "Store: &lt;code&gt; : " to whatever is stored here, so a comment that merely fits the
        /// column can still overrun what Sage is sent.
        /// </param>
        public static string CapComment(string serialNote, string existing, int max = 330)
        {
            string tail = StripSerialNote(existing);
            string joined = serialNote + (string.IsNullOrWhiteSpace(tail) ? "" : " : " + tail);
            return joined.Length <= max ? joined : joined.Substring(0, max);
        }

        /// <summary>Removes a previously written "Serial No: ..." prefix from a comment.</summary>
        public static string StripSerialNote(string comment)
        {
            if (string.IsNullOrEmpty(comment)) return "";
            if (!comment.StartsWith("Serial No:", StringComparison.OrdinalIgnoreCase)) return comment;

            int at = comment.IndexOf(" : ", StringComparison.Ordinal);
            return at >= 0 ? comment.Substring(at + 3) : "";
        }

        /// <summary>
        /// True only when the failure is "the table has not been deployed yet".
        ///
        /// Every read used to swallow EVERY exception and answer "no serials", which is the one
        /// answer that must never be guessed: both the receiving and the picking posting paths
        /// treat an empty list as "not a serial line" and fall back to a single lumped movement -
        /// twenty units against a lot holding one, with the other nineteen stranded on hand. A
        /// deadlock, a timeout or a permissions problem would have produced exactly that, silently.
        ///
        /// A missing table is different: the feature simply is not deployed on this database, and
        /// behaving as "none" is correct. Everything else is rethrown so the operation fails loudly.
        /// </summary>
        private static bool IsMissingTable(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                var sql = e as System.Data.SqlClient.SqlException;
                if (sql != null && (sql.Number == 208 || sql.Number == 2812)) return true;   // invalid object name
                if (e.Message.IndexOf("Invalid object name", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>One received unit: its serial and its own expiry.</summary>
        public class ReceivedUnit
        {
            public string Serial { get; set; }
            public Nullable<DateTime> UseByDate { get; set; }
        }

        /// <summary>
        /// Replaces the serials held against one RECEIVING line. The line itself stays whole -
        /// 10 units is one line and one supplier-invoice line of 10 - so what Sage shows matches
        /// the supplier's paperwork. The stock ledger still moves each unit separately.
        /// </summary>
        public static void SetDocLineSerials(SBMSEntities db, long companyId, long docId, long sbcaLineId,
                                             IEnumerable<ReceivedUnit> units, long byRoleId)
        {
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.DocLineSerials WHERE CompanyID = @p0 AND DocID = @p1 AND SBCALineID = @p2",
                companyId, docId, sbcaLineId);

            foreach (var u in units.Where(x => !string.IsNullOrWhiteSpace(x.Serial)))
            {
                db.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.DocLineSerials (CompanyID, DocID, SBCALineID, Serial, UseByDate, CreatedBy) "
                    + "VALUES (@p0, @p1, @p2, @p3, @p4, @p5)",
                    companyId, docId, sbcaLineId, u.Serial.Trim().ToUpperInvariant(),
                    (object)u.UseByDate ?? DBNull.Value, byRoleId);
            }
        }

        /// <summary>Serials received against one line, in the order they were captured.</summary>
        public static List<ReceivedUnit> GetDocLineSerials(SBMSEntities db, long companyId, long docId, long sbcaLineId)
        {
            try
            {
                return db.Database.SqlQuery<ReceivedUnit>(
                    "SELECT Serial, UseByDate FROM dbo.DocLineSerials "
                    + "WHERE CompanyID = @p0 AND DocID = @p1 AND SBCALineID = @p2 ORDER BY ID",
                    companyId, docId, sbcaLineId).ToList();
            }
            catch (Exception ex) when (IsMissingTable(ex)) { return new List<ReceivedUnit>(); }
        }

        /// <summary>Every serial received on one document (purchase order), capture order.</summary>
        public static List<string> GetDocSerials(SBMSEntities db, long companyId, long docId)
        {
            try
            {
                return db.Database.SqlQuery<string>(
                    "SELECT Serial FROM dbo.DocLineSerials "
                    + "WHERE CompanyID = @p0 AND DocID = @p1 ORDER BY ID",
                    companyId, docId).ToList();
            }
            catch (Exception ex) when (IsMissingTable(ex)) { return new List<string>(); }
        }

        /// <summary>
        /// The serials recorded against the OTHER lines of a slip.
        ///
        /// The stock ledger has no picking-slip-line column, so when a line is un-picked this is
        /// what keeps it from reversing units that belong to another line of the same slip - the
        /// 20-unit split parts of one order line being the case that matters.
        /// </summary>
        public static List<string> SerialsOnOtherLines(SBMSEntities db, long companyId, long psid, long lineId)
        {
            try
            {
                return db.Database.SqlQuery<string>(
                    "SELECT Serial FROM dbo.PickSlipLineSerials "
                    + "WHERE CompanyID = @p0 AND PSID = @p1 AND LineID <> @p2",
                    companyId, psid, lineId).ToList();
            }
            catch (Exception ex) when (IsMissingTable(ex)) { return new List<string>(); }
        }

        /// <summary>Serials already picked against one line, in the order they were captured.</summary>
        public static List<string> GetLineSerials(SBMSEntities db, long companyId, long psid, long lineId)
        {
            try
            {
                return db.Database.SqlQuery<string>(
                    "SELECT Serial FROM dbo.PickSlipLineSerials "
                    + "WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2 ORDER BY ID",
                    companyId, psid, lineId).ToList();
            }
            catch (Exception ex) when (IsMissingTable(ex)) { return new List<string>(); }
        }

        /// <summary>
        /// Of the serials offered, those already picked onto a DIFFERENT line of the same
        /// slip. Picking one twice would despatch the same physical unit twice.
        /// </summary>
        public static List<string> PickedElsewhere(SBMSEntities db, long companyId, long psid,
                                                   long lineId, IEnumerable<string> serials)
        {
            var wanted = serials.Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => x.Trim().ToUpperInvariant()).ToList();
            if (wanted.Count == 0) return new List<string>();

            try
            {
                var onSlip = db.Database.SqlQuery<string>(
                    "SELECT Serial FROM dbo.PickSlipLineSerials "
                    + "WHERE CompanyID = @p0 AND PSID = @p1 AND LineID <> @p2",
                    companyId, psid, lineId).ToList();

                return wanted.Where(x => onSlip.Any(y => string.Equals(x, y, StringComparison.OrdinalIgnoreCase)))
                             .ToList();
            }
            catch (Exception ex) when (IsMissingTable(ex)) { return new List<string>(); }
        }

        /// <summary>
        /// Replaces the serials held against one line. Called when the operator confirms their
        /// selection, so re-picking a line corrects it rather than adding to it.
        /// </summary>
        public static void SetLineSerials(SBMSEntities db, long companyId, long psid, long lineId,
                                          IEnumerable<string> serials, long byRoleId)
        {
            db.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.PickSlipLineSerials WHERE CompanyID = @p0 AND PSID = @p1 AND LineID = @p2",
                companyId, psid, lineId);

            foreach (string serial in serials.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                db.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.PickSlipLineSerials (CompanyID, PSID, LineID, Serial, CreatedBy) "
                    + "VALUES (@p0, @p1, @p2, @p3, @p4)",
                    companyId, psid, lineId, serial.Trim().ToUpperInvariant(), byRoleId);
            }
        }
    }
}
