/*
================================================================================
  Schema changes to support safe handling of Sage-side PO edits after a PO
  has been part-received.

  Adds:
    1) DocLines.RemovedFromPO bit               - flag set true when LoadPOLines
                                                  detects a line is no longer in
                                                  Sage but local has receivings.
       DocLines.RemovedFromPODate datetime      - when that detection happened.

    2) ReceivingOutstandings.SBCALineID bigint  - stable Sage line identity.
                                                  All cross-session joins switch
                                                  to this. Existing rows are
                                                  back-filled where unambiguous.

  Safe to re-run: each ALTER is guarded by COL_LENGTH and each backfill is
  guarded by NULL checks.

  Apply once per environment (Dev / UAT / Prod), then run
  "Update Model from Database" in Visual Studio against SBMS.edmx so the
  generated POCOs pick up the new properties.
================================================================================
*/

-- ---------------------------------------------------------------------------
-- 1. DocLines: RemovedFromPO flag + timestamp
-- ---------------------------------------------------------------------------

IF COL_LENGTH('dbo.DocLines', 'RemovedFromPO') IS NULL
BEGIN
    ALTER TABLE dbo.DocLines
        ADD RemovedFromPO bit NULL
            CONSTRAINT DF_DocLines_RemovedFromPO DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.DocLines', 'RemovedFromPODate') IS NULL
BEGIN
    ALTER TABLE dbo.DocLines
        ADD RemovedFromPODate datetime NULL;
END
GO

UPDATE dbo.DocLines
   SET RemovedFromPO = 0
 WHERE RemovedFromPO IS NULL;
GO

-- ---------------------------------------------------------------------------
-- 2. ReceivingOutstandings: SBCALineID (stable Sage line identity)
-- ---------------------------------------------------------------------------

IF COL_LENGTH('dbo.ReceivingOutstandings', 'SBCALineID') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingOutstandings
        ADD SBCALineID bigint NULL;
END
GO

-- ---------------------------------------------------------------------------
-- 3. Back-fill SBCALineID for existing rows.
--    Only assign where the (PODocID, ItemCode) pair maps to exactly one
--    DocLine. Ambiguous cases (same item on two PO lines) stay NULL and the
--    C# Prerecqty query falls back to ItemCode-based summing for them.
-- ---------------------------------------------------------------------------

UPDATE ro
   SET ro.SBCALineID = dl.SBCALineID
  FROM dbo.ReceivingOutstandings AS ro
 INNER JOIN dbo.DocLines AS dl
    ON dl.DocID    = ro.PODocID
   AND dl.ItemCode = ro.ItemCode
 WHERE ro.SBCALineID IS NULL
   AND NOT EXISTS (
         SELECT 1
           FROM dbo.DocLines AS dl2
          WHERE dl2.DocID    = ro.PODocID
            AND dl2.ItemCode = ro.ItemCode
            AND dl2.LineID   <> dl.LineID
       );
GO

-- ---------------------------------------------------------------------------
-- 4. Diagnostic: which active-receiving rows are still ambiguous after backfill.
--    Review this output - if anything appears, eyeball whether to set
--    SBCALineID by hand. Safe to leave NULL: legacy fallback handles them.
-- ---------------------------------------------------------------------------

SELECT  ro.id,
        ro.PONumber,
        ro.PODocID,
        ro.ItemCode,
        ro.OrigQty,
        ro.RecQty,
        ro.CreatedDate,
        CandidateLineCount = (
            SELECT COUNT(*)
              FROM dbo.DocLines dl
             WHERE dl.DocID    = ro.PODocID
               AND dl.ItemCode = ro.ItemCode
        )
  FROM  dbo.ReceivingOutstandings ro
 WHERE  ro.SBCALineID IS NULL
   AND  ro.Archive    = 0
 ORDER  BY ro.PODocID, ro.ItemCode;
GO
