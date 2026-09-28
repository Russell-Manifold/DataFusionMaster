/* ============================================================================
   Generic Sage login  (Configuration -> Users -> "Use Generic Login")
   ---------------------------------------------------------------------------
   A user flagged UseGenericLogin signs in to Data Fusion with their OWN password
   (checked locally, never sent to Sage) and every Sage API call is then made
   with the company's ONE generic Sage account (Configuration -> Company ->
   Generic Sage Login, stored TripleDES-encrypted in CompanyMaster).
   Super users cannot be generic.

   *** RUN BEFORE DEPLOYING THE NEW BUILD *** - the column is in the EDMX.
   Safe to run more than once. Old build ignores the column (NOT NULL DEFAULT 0).
   ============================================================================ */
IF COL_LENGTH('dbo.UsersMaster', 'UseGenericLogin') IS NULL
BEGIN
    ALTER TABLE dbo.UsersMaster ADD UseGenericLogin BIT NOT NULL
        CONSTRAINT DF_UsersMaster_UseGenericLogin DEFAULT (0);
    PRINT 'Added UsersMaster.UseGenericLogin';
END
ELSE PRINT 'UsersMaster.UseGenericLogin already exists';
GO
