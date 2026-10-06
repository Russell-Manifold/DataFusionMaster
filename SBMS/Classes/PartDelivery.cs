using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Part deliveries (Configuration -> Company -> "Part deliveries: invoice once when the
    /// Sales Order is complete", CompanyMaster.InvoiceWhenSOComplete).
    ///
    /// With the switch OFF nothing here runs: a short pick puts the balance on a NEW Sales
    /// Order and every Sales Order is invoiced on its own.
    ///
    /// With it ON the Sales Order stays open. Each delivery is its own picking slip
    /// (PS...-2, -3) for the outstanding balance, nothing goes to Sage until the order is
    /// complete, and it is then updated and invoiced ONCE for everything delivered.
    ///
    /// The deliveries of an order are its COMPLETED picking slips (PickingSlipMaster.
    /// LinkedSOrdID). Slips are never rewritten by the Sage sync, so the invoice lines are
    /// rebuilt from them when the order completes rather than kept as running totals.
    /// </summary>
    public static class PartDelivery
    {
        /// <summary>Completed slips of this order, oldest first, other than the one being closed.</summary>
        public static List<int> EarlierSlipIds(SBMSEntities db, long coId, long docId, int thisSlipId)
        {
            return db.PickingSlipMasters
                .Where(x => x.CustomerID == coId && x.LinkedSOrdID == docId && x.PSComplete == true && x.PSID != thisSlipId)
                .OrderBy(x => x.PSID)
                .Select(x => x.PSID)
                .ToList();
        }

        /// <summary>
        /// Close-off adds an order line (SBCALineID 0) for every extra lot / serial batch of a
        /// delivery. Those belong to the PREVIOUS delivery; they are rebuilt from the slips
        /// when the order completes, so they are cleared before the next close-off writes its own.
        /// </summary>
        public static void RemoveExtraLines(SBMSEntities db, long coId, long docId)
        {
            var stale = db.DocLines
                .Where(x => x.DocID == docId && x.SBCALineID == 0 && (x.CompanyID == coId || x.CompanyID == null))
                .ToList();
            if (stale.Count > 0) db.DocLines.RemoveRange(stale);
        }

        /// <summary>
        /// The order is completing. The close-off loop has just written THIS slip onto the
        /// order lines; add one order line per pick of every EARLIER slip, so the Sage update
        /// and the single invoice carry everything delivered, each with its own lot and store.
        /// Stock lines this slip did not touch were delivered earlier and are represented by
        /// those added lines, so their own picked quantity is cleared.
        /// </summary>
        public static void AppendEarlierDeliveries(SBMSEntities db, long coId, long docId,
                                                   List<PickSlipLine> thisSlipLines, List<int> earlierSlipIds)
        {
            var parents = db.DocLines.Where(x => x.DocID == docId && x.SBCALineID != 0).OrderBy(x => x.LineID).ToList();
            var onThisSlip = new HashSet<long>(
                thisSlipLines.Where(l => (l.SBCALineID ?? 0) != 0).Select(l => l.SBCALineID.Value));

            foreach (var p in parents.Where(x => (x.LineType ?? 0) == 0 && !onThisSlip.Contains(x.SBCALineID)))
            {
                p.ReceiveQty = 0;
                p.QtyLeft = 0;
                p.ReceiveComplete = true;
                p.Exclusive = 0;
                p.Discount = 0;
                p.Tax = 0;
                p.Total = 0;
                p.localCurrLineVal = 0;
            }

            foreach (int sid in earlierSlipIds)
            {
                long slipId = sid;
                var lines = db.PickSlipLines.Where(x => x.PSID == slipId).OrderBy(x => x.LineID).ToList();
                long parentId = 0;   // split parts (SBCALineID 0) belong to the line before them
                foreach (var l in lines)
                {
                    if ((l.SBCALineID ?? 0) != 0) parentId = l.SBCALineID.Value;
                    if (l.PickComplete != true) continue;
                    if ((l.LineType ?? 0) != 0) continue;   // non-stock lines stay on their own order line, as before
                    decimal qty = l.PickQty ?? l.Quantity ?? 0;
                    if (qty <= 0) continue;

                    // By Sage line id; by item when Sage has renumbered the order's lines since.
                    DocLine parent = parents.FirstOrDefault(x => x.SBCALineID == parentId)
                                  ?? parents.FirstOrDefault(x => x.SelectionId == l.SelectionId);
                    if (parent == null) continue;

                    string serialNote = "";
                    var serials = SerialPicking.GetLineSerials(db, coId, l.PSID, l.LineID);
                    if (serials.Count > 0) serialNote = SerialPicking.SerialNote(serials);

                    long storeId = db.Stores.Where(x => x.StoreCode == l.StoreCodeFrom && x.CompanyID == coId)
                                            .Select(x => x.StoreID).FirstOrDefault();

                    // Same shape as the extra-lot line the close-off loop adds.
                    DocLine d = new DocLine();
                    d.DocID = docId;
                    d.CompanyID = coId;
                    d.SBCALineID = 0;
                    d.SelectionId = l.SelectionId;
                    d.ItemCode = l.ItemCode;
                    d.ItemDescription = l.ItemDescription;
                    d.Quantity = 0;
                    d.ReceiveQty = qty;
                    d.QtyLeft = 0;
                    d.ToReceive = true;
                    d.ReceiveComplete = true;
                    d.StoreCode = l.StoreCodeFrom;
                    d.LotNumber = l.LotNumber;
                    d.Comments = serialNote.Length > 0 ? SerialPicking.CapComment(serialNote, l.Comments) : l.Comments;
                    d.UnitPriceExclusive = parent.UnitPriceExclusive;
                    d.UnitPriceInclusive = parent.UnitPriceInclusive;
                    d.TaxPercentage = parent.TaxPercentage;
                    d.DiscountPercentage = parent.DiscountPercentage;
                    d.Exclusive = d.UnitPriceExclusive * qty;
                    d.Discount = (d.UnitPriceExclusive * qty) * d.DiscountPercentage;
                    // VAT is charged on the discounted-NET amount, not the gross line value.
                    d.Tax = (d.Exclusive - d.Discount) * d.TaxPercentage;
                    d.Total = d.Exclusive - d.Discount + d.Tax;
                    d.AnalysisCategoryId1 = parent.AnalysisCategoryId1;
                    d.AnalysisCategoryId2 = parent.AnalysisCategoryId2;
                    d.AnalysisCategoryId3 = parent.AnalysisCategoryId3;
                    d.UnitCost = StoreCosting.GetStoreAvgCost(db, coId, l.SelectionId, storeId);
                    d.ItemType = parent.ItemType;
                    d.LineTaxTypeID = parent.LineTaxTypeID;
                    d.Unit = parent.Unit;
                    d.LineType = parent.LineType;
                    d.isKit = parent.isKit;
                    d.isBundle = parent.isBundle;
                    d.ExchRate = 1;
                    d.localCurrLineVal = d.Exclusive - d.Discount;
                    db.DocLines.Add(d);
                }
            }
        }

        /// <summary>Cost of everything delivered on the order, as the (negative) DocCost close-off stores.</summary>
        public static decimal TotalCost(SBMSEntities db, long coId, List<int> earlierSlipIds, int thisSlipId)
        {
            var ids = earlierSlipIds.Select(x => (long?)x).ToList();
            ids.Add(thisSlipId);
            decimal sum = db.ItemTransactions
                .Where(x => x.CompanyID == coId && ids.Contains(x.DocumentID))
                .Sum(x => (decimal?)x.TotalLineValExcl) ?? 0m;
            sum = Math.Round(sum, 2);
            return sum != 0 ? sum * -1 : 0;
        }

        /// <summary>
        /// The order stays open: create the next picking slip for what is still outstanding
        /// (the order lines' QtyLeft - the same figure a back-order Sales Order is built from)
        /// and make it the order's current slip. All or nothing. Returns the new slip number.
        /// </summary>
        public static string CreateBalanceSlip(SBMSEntities db, long coId, int roleId, long docId, PickingSlipMaster closedSlip)
        {
            using (var tx = db.Database.BeginTransaction())
            {
                var doc = db.DocHeaders.First(x => x.CompanyID == coId && x.DocID == docId);
                int slipCount = db.PickingSlipMasters.Count(x => x.CustomerID == coId && x.LinkedSOrdID == docId);
                long closedId = closedSlip.PSID;
                var closedLines = db.PickSlipLines.Where(x => x.PSID == closedId).OrderBy(x => x.LineID).ToList();

                PickingSlipMaster ps = new PickingSlipMaster();
                ps.CustomerID = coId;
                // Same numbering as a slip created from the Sales Order: PS..., then -2, -3.
                ps.PSIntNumber = (doc.DocumentNumber ?? "").Replace("SO", "PS") + "-" + (slipCount + 1);
                ps.PSGUID = Guid.NewGuid();
                ps.PSCreatedDate = DateTime.Now;
                ps.PSCreatedByRoleID = 0;
                ps.PSStatus = "Captured";
                ps.PSStationID = db.PickSlipProcesses.Where(x => x.CompanyID == coId && x.Seq == 1).Select(x => x.PSPID).FirstOrDefault();
                ps.PSActive = true;
                ps.PSDueDate = closedSlip.PSDueDate;
                ps.LinkedSOrdID = docId;
                ps.LinkedWONumber = 0;
                ps.FromStoreID = closedSlip.FromStoreID ?? 0;
                db.PickingSlipMasters.Add(ps);
                db.SaveChanges();   // PSID for the lines

                var owing = db.DocLines
                    .Where(x => x.DocID == docId && x.SBCALineID != 0 && x.ItemCode != null && x.QtyLeft > 0)
                    .OrderBy(x => x.LineID).ToList();
                foreach (var ln in owing)
                {
                    var prev = closedLines.FirstOrDefault(x => x.SBCALineID == ln.SBCALineID);
                    var item = db.ItemsMasters.Where(x => x.CompanyID == coId && x.ID == ln.SelectionId).FirstOrDefault();

                    PickSlipLine l = new PickSlipLine();
                    l.PSID = ps.PSID;
                    l.SBCALineID = ln.SBCALineID;
                    l.SelectionId = ln.SelectionId;
                    l.ItemCode = ln.ItemCode;
                    l.ItemDescription = ln.ItemDescription;
                    var bc = db.ItemBarCodeLinks.Where(x => x.ItemID == ln.SelectionId).FirstOrDefault();
                    if (bc != null) l.BarCode = bc.BarCode;
                    l.Quantity = ln.QtyLeft;
                    // The order line's comment may now hold the last delivery's serial list.
                    l.Comments = prev != null ? prev.Comments : SerialPicking.StripSerialNote(ln.Comments);
                    l.PickComplete = false;
                    l.StoreCodeFrom = prev != null ? (prev.StoreCodeFrom ?? "") : "";
                    l.LineType = ln.LineType;
                    l.CompanyID = coId;
                    l.IsLotTracked = item != null && item.IsLotTracked;
                    // Serial items are split into lines of 20, exactly as on the first slip.
                    SerialPicking.AddPickSlipLines(db, l, item != null && item.IsSerialTracked);
                }

                db.PickSlipTransactions.Add(new PickSlipTransaction
                {
                    CompanyID = coId,
                    PSID = ps.PSID,
                    FromStationID = ps.PSStationID,
                    ToStationID = ps.PSStationID,
                    MoveDate = DateTime.Now,
                    MoveQty = 1,
                    MoveBy = roleId,
                    RejectQty = 0
                });

                doc.LinkedPSID = ps.PSID;
                db.SaveChanges();
                tx.Commit();
                return ps.PSIntNumber;
            }
        }

        /// <summary>
        /// Delivery note for ONE delivery: the lines of the latest completed slip that has
        /// something picked, shaped as order lines so the note prints them unchanged.
        /// seq is that slip's position among the order's slips (1, 2, 3) - the note number
        /// suffix; 0 when the order has no delivery yet.
        /// Returns null when the note should print the order lines as it always has: no
        /// delivery yet, or an order delivered complete on a single slip.
        /// </summary>
        public static List<DocLine> DeliveryNoteLines(SBMSEntities db, long coId, long docId, List<DocLine> orderLines,
                                                      bool orderComplete, out int seq)
        {
            seq = 0;
            var slips = db.PickingSlipMasters
                .Where(x => x.CustomerID == coId && x.LinkedSOrdID == docId)
                .OrderBy(x => x.PSID)
                .Select(x => new { x.PSID, x.PSComplete })
                .ToList();

            for (int i = slips.Count - 1; i >= 0; i--)
            {
                if (slips[i].PSComplete != true) continue;
                long slipId = slips[i].PSID;
                var picked = db.PickSlipLines
                    .Where(x => x.PSID == slipId && x.PickComplete == true)
                    .OrderBy(x => x.LineID).ToList()
                    .Where(x => (x.PickQty ?? x.Quantity ?? 0) > 0).ToList();
                if (picked.Count == 0) continue;

                // One slip and the order is complete = an ordinary full delivery: no suffix, order lines as always.
                if (orderComplete && slips.Count == 1) { seq = 0; return null; }
                seq = i + 1;
                var note = new List<DocLine>();
                foreach (var l in picked)
                {
                    var parent = orderLines.FirstOrDefault(x => x.SBCALineID != 0 && x.SBCALineID == (l.SBCALineID ?? 0))
                              ?? orderLines.FirstOrDefault(x => x.SBCALineID != 0 && x.SelectionId == l.SelectionId);
                    var serials = SerialPicking.GetLineSerials(db, coId, l.PSID, l.LineID);
                    note.Add(new DocLine
                    {
                        DocID = docId,
                        SelectionId = l.SelectionId,
                        ItemCode = l.ItemCode,
                        ItemDescription = l.ItemDescription,
                        Unit = parent != null ? parent.Unit : l.Unit,
                        LotNumber = l.LotNumber,
                        StoreCode = l.StoreCodeFrom,
                        Quantity = l.Quantity,
                        ReceiveQty = l.PickQty ?? l.Quantity ?? 0,
                        Comments = serials.Count > 0
                            ? SerialPicking.CapComment(SerialPicking.SerialNote(serials), l.Comments)
                            : l.Comments
                    });
                }
                return note;
            }
            return null;
        }
    }
}
