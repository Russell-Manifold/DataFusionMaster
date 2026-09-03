using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using SBMS.Models;

namespace SBMS.Classes
{
    /// <summary>
    /// The "Post Additional Costs To" account picker, shared by BOMCreate and BOMDetailed.
    ///
    /// Both screens carried identical copies of this, which meant the same-account rule and
    /// the wording had to be changed in two files every time. The rules it enforces decide
    /// whether a real GL journal can be raised at manufacture, so the two screens agreeing
    /// is not optional.
    /// </summary>
    public static class BomAddCostAccounts
    {
        /// <summary>Fills the account list and re-selects the account already on the BOM.</summary>
        public static void Load(DropDownList ddl, UserDetails user, long? selected)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var accts = db.AccountsMasters
                    .Where(x => x.CompanyID == user.CoID && x.AccountAddCosts == true)
                    .OrderBy(x => x.AccountName).ToList();

                ddl.DataSource = accts;
                ddl.DataTextField = "AccountName";
                ddl.DataValueField = "AccountID";
                ddl.DataBind();
                ddl.Items.Insert(0, new ListItem("- None -", "0"));

                if (selected.HasValue && selected.Value > 0)
                {
                    var hit = ddl.Items.FindByValue(selected.Value.ToString());
                    if (hit != null) ddl.SelectedValue = hit.Value;
                }
            }
        }

        /// <summary>
        /// A BOM carrying additional costs must be able to post them. Returns false and shows
        /// the reason if it cannot: no account chosen, no company Stock Adjustment Account, or
        /// both sides the same (which Sage accepts and then saves nothing).
        /// </summary>
        public static bool Validate(Page page, DropDownList ddl, UserDetails user, decimal totalAddCost)
        {
            if (totalAddCost <= 0) return true;   // no cost, no account needed

            long chosen;
            bool hasAccount = long.TryParse(ddl.SelectedValue, out chosen) && chosen > 0;
            if (!hasAccount)
            {
                AlertHelper.ShowSweetAlert(page,
                    "This BOM has additional costs, so it must have an account to post them to. "
                    + "Choose one under \"Post Additional Costs To\" before saving.", "warning");
                return false;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                long contra = db.AccountsMasters
                    .Where(x => x.CompanyID == user.CoID && x.AccountAddCostsContra == true)
                    .Select(x => x.AccountID ?? 0).FirstOrDefault();

                if (contra == chosen)
                {
                    AlertHelper.ShowSweetAlert(page,
                        "The account chosen under \"Post Additional Costs To\" is also this company's "
                        + "Stock Adjustment Account. Debiting and crediting the same account posts nothing "
                        + "to Sage - choose a different account.", "warning");
                    return false;
                }
                if (contra <= 0)
                {
                    AlertHelper.ShowSweetAlert(page,
                        "No Stock Adjustment Account has been set for this company, so additional "
                        + "costs cannot be posted to Sage. Set one under Settings → GL Account Access, then save this BOM.",
                        "warning");
                    return false;
                }
            }
            return true;
        }
    }
}
