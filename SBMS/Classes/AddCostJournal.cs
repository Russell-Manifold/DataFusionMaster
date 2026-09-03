using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Models;

namespace SBMS.Classes
{
    /// <summary>
    /// Posts the journal that clears BOM additional costs off Sage's stock adjustment
    /// account: DEBIT the company's Stock Adjustment Account, CREDIT the account chosen
    /// on the BOM.
    ///
    /// This lived as two byte-identical copies, on the desktop Works Order manufacture
    /// screen and on mobile ManufactureM. Every Sage quirk below had to be fixed twice,
    /// and a fix applied to only one of them would have left the screen and the scanner
    /// posting differently. One copy, one place to change.
    /// </summary>
    public static class AddCostJournal
    {
        public const string Ok = "Success";
        public const string NoAccounts = "No accounts configured";

        public static string Post(UserDetails user, long debitAccountId, long creditAccountId,
                                  decimal amount, string reference, string description)
        {
            if (amount <= 0) return Ok;                                  // nothing to post
            if (debitAccountId <= 0 || creditAccountId <= 0)
                return Fail(user, debitAccountId, creditAccountId, amount, reference, NoAccounts);
            if (debitAccountId == creditAccountId)
                return Fail(user, debitAccountId, creditAccountId, amount, reference,
                            "Debit and credit accounts are the same");    // Sage answers 200 and saves nothing

            try
            {
                // Sage rejects a journal with no tax type ("Tax Type is Required") even though
                // its own API help lists the field as optional. This entry carries no VAT, so
                // use a real zero-rated type - NOT TaxTypeID 0 ("No VAT"), which Sage will not
                // accept - falling back to the default on the stock adjustment account.
                long taxTypeId = 0;
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    taxTypeId = db.TaxTypesMasters
                        .Where(x => x.CompanyID == user.CoID && (x.TaxPerc ?? 0) == 0
                                    && (x.TaxTypeID ?? 0) > 0)
                        .OrderBy(x => x.TaxTypeID)
                        .Select(x => x.TaxTypeID ?? 0).FirstOrDefault();
                    if (taxTypeId <= 0)
                    {
                        taxTypeId = db.AccountsMasters
                            .Where(x => x.CompanyID == user.CoID && x.AccountID == debitAccountId)
                            .Select(x => x.AcctDefTaxTypeID ?? 0).FirstOrDefault();
                    }
                }
                if (taxTypeId <= 0)
                    return Fail(user, debitAccountId, creditAccountId, amount, reference,
                                "No zero-rated tax type found for this company");

                var journal = new
                {
                    Date = DateTime.Now,
                    Effect = 1,                            // 1 = Debit (AccountId is debited)
                    AccountId = debitAccountId,            // stock adjustment account
                    ContraAccountId = creditAccountId,     // account chosen on the BOM
                    TaxTypeId = taxTypeId,                 // required by Sage, zero-rated
                    Reference = reference,
                    Description = description,
                    Exclusive = amount,
                    Tax = 0m,
                    Total = amount,
                    Debit = amount,
                    Credit = 0m
                };

                JObject parsed = new ApiUrlCall().APIPostDocumentNA(
                    "JournalEntry", JsonConvert.SerializeObject(journal, Formatting.Indented), user);

                if (parsed == null)
                    return Fail(user, debitAccountId, creditAccountId, amount, reference,
                                "Null response from API");
                if (parsed["error"] != null)
                {
                    JObject err = (JObject)parsed["error"];
                    return Fail(user, debitAccountId, creditAccountId, amount, reference,
                        err["message"]?.ToString()
                        ?? err["reason"]?.ToString()
                        ?? "Unknown API error posting journal");
                }
                // Sage can answer 200 with a validation payload and save nothing. A real save
                // comes back with the new journal id - and Sage spells it "ID", so a check for
                // "Id" reports a false failure on a journal that actually posted.
                if (parsed["ID"] == null && parsed["Id"] == null)
                    return Fail(user, debitAccountId, creditAccountId, amount, reference,
                                "Sage did not return a journal Id: " + parsed.ToString(Formatting.None));

                return Ok;
            }
            catch (Exception ex)
            {
                return Fail(user, debitAccountId, creditAccountId, amount, reference,
                            "AddCostJournal exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Records a refused journal and hands the reason straight back to the caller.
        ///
        /// The operator sees a warning once and can dismiss it, so without this the only
        /// trace of an unbalanced GL is a line in the error log. The row is written with raw
        /// SQL, outside the EF model, so the table can be added without touching the EDMX -
        /// a mismatched EDMX locks every user out at login. If the table is not there yet,
        /// or the insert fails for any reason, the manufacture is NOT affected.
        ///
        /// Support query:
        ///   SELECT * FROM dbo.FailedAddCostJournals WHERE Resolved = 0 ORDER BY FailedAt DESC;
        /// </summary>
        private static string Fail(UserDetails user, long debitAccountId, long creditAccountId,
                                   decimal amount, string reference, string reason)
        {
            try
            {
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.FailedAddCostJournals "
                        + "(CompanyID, Reference, DebitAccountID, CreditAccountID, Amount, Reason, FailedAt, Resolved) "
                        + "VALUES (@p0, @p1, @p2, @p3, @p4, @p5, GETDATE(), 0)",
                        user.CoID, reference ?? "", debitAccountId, creditAccountId, amount,
                        (reason ?? "").Length > 500 ? reason.Substring(0, 500) : (reason ?? ""));
                }
            }
            catch { /* never let bookkeeping of a failure become a second failure */ }

            try { new ApiUrlCall().LogErrorToFile(
                "BOM ADD-COST JOURNAL FAILED - " + reference + " amount " + amount
                + " Dr " + debitAccountId + " Cr " + creditAccountId + " - " + reason); } catch { }

            return reason;
        }
    }
}
