/* ============================================================================
   Per-user Sage login method  (Configuration -> Users -> Sage Login)
   ---------------------------------------------------------------------------
   0 = Basic auth (Sage username + password, as today)  - DEFAULT
   1 = OAuth 2.0  ("Login with Sage Account" via id.sage.com, with 2FA)

   *** RUN ON BOTH DBs (MyDataFusion + MyDataFusionDemo2) BEFORE DEPLOYING ***
   then Update Model from Database - the column is in the EDMX.
   Safe to run more than once. Old build ignores the column (NOT NULL DEFAULT 0).
   ============================================================================ */
IF COL_LENGTH('dbo.UsersMaster', 'UseSageOAuth') IS NULL
BEGIN
    ALTER TABLE dbo.UsersMaster ADD UseSageOAuth BIT NOT NULL
        CONSTRAINT DF_UsersMaster_UseSageOAuth DEFAULT (0);
    PRINT 'Added UsersMaster.UseSageOAuth';
END
ELSE PRINT 'UsersMaster.UseSageOAuth already exists';
GO
