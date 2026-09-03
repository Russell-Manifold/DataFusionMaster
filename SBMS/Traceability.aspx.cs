using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Traceability and recall.
    ///
    /// One search box takes either a SERIAL (one unit) or a BATCH (the supplier lot a set of
    /// serials was received under). Everything shown is read back out of data the app already
    /// writes - LotTrackingMaster for identity and expiry, ItemTransaction for every movement -
    /// so there is nothing extra to capture and nothing that can drift out of step with stock.
    ///
    /// The recall question is the one that matters: given a batch, who has a unit from it.
    /// </summary>
    public partial class Traceability : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            imgCoImg.ImageUrl = File.Exists(Server.MapPath(imgPath))
                ? ResolveUrl(imgPath)
                : ResolveUrl("~/images/CoImages/0000.png");

            if (!IsPostBack)
            {
                // Deep link from elsewhere in the app: Traceability.aspx?s=SN0001
                string seed = Request.QueryString["s"];
                if (!string.IsNullOrWhiteSpace(seed))
                {
                    txtSearch.Text = seed.Trim();
                    RunSearch();
                }
            }
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e) { RunSearch(); }
        protected void lbtnSearch_Click(object sender, EventArgs e) { RunSearch(); }

        // ── Search ───────────────────────────────────────────────────────────────────────

        private void RunSearch()
        {
            lblErr.Text = "";
            pnlSummary.Visible = pnlUnits.Visible = pnlHistory.Visible = pnlCustomers.Visible = false;

            string term = (txtSearch.Text ?? "").Trim();
            if (term.Length == 0) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // A serial is a lot of quantity 1; a batch is what several serials point at with
                // ParentLotNumber. Try the unit first - that is the common case at a bench.
                var units = db.LotTrackingMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.LotNumber == term)
                    .ToList();

                bool asBatch = false;
                if (units.Count == 0)
                {
                    units = db.LotTrackingMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.ParentLotNumber == term)
                        .OrderBy(x => x.LotNumber).ToList();
                    asBatch = units.Count > 0;
                }

                // Third thing the box takes: a purchase order number - "what serials came in
                // on PO0000079". Asked often enough at a goods-in desk to be worth the box
                // answering it, rather than a screen of its own.
                bool asDoc = false;
                string docLabel = "";
                if (units.Count == 0)
                {
                    var doc = db.DocHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID
                                                             && x.DocumentNumber == term);
                    if (doc != null)
                    {
                        var docSerials = SerialPicking.GetDocSerials(db, CurrentUser.CoID, doc.DocID);
                        if (docSerials.Count > 0)
                        {
                            units = db.LotTrackingMasters
                                .Where(x => x.CompanyID == CurrentUser.CoID && docSerials.Contains(x.LotNumber))
                                .OrderBy(x => x.LotNumber).ToList();
                            asDoc = units.Count > 0;
                            docLabel = (doc.CustSupName ?? "").Trim();
                        }
                    }
                }

                if (units.Count == 0)
                {
                    lblErr.Text = "Nothing found for \"" + Server.HtmlEncode(term) + "\". "
                                + "Try a serial number, a supplier batch, or a purchase order number.";
                    return;
                }

                var serials = units.Select(x => x.LotNumber).ToList();

                // Every movement of those units, oldest first: received, moved, issued.
                var moves = db.ItemTransactions
                    .Where(x => x.CompanyID == CurrentUser.CoID && serials.Contains(x.LotNumber))
                    .OrderBy(x => x.TrnID)
                    .ToList();

                BindSummary(db, term, asBatch, asDoc, docLabel, units, moves);
                BindUnits(units, moves);
                BindHistory(db, moves);
                BindCustomers(db, moves);
            }
        }

        // ── Summary ──────────────────────────────────────────────────────────────────────

        private void BindSummary(SBMSEntities db, string term, bool asBatch, bool asDoc, string docLabel,
                                 List<LotTrackingMaster> units, List<ItemTransaction> moves)
        {
            var first = units[0];
            decimal onHand = moves.Sum(x => x.Qty ?? 0);
            int issued = units.Count(u => UnitOnHand(moves, u.LotNumber) <= 0
                                       && moves.Any(m => string.Equals(m.LotNumber, u.LotNumber, StringComparison.OrdinalIgnoreCase)));

            // The receipt: the first inbound movement carries the supplier document reference.
            var receipt = moves.FirstOrDefault(x => (x.Qty ?? 0) > 0);

            var sb = new System.Text.StringBuilder();
            sb.Append("<div class='trace-fact'><b>")
              .Append(asDoc ? "Purchase order" : asBatch ? "Batch" : "Serial").Append(":</b> ")
              .Append(Server.HtmlEncode(term)).Append("</div>");
            if (asDoc && docLabel.Length > 0)
            {
                sb.Append("<div class='trace-fact'><b>Supplier:</b> ")
                  .Append(Server.HtmlEncode(docLabel)).Append("</div>");
            }
            sb.Append("<div class='trace-fact'><b>Item:</b> ")
              .Append(Server.HtmlEncode(first.ItemCode ?? "")).Append("</div>");

            if (asBatch || asDoc)
            {
                sb.Append("<div class='trace-fact'><b>Units ")
                  .Append(asDoc ? "received" : "in batch").Append(":</b> ").Append(units.Count).Append("</div>");
                sb.Append("<div class='trace-fact'><b>Still on hand:</b> ").Append(onHand.ToString("N0")).Append("</div>");
                sb.Append("<div class='trace-fact'><b>Supplied:</b> ").Append(issued).Append("</div>");
            }
            else if (first.ParentLotNumber != null)
            {
                sb.Append("<div class='trace-fact'><b>Supplier batch:</b> ")
                  .Append(Server.HtmlEncode(first.ParentLotNumber)).Append("</div>");
            }

            if (first.UseByDate.HasValue)
            {
                bool expired = first.UseByDate.Value.Date < DateTime.Today;
                sb.Append("<div class='trace-fact'><b>Expires:</b> <span class='")
                  .Append(expired ? "trace-warn" : "trace-ok").Append("'>")
                  .Append(first.UseByDate.Value.ToString("dd MMM yyyy"))
                  .Append(expired ? " (expired)" : "").Append("</span></div>");
            }

            if (receipt != null)
            {
                sb.Append("<div class='trace-fact'><b>Received:</b> ")
                  .Append(receipt.TransactionDate.HasValue ? receipt.TransactionDate.Value.ToString("dd MMM yyyy") : "")
                  .Append("</div>");
                if (!string.IsNullOrWhiteSpace(receipt.TransactionReference))
                {
                    sb.Append("<div class='trace-fact'><b>Supplier document:</b> ")
                      .Append(Server.HtmlEncode(receipt.TransactionReference)).Append("</div>");
                }
                // A purchase-order search already named the supplier at the top, so don't
                // repeat it from the receipt.
                string supplier = asDoc ? "" : SupplierOf(db, receipt);
                if (supplier.Length > 0)
                {
                    sb.Append("<div class='trace-fact'><b>Supplier:</b> ")
                      .Append(Server.HtmlEncode(supplier)).Append("</div>");
                }
            }

            litSummary.Text = sb.ToString();
            pnlSummary.Visible = true;
        }

        // ── Units ────────────────────────────────────────────────────────────────────────

        private void BindUnits(List<LotTrackingMaster> units, List<ItemTransaction> moves)
        {
            var rows = units.Select(u =>
            {
                decimal qty = UnitOnHand(moves, u.LotNumber);
                return new
                {
                    Serial = u.LotNumber,
                    Batch = u.ParentLotNumber ?? "",
                    u.ItemCode,
                    ItemDescription = moves.Where(m => m.LotNumber == u.LotNumber)
                                           .Select(m => m.ItemDescription).FirstOrDefault() ?? "",
                    u.UseByDate,
                    OnHand = qty,
                    // No movements at all is NOT the same as supplied - it means the unit is
                    // registered but its receipt never reached the stock ledger.
                    Status = qty > 0
                        ? (u.UseByDate.HasValue && u.UseByDate.Value.Date < DateTime.Today ? "In stock - EXPIRED" : "In stock")
                        : (moves.Any(m => string.Equals(m.LotNumber, u.LotNumber, StringComparison.OrdinalIgnoreCase))
                            ? "Supplied" : "No stock movement")
                };
            }).ToList();

            GridUnits.DataSource = rows;
            GridUnits.DataBind();
            pnlUnits.Visible = true;
        }

        /// <summary>Flag expired stock still on the shelf - the row a recall cares about most.</summary>
        protected void GridUnits_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            string status = e.Row.Cells[6].Text ?? "";
            if (status.IndexOf("EXPIRED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                e.Row.BackColor = System.Drawing.Color.MistyRose;
                e.Row.ForeColor = System.Drawing.Color.Firebrick;
                e.Row.Font.Bold = true;
            }
            else if (status.Equals("Supplied", StringComparison.OrdinalIgnoreCase))
            {
                e.Row.ForeColor = System.Drawing.Color.Gray;
            }
        }

        // ── Movements ────────────────────────────────────────────────────────────────────

        private void BindHistory(SBMSEntities db, List<ItemTransaction> moves)
        {
            var stores = db.Stores.Where(x => x.CompanyID == CurrentUser.CoID)
                                  .Select(x => new { x.StoreID, x.StoreCode }).ToList()
                                  .ToDictionary(x => (long)x.StoreID, x => x.StoreCode);

            var rows = moves.Select(m => new
            {
                m.TransactionDate,
                Serial = m.LotNumber,
                TypeDescr = MovementDescription(m),
                Reference = m.TransactionReference ?? "",
                StoreCode = (m.ToID.HasValue && stores.ContainsKey(m.ToID.Value)) ? stores[m.ToID.Value] : "",
                Qty = m.Qty ?? 0
            }).ToList();

            GridHistory.DataSource = rows;
            GridHistory.DataBind();
            pnlHistory.Visible = true;
        }

        // ── Recall list ──────────────────────────────────────────────────────────────────

        private void BindCustomers(SBMSEntities db, List<ItemTransaction> moves)
        {
            // Units that left: a picking-slip movement out of stock. The slip points at the
            // sales order, which carries the customer and the Sage document number.
            // Picking-slip movements only. Transfers and adjustments out also carry a negative
            // quantity, and counting those would name a customer for stock that never left the
            // building - the worst possible answer on a recall.
            var issues = moves.Where(x => (x.Qty ?? 0) < 0
                                       && x.DocumentID.HasValue && x.DocumentID.Value > 0
                                       && string.Equals(x.TransactionType, "PS", StringComparison.OrdinalIgnoreCase))
                              .ToList();

            // A unit picked and then un-picked has a matching +1 against the same slip. Without
            // netting it off, a recall would tell them to contact a customer who never received
            // it - the one answer this screen must never give.
            issues = issues.Where(x => moves.Where(m => m.DocumentID == x.DocumentID
                                                     && string.Equals(m.LotNumber, x.LotNumber, StringComparison.OrdinalIgnoreCase)
                                                     && string.Equals(m.TransactionType, "PS", StringComparison.OrdinalIgnoreCase))
                                            .Sum(m => m.Qty ?? 0) < 0)
                           .ToList();
            if (issues.Count == 0) return;

            // Two queries for the whole recall list, not two per picking slip. A batch spanning
            // 200 slips was 400 sequential round-trips before an answer appeared.
            var psids = issues.Select(x => (int)x.DocumentID.Value).Distinct().ToList();

            var slips = db.PickingSlipMasters
                .Where(x => x.CustomerID == CurrentUser.CoID && psids.Contains(x.PSID))
                .Select(x => new { x.PSID, x.LinkedSOrdID })
                .ToList();

            var orderIds = slips.Where(x => x.LinkedSOrdID.HasValue)
                                .Select(x => x.LinkedSOrdID.Value).Distinct().ToList();

            var orders = db.DocHeaders
                .Where(x => x.CompanyID == CurrentUser.CoID && orderIds.Contains(x.DocID))
                .Select(x => new { x.DocID, x.CustSupName, x.DocumentNumber, x.DocDate })
                .ToList()
                .GroupBy(x => x.DocID)
                .ToDictionary(g => g.Key, g => g.First());

            var byDoc = new Dictionary<long, Tuple<string, string, DateTime?>>();
            foreach (var slip in slips)
            {
                string customer = "", reference = "";
                DateTime? when = null;
                if (slip.LinkedSOrdID.HasValue && orders.ContainsKey(slip.LinkedSOrdID.Value))
                {
                    var doc = orders[slip.LinkedSOrdID.Value];
                    customer = doc.CustSupName ?? "";
                    reference = doc.DocumentNumber ?? "";
                    when = doc.DocDate;
                }
                byDoc[slip.PSID] = Tuple.Create(customer, reference, when);
            }

            var rows = issues
                .GroupBy(x => x.DocumentID.Value)
                .Select(g => new
                {
                    Customer = byDoc.ContainsKey(g.Key) && byDoc[g.Key].Item1.Length > 0
                        ? byDoc[g.Key].Item1 : "(not linked to an order)",
                    Reference = byDoc.ContainsKey(g.Key) ? byDoc[g.Key].Item2 : "",
                    Supplied = (byDoc.ContainsKey(g.Key) ? byDoc[g.Key].Item3 : null) ?? g.Max(x => x.TransactionDate),
                    // Netted, not summed: a unit picked, un-picked and re-picked on the same slip
                    // has two -1 rows and one +1. Summing only the negatives reports two units
                    // supplied where one was - on the screen that must never overstate a recall.
                    Units = Math.Abs(moves.Where(m => m.DocumentID == g.Key
                                                   && string.Equals(m.TransactionType, "PS", StringComparison.OrdinalIgnoreCase)
                                                   && g.Select(i => i.LotNumber).Contains(m.LotNumber))
                                          .Sum(m => m.Qty ?? 0)),
                    Serials = string.Join(", ", g.Select(x => x.LotNumber).Distinct().OrderBy(x => x))
                })
                .OrderByDescending(x => x.Supplied)
                .ToList();

            GridCustomers.DataSource = rows;
            GridCustomers.DataBind();
            pnlCustomers.Visible = true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────────

        /// <summary>Quantity of one unit still on hand: the sum of its movements.</summary>
        private static decimal UnitOnHand(List<ItemTransaction> moves, string serial)
        {
            return moves.Where(x => string.Equals(x.LotNumber, serial, StringComparison.OrdinalIgnoreCase))
                        .Sum(x => x.Qty ?? 0);
        }

        private static string MovementDescription(ItemTransaction m)
        {
            bool inbound = (m.Qty ?? 0) > 0;
            switch ((m.TransactionType ?? "").ToUpperInvariant())
            {
                case "GRN": return "Received from supplier";
                case "PS":  return inbound ? "Returned to stock" : "Picked for customer";
                case "TRF": return inbound ? "Transferred in" : "Transferred out";
                case "ADJ": return inbound ? "Adjusted in" : "Adjusted out";
                default:    return inbound ? "In" : "Out";
            }
        }

        /// <summary>Supplier on the purchase document a unit was received against.</summary>
        private string SupplierOf(SBMSEntities db, ItemTransaction receipt)
        {
            if (!receipt.DocumentID.HasValue || receipt.DocumentID.Value <= 0) return "";
            long docId = receipt.DocumentID.Value;
            var doc = db.DocHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.DocID == docId);
            return doc != null ? (doc.CustSupName ?? "") : "";
        }

        // ── Export ───────────────────────────────────────────────────────────────────────

        protected void lbtnExcel_Click(object sender, EventArgs e)
        {
            // The recall list is the sheet worth having off this screen: who to contact.
            if (!pnlCustomers.Visible || GridCustomers.Rows.Count == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Search a serial or batch that has been supplied first.", "warning");
                return;
            }

            DataTable dt = new DataTable();
            foreach (DataControlField col in GridCustomers.Columns) dt.Columns.Add(col.HeaderText);
            foreach (GridViewRow row in GridCustomers.Rows)
            {
                DataRow dr = dt.NewRow();
                for (int i = 0; i < GridCustomers.Columns.Count; i++) dr[i] = row.Cells[i].Text.Replace("&nbsp;", " ").Trim();
                dt.Rows.Add(dr);
            }

            ExcelHelper.ExportToExcel(dt, "Recall_" + txtSearch.Text.Trim(), "Recall");
        }

        // ── Navigation ───────────────────────────────────────────────────────────────────

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
