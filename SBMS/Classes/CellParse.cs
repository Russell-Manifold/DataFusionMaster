using System.Globalization;
using System.Web;

namespace SBMS.Classes
{
    // Parses GridView cell text back to numbers, regardless of the machine's
    // regional settings. Cell text is the rendered HTML: it is HTML-encoded and
    // culture-formatted, e.g. "28&#160;394,88" (NBSP thousands separator, comma
    // decimal) on a South African locale, or "28,394.88" on en-US.
    // Convert.ToDecimal / InvariantCulture parses either throw on these or —
    // worse — silently misread them ("4,00" as invariant = 400).
    public static class CellParse
    {
        // Never throws. Empty cells ("&nbsp;") and unparseable text return 0.
        public static decimal ToDecimal(string cellText)
        {
            string s = HttpUtility.HtmlDecode(cellText ?? string.Empty)
                .Replace("\u00A0", "").Replace(" ", "").Trim();
            if (s.Length == 0) return 0m;

            // The cell was formatted by this same server/thread culture, so that
            // culture round-trips it correctly (group separators already stripped).
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal v)) return v;

            // Last resort: treat a lone comma as the decimal point.
            if (decimal.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;

            return 0m;
        }
    }
}
