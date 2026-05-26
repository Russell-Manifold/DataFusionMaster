using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SBMS.Models
{
    /// <summary>
    /// Returned by ApiUrlCall.LoadPOLines so the Receiving page can show
    /// the user what changed when a PO was refreshed from Sage.
    ///
    /// Skipped = true means the Sage call failed or returned no usable data,
    /// so NO reconciliation was performed (we never delete/hide on bad data).
    /// </summary>
    public class POReconcileSummary
    {
        public int Added { get; set; }
        public int QtyChanged { get; set; }
        public int Removed { get; set; }   // deleted locally (Sage removed it, no receivings)
        public int Kept { get; set; }      // Sage removed it but had receivings -> kept active locally
        public bool Skipped { get; set; }

        // Back-compat alias: existing call sites read .Hidden during transition.
        public int Hidden
        {
            get { return Kept; }
            set { Kept = value; }
        }

        public bool HasChanges
        {
            get { return Added > 0 || QtyChanged > 0 || Removed > 0 || Kept > 0; }
        }

        /// <summary>
        /// Human-readable single-sentence summary for the banner.
        /// Returns empty string when nothing changed.
        /// </summary>
        public string ToBannerText()
        {
            if (Skipped || !HasChanges) return string.Empty;

            var parts = new List<string>();
            if (Added > 0)      parts.Add(Added      + (Added      == 1 ? " line added"       : " lines added"));
            if (Removed > 0)    parts.Add(Removed    + (Removed    == 1 ? " line removed"     : " lines removed"));
            if (QtyChanged > 0) parts.Add(QtyChanged + (QtyChanged == 1 ? " line qty-changed" : " lines qty-changed"));

            string head = parts.Count > 0
                ? "PO refreshed from Sage: " + string.Join(", ", parts) + "."
                : string.Empty;

            // Kept lines (removed in Sage after part-receiving) get a stronger,
            // separate sentence so the user is nudged to reconcile in Sage.
            if (Kept > 0)
            {
                string keptMsg = Kept == 1
                    ? "1 line was removed from the Sage PO after being part-received - kept locally for processing. Please reconcile in Sage afterwards."
                    : Kept + " lines were removed from the Sage PO after being part-received - kept locally for processing. Please reconcile in Sage afterwards.";
                head = (head.Length > 0 ? head + " " : string.Empty) + keptMsg;
            }

            return head;
        }
    }
}
