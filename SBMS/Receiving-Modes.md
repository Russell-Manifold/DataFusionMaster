# SBMS_v2 Goods-In — Receiving Modes

One scanner + web platform that supports **three** ways of getting stock from a Purchase
Order into the warehouse. Each mode can be switched on/off per customer/user (the on/off
config is planned; the behaviours below are what each mode does).

---

## Warehouse roles (Store flags)

The behaviour is driven entirely by flags on each **Store** — no special warehouse types.

| Flag | Meaning |
|---|---|
| `AllowReceiving` | The **default receiving warehouse** (the "holding" store). One per company. |
| `IsRejectStore`  | The **reject / quarantine warehouse**. One per company. |
| `AllowPicking`   | A normal **bin / pick location**. |
| `IsWip`          | A **work-in-progress** location. |

## Supporting records

| Record | Role |
|---|---|
| `DocHeader.RecStatus` | `0` = Started, `1` = Ready (counted, not yet posted), `2` = Submitted (posted to Sage). |
| `ItemTransaction` | The stock-movement ledger. `GRN` = receipt from supplier; `TRF` = internal store-to-store transfer. |
| `ReceivingOutstanding` | The short-received register — how much of a PO line is still owed. |

---

## Mode 1 — Count on the scanner, receive on the web

The floor only **recognises and counts**; the receiving desk does the financial receipt.

1. **Scanner:** open the PO → scan an item → type the **Accept qty** and/or **Reject qty** for
   that line → **Mark Ready**. Nothing else — no Sage, no stock movement, no location.
   The header is set to `RecStatus = 1` (Ready).
2. **Web (`Receiving.aspx`):** the receiver opens the Ready PO, checks the counts, and posts
   the **GRN to Sage**. Accepted stock is booked into the **default receiving warehouse**;
   rejected stock into the **reject warehouse**. No other warehouse is selectable.

**Use when:** the people unloading/counting are not the people doing the paperwork.

---

## Mode 2 — Direct receive on the scanner

One operator receives **and** places, in a single pass.

1. **Scanner:** open the PO → scan an item → type the **qty** → scan the **destination
   location** (any `AllowPicking` bin, `IsWip`, or `IsRejectStore`) → ✓ to capture the line.
   Repeat. Then **Finalise** — the scanner posts the **GRN to Sage** and books each line's
   stock straight into the location it was scanned to.
2. **Web:** nothing.

**Use when:** a single person receives and shelves at the same time.

---

## Mode 3 — Receive on the web, put away on the scanner

Two steps, usually two people: book the delivery in fast, shelve it later.

1. **Web (`Receiving.aspx`):** receive the PO → posts the **GRN to Sage** into the
   **default receiving warehouse** (holding).
2. **Scanner (Put-away):** lists what is sitting in the receiving warehouse → scan an item →
   **qty + location** → an internal **transfer (`TRF`)** moves it from the receiving warehouse
   into the chosen `AllowPicking` / `IsWip` / `IsRejectStore` location. No Sage.

**Use when:** e.g. a truck with 100 lines — check & book against the PO now, put away to bins later.

---

## At a glance

| | Mode 1 | Mode 2 | Mode 3 |
|---|---|---|---|
| Counting | scanner | scanner | web |
| Posts Sage GRN | web | scanner | web |
| Destinations | receiving + reject only | any location | holding, then any location |
| Places to bins | no (web only) | yes, at receipt | yes, at put-away |
| Steps / people | 2 | 1 | 2 |

---

## Pages

| Mode | Scanner page | Web page |
|---|---|---|
| 1 | `SBMSMobile/ReceiveCountM.aspx` *(to build)* | `Receiving.aspx` |
| 2 | `SBMSMobile/ReceiveScanM.aspx` | — |
| 3 | `SBMSMobile/ReceivingM.aspx` (put-away) | `Receiving.aspx` |

The PO list `SBMSMobile/OSPurchaseOrdersM.aspx` is the scanner entry point for the
PO-based modes (1 and 2); the dashboard `DashboardM.aspx` carries the tiles.

## Lot numbers

Gated by `CompanyUseLotNumbers` (lot tracking on/off) and the new
`CompanyMaster.AllowSystemLotNumbers` (auto vs manual).

- **System-generated (auto)** — the app mints the lot as `ddMMyyyy + <store> + <today's sequence>`
  (`GetLotNum`), writing a `LotTrackingMaster` row so the sequence stays unique. Created at the
  point of receipt: **Mode 1** scanner (receiving store), **Mode 2** scanner (scanned location),
  **Mode 3** at the web receive. The web `Receiving.aspx` finalise consumes the line's existing
  lot — it does not regenerate. Mode 3 put-away carries the lot, never creates one.
- **Manual** (`AllowSystemLotNumbers = off`) — the user types their own lot. The scanner receive
  modes (1 & 2) are **not usable** (a real lot must be entered against the goods); receiving is
  **web only**. Put-away is unaffected.

## Configuration

- **`CompanyMaster.AllowSystemLotNumbers`** (bit, default 1) — ConfigCompany toggle
  "System-Generated Lot Numbers". Drives auto-vs-manual lots and, in turn, whether the scanner
  receive modes are offered.
- Each mode is also a dashboard tile, switched on/off per customer/user (tile gating still to be
  wired). A customer typically enables **one** receive flow (Mode 1 *or* Mode 2) plus optionally
  **Put-away** (Mode 3).

## Build status

- **Mode 1** — fully wired: `ReceiveCountM` (accept/reject toggle, auto-lot, Mark Ready), web
  `Receiving.aspx` shows a **Reject_Qty** column and on GRN bills accept+reject, receives accept
  into the receiving store and **splits reject into the `IsRejectStore`** (option A). Manual-lot
  companies are gated to web-only receive. Needs `DocLine.RejectQty` + `TempDocLine.RejectQty` columns.
  *Caveat:* with additional-costs **and** rejects on the same line, the reject portion is not uplifted
  by the add-costs (minor average-cost edge) — verify if that combination is used.
- **Mode 2** and **Mode 3** — built.
- **Mode 1** — scanner side **built**: `ReceiveCountM` (per-line Accept/Reject toggle, default
  Accept; accumulates into `DocLine.ReceiveQty` / `DocLine.RejectQty`; **Mark Ready** sets
  `RecStatus = 1`). Dashboard now has **Count** (Mode 1) and **Receive** (Mode 2) tiles; the PO
  list routes by `?mode=`. Needs `DocLine.RejectQty` column (SQL run + EDMX refresh).
  **Remaining:** enhance the web `Receiving.aspx` to book accepted → receiving warehouse and
  rejected → reject warehouse when it posts the GRN for a Ready (`RecStatus = 1`) PO.

## Dashboard tiles → pages

- **Count** (`imgbCount`) → `OSPurchaseOrdersM?mode=count` → `ReceiveCountM` (Mode 1)
- **Receive** (`imgbReceive`) → `OSPurchaseOrdersM?mode=direct` → `ReceiveScanM` (Mode 2)
- **Put-away** (`imgbRec`) → `ReceivingM` (Mode 3)
