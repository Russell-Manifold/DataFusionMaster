-- Works order component tailoring ("Edit Components" screen).
--
-- WorksOrderLines.RMCustomised
--   0 = the component list is still the standard BOM/Kit explosion (default)
--   1 = the operator has tailored the components for THIS works order line
--
-- Why it exists: WorksOrdersDetailed.lbtnLineSave_Click deletes every WorksOrderRMLine
-- for the line and re-explodes it from the BOM/Kit master on every line save. Without
-- this flag, any hand-tailored component list is silently wiped the next time the
-- operator clicks save on the works order line. The rebuild is skipped when the flag
-- is set. Changing the line's ITEM or TYPE still rebuilds and clears the flag - the
-- operator has changed what is being made, so the old component list is meaningless.
--
-- The BOM/Kit master is never touched by the edit screen; only this works order's
-- WorksOrderRMLines rows change.
--
-- After running, do "Update Model from Database" on the EDMX so
-- WorksOrderLine.RMCustomised appears on the entity.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.WorksOrderLines') AND name = 'RMCustomised')
BEGIN
    ALTER TABLE dbo.WorksOrderLines
        ADD RMCustomised bit NOT NULL CONSTRAINT DF_WorksOrderLines_RMCustomised DEFAULT 0;
END
GO
