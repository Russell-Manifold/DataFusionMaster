using System;
using System.Linq;
using SBMS.Models;

namespace SBMS.Classes
{
    /// <summary>
    /// The one rule serial tracking rests on: a serial IS a lot holding exactly one unit.
    ///
    /// Receiving and picking enforce it, but every other screen that writes stock - adjustments,
    /// transfers, UOM conversion, works order manufacture - can create or move a serial-item lot
    /// holding several units. Such a lot is then filtered out of the picking chooser (which only
    /// offers single units), so the stock exists, is valued, and can never be issued or traced.
    ///
    /// These helpers are the check those screens make before they write.
    /// </summary>
    public static class SerialGuard
    {
        /// <summary>Is this item serial tracked? Cheap enough to call on a save path.</summary>
        public static bool IsSerialItem(SBMSEntities db, long companyId, long itemId)
        {
            if (itemId <= 0) return false;
            return db.ItemsMasters
                     .Where(x => x.CompanyID == companyId && x.ID == itemId)
                     .Select(x => x.IsSerialTracked).FirstOrDefault();
        }

        /// <summary>
        /// Null when the operation is allowed, otherwise the reason to show the operator.
        ///
        /// Whole units only: a serial lot can be moved (a transfer of one unit is perfectly
        /// ordinary), but it can never carry a quantity other than one.
        /// </summary>
        public static string CheckUnitQty(SBMSEntities db, long companyId, long itemId,
                                          string itemCode, decimal qty)
        {
            if (!IsSerialItem(db, companyId, itemId)) return null;
            if (Math.Abs(qty) == 1) return null;

            return (itemCode ?? "This item") + " is serial tracked, so each unit is its own "
                 + "number and moves one at a time - a quantity of " + qty.ToString("0.##")
                 + " cannot be recorded here. Handle the units individually.";
        }

        /// <summary>
        /// Null when allowed, otherwise the reason. Used where a screen would create stock for a
        /// serial item outside receiving, which is the only place a serial number can be captured.
        /// </summary>
        public static string CheckCanCreateStock(SBMSEntities db, long companyId, long itemId, string itemCode)
        {
            if (!IsSerialItem(db, companyId, itemId)) return null;

            return (itemCode ?? "This item") + " is serial tracked. Stock for it can only be "
                 + "brought in through Receiving, where each unit's serial number and expiry are "
                 + "captured - stock created here would have no serial and could never be picked.";
        }
    }
}
