using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Closing off a picking slip - ONE implementation for the web Picking Slip and the mobile
    /// Close Off (moved here unchanged from PickingSlip.aspx.cs LbtnPickSave_Click, 2026-11-11;
    /// the mobile screen had its own shorter copy that missed split serial lines and part
    /// deliveries).
    ///
    /// Stock was already moved as lines were picked; this only writes the Sales Order lines,
    /// header, slip status and, for part deliveries, the next slip. Nothing is sent to Sage here.
    ///
    /// Choices (the picker's answer):
    ///   full      - everything is picked
    ///   backorder - short pick, keep the balance (part delivery: next slip on THIS order;
    ///               otherwise a new Sales Order for it when this one is posted to Sage)
    ///   shortship - short pick, cancel the balance
    /// </summary>
    public static class PickCloseOff
    {
        public class SlipState
        {
            public bool HasShortfall;
            public bool AnyPicked;
            public bool PartDelivery;
            public List<int> EarlierSlips = new List<int>();
        }

        public class Result
        {
            public string Error;          // set = nothing closed off; show it to the user
            public bool HasBackOrder;     // balance still owing
            public bool StayOpen;         // part delivery: the order waits for the next delivery
            public string BalanceSlip = ""; // part delivery: the slip created for the balance
        }

        /// <summary>What the picker has to be asked: is anything short, anything picked, earlier deliveries.</summary>
        public static SlipState Inspect(SBMSEntities db, UserDetails user, int slipid, long docid)
        {
            var lines = db.PickSlipLines.Where(x => x.PSID == slipid).ToList();
            decimal orderedQty = lines.Sum(l => (decimal)(l.Quantity ?? 0));
            decimal pickedQty = lines.Sum(l => (l.PickComplete == true ? (decimal)(l.PickQty ?? l.Quantity ?? 0) : 0m));
            var s = new SlipState
            {
                HasShortfall = (orderedQty - pickedQty) > 0.0001m,
                AnyPicked = pickedQty > 0.0001m,
                PartDelivery = user.InvoiceWhenSOComplete
            };
            // Part delivery (Configuration -> Company): the Sales Order stays open across
            // deliveries and is invoiced once. Its earlier completed slips are the deliveries
            // already made. Switch off = empty and nothing below changes.
            if (s.PartDelivery) s.EarlierSlips = PartDelivery.EarlierSlipIds(db, user.CoID, docid, slipid);
            return s;
        }

        public static Result Close(SBMSEntities db, UserDetails user, int slipid, long docid, string closeChoice)
        {
            var state = Inspect(db, user, slipid, docid);
            bool partDelivery = state.PartDelivery;
            List<int> earlierSlips = state.EarlierSlips;

            // Safety: 'full' is only valid when nothing is short.
            if (closeChoice == "full" && state.HasShortfall)
                return new Result { Error = "Some lines are not fully picked. Choose Back Order or Cancel Balance." };
            // Nothing picked = nothing to invoice, unless earlier deliveries complete the order.
            if (!state.AnyPicked && !(closeChoice == "shortship" && earlierSlips.Count > 0))
                return new Result { Error = "No items have been picked yet. Pick at least one item before closing off, or delete this picking slip." };

            // keepBackOrder = record the unpicked balance in QtyLeft; a new Sales Order is created
            // for it when this SO is posted to Sage. full / shortship both close the SO line off
            // with nothing owing (short-ship simply abandons the balance).
            bool keepBackOrder = (closeChoice == "backorder");
            // Part delivery: same line arithmetic as a back order (QtyLeft = outstanding);
            // what differs is that the order stays open instead of the balance moving to a new one.
            bool keepOpen = partDelivery && keepBackOrder;
            // Extra lot/serial lines the PREVIOUS delivery left on the order: this close-off
            // writes its own, and all deliveries are rebuilt from the slips on completion.
            if (earlierSlips.Count > 0) PartDelivery.RemoveExtraLines(db, user.CoID, docid);

            long FirstLineID = 0;
            // update SO lines from Picking Slip
            var PSLines = db.PickSlipLines.Where(x => x.PSID == slipid).OrderBy(x => x.LineID).ToList();

            // How much of each order line is still allowed to be invoiced on this run.
            //
            // A serial order line is split into batches of 20 when the slip is created, and the
            // split parts carry SBCALineID = 0 - they find their parent through FirstLineID and
            // clamp themselves to its QtyLeft. But the parent is processed FIRST and sets its own
            // QtyLeft to 0 on any close that is not a back order, so every later part clamped to
            // zero: 50 units left the building and 20 were invoiced.
            //
            // Tracking the allowance here, seeded from the balance BEFORE the parent is touched
            // and decremented as each part takes its share, keeps the original protection (a
            // re-closed slip cannot re-invoice a banked quantity) without the parts fighting
            // over a value the parent has already overwritten.
            var lineAllowance = new Dictionary<long, decimal>();

            foreach (var PsL in PSLines)
            {
                // Only lines the picker actually marked complete count as picked this cycle;
                // everything else is left on back order (pickQty = 0, no stock was moved for it).
                decimal pickQty = 0;
                if (PsL.PickComplete == true)
                {
                    pickQty = (decimal)(PsL.PickQty ?? PsL.Quantity);
                }

                // Serials picked against THIS line. They travel on the document line comment,
                // which is what reaches Sage as the invoice line's Comments - so the customer
                // can count the serials on a line against that line's quantity.
                string serialNote = "";
                var lineSerials = SerialPicking.GetLineSerials(db, user.CoID, PsL.PSID, PsL.LineID);
                if (lineSerials.Count > 0) serialNote = SerialPicking.SerialNote(lineSerials);
                if (PsL.SBCALineID != 0)
                {
                    var SOLine = db.DocLines.Where(x => x.CompanyID == user.CoID && x.DocID == docid && x.SBCALineID == PsL.SBCALineID).FirstOrDefault();
                    if (SOLine != null)
                    {
                        FirstLineID = (long)PsL.SBCALineID;
                        // QtyLeft is the running outstanding balance (mirrors the receiving flow):
                        // null on the first cycle = full ordered qty, then it counts down by what's picked.
                        decimal prevLeft = SOLine.QtyLeft ?? (decimal)SOLine.Quantity;

                        // Seeded once per order line, from the balance as it stands before this
                        // run touches it. Split parts of the same line read and decrement it.
                        if (!lineAllowance.ContainsKey(FirstLineID)) lineAllowance[FirstLineID] = prevLeft;

                        // Clamp to the outstanding balance so re-closing the same slip after a back
                        // order can't re-invoice qty that was already banked in an earlier cycle.
                        if (pickQty > lineAllowance[FirstLineID]) pickQty = lineAllowance[FirstLineID];
                        lineAllowance[FirstLineID] -= pickQty;

                        // What is still owing after every part of this line has taken its share.
                        // The parts are processed after this one, so their quantities are added
                        // to ReceiveQty and taken off QtyLeft as they go.
                        decimal outstanding = lineAllowance[FirstLineID];
                        if (outstanding < 0) outstanding = 0;
                        // QtyLeft = outstanding back-order qty (0 once fully picked, or when the balance is cancelled).
                        SOLine.QtyLeft = keepBackOrder ? outstanding : 0;
                        SOLine.ReceiveQty = pickQty;   // qty to invoice THIS cycle
                        SOLine.ReceiveComplete = (SOLine.QtyLeft == 0);
                        SOLine.StoreCode = PsL.StoreCodeFrom;
                        SOLine.LotNumber = PsL.LotNumber;
                        if (serialNote.Length > 0)
                        {
                            SOLine.Comments = SerialPicking.CapComment(serialNote, PsL.Comments);
                        }
                        SOLine.Exclusive = SOLine.UnitPriceExclusive * pickQty;
                        SOLine.Discount = (SOLine.UnitPriceExclusive * pickQty) * SOLine.DiscountPercentage;
                        // VAT is charged on the discounted-NET amount, not the gross line value.
                        SOLine.Tax = (SOLine.Exclusive - SOLine.Discount) * SOLine.TaxPercentage;
                        SOLine.Total = SOLine.Exclusive - SOLine.Discount + SOLine.Tax;
                        SOLine.LotNumber = PsL.LotNumber;
                        SOLine.ExchRate = 1;
                        SOLine.localCurrLineVal = SOLine.Exclusive - SOLine.Discount;
                    }
                }
                else
                {
                    // add new line
                    DocLine DLn = new DocLine();
                    DLn.DocID = docid;
                    DLn.SBCALineID = (long)PsL.SBCALineID;
                    DLn.SelectionId = PsL.SelectionId;
                    DLn.ItemCode = PsL.ItemCode;
                    DLn.ItemDescription = PsL.ItemDescription;
                    DLn.Quantity = 0;
                    DLn.ReceiveQty = pickQty;
                    DLn.Comments = serialNote.Length > 0
                        ? SerialPicking.CapComment(serialNote, PsL.Comments)
                        : PsL.Comments;
                    DLn.QtyLeft = 0;
                    DLn.ReceiveQty = pickQty;
                    DLn.ToReceive = true;
                    DLn.ReceiveComplete = true;
                    DLn.StoreCode = PsL.StoreCodeFrom;
                    DLn.LotNumber = PsL.LotNumber;

                    // Get values from first line of the same item
                    var FirstSOLine = db.DocLines.Where(x => x.CompanyID == user.CoID && x.DocID == docid && x.SBCALineID == FirstLineID).FirstOrDefault();

                    // Take this part's share out of the line's remaining allowance. Reading the
                    // parent's QtyLeft here instead clamped every part to zero, because the
                    // parent had already been closed off a few lines above.
                    decimal allowLeft;
                    if (!lineAllowance.TryGetValue(FirstLineID, out allowLeft))
                    {
                        allowLeft = FirstSOLine != null ? (FirstSOLine.QtyLeft ?? (decimal)FirstSOLine.Quantity) : pickQty;
                        lineAllowance[FirstLineID] = allowLeft;
                    }
                    if (pickQty > allowLeft) pickQty = allowLeft;
                    lineAllowance[FirstLineID] -= pickQty;

                    // The parent carries the order line's balance, so what this part takes has
                    // to come off it - otherwise the balance still shows units that have gone.
                    if (FirstSOLine != null && pickQty > 0)
                    {
                        decimal parentLeft = FirstSOLine.QtyLeft ?? 0;
                        parentLeft -= pickQty;
                        FirstSOLine.QtyLeft = parentLeft < 0 ? 0 : parentLeft;
                        FirstSOLine.ReceiveComplete = (FirstSOLine.QtyLeft == 0);
                    }

                    if (FirstSOLine == null)
                    {
                        return new Result { Error = $"Sales order line for item {PsL.ItemCode} was not found on the order. Re-open the sales order to refresh its lines, then save the pick again." };
                    }
                    DLn.ReceiveQty = pickQty;
                    DLn.UnitPriceExclusive = FirstSOLine.UnitPriceExclusive;
                    DLn.UnitPriceInclusive = FirstSOLine.UnitPriceInclusive;
                    DLn.TaxPercentage = FirstSOLine.TaxPercentage;
                    DLn.DiscountPercentage = FirstSOLine.DiscountPercentage;
                    DLn.Exclusive = DLn.UnitPriceExclusive * pickQty;
                    DLn.Discount = (DLn.UnitPriceExclusive * pickQty) * DLn.DiscountPercentage;
                    // VAT is charged on the discounted-NET amount, not the gross line value.
                    DLn.Tax = (DLn.Exclusive - DLn.Discount) * DLn.TaxPercentage;
                    DLn.Total = DLn.Exclusive - DLn.Discount + DLn.Tax;
                    DLn.AnalysisCategoryId1 = FirstSOLine.AnalysisCategoryId1;
                    DLn.AnalysisCategoryId2 = FirstSOLine.AnalysisCategoryId2;
                    DLn.AnalysisCategoryId3 = FirstSOLine.AnalysisCategoryId3;
                    // Cost of sale = the pick store's running weighted average (feeds the Sage
                    // invoice line UnitCost and, via the API, the line GP).
                    long pickStoreId = db.Stores.Where(x => x.StoreCode == PsL.StoreCodeFrom && x.CompanyID == user.CoID).Select(x => x.StoreID).FirstOrDefault();
                    DLn.UnitCost = StoreCosting.GetStoreAvgCost(db, user.CoID, (long)PsL.SelectionId, pickStoreId);
                    DLn.ItemType = FirstSOLine.ItemType;
                    DLn.LineTaxTypeID = FirstSOLine.LineTaxTypeID;
                    DLn.Unit = FirstSOLine.Unit;
                    DLn.LineType = FirstSOLine.LineType;
                    DLn.ExchRate = 1;
                    DLn.localCurrLineVal = DLn.Exclusive - DLn.Discount;
                    db.DocLines.Add(DLn);

                    // The parent's balance is decremented where this part's quantity is
                    // decided, above - doing it again here would take the same units off twice.
                }
            }
            db.SaveChanges();

            // Back order: this SO closes normally for the picked quantities. Any outstanding
            // balance is recorded in QtyLeft; when the SO is posted to Sage, a NEW Sales Order
            // is created for that balance (linked via Reference) and follows the normal workflow.
            bool hasBackOrder = keepBackOrder && db.DocLines.Any(x => x.DocID == docid && (x.QtyLeft ?? 0) > 0.0001m);

            // Part delivery. stayOpen = balance still owing, order waits for the next delivery.
            // Otherwise the order is completing: add the earlier deliveries to the order lines
            // so the one Sage update / invoice that follows carries everything delivered.
            bool stayOpen = keepOpen && hasBackOrder;
            if (partDelivery && !stayOpen && earlierSlips.Count > 0)
            {
                PartDelivery.AppendEarlierDeliveries(db, user.CoID, docid, PSLines, earlierSlips);
                db.SaveChanges();
            }

            var DocH = db.DocHeaders.Where(x => x.CompanyID == user.CoID && x.DocID == docid).FirstOrDefault();
            DocH.Complete = true;
            DocH.Active = true;
            DocH.CompBy = user.RoleID;
            DocH.CompleteDate = DateTime.Today;
            if (hasBackOrder && !stayOpen)
            {
                // Cleared back to "Invoiced" once the balance SO has been created at post time.
                DocH.Status = "Partially Invoiced";
            }
            if (stayOpen)
            {
                // Part delivery: the order is NOT complete - it stays open for the balance
                // and nothing is sent to Sage until the last delivery completes it.
                DocH.Complete = false;
                DocH.CompleteDate = null;
            }

            // get total cost from transactions
            DocH.DocCost = Convert.ToDecimal((db.ItemTransactions.Where(x => x.CompanyID == user.CoID && x.DocumentID == slipid).Sum(x => (decimal?)x.TotalLineValExcl) ?? 0m).ToString("N2"));
            if (DocH.DocCost != 0) DocH.DocCost = DocH.DocCost * -1;
            // Part delivery completing: cost of ALL its deliveries, not just this slip.
            if (partDelivery && !stayOpen && earlierSlips.Count > 0)
            {
                DocH.DocCost = PartDelivery.TotalCost(db, user.CoID, earlierSlips, slipid);
            }
            decimal DocValue = Convert.ToDecimal((db.DocLines.Where(x => x.CompanyID == user.CoID && x.DocID == docid).Sum(x => (decimal?)x.Exclusive) ?? 0m));
            decimal cost = DocH.DocCost ?? 0m;
            decimal gp = 0m;
            if (DocValue != 0)
            {
                gp = (DocValue - cost) / DocValue;   // e.g. 0.25
            }
            DocH.DocGP = gp;

            var Stat = db.PickSlipProcesses.Where(ws => ws.CompanyID == user.CoID).OrderByDescending(ws => ws.Seq).Select(ws => new { ws.Seq, ws.PSName, ws.PSPID }).FirstOrDefault();
            var PSH = db.PickingSlipMasters.Where(x => x.CustomerID == user.CoID && x.PSID == slipid).FirstOrDefault();
            int fromstat = (int)PSH.PSStationID;
            PSH.PSStationID = Stat.PSPID;
            PSH.PSStatus = (user.UsePickSlipTracking == true) ? Stat.PSName.ToString() : "Complete";
            PSH.PSComplete = true;
            PSH.PSCompleteDate = DateTime.Now;
            PSH.PSCompleteBy = user.RoleID;
            PSH.PSActive = false;
            PSH.LinkedSOrdID = docid;

            var PickTransaction = new PickSlipTransaction
            {
                PSID = (int?)slipid,
                MoveQty = 1,
                RejectQty = 0,
                MoveDate = DateTime.Now,
                FromStationID = fromstat,
                ToStationID = Stat.PSPID,
                CompanyID = user.CoID,
                MoveBy = user.RoleID
            };
            db.PickSlipTransactions.Add(PickTransaction);

            // get users linked to PS notifications
            var PSUsers = db.RolesMasters.Where(x => x.CompanyID == user.CoID && x.NotifyPSMove == true).ToList();
            foreach (var usr in PSUsers)
            {
                db.Notifications.Add(new Notification
                {
                    Message = PSH.PSIntNumber + " Moved to Process " + PSH.PSStatus.ToString(),
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    CompanyID = user.CoID,
                    UserRoleID = usr.RoleID
                });
            }
            db.SaveChanges();

            // Part delivery: the next picking slip, for the outstanding balance.
            var result = new Result { HasBackOrder = hasBackOrder, StayOpen = stayOpen };
            if (stayOpen)
            {
                try
                {
                    result.BalanceSlip = PartDelivery.CreateBalanceSlip(db, user.CoID, user.RoleID, docid, PSH);
                }
                catch (Exception ex)
                {
                    new ApiUrlCall().LogErrorToFile($"CoID:{user.CoID} PickingSlip part delivery - balance slip not created for DocID:{docid} - {ex.Message}");
                }
            }
            return result;
        }
    }
}
