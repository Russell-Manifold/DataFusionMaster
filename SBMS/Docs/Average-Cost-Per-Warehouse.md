# Average Cost by Warehouse

**How Data Fusion calculates, stores and uses average cost — and how it flows through manufacturing.**

---

## The principle

Sage holds **one average cost per item**. It has no concept of warehouses.

Data Fusion holds **one average cost per item, per warehouse**. The same item can sit in Johannesburg at R11.00 and in George at R12.00, and both figures are correct — they reflect what was actually paid for the stock in each place.

## Where it is stored

There is no separate cost table. **The stock ledger is the record.**

Every stock movement writes a row, and each row carries the warehouse's running average cost *after* that movement. The current average for an item in a warehouse is simply the figure on its most recent movement there.

This means the cost is always explainable: every value can be traced back to the movement that produced it.

## How it is calculated

Two rules govern it.

**1. Stock coming in re-blends the average.**

```
new average  =  (existing qty × existing average) + (value coming in)
                ─────────────────────────────────────────────────────
                          existing qty + qty coming in
```

**2. Stock going out never changes it.** Goods leave at the current average, and the average of what remains is untouched.

### Example

Johannesburg holds **100 units at R10.00** — R1,000 of stock.

A delivery of **50 units at R13.00** arrives, including R100 of freight allocated to that line:

| | |
|---|---|
| Value coming in | (50 × R13.00) + R100 = R750 |
| New total value | R1,000 + R750 = R1,750 |
| New quantity | 150 |
| **New Johannesburg average** | **R11.67** |

George is not involved and does not change.

## How it is used in manufacturing

A works order draws its components from **one nominated warehouse** and delivers the finished goods into a warehouse. The cost follows the same two rules.

Using the item above at **R11.67 in Johannesburg**, with a BOM that consumes **2 units per finished item** and carries **R3.00 of additional cost per unit**, making **10**:

**Step 1 — Components are drawn.**
20 units leave Johannesburg at R11.67 = **R233.40**.
Johannesburg's average stays at R11.67; only the quantity falls, to 130. *(Rule 2.)*

**Step 2 — The finished good is costed.**

| | |
|---|---|
| Components | 2 × R11.67 = R23.34 |
| BOM additional costs | R3.00 |
| **Finished good unit cost** | **R26.34** |

**Step 3 — The finished goods are received.**
10 units enter the destination warehouse at R26.34, blending into that warehouse's average for the finished item. *(Rule 1.)*

Throughout, **George is untouched.** Its stock and its average are unaffected by anything happening in Johannesburg.

## Why it works this way

If one average were shared across all warehouses, receiving cheaper stock in one branch would silently restate the value of stock standing in another. Stock counts, valuations and margins would stop reconciling to the movements that produced them.

Costing per warehouse keeps each location's valuation true to what that location actually paid.

## What Sage receives

Sage is kept in step at **item level**, which is the only level it supports. As stock moves, Data Fusion posts item adjustments so that Sage's single average cost reflects the blended position across all warehouses.

The warehouse-level detail lives in Data Fusion. Sage carries the consolidated figure.

---

*Manual edits to an average cost directly in Sage behave differently and are covered separately.*
