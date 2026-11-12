using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS.Classes
{
    /// <summary>
    /// The item x column quantity grid that replaced the drag-and-drop pivots (Stock Enquiry,
    /// FG Demands, Raw Materials Demands): one row per item (per item + lot), one column per
    /// store / transaction type, and a Total. Pages bind the DataTable to an auto-generated
    /// GridView and call FormatRow from its RowDataBound. Styles: prologue/assets/css/qtygrid.css.
    /// </summary>
    public static class QtyMatrix
    {
        public class Line
        {
            public string Code;
            public string Description;
            public string Lot;
            public string Column;
            public decimal Qty;
        }

        /// <param name="columnOrder">Columns in this order first (the rest alphabetical); null = alphabetical.</param>
        /// <param name="header">Header text for a column key; null = the key itself.</param>
        /// <param name="hideZero">Drop rows (and columns) that are zero everywhere.</param>
        /// <param name="negativeOnly">Keep only rows whose total is below zero (shortages).</param>
        /// <param name="firstQtyCol">Index of the first quantity column, for FormatRow.</param>
        public static DataTable Build(IEnumerable<Line> source, bool withLot, bool hideZero, bool negativeOnly,
                                      IList<string> columnOrder, Func<string, string> header, string totalName,
                                      out int firstQtyCol)
        {
            var rows = source
                .GroupBy(r => new { r.Code, r.Description, Lot = withLot ? (r.Lot ?? "") : "" })
                .Select(g => new
                {
                    g.Key.Code, g.Key.Description, g.Key.Lot,
                    Qty = g.GroupBy(r => string.IsNullOrEmpty(r.Column) ? "(none)" : r.Column)
                           .ToDictionary(c => c.Key, c => c.Sum(r => r.Qty))
                })
                .Where(r => !hideZero || r.Qty.Values.Any(q => q != 0))
                .Where(r => !negativeOnly || r.Qty.Values.Sum() < 0)
                .OrderBy(r => r.Code).ThenBy(r => r.Lot)
                .ToList();

            var keys = rows.SelectMany(r => r.Qty.Where(c => !hideZero || c.Value != 0).Select(c => c.Key))
                           .Distinct()
                           .OrderBy(k => columnOrder == null || !columnOrder.Contains(k) ? int.MaxValue : columnOrder.IndexOf(k))
                           .ThenBy(k => k)
                           .ToList();

            var dt = new DataTable();
            dt.Columns.Add("Code");
            dt.Columns.Add("Description");
            if (withLot) dt.Columns.Add("Lot");
            firstQtyCol = dt.Columns.Count;
            var names = new List<string>();
            foreach (string k in keys)
            {
                string name = header == null ? k : header(k);
                if (dt.Columns.Contains(name) || name.Equals(totalName, StringComparison.OrdinalIgnoreCase)) name += " (" + k + ")";
                dt.Columns.Add(name, typeof(decimal));
                names.Add(name);
            }
            dt.Columns.Add(totalName, typeof(decimal));

            foreach (var r in rows)
            {
                DataRow d = dt.NewRow();
                d["Code"] = r.Code;
                d["Description"] = r.Description;
                if (withLot) d["Lot"] = r.Lot;
                for (int i = 0; i < keys.Count; i++)
                    if (r.Qty.TryGetValue(keys[i], out decimal q)) d[names[i]] = q;
                d[totalName] = r.Qty.Values.Sum();
                dt.Rows.Add(d);
            }
            return dt;
        }

        /// <summary>The table as a view sorted on the column the user clicked (ignored if that column is gone).</summary>
        public static DataView Sorted(DataTable dt, string sortCol, bool desc)
        {
            DataView view = dt.DefaultView;
            if (sortCol != null && dt.Columns.Contains(sortCol))
                view.Sort = "[" + sortCol.Replace("]", "\\]") + "]" + (desc ? " DESC" : " ASC");
            return view;
        }

        /// <summary>
        /// Quantity cells right-aligned to their headers; a column cell is blank when zero, the
        /// Total is always shown, negatives are red.
        /// </summary>
        public static void FormatRow(GridViewRowEventArgs e, int firstQtyCol)
        {
            int last = e.Row.Cells.Count - 1;
            if (e.Row.RowType == DataControlRowType.Header)
            {
                for (int i = firstQtyCol; i <= last; i++)
                    e.Row.Cells[i].CssClass = "q" + (i == last ? " tot" : "");
            }
            else if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var drv = (DataRowView)e.Row.DataItem;
                for (int i = firstQtyCol; i <= last; i++)
                {
                    decimal q = drv[i] == DBNull.Value ? 0 : (decimal)drv[i];
                    e.Row.Cells[i].Text = q == 0 && i != last ? "" : q.ToString("N2");
                    e.Row.Cells[i].CssClass = "q" + (q < 0 ? " neg" : "") + (i == last ? " tot" : "");
                }
            }
        }
    }
}
