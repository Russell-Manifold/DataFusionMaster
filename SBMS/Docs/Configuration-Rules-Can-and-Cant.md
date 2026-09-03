# Configuration Rules — What Each Setting Allows and Prevents

**Every rule the app enforces based on how a company and its items are configured. Each one is enforced in code, not by convention.**

---

## The two levels

Almost every capability has **two switches**, and both must be on.

| Level | Where | What it decides |
|---|---|---|
| **Company** | Configuration → Company | Whether the feature is available to this company at all |
| **Item** | Item Edit | Whether this particular item uses it |

The company switch is a **licence**, not a behaviour. Turning on "Serial Numbers" for a company changes nothing on its own — it only makes the tick box available on the item. Behaviour follows the **item** flag alone.

This matters when you are diagnosing something: if an item is not behaving as expected, check the item first, not the company.

---

## Lot Tracking

**Company: Use Lot Tracking. Item: Item is Lot Tracked.**

| Can | Cannot |
|---|---|
| Capture a lot number at receiving | Use Auto Manufacture (see below) |
| Pick from a chosen lot, or split a pick across several lots | |
| Part pick a line — the balance goes on back order | |
| Record an expiry (Use By) date per lot | |

### System-generated lot numbers

**Company: Allow System Lot Numbers.**

When lot tracking is on and system lot numbers are **off**, the company enters real lot numbers by hand — the supplier's own batch codes. The scanner cannot invent those, so:

- **Scanner Count and Scanner Receive are hidden.** The GRN must be done on the web, where the operator can type the real lot number.
- **Put-away is unaffected** — the lot already exists by then.

When system lot numbers are **on**, the app generates them (date + store + sequence) and scanner receiving is available.

---

## Serial Numbers

**Company: Use Serial Numbers. Item: Serial Numbers.**

### 1. Serial tracking must have lot tracking

A serial number **is** a lot number that holds exactly one unit. There is no separate serial table and no second tracking dimension — that is the design the whole feature rests on.

So serial tracking cannot exist without lot tracking, and this is enforced in three places so no route around it exists:

- **Company screen** — switching lot tracking off switches serial numbers off with it, automatically.
- **Item screen** — the Serial Numbers tick box is disabled unless the item is lot tracked.
- **The database** — a constraint (`CK_ItemsMaster_SerialNeedsLot`) rejects the combination outright, so an import, a restore or a direct update cannot create it either.

### 2. A serial always has a quantity of one

This is the whole difference between a serial and a lot. A lot holds many units; a serial holds one. Every screen that writes stock enforces it.

### 3. What serial items can and cannot do

| Can | Cannot |
|---|---|
| Be received **on the web**, capturing a serial and an expiry date per unit | Be received on the scanner (Count or Receive) |
| Be picked on the web by ticking units from a list | Be put on a works order, or drawn into one as a component |
| Be picked on the handheld by **scanning each unit** | Have stock created by a stock adjustment |
| Be part picked — the balance goes on back order | Be adjusted, transferred or moved in any quantity other than one |
| Be transferred or moved **one unit at a time** | Be picked using pick-by-bin or box/label picking |
| Be traced by serial, by supplier batch, or by purchase order number | |

### 4. Expiry dates

For a serial-tracked item, an expiry date is **mandatory at receiving** — it is captured per unit, at the same moment as the serial number, and the line cannot be saved without both.

Expired or short-dated stock at picking **warns, it never blocks**. The picker may have a good reason to ship it; they must simply not do it unknowingly. On the handheld the app also warns when an earlier-dated unit of the same item is still sitting in that store, because a scanner cannot enforce first-expired-first-out — the picker takes whatever they physically reach.

### 5. Receiving must be captured in one go

Every serial for a receiving line goes in **one capture**. Units from different supplier batches can go in together, because each unit carries its own expiry date.

Saving a second batch separately against the same line **replaces the first** rather than adding to it. The app says so before it happens.

### 6. Line quantities on picking

A sales order for more than 20 units of a serial item is split into picking-slip lines of at most 20 (50 becomes 20, 20 and 10). Each line carries its own serials, so the customer can count the serials shown against that line's quantity.

The serial numbers travel to Sage in the **invoice line's comment**, and the same text is printed on the delivery note — so the paper the driver carries matches the invoice the customer receives.

---

## Auto Manufacture

**Company: Use Auto Manufacture.**

**Auto Manufacture requires lot tracking to be OFF.** The two cannot both be on.

Auto Manufacture backflushes components automatically. A lot-tracked company has to say *which* lot each component came out of, and there is nothing in an automatic backflush that can answer that. So:

- The "Draw from" warehouse selector on the works order screen appears only when Auto Manufacture is on and lot tracking is off.
- The **Manufacture** tile on the mobile dashboard appears under exactly the same condition.

---

## Warehouses

**Warehouse: Allow Receiving, Allow Picking.**

- **Any number** of warehouses can be flagged Allow Receiving, and any number Allow Picking. There is no limit and no "only one" rule.
- A warehouse with neither flag still holds stock — it simply is not offered as a source or destination on those screens.
- If **no** warehouse is flagged Allow Receiving, scanner receiving stops with a message asking an administrator to flag one.

---

## Mobile

**Company: Mobile Module**, plus the individual scanner permissions (Scanner Count, Scanner Receive, Scanner Put-Away).

The mobile app is deliberately **narrower** than the web. Where it cannot capture something correctly, it refuses and says which screen to use instead — it never writes a half-correct movement.

| Mobile can | Mobile cannot |
|---|---|
| Pick serial items by scanning each unit | Receive serial items |
| Pick, count, receive, put away and move ordinary stock | Manufacture serial items |
| Move a serial one unit at a time | Receive at all when lot numbers are entered manually |
| | Pick serial items using pick-by-bin or box labels |

---

## Quick diagnosis

When something is refused and you are not sure why, work down this list:

1. Is the **item** flagged for it — not just the company?
2. Is it a **serial** item? Most refusals are the one-unit rule.
3. Is lot tracking on but **system lot numbers off**? That closes scanner receiving.
4. Is the warehouse flagged **Allow Receiving** or **Allow Picking**?
5. Is the picking slip already **closed off**? Nothing can be reset after that.
