-- =====================================================================
-- Durable manufacture-adjustment dedup (2026-07-19)
--
-- Replaces the in-Session "SentKeys" HashSet (wiped on every page reload)
-- with a DURABLE per-adjustment claim, so a Works Order manufacture that
-- posts irreversible Sage item adjustments (FG "H" + component "L" draws)
-- can never post the SAME adjustment twice - whether via a page reload
-- after a mid-sequence failure, or two operators on the same WO at once.
--
-- Pattern in code (WorksOrdersManf.aspx.cs):
--   1. INSERT the adjustment key (unique index) = CLAIM it. If the insert
--      hits the unique violation, someone already posted/claimed it -> skip.
--   2. Only after a successful claim, post to Sage (DoItemAdjustment).
--   3. If Sage fails, DELETE the claim (release) so a retry can redo it.
--   4. If Sage succeeds, the claim stays (committed).
--
-- A WO line is manufactured exactly once (completion sets Complete=true and
-- the manufacture guard skips Complete lines), so a claim never wrongly
-- blocks a legitimate re-manufacture.
--
-- Deliberately OUTSIDE the EF model (raw SQL only) - do NOT add to the EDMX.
-- Idempotent. Run on EVERY tenant database.
-- =====================================================================

IF OBJECT_ID('dbo.ManfPostedAdjustments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ManfPostedAdjustments (
        ID        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ManfPostedAdjustments PRIMARY KEY,
        CompanyID bigint       NOT NULL,
        WOID      bigint       NOT NULL,
        AdjKey    varchar(300) NOT NULL,
        PostedAt  datetime     NOT NULL CONSTRAINT DF_ManfPostedAdjustments_PostedAt DEFAULT (GETDATE()),
        PostedBy  bigint       NULL
    );

    -- The unique index is the atomic gate: the INSERT that claims a key fails
    -- for any concurrent/duplicate attempt.
    CREATE UNIQUE NONCLUSTERED INDEX UX_ManfPostedAdjustments_Key
        ON dbo.ManfPostedAdjustments (CompanyID, WOID, AdjKey);
END
GO
