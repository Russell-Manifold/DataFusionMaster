using SBMS.Models;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Slim manufacturing (CompanyMaster.SageStockOnly): the company has exactly one store, SAGE.
    /// Every works-order draw / produce is booked to it locally; on-hand and cost come from Sage.
    /// </summary>
    public static class SageStore
    {
        public const string Code = "SAGE";

        /// <summary>The company's SAGE store, created if it does not exist yet.</summary>
        public static Store Ensure(SBMSEntities db, long coId)
        {
            Store s = db.Stores.FirstOrDefault(x => x.CompanyID == coId && x.StoreCode == Code);
            if (s != null) return s;

            s = new Store
            {
                CompanyID = coId,
                StoreCode = Code,
                StoreDescript = "Sage Stock",
                StoreActive = true,
                StorePerm = true,
                AllowPicking = false,
                AllowReceiving = false
            };
            db.Stores.Add(s);
            db.SaveChanges();
            return s;
        }
    }
}
