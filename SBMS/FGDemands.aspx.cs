using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class FGDemands : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false);
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            if (File.Exists(Server.MapPath(imgPath)))
            {
                imgCoImg.ImageUrl = ResolveUrl(imgPath);
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
            if (!IsPostBack)
            {
                showhidebuttons();

                bool seeFinished, seeRaw;
                if (!CanSee(out seeFinished, out seeRaw))
                {
                    Response.Redirect("~/Dashboard.aspx", false); Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                RenderTimeline(seeFinished, seeRaw);
            }
        }

        private void showhidebuttons()
        {
            if (CurrentUser.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (CurrentUser.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (CurrentUser.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (CurrentUser.UseModule2 == true)
            {
                if (CurrentUser.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (CurrentUser.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (CurrentUser.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                ibtnFCasts.Style.Add("display", "none");
                ibtnmrp.Style.Add("display", "none");
                ibtnJobTrack.Style.Add("display", "none");
            }
            if (CurrentUser.UseModule3 == true)
            {
                if (CurrentUser.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (CurrentUser.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (CurrentUser.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
            }
            else
            {
                ibtmWorksOrders.Style.Add("display", "none");
                ibtmWOrdMgment.Style.Add("display", "none");
                ibtnRMD.Style.Add("display", "none");
            }
        }
        public class ItemsList
        {
            public long ItemID { get; set; }
            public string descript { get; set; }
        }

        // Item Demands (ported from Fuse Essentials): finished goods (Module 2) and raw materials
        // (Module 3) in one timeline. Raw Materials Demands redirects here.
        // Same rules the dashboard buttons used: finished goods for every company (Module 2 adds the
        // "See FG Demands" role right), raw materials with Module 3 and "View RMD".
        public static bool CanSee(UserDetails user, out bool seeFinished, out bool seeRaw)
        {
            seeFinished = user != null && !user.SageStockOnly && (user.UseModule2 != true || user.CanSeeFGDemands == true);
            seeRaw = user != null && !user.SageStockOnly && user.UseModule3 == true && user.CanViewRMD == true;
            return seeFinished || seeRaw;
        }

        private bool CanSee(out bool seeFinished, out bool seeRaw)
        {
            return CanSee(CurrentUser, out seeFinished, out seeRaw);
        }

        /// <summary>The "Up to" date: documents due after it are left out. Null = no date chosen.</summary>
        private DateTime? UpTo()
        {
            DateTime d;
            if (!DateTime.TryParseExact(txtUpTo.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return null;
            return d < DateTime.Today ? DateTime.Today : d;
        }

        private void RenderTimeline(bool seeFinished, bool seeRaw)
        {
            DateTime today = DateTime.Today;
            DateTime? upTo = UpTo();
            int weeks = upTo == null ? StockTimeline.Weeks : StockTimeline.WeeksUpTo(upTo.Value, today);
            List<StockTimelineRow> rows = LoadTimeline(seeFinished, seeRaw, upTo, weeks);
            litTimeline.Text = StockTimeline.Render(rows, today, weeks, upTo == null);

            // Category list from the items on the timeline; the choice is kept when the date changes.
            string keep = stlCat.Value;
            stlCat.Items.Clear();
            stlCat.Items.Add(new ListItem("All", ""));
            foreach (string c in rows.Select(r => r.Category).Where(c => c != "").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c))
                stlCat.Items.Add(new ListItem(c, c));
            if (stlCat.Items.FindByValue(keep ?? "") != null) stlCat.Value = keep;
        }

        protected void txtUpTo_TextChanged(object sender, EventArgs e)
        {
            bool seeFinished, seeRaw;
            if (CanSee(out seeFinished, out seeRaw)) RenderTimeline(seeFinished, seeRaw);
        }

        private List<StockTimelineRow> LoadTimeline(bool seeFinished, bool seeRaw, DateTime? upTo, int weeks)
        {
            long coId = CurrentUser.CoID;

            // Keyed on item code: documents pulled from Intacct carry the code but no SelectionId.
            var items = new Dictionary<string, StockTimelineItem>(StringComparer.OrdinalIgnoreCase);

            void AddEvent(string itemCode, DateTime? date, string kind, string document, decimal qty)
            {
                StockTimelineItem item;
                if (qty != 0 && (upTo == null || date == null || date.Value.Date <= upTo.Value)
                    && items.TryGetValue((itemCode ?? "").Trim(), out item))
                {
                    item.Events.Add(new StockEvent { Date = date, Kind = kind, Document = document, Qty = qty });
                }
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Active BOMs. The works order screen takes a finished item's first one, so this does too.
                var bomHeaders = _db.BOMHeaders
                    .Where(x => x.CompanyID == coId && x.BomActive == true)
                    .Select(x => new { x.BomHID, x.FGCode })
                    .ToList();
                var bomLines = (from h in _db.BOMHeaders
                                from l in _db.BOMLines
                                where h.CompanyID == coId && h.BomActive == true && l.BomHID == h.BomHID
                                      && l.ItemID != null && l.RMQty > 0
                                select new { h.BomHID, l.ItemCode, l.RMQty }).ToList();
                var onABom = new HashSet<string>(bomLines.Select(x => (x.ItemCode ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
                var madeFromBom = new HashSet<string>(bomHeaders.Select(x => (x.FGCode ?? "").Trim()), StringComparer.OrdinalIgnoreCase);

                var master = _db.ItemsMasters
                    .Where(x => x.CompanyID == coId && x.Active == true)
                    .Select(x => new { x.Code, x.Description, x.QuantityOnHand, x.ReorderLevel, x.IsFinishedGoods, x.Physical, x.IsBOMComponent, x.IsFromBOM, x.CategoryDescript })
                    .ToList();

                foreach (var m in master)
                {
                    string code = (m.Code ?? "").Trim();
                    bool sold = m.IsFinishedGoods == true && m.Physical == true;
                    // The Show filter follows the item's own ticks (Item Master): "Is Made Up From A BOM"
                    // = Finished goods, "Is a BOM Component" = Raw materials. An item on a BOM without
                    // the tick, or a bought-in item, is still planned and shows under All.
                    // Labour and other services have no stock to run out of.
                    bool finished = m.Physical != false && m.IsFromBOM == true;
                    bool raw = m.Physical != false && m.IsBOMComponent == true;
                    bool onBom = m.Physical != false && (onABom.Contains(code) || madeFromBom.Contains(code));
                    if (code.Length == 0 || (!sold && !finished && !raw && !onBom)) continue;

                    StockTimelineItem item;
                    if (!items.TryGetValue(code, out item))
                    {
                        item = new StockTimelineItem { Code = code, Description = m.Description, Category = (m.CategoryDescript ?? "").Trim() };
                        items[code] = item;
                    }
                    item.OnHand += m.QuantityOnHand ?? 0;
                    item.Minimum = Math.Max(item.Minimum, m.ReorderLevel);
                    item.IsFinishedItem |= sold;
                    item.IsFinished |= finished;
                    item.IsRaw |= raw;
                }

                var linesByBom = bomLines.ToLookup(x => x.BomHID);
                foreach (var bom in bomHeaders.OrderBy(x => x.BomHID))
                {
                    StockTimelineItem item;
                    if (items.TryGetValue((bom.FGCode ?? "").Trim(), out item) && item.Bom.Count == 0)
                    {
                        item.Bom.AddRange(linesByBom[bom.BomHID].Select(x => new BomComponent { Code = (x.ItemCode ?? "").Trim(), QtyPerUnit = x.RMQty.Value }));
                    }
                }

                // ---- Stock coming in

                string[] cancelled = { "Deleted", "Cancelled" };
                var purchaseOrders = (from h in _db.DocHeaders
                                      from l in _db.DocLines
                                      where h.CompanyID == coId && h.DocType == 1 && l.DocID == h.DocID
                                            && (l.DocType == null || l.DocType == 1)
                                            && (l.CompanyID == null || l.CompanyID == coId)
                                            && l.ReceiveComplete != true
                                      select new { h.DocumentNumber, h.DueDelDate, h.Status, l.ItemCode, l.QtyLeft }).ToList();
                foreach (var po in purchaseOrders.Where(x => !cancelled.Contains(x.Status, StringComparer.OrdinalIgnoreCase)))
                {
                    AddEvent(po.ItemCode, po.DueDelDate, "Purchase order", po.DocumentNumber, po.QtyLeft ?? 0);
                }

                // Active works orders: the same test WorksOrdersManf uses for a line that is still open.
                var woLines = (from h in _db.WorksOrderHeaders
                               from l in _db.WorksOrderLines
                               where h.CompanyID == coId && h.Active == true && l.WOID == h.ID
                               select new { h.WONum, h.DueDate, l.LineID, l.ItemCode, l.Quantity, l.Active, l.Complete }).ToList();
                foreach (var wo in woLines.Where(x => x.Active == true && x.Complete != true))
                {
                    AddEvent(wo.ItemCode, wo.DueDate, "Works order", "WO" + wo.WONum, wo.Quantity ?? 0);
                }

                // ---- Stock going out

                // Materials for a line already manufactured have been used, so only open lines still need them.
                var madeLines = new HashSet<int>(woLines.Where(x => x.Complete == true).Select(x => x.LineID));
                var woMaterials = (from h in _db.WorksOrderHeaders
                                   from r in _db.WorksOrderRMLines
                                   where h.CompanyID == coId && h.Active == true && r.WOID == h.ID
                                   select new { h.WONum, h.DueDate, r.ItemCode, r.Quantity, r.LinkedWOLineID }).ToList();
                foreach (var rm in woMaterials.Where(x => x.LinkedWOLineID == null || !madeLines.Contains(x.LinkedWOLineID.Value)))
                {
                    AddEvent(rm.ItemCode, rm.DueDate, "Works order", "WO" + rm.WONum, -(rm.Quantity ?? 0));
                }

                var slips = (from p in _db.PickingSlipMasters
                             from h in _db.DocHeaders
                             from l in _db.PickSlipLines
                             where p.PSActive == true && h.LinkedPSID == p.PSID && h.DocType == 5 && h.CompanyID == coId && l.PSID == p.PSID
                             select new { p.PSIntNumber, p.PSDueDate, h.DueDelDate, l.ItemCode, l.Quantity }).ToList();
                foreach (var ps in slips)
                {
                    AddEvent(ps.ItemCode, ps.PSDueDate ?? ps.DueDelDate, "Picking slip", ps.PSIntNumber, -(ps.Quantity ?? 0));
                }

                var jobCards = (from j in _db.JobCardsMasters
                                from l in _db.JobCardLines
                                where j.CustomerID == coId && j.JCActive == true && l.JCID == j.JCID && l.Physical == true
                                select new { j.JCNumber, l.LinePickDate, l.ItemCode, l.Quantity }).ToList();
                foreach (var jc in jobCards)
                {
                    AddEvent(jc.ItemCode, jc.LinePickDate, "Job card", jc.JCNumber, -(jc.Quantity ?? 0));
                }

                var forecasts = (from f in _db.ForCastHeaders
                                 from l in _db.ForCastLines
                                 where f.CompanyID == coId && l.DocID == f.ID && l.Active == true && l.DueDelDate != null
                                 select new { f.CustSupName, l.DueDelDate, l.ItemCode, l.Quantity }).ToList();
                foreach (var fc in forecasts)
                {
                    AddEvent(fc.ItemCode, fc.DueDelDate, "Forecast", fc.CustSupName, -(fc.Quantity ?? 0));
                }

                // Open sales orders with no picking slip or job card yet; once they have one, that document is the demand.
                string[] closed = { "Invoiced", "Cancelled", "Deleted" };
                var salesOrders = (from h in _db.DocHeaders
                                   from l in _db.DocLines
                                   where h.CompanyID == coId && h.DocType == 5 && l.DocID == h.DocID
                                         && (l.DocType == null || l.DocType == 5)
                                         && (l.CompanyID == null || l.CompanyID == coId)
                                         && h.Active == true && h.Complete == false && h.MissingInSage == null
                                         && h.LinkedPSID == null && h.LinkedJCID == null
                                   select new { h.DocumentNumber, h.DueDelDate, h.Status, l.ItemCode, l.Quantity }).ToList();
                foreach (var so in salesOrders.Where(x => !closed.Contains(x.Status, StringComparer.OrdinalIgnoreCase)))
                {
                    AddEvent(so.ItemCode, so.DueDelDate, "Sales order", so.DocumentNumber, -(so.Quantity ?? 0));
                }
            }

            DateTime today = DateTime.Today;
            StockTimeline.AddPlannedDemand(items, today);

            // Items with nothing open and stock above minimum need no planning, so they are left off.
            List<StockTimelineRow> rows = items.Values
                .Where(x => (seeFinished && (x.IsFinishedItem || x.IsFinished || !x.IsRaw)) || (seeRaw && x.IsRaw))
                .Select(x => StockTimeline.Build(x, today, weeks))
                .Where(x => x.Lines.Count > 0 || x.Attention < 2)
                .OrderBy(x => x.Attention)
                .ThenBy(x => x.ShortFrom ?? DateTime.MaxValue)
                .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return rows;
        }

        /// <summary>
        /// Every item on the timeline as one Excel row: what it is, when it runs short, and the
        /// stock left at the end of each week.
        /// </summary>
        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            bool seeFinished, seeRaw;
            if (!CanSee(out seeFinished, out seeRaw)) return;
            DateTime today = DateTime.Today;
            DateTime? upTo = UpTo();
            int weeks = upTo == null ? StockTimeline.Weeks : StockTimeline.WeeksUpTo(upTo.Value, today);
            DateTime monday = StockTimeline.WeekStart(today);

            var dt = new DataTable();
            dt.Columns.Add("Item Code");
            dt.Columns.Add("Description");
            dt.Columns.Add("Type");
            dt.Columns.Add("Status");
            dt.Columns.Add("Short From", typeof(DateTime));
            dt.Columns.Add("On Hand", typeof(decimal));
            dt.Columns.Add("Minimum", typeof(decimal));
            dt.Columns.Add("Overdue", typeof(decimal));
            for (int w = 0; w < weeks; w++)
                dt.Columns.Add(monday.AddDays(7 * w).ToString("d MMM yyyy", CultureInfo.InvariantCulture), typeof(decimal));
            if (upTo == null) dt.Columns.Add("Later", typeof(decimal));

            foreach (StockTimelineRow r in LoadTimeline(seeFinished, seeRaw, upTo, weeks))
            {
                DataRow d = dt.NewRow();
                d["Item Code"] = r.Code;
                d["Description"] = r.Description;
                d["Type"] = StockTimeline.TypeTag(r);
                d["Status"] = r.Attention == 0 ? "Short" : (r.Attention == 1 ? "Below minimum" : "");
                if (r.ShortFrom != null) d["Short From"] = r.ShortFrom.Value < today ? today : r.ShortFrom.Value;
                d["On Hand"] = r.OnHand;
                d["Minimum"] = r.Minimum;
                d["Overdue"] = r.Overdue;
                for (int w = 0; w < weeks; w++) d[8 + w] = r.WeekEnd[w];
                if (upTo == null) d["Later"] = r.Later;
                dt.Rows.Add(d);
            }
            ExcelHelper.ExportToExcel(dt, "ItemDemands", "Item Demands");
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
       


        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgbTrf_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Transfer.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/FGDemands.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/JobTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ProductionRMD.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgdash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }
    }
}