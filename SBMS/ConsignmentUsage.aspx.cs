using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Stock Control -> Consignment Stock. Month end for a consignment location: read what the
    /// location holds, type the count, check the statement, create the Sage sales order for what
    /// was used. The arithmetic and SQL live in Classes/ConsignmentUsage.cs.
    /// </summary>
    public partial class ConsignmentUsagePage : BasePage
    {
        // Set by LoadRun before the grid binds; RowDataBound reads it to lock a finished run.
        private bool _draft = true;
        private long UsageId
        {
            get { long v; return long.TryParse(hfUsageID.Value, out v) ? v : 0; }
            set { hfUsageID.Value = value == 0 ? "" : value.ToString(); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (CurrentUser.CanStockControl != true || !CurrentUser.UseConsignment)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
                return;
            }
            if (!IsPostBack)
            {
                lblUsername.Text = $":.. {CurrentUser.UserName}..:  ";
                string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
                imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");
                txtCountDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (!ConsignmentUsage.TablesExist(_db))
                    {
                        pnlNotReady.Visible = true; pnlMain.Visible = false;
                        return;
                    }
                    var stores = ConsignmentUsage.Stores(_db, CurrentUser.CoID);
                    DDStore.Items.Clear();
                    DDStore.Items.Add(new System.Web.UI.WebControls.ListItem("- Select -", ""));
                    foreach (var s in stores)
                        DDStore.Items.Add(new System.Web.UI.WebControls.ListItem(s.ConsignCustomer + "  -  " + s.StoreDescript + " (" + s.StoreCode + ")", s.StoreID.ToString()));
                    pnlNoStores.Visible = stores.Count == 0;
                    if (stores.Count == 1) { DDStore.SelectedIndex = 1; ShowOpenDraft(_db); }
                    LoadHistory(_db);
                }
            }
        }

        private void LoadHistory(SBMSEntities _db)
        {
            GridHistory.DataSource = ConsignmentUsage.History(_db, CurrentUser.CoID);
            GridHistory.DataBind();
        }

        private int SelectedStoreId()
        {
            int id; return int.TryParse(DDStore.SelectedValue, out id) ? id : 0;
        }

        /// <summary>A location can only have one unfinished count; if there is one, show it.</summary>
        private void ShowOpenDraft(SBMSEntities _db)
        {
            int storeId = SelectedStoreId();
            if (storeId == 0) { pnlRun.Visible = false; UsageId = 0; return; }
            var draft = ConsignmentUsage.OpenDraft(_db, CurrentUser.CoID, storeId);
            if (draft != null) LoadRun(_db, draft.UsageID);
            else { pnlRun.Visible = false; UsageId = 0; }
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString())) ShowOpenDraft(_db);
        }

        protected void lbtnStart_Click(object sender, EventArgs e)
        {
            int storeId = SelectedStoreId();
            if (storeId == 0) { AlertHelper.ShowSweetAlert(this, "Pick the consignment location first.", "warning"); return; }
            DateTime countDate;
            if (!DateTime.TryParse(txtCountDate.Text, out countDate)) { AlertHelper.ShowSweetAlert(this, "Enter the date you counted the stock.", "warning"); return; }
            if (countDate.Date > DateTime.Today) { AlertHelper.ShowSweetAlert(this, "The count date cannot be in the future.", "warning"); return; }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lastCount = ConsignmentUsage.LastCountDate(_db, CurrentUser.CoID, storeId);
                if (lastCount.HasValue && countDate.Date <= lastCount.Value.Date)
                {
                    AlertHelper.ShowSweetAlert(this, "The last month end for this location was counted on " + lastCount.Value.ToString("dd MMM yyyy") + ". The new count date must be after that.", "warning");
                    return;
                }
                var open = ConsignmentUsage.OpenDraft(_db, CurrentUser.CoID, storeId);
                if (open != null)
                {
                    LoadRun(_db, open.UsageID);
                    AlertHelper.ShowSweetAlert(this, "This location already has a count in progress (started " + open.CreatedDate.ToString("dd MMM yyyy") + "). Finish it, or Discard it to start again.", "info");
                    return;
                }
                var store = ConsignmentUsage.Stores(_db, CurrentUser.CoID).FirstOrDefault(x => x.StoreID == storeId);
                if (store == null) { AlertHelper.ShowSweetAlert(this, "That location is no longer a consignment location.", "error"); return; }
                long id = ConsignmentUsage.StartDraft(_db, CurrentUser, store, countDate);
                LoadRun(_db, id);
                LoadHistory(_db);
            }
        }

        private void LoadRun(SBMSEntities _db, long usageId)
        {
            var h = ConsignmentUsage.Header(_db, CurrentUser.CoID, usageId);
            if (h == null) { pnlRun.Visible = false; UsageId = 0; return; }
            UsageId = usageId;
            pnlRun.Visible = true;
            bool draft = h.Status == "Draft";
            _draft = draft;
            var lines = ConsignmentUsage.Lines(_db, CurrentUser.CoID, usageId);
            GridLines.DataSource = lines;
            GridLines.DataBind();
            lblTotal.Text = lines.Sum(x => x.LineExcl).ToString("N2");
            string period = h.PeriodFrom.HasValue ? "after the last count on " + h.PeriodFrom.Value.ToString("dd MMM yyyy") : "from the first delivery (first month end for this location)";
            lblRunInfo.Text = "<b>" + h.CustomerName + "</b> - " + h.StoreDescript + " (" + h.StoreCode + "). Count date <b>" + h.CountDate.ToString("dd MMM yyyy") +
                              "</b>. Movements " + period + " up to the end of the count date. Statement no. " + h.UsageID + "." +
                              (h.Status == "Draft" && h.SageSODocID.HasValue ? " <b style='color:#b02a2a'>Sales order " + h.SageSONumber + " was created but the stock booking did not finish - click Create Sales Order to finish it (no second order is created).</b>" : "");
            lbtnSave.Visible = lbtnCreateSO.Visible = lbtnDelete.Visible = draft;
            lblDone.Visible = !draft;
            if (!draft)
                lblDone.Text = "Month end done on " + (h.OrderedDate ?? h.CreatedDate).ToString("dd MMM yyyy HH:mm") + ". Sales order <b>" + (h.SageSONumber ?? "") +
                               "</b> raised for the used quantities (" + h.TotalExcl.ToString("N2") + " excl) and the stock taken out of the location. Confirm the order and invoice it the way you normally do.";
            if (DDStore.Items.FindByValue(h.StoreID.ToString()) != null) DDStore.SelectedValue = h.StoreID.ToString();
            txtCountDate.Text = h.CountDate.ToString("yyyy-MM-dd");
        }

        protected void GridLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var l = e.Row.DataItem as ConsignmentUsage.UsageLine;
            if (l == null) return;
            bool draft = _draft;
            ((TextBox)e.Row.FindControl("txtCounted")).Enabled = draft;
            ((TextBox)e.Row.FindControl("txtPrice")).Enabled = draft;
            var used = (Label)e.Row.FindControl("lblUsed");
            used.CssClass = l.Used < 0 ? "cu-neg" : "cu-used";
            if (l.Used < 0) used.ToolTip = "Counted more than the location should hold - check the count. Nothing is invoiced for this line.";
            if (l.Counted == null && draft) e.Row.Cells[6].Style["background"] = "#fff6e5";
        }

        /// <summary>Reads every row's count and price into the draft. Returns false (with a message) on bad input.</summary>
        private bool SaveCounts(SBMSEntities _db)
        {
            if (UsageId == 0) return false;
            foreach (GridViewRow row in GridLines.Rows)
            {
                if (row.RowType != DataControlRowType.DataRow) continue;
                long lineId = Convert.ToInt64(GridLines.DataKeys[row.RowIndex].Value);
                string code = row.Cells[0].Text;
                string cntText = ((TextBox)row.FindControl("txtCounted")).Text.Trim();
                string priceText = ((TextBox)row.FindControl("txtPrice")).Text.Trim();
                decimal? counted = null; decimal cnt, price;
                if (cntText.Length > 0)
                {
                    if (!decimal.TryParse(cntText, NumberStyles.Any, CultureInfo.InvariantCulture, out cnt) || cnt < 0)
                    { AlertHelper.ShowSweetAlert(this, "Counted for " + code + " must be a number of 0 or more.", "error"); return false; }
                    counted = cnt;
                }
                if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out price) || price < 0)
                { AlertHelper.ShowSweetAlert(this, "Price for " + code + " must be a number of 0 or more.", "error"); return false; }
                ConsignmentUsage.SaveLine(_db, CurrentUser.CoID, lineId, counted, price);
            }
            ConsignmentUsage.RefreshTotal(_db, CurrentUser.CoID, UsageId);
            return true;
        }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (!SaveCounts(_db)) return;
                LoadRun(_db, UsageId);
                var lines = ConsignmentUsage.Lines(_db, CurrentUser.CoID, UsageId);
                int missing = lines.Count(x => x.Counted == null);
                AlertHelper.ShowSweetAlert(this, missing == 0 ? "Counts saved." : "Counts saved. " + missing + " line(s) still have no count (highlighted).", missing == 0 ? "success" : "info");
            }
        }

        protected void lbtnDelete_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                ConsignmentUsage.DeleteDraft(_db, CurrentUser.CoID, UsageId);
                UsageId = 0; pnlRun.Visible = false;
                LoadHistory(_db);
                AlertHelper.ShowSweetAlert(this, "Count discarded. Nothing was sent to Sage.", "success");
            }
        }

        protected void GridHistory_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "OpenRun") return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                LoadRun(_db, Convert.ToInt64(e.CommandArgument));
        }

        // ───────────────────────── Create the Sage sales order ─────────────────────────
        protected void lbtnCreateSO_Click(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(CreateSalesOrderAsync));
        }

        private async Task CreateSalesOrderAsync()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (!SaveCounts(_db)) return;
                var h = ConsignmentUsage.Header(_db, CurrentUser.CoID, UsageId);
                if (h == null || h.Status != "Draft") { AlertHelper.ShowSweetAlert(this, "This count has already been turned into a sales order.", "info"); LoadRun(_db, UsageId); return; }
                var lines = ConsignmentUsage.Lines(_db, CurrentUser.CoID, UsageId);
                var uncounted = lines.Where(x => x.Counted == null).Select(x => x.ItemCode).ToList();
                if (uncounted.Count > 0)
                {
                    LoadRun(_db, UsageId);
                    AlertHelper.ShowSweetAlert(this, "Every line needs a count before the order can be created. Missing: " + string.Join(", ", uncounted.Take(8)) + (uncounted.Count > 8 ? " ..." : "") + ". Type 0 if the customer holds none.", "warning");
                    return;
                }
                var used = lines.Where(x => x.Used > 0).ToList();
                if (used.Count == 0) { LoadRun(_db, UsageId); AlertHelper.ShowSweetAlert(this, "Nothing was used: every count matches what the location holds. There is nothing to invoice. Discard the count, or correct the counts.", "info"); return; }
                var zeroPrice = used.Where(x => x.UnitPrice <= 0).Select(x => x.ItemCode).ToList();
                if (zeroPrice.Count > 0) { LoadRun(_db, UsageId); AlertHelper.ShowSweetAlert(this, "These used items have no price: " + string.Join(", ", zeroPrice) + ". Enter a price (or 0 is not allowed for a used item).", "warning"); return; }
                var over = lines.Where(x => x.Used < 0).Select(x => x.ItemCode).ToList();

                long soId = h.SageSODocID ?? 0; string soNum = h.SageSONumber ?? "UAT";
                bool alreadyClaimed = h.SageSODocID.HasValue;   // Sage order exists from an earlier attempt: only finish the booking
                if (CurrentUser.UATMode == false && !alreadyClaimed)
                {
                    string json = ConsignmentUsage.BuildSalesOrderJson(h, lines);
                    JObject r;
                    try { r = await new ApiUrlCall().APIPostDocumentAsync("SalesOrder", json, CurrentUser); }
                    catch (Exception ex)
                    {
                        new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Consignment usage {UsageId} SalesOrder/Save threw - {ex}");
                        LoadRun(_db, UsageId);
                        AlertHelper.ShowSweetAlert(this, "Sage did not answer, so nothing was created or changed. Try again in a minute.", "error");
                        return;
                    }
                    if (r == null || r["ID"] == null)
                    {
                        string why = r?["Message"]?.ToString() ?? r?["error"]?.ToString() ?? r?.ToString() ?? "no response";
                        new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Consignment usage {UsageId} SalesOrder/Save refused - {why}");
                        LoadRun(_db, UsageId);
                        AlertHelper.ShowSweetAlert(this, "Sage refused the sales order, so nothing was created or changed: " + why, "error");
                        return;
                    }
                    soId = Convert.ToInt64(r["ID"].ToString());
                    soNum = r["DocumentNumber"]?.ToString() ?? soId.ToString();
                    // Claim it on the draft at once: whatever fails below, a retry must never create a second order.
                    ConsignmentUsage.ClaimOrder(_db, CurrentUser.CoID, UsageId, soId, soNum);
                }
                // Status + stock booking together, or neither.
                using (var tx = _db.Database.BeginTransaction())
                {
                    try
                    {
                        ConsignmentUsage.MarkOrdered(_db, CurrentUser.CoID, UsageId, DateTime.Now);
                        ConsignmentUsage.BookUsage(_db, CurrentUser, h, lines, soId, soNum);
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Consignment usage {UsageId}: Sage order {soNum} exists but booking failed - {ex}");
                        LoadRun(_db, UsageId);
                        AlertHelper.ShowSweetAlert(this, "Sales order " + soNum + " was created in Sage, but recording it here failed. Click Create Sales Order again to finish - no second order will be created. If it fails again, contact support.", "error");
                        return;
                    }
                }
            }
            // Pull the new order into Data Fusion straight away so it is on the Sales Orders page.
            // Separate try: the order exists in Sage and the stock is booked whatever happens here.
            long usageAfter = UsageId;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var h = ConsignmentUsage.Header(_db, CurrentUser.CoID, usageAfter);
                if (h != null && h.SageSODocID.HasValue && h.SageSODocID.Value > 0)
                {
                    try { await ApiUrlCall.GetOneSalesOrder(CurrentUser, h.SageSODocID.Value); }
                    catch (Exception ex) { new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} Consignment usage {usageAfter}: SO {h.SageSONumber} created in Sage but local pull failed - {ex.Message}. It will arrive on the next sync."); }
                }
                LoadRun(_db, usageAfter);
                LoadHistory(_db);
                string msg = CurrentUser.UATMode
                    ? "Testing Mode: no sales order was sent to Sage. The used quantities were taken out of the location."
                    : "Sales order " + (h?.SageSONumber ?? "") + " created in Sage for " + (h?.TotalExcl ?? 0).ToString("N2") + " excl. The used quantities were taken out of the location. Confirm the order and invoice it the way you normally do.";
                AlertHelper.ShowSweetAlert(this, msg, "success");
            }
        }

        // ───────────────────────── Statement PDF ─────────────────────────
        protected void lbtnStatement_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (lbtnSave.Visible && !SaveCounts(_db)) return;
                var h = ConsignmentUsage.Header(_db, CurrentUser.CoID, UsageId);
                if (h == null) return;
                var lines = ConsignmentUsage.Lines(_db, CurrentUser.CoID, UsageId);
                var co = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
                string fname = "Consignment_" + h.UsageID;
                CreateStatementPdf(h, lines, co, fname);
                Response.Redirect("~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD + "\\" + fname, false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        private void CreateStatementPdf(ConsignmentUsage.UsageHeader h, List<ConsignmentUsage.UsageLine> lines, CompanyMaster co, string fname)
        {
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 9, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 16, BaseColor.BLACK);
            string dir = Server.MapPath("~\\PDFs\\" + CurrentUser.UserGuiD);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string filepath = Path.Combine(dir, fname + ".PDF");
            if (File.Exists(filepath)) File.Delete(filepath);

            var doc = new iTextSharp.text.Document(PageSize.A4, 40, 40, 40, 40);
            var writer = PdfWriter.GetInstance(doc, new FileStream(filepath, FileMode.Create));
            var footer = new PDFFooter { LeftLabel = "Prepared by", CenterLabel = "Customer (sign)" };
            writer.PageEvent = footer;
            doc.Open();
            doc.SetMargins(28f, 28f, 100f, 80f);

            // Header: logo | title + parties
            var table = new PdfPTable(3);
            table.SetWidths(new int[] { 150, 285, 150 });
            table.TotalWidth = doc.PageSize.Width - 80; table.LockedWidth = true;
            string imgpath = Server.MapPath("~/images/CoImages/" + CurrentUser.CoID + ".png");
            PdfPCell cell;
            try { var gif = iTextSharp.text.Image.GetInstance(imgpath); gif.ScaleToFit(125f, 60f); cell = new PdfPCell(gif); }
            catch { cell = new PdfPCell(new Phrase("")); }
            cell.Border = 0; cell.Rowspan = 6; table.AddCell(cell);
            Action<string, string> row = (label, value) =>
            {
                var c1 = new PdfPCell(new Phrase(label, medfont)) { Border = 0, HorizontalAlignment = 2 }; table.AddCell(c1);
                var c2 = new PdfPCell(new Phrase(value ?? "", medfont)) { Border = 0, HorizontalAlignment = 2 }; table.AddCell(c2);
            };
            var t1 = new PdfPCell(new Phrase("Consignment Usage Statement", headfont)) { Border = 0, HorizontalAlignment = 2, Colspan = 2 }; table.AddCell(t1);
            var fromLines = new List<string> { co?.CompanyName };
            foreach (string a in new[] { co?.BillAddress1, co?.BillAddress2, co?.BillAddress3, co?.BillAddress4 })
                if (!string.IsNullOrWhiteSpace(a)) fromLines.Add(a.Trim());
            row("From:", string.Join("\n", fromLines.Where(x => !string.IsNullOrWhiteSpace(x))));
            row("Customer:", (h.CustomerName ?? "") + (string.IsNullOrWhiteSpace(h.ConsignAddress) ? "" : "\n" + h.ConsignAddress.Trim()));
            row("Location:", h.StoreDescript + " (" + h.StoreCode + ")");
            row("Period:", (h.PeriodFrom.HasValue ? h.PeriodFrom.Value.ToString("dd MMM yy") : "Start") + " - " + h.CountDate.ToString("dd MMM yy"));
            row("Statement:", h.UsageID + (h.Status == "Ordered" ? "   Sales order " + h.SageSONumber : "   DRAFT - for agreement"));
            doc.Add(table);

            var t4 = new PdfPTable(9);
            t4.SpacingBefore = 14f;
            t4.SetWidths(new int[] { 55, 165, 40, 45, 45, 45, 45, 45, 55 });
            t4.TotalWidth = doc.PageSize.Width - 80; t4.LockedWidth = true;
            foreach (string hd in new[] { "Item Code", "Description", "Opening", "Delivered", "Other", "Counted", "Used", "Price", "Value (excl)" })
            {
                var c = new PdfPCell(new Phrase(hd, regfont)) { HorizontalAlignment = 1, BackgroundColor = BaseColor.LIGHT_GRAY, BorderColor = new BaseColor(211, 211, 211) };
                t4.AddCell(c);
            }
            Func<string, int, PdfPCell> cellOf = (text, align) => new PdfPCell(new Phrase(text ?? "", regfont)) { HorizontalAlignment = align, BorderColor = new BaseColor(211, 211, 211) };
            decimal total = 0;
            foreach (var l in lines)
            {
                t4.AddCell(cellOf(l.ItemCode, 0));
                t4.AddCell(cellOf(l.ItemDescription, 0));
                t4.AddCell(cellOf(l.Opening.ToString("N2"), 2));
                t4.AddCell(cellOf(l.Delivered.ToString("N2"), 2));
                t4.AddCell(cellOf(l.OtherMoves.ToString("N2"), 2));
                t4.AddCell(cellOf(l.Counted.HasValue ? l.Counted.Value.ToString("N2") : "", 2));
                t4.AddCell(cellOf(l.Used.ToString("N2"), 2));
                t4.AddCell(cellOf(l.UnitPrice.ToString("N2"), 2));
                t4.AddCell(cellOf(l.LineExcl.ToString("N2"), 2));
                total += l.LineExcl;
            }
            var tot = new PdfPCell(new Phrase("Total used (excl VAT)", regfont)) { Colspan = 8, HorizontalAlignment = 2, BorderColor = new BaseColor(211, 211, 211) }; t4.AddCell(tot);
            t4.AddCell(cellOf(total.ToString("N2"), 2));
            doc.Add(t4);
            doc.AddTitle("Consignment Usage Statement " + h.UsageID);
            doc.AddAuthor("Data Fusion");
            doc.Close();
        }

        // ───────────────────────── nav ─────────────────────────
        protected void lbtnDash_Click(object sender, EventArgs e) { Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false); }
        protected void lbtnStockControl_Click(object sender, EventArgs e) { Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false); }
        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
