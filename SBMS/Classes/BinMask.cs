using System;
using System.Collections.Generic;
using System.Linq;

namespace SBMS.Classes
{
    /// <summary>
    /// Per-company bin / location naming. A single Bin ID encodes the whole hierarchy
    /// (e.g. Warehouse-Aisle-Row-Bin → "WH1-A03-R2-B05"). The company defines its own depth
    /// via an ordered list of segment labels and a separator; blank labels = free-text bins.
    /// The Bin ID is always stored as one normalised key — segments are only for validation
    /// and display, never split into columns.
    /// </summary>
    public static class BinMask
    {
        // Trim + upper so "a03" and "A03" are the same bin.
        public static string Normalize(string code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }

        public static string Separator(UserDetails u)
        {
            return string.IsNullOrEmpty(u?.BinDelimiter) ? "-" : u.BinDelimiter;
        }

        // Ordered segment labels (max 4); empty = no enforcement.
        public static List<string> Segments(UserDetails u)
        {
            if (string.IsNullOrWhiteSpace(u?.BinSegments)) return new List<string>();
            return u.BinSegments.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Take(4)
                    .ToList();
        }

        public static bool IsEnforced(UserDetails u)
        {
            return Segments(u).Count > 0;
        }

        /// <summary>
        /// Validates a scanned / entered Bin ID against the company convention.
        /// Returns the normalised code via <paramref name="normalised"/>. When no convention is
        /// configured, any non-empty code is accepted (free-text bins).
        /// </summary>
        public static bool Validate(UserDetails u, string code, out string normalised, out string error)
        {
            error = null;
            normalised = Normalize(code);

            if (string.IsNullOrEmpty(normalised)) { error = "Bin / location is required."; return false; }

            var labels = Segments(u);
            if (labels.Count == 0) return true;   // free-text bins

            string sep = Separator(u);
            var parts = normalised.Split(new[] { sep }, StringSplitOptions.None);

            if (parts.Length != labels.Count)
            {
                error = $"Bin must be {labels.Count} parts separated by '{sep}' ({string.Join(sep, labels)}).";
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) { error = $"{labels[i]} is empty."; return false; }
                if (!parts[i].All(char.IsLetterOrDigit)) { error = $"{labels[i]} must be letters / numbers only."; return false; }
            }
            return true;
        }

        // Convenience for display: "Aisle: A03" pairs, in order. Safe to call on any code.
        public static IEnumerable<KeyValuePair<string, string>> Describe(UserDetails u, string code)
        {
            var labels = Segments(u);
            if (labels.Count == 0) yield break;
            var parts = Normalize(code).Split(new[] { Separator(u) }, StringSplitOptions.None);
            for (int i = 0; i < labels.Count && i < parts.Length; i++)
                yield return new KeyValuePair<string, string>(labels[i], parts[i]);
        }
    }
}
