using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace SBMS.Classes
{
    /// <summary>One open document for an item. Qty is positive coming in, negative going out.</summary>
    public sealed class StockEvent
    {
        /// <summary>Null when the document has no date; it is then treated as due today.</summary>
        public DateTime? Date { get; set; }
        public string Kind { get; set; }
        public string Document { get; set; }
        public decimal Qty { get; set; }
    }

    public sealed class BomComponent
    {
        public string Code { get; set; }
        public decimal QtyPerUnit { get; set; }
    }

    /// <summary>One material a finished item still needs. Available is what is left after earlier needs.</summary>
    public sealed class MaterialNeed
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal Needed { get; set; }
        public decimal Available { get; set; }
        public decimal Short => Needed - Available;
    }

    public sealed class StockTimelineItem
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal OnHand { get; set; }
        public decimal Minimum { get; set; }
        /// <summary>Item Master ticks: Is Finished Item / Is Made Up From A BOM / Is a BOM Component.</summary>
        public bool IsFinishedItem { get; set; }
        public bool IsFinished { get; set; }
        public bool IsRaw { get; set; }
        public List<StockEvent> Events { get; } = new List<StockEvent>();
        public List<BomComponent> Bom { get; } = new List<BomComponent>();
        public decimal Uncovered { get; set; }
        public List<MaterialNeed> Materials { get; } = new List<MaterialNeed>();
    }

    public sealed class StockTimelineLine
    {
        public StockEvent Event { get; set; }
        public bool Late { get; set; }
        public decimal Balance { get; set; }
    }

    public sealed class StockTimelineRow
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal OnHand { get; set; }
        public decimal Minimum { get; set; }
        /// <summary>Item Master ticks: Is Finished Item / Is Made Up From A BOM / Is a BOM Component.</summary>
        public bool IsFinishedItem { get; set; }
        public bool IsFinished { get; set; }
        public bool IsRaw { get; set; }
        public decimal Uncovered { get; set; }
        public List<MaterialNeed> Materials { get; set; }
        public decimal Overdue { get; set; }
        public decimal[] WeekEnd { get; set; }
        public decimal[] WeekLow { get; set; }
        public decimal Later { get; set; }
        public decimal LaterLow { get; set; }
        public decimal Lowest { get; set; }
        public DateTime? ShortFrom { get; set; }
        public List<StockTimelineLine> Lines { get; } = new List<StockTimelineLine>();

        /// <summary>0 = goes short, 1 = drops below minimum, 2 = fine.</summary>
        public int Attention => ShortFrom != null ? 0 : (Minimum > 0 && Lowest < Minimum ? 1 : 2);
    }

    /// <summary>
    /// Projected stock per week for an item: on hand, plus everything coming in, less everything
    /// going out, in date order. Each week holds the balance at the end of that week and the lowest
    /// point during it, so a shortage that recovers before Sunday is still seen.
    /// </summary>
    public static class StockTimeline
    {
        /// <summary>Weeks shown when no "Up to" date is chosen; anything later goes in the Later column.</summary>
        public const int Weeks = 8;

        /// <summary>Weeks from this week up to and including the week of <paramref name="upTo"/> (1 to 52).</summary>
        public static int WeeksUpTo(DateTime upTo, DateTime today)
        {
            int weeks = (WeekStart(upTo) - WeekStart(today)).Days / 7 + 1;
            return Math.Max(1, Math.Min(52, weeks));
        }

        public static DateTime WeekStart(DateTime date)
        {
            return date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }

        public static StockTimelineRow Build(StockTimelineItem item, DateTime today, int weeks = Weeks)
        {
            today = today.Date;
            DateTime monday = WeekStart(today);

            // On the same day, stock coming in is counted before stock going out.
            var ordered = item.Events
                .Select(e => new { Event = e, When = (e.Date ?? today).Date })
                .OrderBy(x => x.When)
                .ThenByDescending(x => x.Event.Qty)
                .ToList();

            var row = new StockTimelineRow
            {
                Code = item.Code,
                Description = item.Description,
                Category = item.Category,
                OnHand = item.OnHand,
                Minimum = item.Minimum,
                IsFinishedItem = item.IsFinishedItem,
                IsFinished = item.IsFinished,
                IsRaw = item.IsRaw,
                Uncovered = item.Uncovered,
                Materials = item.Materials,
                WeekEnd = new decimal[weeks],
                WeekLow = new decimal[weeks]
            };

            decimal balance = item.OnHand;
            int i = 0;

            // Anything dated before this week is late, so it lands now.
            for (; i < ordered.Count && ordered[i].When < monday; i++)
            {
                balance += ordered[i].Event.Qty;
                row.Overdue += ordered[i].Event.Qty;
                row.Lines.Add(new StockTimelineLine { Event = ordered[i].Event, Late = true, Balance = balance });
            }

            if (balance < 0) row.ShortFrom = today;

            // One pass per week, then a last pass for everything after the final week.
            for (int w = 0; w <= weeks; w++)
            {
                DateTime end = w < weeks ? monday.AddDays(7 * (w + 1)) : DateTime.MaxValue;
                // Only this week starts from today's real position. A later week that opens short
                // and is fixed by a delivery must not stay red, so it counts from its first document.
                decimal? lowest = w == 0 ? balance : (decimal?)null;

                for (; i < ordered.Count && ordered[i].When < end; i++)
                {
                    balance += ordered[i].Event.Qty;
                    row.Lines.Add(new StockTimelineLine { Event = ordered[i].Event, Balance = balance });
                    if (lowest == null || balance < lowest) lowest = balance;
                    if (balance < 0 && row.ShortFrom == null) row.ShortFrom = ordered[i].When;
                }

                decimal low = lowest ?? balance;
                if (w == 0 || low < row.Lowest) row.Lowest = low;
                if (w < weeks)
                {
                    row.WeekEnd[w] = balance;
                    row.WeekLow[w] = low;
                }
                else
                {
                    row.Later = balance;
                    row.LaterLow = low;
                }
            }

            return row;
        }

        private sealed class PlannedNeed
        {
            public StockTimelineItem Parent;
            public StockTimelineItem Material;
            public DateTime Date;
            public decimal Qty;
        }

        /// <summary>
        /// Turns any shortage nothing will ever cover into planned demand on the materials in its BOM.
        /// A shortage that stock or an open order covers later is only late, and an open works order's
        /// materials are already counted, so neither plans anything. Quantities match how a works order
        /// explodes a BOM: quantity to make x RMQty.
        /// </summary>
        public static void AddPlannedDemand(IDictionary<string, StockTimelineItem> items, DateTime today)
        {
            today = today.Date;

            // Free stock of each material after everything already on order, before any planned demand.
            var free = items.Values.ToDictionary(x => x, x => Math.Max(0, Closing(x)));
            var needs = new List<PlannedNeed>();

            foreach (StockTimelineItem item in ParentsFirst(items))
            {
                if (item.Bom.Count == 0) continue;
                decimal gap = -Closing(item);

                // Supply goes to the earliest orders first, so what is left uncovered is the latest.
                var demands = item.Events.Where(e => e.Qty < 0).OrderByDescending(e => (e.Date ?? today).Date).ToList();
                foreach (StockEvent demand in demands)
                {
                    if (gap <= 0) break;
                    decimal make = Math.Min(gap, -demand.Qty);
                    gap -= make;
                    item.Uncovered += make;

                    foreach (BomComponent part in item.Bom)
                    {
                        StockTimelineItem material;
                        if (!items.TryGetValue(part.Code, out material)) continue;

                        decimal qty = make * part.QtyPerUnit;
                        material.Events.Add(new StockEvent { Date = demand.Date, Kind = "Planned for", Document = item.Code, Qty = -qty });
                        needs.Add(new PlannedNeed { Parent = item, Material = material, Date = (demand.Date ?? today).Date, Qty = qty });
                    }
                }
            }

            // Earliest need gets the free stock first, so two items can't both claim the same material.
            foreach (PlannedNeed need in needs.OrderBy(x => x.Date).ThenBy(x => x.Parent.Code, StringComparer.OrdinalIgnoreCase))
            {
                MaterialNeed line = need.Parent.Materials.FirstOrDefault(x => x.Code == need.Material.Code);
                if (line == null)
                {
                    line = new MaterialNeed { Code = need.Material.Code, Description = need.Material.Description };
                    need.Parent.Materials.Add(line);
                }

                decimal available = Math.Min(free[need.Material], need.Qty);
                free[need.Material] -= available;
                line.Needed += need.Qty;
                line.Available += available;
            }
        }

        private static decimal Closing(StockTimelineItem item)
        {
            return item.OnHand + item.Events.Sum(e => e.Qty);
        }

        /// <summary>A finished item comes before its materials, so sub-assemblies carry their parents' demand down.</summary>
        private static List<StockTimelineItem> ParentsFirst(IDictionary<string, StockTimelineItem> items)
        {
            var seen = new HashSet<StockTimelineItem>();
            var order = new List<StockTimelineItem>();

            void Visit(StockTimelineItem item)
            {
                if (!seen.Add(item)) return;   // also stops a BOM that loops back on itself
                foreach (BomComponent part in item.Bom)
                {
                    StockTimelineItem child;
                    if (items.TryGetValue(part.Code, out child)) Visit(child);
                }
                order.Add(item);
            }

            foreach (StockTimelineItem item in items.Values) Visit(item);
            order.Reverse();
            return order;
        }

        /// <param name="weeks">Week columns to show.</param>
        /// <param name="showLater">False when the user chose an "Up to" date: nothing after it is counted.</param>
        public static string Render(IList<StockTimelineRow> rows, DateTime today, int weeks = Weeks, bool showLater = true)
        {
            if (rows.Count == 0)
            {
                return "<p class=\"fuse-hint\">Nothing to plan: no open documents, and all stock is above its minimum level.</p>";
            }

            today = today.Date;
            DateTime monday = WeekStart(today);
            var sb = new StringBuilder();

            sb.Append("<div class=\"fuse-tablewrap\"><table class=\"gridview stl\" id=\"stlTable\"><thead><tr class=\"gridViewHeader\">");
            sb.Append("<th class=\"stl-item\">Item</th><th>Short from</th><th class=\"stl-num\">On hand</th>");
            sb.Append("<th class=\"stl-num\" title=\"Minimum Level set on the item. Weeks that drop below it show amber.\">Minimum</th>");
            sb.Append("<th class=\"stl-num\" title=\"Late documents, in minus out. Already included in the weekly balances.\">Overdue</th>");
            for (int w = 0; w < weeks; w++)
            {
                DateTime week = monday.AddDays(7 * w);
                sb.Append("<th class=\"stl-num\" title=\"Stock left at the end of the week starting ").Append(LongDate(week)).Append("\">").Append(ShortDate(week)).Append("</th>");
            }
            if (showLater)
                sb.Append("<th class=\"stl-num\" title=\"Stock left after everything due from ").Append(LongDate(monday.AddDays(7 * weeks))).Append(" onwards\">Later</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (StockTimelineRow row in rows)
            {
                AppendRow(sb, row, today, showLater);
                AppendDetail(sb, row, today, weeks + (showLater ? 6 : 5));
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, StockTimelineRow row, DateTime today, bool showLater)
        {
            string find = HttpUtility.HtmlAttributeEncode((row.Code + " " + row.Description).ToLowerInvariant());

            // Show filter letters: g = finished item, f = made from a BOM, r = BOM component.
            string type = (row.IsFinishedItem ? "g" : "") + (row.IsFinished ? "f" : "") + (row.IsRaw ? "r" : "");
            string tag = TypeTag(row);

            sb.Append("<tr class=\"stl-row\" data-find=\"").Append(find).Append("\" data-type=\"").Append(type).Append("\" data-attn=\"").Append(row.Attention)
              .Append("\" data-cat=\"").Append(HttpUtility.HtmlAttributeEncode((row.Category ?? "").Trim().ToLowerInvariant())).Append("\">");
            sb.Append("<td class=\"stl-item\"><a href=\"#\" class=\"stl-toggle\" role=\"button\" aria-expanded=\"false\"><i class=\"fa fa-angle-right\" aria-hidden=\"true\"></i>")
              .Append(HttpUtility.HtmlEncode(row.Code)).Append("</a>")
              .Append(tag == "" ? "" : "<span class=\"stl-type\">" + tag + "</span>")
              .Append("<span class=\"stl-desc\">").Append(HttpUtility.HtmlEncode(row.Description)).Append("</span></td>");

            sb.Append(row.ShortFrom == null ? "<td></td>" : "<td class=\"stl-when\">" + (row.ShortFrom <= today ? "Now" : DateLabel(row.ShortFrom.Value, today)) + "</td>");
            sb.Append("<td class=\"stl-num\">").Append(Qty(row.OnHand)).Append("</td>");
            sb.Append("<td class=\"stl-num\">").Append(row.Minimum == 0 ? "" : Qty(row.Minimum)).Append("</td>");
            sb.Append("<td class=\"stl-num\">").Append(row.Overdue == 0 ? "" : Qty(row.Overdue)).Append("</td>");

            for (int w = 0; w < row.WeekEnd.Length; w++)
            {
                AppendBalance(sb, row.WeekEnd[w], row.WeekLow[w], row.Minimum);
            }
            if (showLater) AppendBalance(sb, row.Later, row.LaterLow, row.Minimum);
            sb.Append("</tr>");
        }

        /// <summary>Colour comes from the lowest point in the period, so a mid-week shortage is never hidden.</summary>
        private static void AppendBalance(StringBuilder sb, decimal end, decimal low, decimal minimum)
        {
            string band = Band(low, minimum);
            string title = band == Band(end, minimum) ? "" : " title=\"Drops to " + Qty(low) + " during this period\"";
            sb.Append("<td class=\"stl-num").Append(band).Append("\"").Append(title).Append(">").Append(Qty(end)).Append("</td>");
        }

        private static void AppendDetail(StringBuilder sb, StockTimelineRow row, DateTime today, int columns)
        {
            sb.Append("<tr class=\"stl-detail\" hidden=\"hidden\"><td colspan=\"").Append(columns).Append("\"><div class=\"stl-lines\">");
            if (row.Minimum > 0)
            {
                sb.Append("<p class=\"fuse-hint\">Minimum level ").Append(Qty(row.Minimum)).Append("</p>");
            }
            sb.Append("<table><thead><tr><th>Date</th><th>Document</th><th class=\"stl-num\">In</th><th class=\"stl-num\">Out</th><th class=\"stl-num\">Balance</th></tr></thead><tbody>");
            sb.Append("<tr><td>Today</td><td>On hand</td><td></td><td></td><td class=\"stl-num").Append(Band(row.OnHand, row.Minimum)).Append("\">").Append(Qty(row.OnHand)).Append("</td></tr>");

            foreach (StockTimelineLine line in row.Lines)
            {
                StockEvent ev = line.Event;
                string date = ev.Date == null ? "No date" : DateLabel(ev.Date.Value, today);

                sb.Append("<tr><td>").Append(date);
                if (line.Late) sb.Append(" <span class=\"stl-late\">Late</span>");
                sb.Append("</td><td>").Append(HttpUtility.HtmlEncode((ev.Kind + " " + ev.Document).Trim())).Append("</td>");
                sb.Append("<td class=\"stl-num\">").Append(ev.Qty > 0 ? Qty(ev.Qty) : "").Append("</td>");
                sb.Append("<td class=\"stl-num\">").Append(ev.Qty < 0 ? Qty(-ev.Qty) : "").Append("</td>");
                sb.Append("<td class=\"stl-num").Append(Band(line.Balance, row.Minimum)).Append("\">").Append(Qty(line.Balance)).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");

            if (row.Materials.Count > 0)
            {
                sb.Append("<p class=\"fuse-hint\">").Append(Qty(row.Uncovered)).Append(" not covered by stock or open orders. Materials needed to make them:</p>");
                sb.Append("<table><thead><tr><th>Material</th><th class=\"stl-num\">Needed</th><th class=\"stl-num\">Available</th><th class=\"stl-num\">Short</th></tr></thead><tbody>");
                foreach (MaterialNeed m in row.Materials.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
                {
                    sb.Append("<tr><td>").Append(HttpUtility.HtmlEncode(m.Code + " " + m.Description)).Append("</td>");
                    sb.Append("<td class=\"stl-num\">").Append(Qty(m.Needed)).Append("</td>");
                    sb.Append("<td class=\"stl-num\">").Append(Qty(m.Available)).Append("</td>");
                    sb.Append(m.Short > 0
                        ? "<td class=\"stl-num stl-short\">" + Qty(m.Short) + "</td>"
                        : "<td class=\"stl-num\"></td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table>");
            }

            sb.Append("</div></td></tr>");
        }

        /// <summary>The item's ticks as a short label, e.g. "FG + From BOM".</summary>
        public static string TypeTag(StockTimelineRow row)
        {
            var parts = new List<string>();
            if (row.IsFinishedItem) parts.Add("FG");
            if (row.IsFinished) parts.Add("From BOM");
            if (row.IsRaw) parts.Add("Component");
            return string.Join(" + ", parts);
        }

        private static string Band(decimal value, decimal minimum)
        {
            if (value < 0) return " stl-short";
            return minimum > 0 && value < minimum ? " stl-low" : "";
        }

        private static string Qty(decimal value)
        {
            return value.ToString("#,##0.##", CultureInfo.CurrentCulture);
        }

        private static string ShortDate(DateTime date)
        {
            return date.ToString("d MMM", CultureInfo.InvariantCulture);
        }

        private static string LongDate(DateTime date)
        {
            return date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        }

        private static string DateLabel(DateTime date, DateTime today)
        {
            return date.ToString(date.Year == today.Year ? "d MMM" : "d MMM yyyy", CultureInfo.InvariantCulture);
        }
    }
}
