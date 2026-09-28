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
    public partial class LabelPrint : BasePage
    {
        long itmid;
        const float MM = 72f / 25.4f;   // points per millimetre

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            long.TryParse(Request.QueryString["itm"], out itmid);
            lbtnBack.Visible = itmid > 0;

            string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
            imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");

            if (!IsPostBack) BindValues();
        }

        // The item's own barcodes (ItemBarCodeLink) first, then its item code.
        private void BindValues()
        {
            if (itmid <= 0) { ddValue.Visible = false; return; }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string code = _db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid)
                    .Select(x => x.Code).FirstOrDefault() ?? "";
                var barcodes = _db.ItemBarCodeLinks
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmid && x.BarCode != null && x.BarCode != "")
                    .Select(x => x.BarCode).ToList();

                lblItemCode.Text = code;
                foreach (var b in barcodes.Distinct()) ddValue.Items.Add(b);
                if (code != "" && !barcodes.Contains(code)) ddValue.Items.Add(code);
            }
        }

        protected void lbtnPrint_Click(object sender, EventArgs e)
        {
            string value = txtCustom.Text.Trim();
            if (value == "" && ddValue.Visible && ddValue.SelectedItem != null) value = ddValue.SelectedValue;

            int qty, across;
            decimal w, h, gap;
            if (value == "") { lblMsg.Text = "Nothing to print - choose a barcode or type a value."; return; }
            if (value.Any(c => c > 127)) { lblMsg.Text = "Barcodes can only contain plain keyboard characters."; return; }
            if (!int.TryParse(txtQty.Text, out qty) || qty < 1 || qty > 1000) { lblMsg.Text = "Number of labels must be 1 to 1000."; return; }
            if (!decimal.TryParse(txtWidth.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out w) || w < 10 || w > 300) { lblMsg.Text = "Label width must be 10 to 300 mm."; return; }
            if (!decimal.TryParse(txtHeight.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out h) || h < 5 || h > 300) { lblMsg.Text = "Label height must be 5 to 300 mm."; return; }
            if (!decimal.TryParse(txtGap.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out gap) || gap < 0 || gap > 50) { lblMsg.Text = "Gap must be 0 to 50 mm."; return; }
            if (!int.TryParse(ddAcross.SelectedValue, out across) || across < 1 || across > 3) across = 1;

            byte[] pdf = BuildLabels(value, qty, (float)w * MM, (float)h * MM, (float)gap * MM, across, chkShowText.Checked);
            if (pdf == null) { lblMsg.Text = "The label is too small for this barcode - make it taller or turn the numbers off."; return; }

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("Content-Disposition", "inline; filename=Labels.pdf");
            Response.BinaryWrite(pdf);
            Response.Flush();
            Response.SuppressContent = true;
            Context.ApplicationInstance.CompleteRequest();
        }

        // One PDF page = one row across the roll, so the printer driver's paper size
        // is (across x width + gaps) by height. Code 128, bars stretched to fill the label.
        private byte[] BuildLabels(string value, int qty, float w, float h, float gap, int across, bool showText)
        {
            float pad = 2f * MM;                  // quiet zone / edge margin
            float availW = w - 2 * pad, availH = h - 2 * pad;

            var bc = new Barcode128 { Code = value, CodeType = Barcode.CODE128 };
            BaseFont font = bc.Font;
            bc.Font = null; bc.X = 1f; bc.BarHeight = 1f;
            float modules = bc.BarcodeSize.Width;  // bar width at 1pt per module
            bc.X = availW / modules;

            float textH = 0f;
            if (showText)
            {
                bc.Font = font;
                bc.Size = Math.Min(10f, availH * 0.2f);
                bc.Baseline = bc.Size;
                bc.BarHeight = 0f;
                textH = bc.BarcodeSize.Height;
            }
            bc.BarHeight = availH - textH;
            if (bc.BarHeight < 3f * MM) return null;

            using (var ms = new MemoryStream())
            {
                var doc = new iTextSharp.text.Document(new Rectangle(across * w + (across - 1) * gap, h), 0, 0, 0, 0);
                var writer = PdfWriter.GetInstance(doc, ms);
                doc.Open();
                PdfContentByte cb = writer.DirectContent;
                PdfTemplate tpl = bc.CreateTemplateWithBarcode(cb, BaseColor.BLACK, BaseColor.BLACK);
                float s = Math.Min(1f, Math.Min(availW / tpl.Width, availH / tpl.Height));

                for (int i = 0; i < qty; i++)
                {
                    int col = i % across;
                    if (col == 0 && i > 0) doc.NewPage();
                    float x = col * (w + gap) + (w - tpl.Width * s) / 2;
                    float y = (h - tpl.Height * s) / 2;
                    cb.AddTemplate(tpl, s, 0, 0, s, x, y);
                }
                doc.Close();
                return ms.ToArray();
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/ItemEdit.aspx?itm=" + itmid, false);
        }
    }
}
