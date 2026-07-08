# Back Order — Final Model (new SO for the balance)

**Rule: one document = one pick = one invoice.** Multiple pick cycles on a single
DocLine proved too fragile. Instead, when items are short-picked and kept on back
order, the original SO closes and invoices normally for the **picked** quantities,
and a **new Sales Order is created in Sage for the outstanding balance**. The new SO
syncs back into SBMS on the next Sales Order load and follows the normal workflow
(own picking slip, own invoice). Repeats recursively if the balance is short again.

**Link:** the new SO's `Reference` = `B/O <original DocumentNumber>`.

---

## Flow

1. **Picking slip close-off** (`PickingSlip.aspx.cs` — `LbtnPickSave_Click`)
   - SweetAlert chain via `hfCloseChoice`: `full` | `backorder` | `shortship`.
     Nothing picked at all → blocked (pick something or delete the slip).
   - Per line: `ReceiveQty = pickQty` (qty to invoice), `QtyLeft = Quantity - pickQty`
     (back order) or `0` (full / shortship). pickQty is clamped to the outstanding
     balance so nothing can ever be invoiced above the ordered qty.
   - SO header: `Complete = true` (normal close). If a balance is owing, `Status =
     "Partially Invoiced"` — cleared to "Invoiced" once the balance SO exists.
   - The picking slip completes normally (final station, notifications).

2. **Update Sage SO** (`SalesOrder.aspx.cs` — `PostOrder`)
   - Original SO updated in Sage with **picked** quantities (existing behaviour) and
     invoiced as normal.
   - NEW: if any line has `QtyLeft > 0`, a new Sales Order payload (balance
     quantities, same prices/discount/tax/analysis, `ID = 0`, Reference = `B/O <docnum>`)
     is sent via `SendSalesOrder` (`SalesOrder/Save` creates when ID = 0).
   - On success: `QtyLeft` zeroed on the original's lines (balance transferred);
     `lblErr` shows the new SO number.
   - On failure: `QtyLeft` stays owing → SO remains "Partially Invoiced" as a visible
     flag; updating to Sage again retries the creation.

3. **Invoice** (`GenerateTaxInvoiceAsync`)
   - Status = `"Partially Invoiced"` while any `QtyLeft > 0`, else `"Invoiced"`.

4. **New SO appears** on OSSalesOrders via the existing Sage → SBMS sync
   (`LoadSalesOrders`) → generate picking slip → normal workflow.

---

## Data model
No new columns. `DocLine.QtyLeft` = outstanding balance pending transfer to the new
SO; `ReceiveQty` = picked qty invoiced on this SO. Transient only — zeroed once the
balance SO is created.

## Supporting fixes (kept from earlier work)
- Stock movement + sufficiency check use `PickQty` (not ordered qty) —
  `lbtnLineSave_Click`.
- Read-only **Qty_Left** column on the picking-slip grid (from the linked SO line).

## Test
Order 10 → pick 6 → close (back order) → update Sage → invoice #1 (6), new SO for 4
created in Sage with Reference `B/O <num>` → appears on OSSalesOrders after sync →
pick 4 → invoice #2 (4). Stock moves 6 then 4. Original SO ends "Invoiced".
