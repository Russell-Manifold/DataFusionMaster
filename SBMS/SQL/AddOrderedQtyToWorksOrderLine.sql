/*
    Part Manufacture feature
    Adds OrderedQty to WorksOrderLine (the finished-good / item-to-produce line)
    to retain the ORIGINAL ordered quantity for that item.

    On a Part Manufacture split, OrderedQty stays the same on every resulting
    line (e.g. 10000 on both the 2000 batch line and the 8000 balance line), so
    ordered-vs-delivered can still be reported after partial splits, early close
    ("Close Remaining") and over-supply.

    Nullable: existing rows stay NULL (treated as "OrderedQty = Quantity").
    Safe to re-run.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.WorksOrderLine')
      AND name = N'OrderedQty'
)
BEGIN
    ALTER TABLE dbo.WorksOrderLine
        ADD OrderedQty decimal(18,4) NULL;
END
GO
