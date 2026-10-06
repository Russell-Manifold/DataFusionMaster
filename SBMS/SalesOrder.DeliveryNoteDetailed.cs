using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SBMS
{
    /// <summary>
    /// The "Detailed" delivery note (Configuration -> Company -> Delivery Note -> Layout).
    ///
    /// A second layout beside the standard note in SalesOrder.aspx.cs CreatePDF(), which is
    /// not touched: customer block (VAT no, contact, purchase order, sales order), one row per
    /// item with Order / Inv / Back Order quantities, a sign-off block and the company footer.
    ///
    /// One note = one delivery = one completed picking slip. Back Order is what is still
    /// owing after THIS delivery: ordered, less everything delivered on this and earlier slips.
    ///
    /// Its settings and the running delivery note number live in columns read by raw SQL
    /// (SQL/Add_DeliveryNoteLayout.sql), outside the EF model. If they cannot be read, or
    /// anything here fails, the caller prints the standard note instead.
    /// </summary>
    public partial class SalesOrder
    {
        public class DnSettings
        {
            public int DNLayout { get; set; }
            public string CompanyName { get; set; }
            public string CoRegNo { get; set; }
            public string CoVatNo { get; set; }
            public string DocFooter { get; set; }
            public string PODPrefix { get; set; }
            public int PODNextNumber { get; set; }
        }

        /// <summary>Null when the columns are not there yet (script not run) - caller prints the standard note.</summary>
        private DnSettings LoadDnSettings()
        {
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    return _db.Database.SqlQuery<DnSettings>(
                        "SELECT DNLayout, CompanyName, CoRegNo, CoVatNo, DocFooter, PODPrefix, PODNextNumber " +
                        "FROM dbo.CompanyMaster WHERE SBCACoID = @p0", CurrentUser.CoID).FirstOrDefault();
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// The customer's VAT number, contact person and telephone are on the Sage customer
        /// record, which is not kept locally - read the order from Sage with its customer.
        /// Never allowed to stop the note printing: on any failure those three print blank.
        /// </summary>
        private async Task<JObject> FetchDnCustomerAsync()
        {
            try
            {
                long soId = Convert.ToInt64(lblDocID.Text);
                string url = ApiUrlCall.sageurl + "SalesOrder/GET/" + soId + "?apikey={" + ApiUrlCall.APIKey + "}&CompanyID="
                           + CurrentUser.CoID + "&includeDetail=true&includeCustomerDetails=true";
                JObject so = await new ApiUrlCall().ApiCallAsync(url, CurrentUser);
                return (so != null && so["error"] == null) ? so : null;
            }
            catch { return null; }
        }

        private static string DnText(JToken t)
        {
            return (t == null || t.Type == JTokenType.Null) ? "" : t.ToString().Trim();
        }

        /// <summary>
        /// The delivery's number. With a running number configured (PODNextNumber > 0) the
        /// slip is given the next one the first time its note is printed and keeps it, so a
        /// reprint shows the same number. Otherwise the Sales Order number, with the
        /// delivery's sequence when the order has more than one delivery.
        /// </summary>
        private string DnNumber(SBMSEntities _db, DnSettings s, DocHeader DH, int? slipId, int seq, int slipCount)
        {
            string fromOrder = (DH.DocumentNumber ?? "").Replace("SO", "");
            if (slipCount > 1 && seq > 0) fromOrder = fromOrder + "-" + seq;
            if (slipId == null || s.PODNextNumber <= 0) return fromOrder;

            try
            {
                string existing = _db.Database.SqlQuery<string>(
                    "SELECT PODNumber FROM dbo.PickingSlipMaster WHERE PSID = @p0", slipId.Value).FirstOrDefault();
                if (!string.IsNullOrEmpty(existing)) return existing;

                // Take the next number and move the counter on in ONE statement, so two people
                // printing at the same moment cannot be handed the same number.
                int next = _db.Database.SqlQuery<int>(
                    "UPDATE dbo.CompanyMaster SET PODNextNumber = PODNextNumber + 1 " +
                    "OUTPUT deleted.PODNextNumber WHERE SBCACoID = @p0 AND PODNextNumber > 0", CurrentUser.CoID).FirstOrDefault();
                if (next <= 0) return fromOrder;

                _db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.PickingSlipMaster SET PODNumber = @p0 WHERE PSID = @p1 AND PODNumber IS NULL",
                    (s.PODPrefix ?? "").Trim() + next, slipId.Value);
                return _db.Database.SqlQuery<string>(
                    "SELECT PODNumber FROM dbo.PickingSlipMaster WHERE PSID = @p0", slipId.Value).FirstOrDefault() ?? fromOrder;
            }
            catch { return fromOrder; }
        }

        private string DnQty(decimal qty)
        {
            return Convert.ToDecimal(ApiUrlCall.NumberToDecimal(qty.ToString(), CurrentUser.CompanyDecPlaces)).ToString();
        }

        private void CreatePDFDetailed(DnSettings s, JObject sageOrder)
        {
            string fontPath = Server.MapPath("~/fonts/Roboto-Regular.ttf");
            var regfont = FontFactory.GetFont(fontPath, 10, BaseColor.BLACK);
            var boldfont = FontFactory.GetFont(fontPath, 10, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            var smallfont = FontFactory.GetFont(fontPath, 8, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(fontPath, 12, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            var cofont = FontFactory.GetFont(fontPath, 13, BaseColor.BLACK);

            Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DH = _db.DocHeaders.Where(x => x.DocGUID == DocGuid).FirstOrDefault();
                if (DH == null) return;
                long coId = CurrentUser.CoID;
                long docId = DH.DocID;

                // ── What was ordered, and what each delivery took ────────────────────────
                var orderLines = _db.DocLines
                    .Where(x => x.DocID == docId && x.SBCALineID != 0 && x.ItemCode != null)
                    .OrderBy(x => x.LineID).ToList();

                var slips = _db.PickingSlipMasters
                    .Where(x => x.CustomerID == coId && x.LinkedSOrdID == docId)
                    .OrderBy(x => x.PSID).ToList();

                // The note is for the latest completed slip that has something picked.
                PickingSlipMaster noteSlip = null;
                int seq = 0;
                var picksBySlip = new Dictionary<int, List<PickSlipLine>>();
                for (int i = 0; i < slips.Count; i++)
                {
                    if (slips[i].PSComplete != true) continue;
                    long sid = slips[i].PSID;
                    var picked = _db.PickSlipLines.Where(x => x.PSID == sid && x.PickComplete == true)
                                    .OrderBy(x => x.LineID).ToList()
                                    .Where(x => (x.PickQty ?? x.Quantity ?? 0) > 0).ToList();
                    if (picked.Count == 0) continue;
                    picksBySlip[slips[i].PSID] = picked;
                    noteSlip = slips[i];
                    seq = i + 1;
                }

                var deliveredToDate = new Dictionary<long, decimal>();   // by item, up to and including this note
                var onThisNote = new Dictionary<long, decimal>();
                if (noteSlip != null)
                {
                    foreach (var kv in picksBySlip)
                    {
                        foreach (var p in kv.Value)
                        {
                            decimal q = p.PickQty ?? p.Quantity ?? 0;
                            deliveredToDate[p.SelectionId] = (deliveredToDate.ContainsKey(p.SelectionId) ? deliveredToDate[p.SelectionId] : 0) + q;
                            if (kv.Key == noteSlip.PSID)
                                onThisNote[p.SelectionId] = (onThisNote.ContainsKey(p.SelectionId) ? onThisNote[p.SelectionId] : 0) + q;
                        }
                    }
                }
                else
                {
                    // No picking slip (job card route, or nothing picked yet): the order lines' own figures.
                    foreach (var l in orderLines)
                    {
                        decimal q = l.ReceiveQty ?? 0;
                        deliveredToDate[l.SelectionId] = (deliveredToDate.ContainsKey(l.SelectionId) ? deliveredToDate[l.SelectionId] : 0) + q;
                        onThisNote[l.SelectionId] = (onThisNote.ContainsKey(l.SelectionId) ? onThisNote[l.SelectionId] : 0) + q;
                    }
                }

                DateTime noteDate = (noteSlip != null && noteSlip.PSCompleteDate != null) ? noteSlip.PSCompleteDate.Value : DateTime.Today;
                string noteNumber = DnNumber(_db, s, DH, noteSlip != null ? (int?)noteSlip.PSID : null, seq, slips.Count);

                // ── Document ─────────────────────────────────────────────────────────────
                iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
                string folder = Server.MapPath("~\\PDFs\\" + CurrentUser.UserGuiD.ToString());
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string filepath = folder + "\\DN_" + lblDocNum.Text + ".PDF";
                // Built in memory and written to disk only once it is complete. If anything fails
                // part-way, no file is left open or half-written, so the standard note (the
                // caller's fallback) can still be written to the same path.
                MemoryStream pdfBytes = new MemoryStream();
                PdfWriter writer = PdfWriter.GetInstance(doc, pdfBytes);
                writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_7);
                writer.SetFullCompression();
                doc.Open();
                float usable = doc.PageSize.Width - 80;

                // Logo, top right
                PdfPTable logo = new PdfPTable(1);
                logo.TotalWidth = usable; logo.LockedWidth = true;
                PdfPCell lc;
                string imgpath = Server.MapPath("~/images/CoImages/" + CurrentUser.CoID + ".png");
                if (File.Exists(imgpath))
                {
                    iTextSharp.text.Image gif = iTextSharp.text.Image.GetInstance(imgpath);
                    gif.ScaleToFit(170.0F, 65.0F);
                    lc = new PdfPCell(gif);
                }
                else lc = new PdfPCell(new Phrase(""));
                lc.Border = 0; lc.HorizontalAlignment = Element.ALIGN_RIGHT; lc.FixedHeight = 70f;
                logo.AddCell(lc);
                doc.Add(logo);

                // Customer (left) | this company (right)
                JToken cust = sageOrder != null ? sageOrder["Customer"] : null;
                string custVat = sageOrder != null ? DnText(sageOrder["TaxReference"]) : "";
                if (custVat == "" && cust != null && cust.Type == JTokenType.Object) custVat = DnText(cust["TaxReference"]);
                string custContact = "", custPhone = "";
                if (cust != null && cust.Type == JTokenType.Object)
                {
                    custContact = DnText(cust["ContactName"]);
                    custPhone = DnText(cust["Telephone"]);
                    if (custPhone == "") custPhone = DnText(cust["Mobile"]);
                }

                var left = new List<Phrase>();
                left.Add(new Phrase("CUSTOMER", boldfont));
                left.Add(new Phrase(DH.CustSupName ?? "", regfont));
                foreach (string a in new[] { DH.DelAddress1, DH.DelAddress2, DH.DelAddress3, DH.DelAddress4, DH.DelAddress5 })
                    if (!string.IsNullOrWhiteSpace(a)) left.Add(new Phrase(a.Trim(), regfont));
                if (custVat != "") left.Add(new Phrase("Vat No: " + custVat, regfont));
                if (custContact != "") left.Add(new Phrase("Contact Person: " + custContact, regfont));
                if (custPhone != "") left.Add(new Phrase("Contact Number: " + custPhone, regfont));
                left.Add(new Phrase("Purchase Order: " + (DH.Reference ?? ""), regfont));
                left.Add(new Phrase("Sales Order No: " + (DH.DocumentNumber ?? ""), regfont));

                var right = new List<Phrase>();
                right.Add(new Phrase("DELIVERY NOTE", headfont));
                right.Add(new Phrase(s.CompanyName ?? "", cofont));
                right.Add(new Phrase(" ", regfont));
                if (!string.IsNullOrWhiteSpace(s.CoRegNo)) right.Add(new Phrase("Reg No: " + s.CoRegNo.Trim(), regfont));
                if (!string.IsNullOrWhiteSpace(s.CoVatNo)) right.Add(new Phrase("VAT No: " + s.CoVatNo.Trim(), regfont));
                right.Add(new Phrase(" ", regfont));
                right.Add(new Phrase(noteDate.ToString("dd MMMM yyyy"), regfont));

                PdfPTable head = new PdfPTable(2);
                head.TotalWidth = usable; head.LockedWidth = true;
                head.SetWidths(new int[] { 55, 45 });
                int rows = Math.Max(left.Count, right.Count);
                for (int i = 0; i < rows; i++)
                {
                    PdfPCell c1 = new PdfPCell(i < left.Count ? left[i] : new Phrase(" ", regfont));
                    c1.Border = 0; c1.PaddingBottom = 5f;
                    head.AddCell(c1);
                    PdfPCell c2 = new PdfPCell(i < right.Count ? right[i] : new Phrase(" ", regfont));
                    c2.Border = 0; c2.PaddingBottom = 5f;
                    head.AddCell(c2);
                }
                head.SpacingAfter = 14f;
                doc.Add(head);

                // Lines
                PdfPTable grid = new PdfPTable(6);
                grid.TotalWidth = usable; grid.LockedWidth = true;
                grid.SetWidths(new int[] { 34, 12, 12, 14, 13, 15 });
                foreach (string h in new[] { "ITEM DESCRIPTION", "ORDER QTY", "INV QTY", "BACK ORDER", "DATE", "POD NO" })
                {
                    PdfPCell hc = new PdfPCell(new Phrase(h, boldfont));
                    hc.HorizontalAlignment = Element.ALIGN_CENTER; hc.VerticalAlignment = Element.ALIGN_MIDDLE;
                    hc.FixedHeight = 22f;
                    grid.AddCell(hc);
                }

                // No picking slip (job card route, or nothing picked yet): print the order's OWN
                // lines, one row each. That includes lines added on the job card (misc, transport),
                // which have no Sage line id and are therefore not in orderLines - on a slip order
                // a line without a Sage id is a split part, on a job card order it is a real line.
                if (noteSlip == null)
                {
                    var ownLines = _db.DocLines.Where(x => x.DocID == docId && x.ItemCode != null).OrderBy(x => x.LineID).ToList();
                    // Job card with "Print all additional lines" unticked: the lines added on the job
                    // card are internal and left off; the first printed line shows the Job Card Summary.
                    JobCardHide.JobInfo dnJob = JobCardHide.ForOrder(_db, coId, docId);
                    bool dnHide = dnJob != null && !dnJob.PrintAllLines && ownLines.Any(x => x.SBCALineID != 0);
                    bool dnFirstPrinted = true;
                    foreach (var ol in ownLines)
                    {
                        if (dnHide && ol.SBCALineID == 0) continue;
                        string olDescription = ol.ItemDescription ?? ol.ItemCode ?? "";
                        if (dnHide && dnFirstPrinted && !string.IsNullOrWhiteSpace(dnJob.JCSummary)) olDescription = dnJob.JCSummary.Trim();
                        dnFirstPrinted = false;
                        decimal olOrdered = ol.Quantity ?? 0;
                        decimal olInv = ol.ReceiveQty ?? 0;
                        decimal olBack = olOrdered - olInv;
                        if (olBack < 0) olBack = 0;
                        string[] olVals =
                        {
                            olDescription,
                            DnQty(olOrdered), DnQty(olInv), DnQty(olBack),
                            olInv > 0 ? noteDate.ToString("dd/MM/yy") : "",
                            olInv > 0 ? noteNumber : ""
                        };
                        for (int c = 0; c < olVals.Length; c++)
                        {
                            PdfPCell vc = new PdfPCell(new Phrase(olVals[c], c == 0 || c == 5 ? boldfont : regfont));
                            vc.HorizontalAlignment = Element.ALIGN_CENTER; vc.VerticalAlignment = Element.ALIGN_MIDDLE;
                            vc.MinimumHeight = 22f; vc.PaddingBottom = 4f;
                            grid.AddCell(vc);
                        }
                    }
                    orderLines.Clear();   // rows are printed; nothing left for the per-item loop below
                }

                // Picking slip order: one row per item, in the order the items appear on the order.
                var itemIds = new List<long>();
                foreach (var l in orderLines) if (!itemIds.Contains(l.SelectionId)) itemIds.Add(l.SelectionId);
                foreach (long itemId in itemIds)
                {
                    var first = orderLines.First(x => x.SelectionId == itemId);
                    decimal ordered = orderLines.Where(x => x.SelectionId == itemId).Sum(x => x.Quantity ?? 0);
                    decimal inv = onThisNote.ContainsKey(itemId) ? onThisNote[itemId] : 0;
                    decimal delivered = deliveredToDate.ContainsKey(itemId) ? deliveredToDate[itemId] : 0;
                    decimal back = ordered - delivered;
                    if (back < 0) back = 0;

                    string[] vals =
                    {
                        first.ItemDescription ?? first.ItemCode ?? "",
                        DnQty(ordered), DnQty(inv), DnQty(back),
                        inv > 0 ? noteDate.ToString("dd/MM/yy") : "",
                        inv > 0 ? noteNumber : ""
                    };
                    for (int c = 0; c < vals.Length; c++)
                    {
                        PdfPCell vc = new PdfPCell(new Phrase(vals[c], c == 0 || c == 5 ? boldfont : regfont));
                        vc.HorizontalAlignment = Element.ALIGN_CENTER; vc.VerticalAlignment = Element.ALIGN_MIDDLE;
                        vc.MinimumHeight = 22f; vc.PaddingBottom = 4f;
                        grid.AddCell(vc);
                    }

                    // Lots / serials delivered on this note - only when there is something to say.
                    if (noteSlip != null && picksBySlip.ContainsKey(noteSlip.PSID))
                    {
                        var parts = new List<string>();
                        foreach (var p in picksBySlip[noteSlip.PSID].Where(x => x.SelectionId == itemId))
                        {
                            var serials = SerialPicking.GetLineSerials(_db, coId, p.PSID, p.LineID);
                            if (serials.Count > 0) { parts.Add(SerialPicking.SerialNote(serials)); continue; }
                            string lot = (p.LotNumber ?? "").Trim();
                            if (lot != "" && !lot.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                                parts.Add("Lot " + lot + " x " + DnQty(p.PickQty ?? p.Quantity ?? 0));
                        }
                        if (parts.Count > 0)
                        {
                            PdfPCell sub = new PdfPCell(new Phrase(string.Join("    ", parts), smallfont));
                            sub.Colspan = 6; sub.PaddingLeft = 6f; sub.PaddingBottom = 4f;
                            grid.AddCell(sub);
                        }
                    }
                }
                grid.SpacingAfter = 10f;
                doc.Add(grid);

                // Returnables (pallets, crates, cylinders) this customer is still holding, this
                // delivery included. Stock Control -> Returnables. Nothing prints when there are none.
                if (DH.CustSuppID != null)
                {
                    var held = Returnables.TryBalances(_db, coId, DH.CustSuppID).Where(x => x.Outstanding > 0).ToList();
                    if (held.Count > 0)
                    {
                        PdfPTable rt = new PdfPTable(1);
                        rt.TotalWidth = usable; rt.LockedWidth = true;
                        foreach (var heldRow in held)
                        {
                            PdfPCell rc = new PdfPCell(new Phrase(
                                (heldRow.ItemDescription ?? heldRow.ItemCode ?? "Returnable") + " still to be returned: " + DnQty(heldRow.Outstanding), boldfont));
                            rc.Border = 0; rc.PaddingBottom = 3f;
                            rt.AddCell(rc);
                        }
                        rt.SpacingAfter = 8f;
                        doc.Add(rt);
                    }
                }

                // Sign-off
                string dots = "..................................................";
                PdfPTable sign = new PdfPTable(2);
                sign.TotalWidth = usable; sign.LockedWidth = true;
                sign.SetWidths(new int[] { 60, 40 });
                string[] signLeft = { "Name", "Surname", "Signature", "Registration" };
                for (int i = 0; i < signLeft.Length; i++)
                {
                    PdfPCell a = new PdfPCell(new Phrase(signLeft[i] + dots, regfont));
                    a.Border = 0; a.FixedHeight = 34f; a.VerticalAlignment = Element.ALIGN_BOTTOM;
                    sign.AddCell(a);
                    PdfPCell b = new PdfPCell(new Phrase(i == 0 ? "Date" + dots.Substring(0, 34) : " ", regfont));
                    b.Border = 0; b.FixedHeight = 34f; b.VerticalAlignment = Element.ALIGN_BOTTOM;
                    sign.AddCell(b);
                }
                doc.Add(sign);

                // Company footer, at the foot of the page
                if (!string.IsNullOrWhiteSpace(s.DocFooter))
                {
                    string[] foot = s.DocFooter.Replace("\r", "").Split('\n').Where(x => x.Trim() != "").ToArray();
                    PdfContentByte cb = writer.DirectContent;
                    float y = 40f + (foot.Length - 1) * 13f;
                    foreach (string line in foot)
                    {
                        ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT, new Phrase(line.Trim(), smallfont), 40f, y, 0);
                        y -= 13f;
                    }
                }

                doc.AddTitle("Delivery Note: " + noteNumber);
                doc.AddAuthor("Data Fusion");
                doc.Close();
                File.WriteAllBytes(filepath, pdfBytes.ToArray());
            }
        }
    }
}
