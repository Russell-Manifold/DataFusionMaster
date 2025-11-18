using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SBMS
{
    public partial class WorksOrderPDFCreate : BasePage
    {
        long woid = 0;
        string wonum;
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            UserDetails userDetails = CurrentUser;
            if (CurrentUser == null) Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
            woid = Convert.ToInt64(Request.QueryString["woid"].ToString());
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                wonum = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == woid).WONum.ToString();
            }
            if (!IsPostBack)
            {
                CreatePDF();
                Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\WO_" + wonum, false);
            }
        }
        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 9, BaseColor.BLACK);
            //var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
            //docid = Convert.ToInt64(lblPSid.Text);
            //Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WO = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == woid).FirstOrDefault();
                //var DH = _db.DocHeaders.Where(x => x.DocGUID == DocGuid).FirstOrDefault();
                if (WO != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\WO_" + wonum + ".PDF");
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(filepath, FileMode.Create));
                        writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_7);
                        writer.SetFullCompression();
                        writer.PageEvent = new PDFFooter();
                    }
                    catch { }
                    doc.Open();
                    doc.SetMargins(28f, 28f, 100f, 80f);

                    #region Headerinfo
                    PdfPTable table = new PdfPTable(3);
                    PdfPCell cell;
                    table.SetWidths(new int[] { 150, 285, 150 });
                    table.TotalWidth = doc.PageSize.Width - 80;
                    table.LockedWidth = true;
                    iTextSharp.text.Image gif;
                    string imgpath = Server.MapPath("~/images/CoImages/" + CurrentUser.CoID + ".png");
                    if (File.Exists(imgpath))
                    {
                        gif = iTextSharp.text.Image.GetInstance(imgpath);
                        gif.ScaleToFit(170.0F, 65.0F);
                    }
                    else
                    {
                        gif = null;
                    }

                    try
                    {
                        cell = new PdfPCell(gif);
                    }
                    catch
                    {
                        cell = new PdfPCell(new Phrase(""));
                    }
                    cell.Border = 0;
                    cell.HorizontalAlignment = 0;
                    cell.Rowspan = 5;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Works Order #", headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(WO.WONum.ToString(), headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Customer:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((WO.CustSupName ?? "").ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Reference:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(WO.Reference.ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Due Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(WO.DueDate).ToString("dd MMM yyyy"), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Linked Document:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(WO.LinkedDocumentNum ?? "" , medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    doc.Add(table);
                    #endregion

                    #region messages
                    PdfPTable tableM = new PdfPTable(3);
                    PdfPCell cellM;
                    tableM.SpacingBefore = 15f;
                    tableM.TotalWidth = doc.PageSize.Width - 80;
                    tableM.LockedWidth = true;

                    cellM = new PdfPCell(new Phrase("Message:- " + Environment.NewLine + (WO.Message ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 80f; ;
                    tableM.AddCell(cellM);

                    doc.Add(tableM);
                    #endregion


                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(7);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 230, 40, 30, 70, 40, 40});
                    table4.TotalWidth = doc.PageSize.Width - 80;
                    table4.LockedWidth = true;

                    cell4 = new PdfPCell(new Phrase("Code", regfont));
                    cell4.HorizontalAlignment = 0;
                    cell4.FixedHeight = 20f; ;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Description", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Store", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Lot Number", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Use", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Scrap", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    //cell4 = new PdfPCell(new Phrase("Reject", regfont));
                    //cell4.HorizontalAlignment = 1;
                    //cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    //cell4.BorderColor = new BaseColor(211, 211, 211);
                    //table4.AddCell(cell4);
                    #endregion

                    var WOLines = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.WOID == woid && (x.Quantity !=0 && x.Quantity != null)).OrderBy(x => x.LineID).ToList();
                    foreach (var WoL in WOLines)
                    {
                        string itemCode = WoL.ItemCode ?? string.Empty;
                        string itemDesc = WoL.ItemDescription ?? string.Empty;
                        string lotNumber = WoL.LotNumber ?? string.Empty;
                        decimal quantity = 0;
                        try { quantity = WoL.Quantity != null ? Convert.ToDecimal(WoL.Quantity) : 0; } catch { quantity = 0; }
                        quantity = ApiUrlCall.NumberToDecimal(quantity, CurrentUser?.CompanyDecPlaces ?? 0);
                        int qtyInt = 0;
                        try { qtyInt = (int)quantity; } catch { qtyInt = 0; }
                        cell4 = new PdfPCell(new Phrase(itemCode, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.HorizontalAlignment = 0;
                        cell4.FixedHeight = 22f;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(itemDesc, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.HorizontalAlignment = 0;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(qtyInt.ToString(), regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(string.Empty, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(lotNumber, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(" ", regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.Colspan = 3;
                        cell4.BorderColor = new BaseColor(211, 211, 211);
                        table4.AddCell(cell4);

                        // get RM lines, iterate through and add
                        var WoRMLs = _db.WorksOrderRMLines.Where(x => x.LinkedWOLineID == WoL.LineID).OrderBy(x => x.LineID).ToList();
                        foreach (var rmL in WoRMLs)
                        {
                            string rmItemCode = rmL.ItemCode?.ToString() ?? string.Empty;
                            string rmItemDesc = rmL.ItemDescription?.ToString() ?? string.Empty;
                            string rmStoreCodeFrom = rmL.StoreCodeFrom ?? string.Empty;
                            string rmLotNumber = rmL.LotNumber ?? string.Empty;
                            decimal rmQuantity = 0;
                            try { rmQuantity = !string.IsNullOrEmpty(rmL.Quantity?.ToString()) ? Convert.ToDecimal(rmL.Quantity) : 0; } catch { rmQuantity = 0; }
                            rmQuantity = ApiUrlCall.NumberToDecimal(rmQuantity, CurrentUser?.CompanyDecPlaces ?? 0);
                            decimal useQty = 0;
                            try { useQty = rmL.UseQty != null ? ApiUrlCall.NumberToDecimal(Convert.ToDecimal(rmL.UseQty), CurrentUser?.CompanyDecPlaces ?? 0) : 0; } catch { useQty = 0; }
                            decimal scrapQty = 0;
                            try { scrapQty = rmL.ScrapQty != null ? ApiUrlCall.NumberToDecimal(Convert.ToDecimal(rmL.ScrapQty), CurrentUser?.CompanyDecPlaces ?? 0) : 0; } catch { scrapQty = 0; }
                            cell4 = new PdfPCell(new Phrase(" ", regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.FixedHeight = 22f;
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(rmItemCode + ":- " + rmItemDesc, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 0;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(rmQuantity.ToString(), regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 1;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(rmStoreCodeFrom, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 1;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(rmLotNumber, regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 0;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(useQty != 0 ? useQty.ToString() : " ", regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 1;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase(scrapQty != 0 ? scrapQty.ToString() : " ", regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                            cell4.HorizontalAlignment = 1;
                            cell4.BorderColor = new BaseColor(211, 211, 211);
                            cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                            table4.AddCell(cell4);
                        }
                        cell4 = new PdfPCell(new Phrase(" ", regfont ?? FontFactory.GetFont(FontFactory.HELVETICA, 9)));
                        cell4.Colspan = 8;
                        cell4.Border = 0;
                        table4.AddCell(cell4);
                    }
                    doc.Add(table4);
                    doc.AddTitle($"Works Order: {wonum}");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();
                }
            }
        }
    }
}
