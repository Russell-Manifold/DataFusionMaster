using System;

namespace SBMS.Models
{
    // Marked serializable so it can be held in ViewState — ItemConvertUOM caches the selected
    // store's opening balances in ViewState["OpeningBalances"], and ViewState serialization
    // requires the type to be [Serializable]. Kept in a separate partial so a model regen from
    // the .edmx/.tt does not overwrite the attribute.
    [Serializable]
    public partial class GetOpeningBalancesByStore_Result
    {
    }
}
