using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using System.Linq;

namespace SBMS
{
    public partial class StockCountUpload : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
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

            if (!IsPostBack)
            {
                showhidebuttons();
            }
        }

        protected void btnProcess_Click(object sender, EventArgs e)
        {
            lblerr.Text = "";

            if (!fileUpload.HasFile)
            {
                lblerr.Text = "Please select a file before uploading.";
                return;
            }

            string ext = Path.GetExtension(fileUpload.FileName).ToLower();
            if (ext != ".xlsx")
            {
                lblerr.Text = "Please upload an .xlsx file only.";
                return;
            }

            ProcessUpload();
        }

        private void ProcessUpload()
        {
            try
            {
                using (var stream = new MemoryStream(fileUpload.FileBytes))
                using (var wb = new XLWorkbook(stream))
                {
                    // ── Read CountID from Instructions cover page ─────────────
                    var coverSheet = wb.Worksheets.FirstOrDefault(w =>
                        w.Name.Trim().Equals("Instructions",
                        StringComparison.OrdinalIgnoreCase));

                    if (coverSheet == null)
                    {
                        lblerr.Text = "Could not find the Instructions tab. " +
                                      "Please ensure you are uploading a Data Fusion generated count sheet.";
                        return;
                    }

                    var countCell = coverSheet.Cell("C5").GetValue<string>();

                    if (!int.TryParse(countCell, out int countID))
                    {
                        lblerr.Text = "Could not read Count ID from the workbook (expected in cell C5 " +
                                      "of the Instructions tab). " +
                                      "Please ensure you are uploading a Data Fusion generated count sheet.";
                        return;
                    }

                    int updatedLines = 0;
                    int skippedLines = 0;
                    int unmatchedLines = 0;
                    var warnings = new List<string>();

                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var countExists = _db.StockCountMasters.Any(c =>
                            c.StCntID == countID &&
                            c.CompanyID == CurrentUser.CoID &&
                            (c.Complete == false || c.Complete == null));

                        if (!countExists)
                        {
                            lblerr.Text = $"An active Count ID {countID} was not found for your company. " +
                                           "Please check you are uploading the correct file.";
                            return;
                        }

                        foreach (var ws in wb.Worksheets)
                        {
                            // ── Skip non-data tabs ────────────────────────────
                            string tabName = ws.Name.Trim();

                            if (tabName.Equals("Instructions", StringComparison.OrdinalIgnoreCase) ||
                                tabName.Equals("Collated", StringComparison.OrdinalIgnoreCase))
                                continue;

                            string storeCode = tabName;
                            int headerRow = 7;
                            int codeCol = 2;
                            int count1Col = 4;
                            int count2Col = 5;
                            int finalCol = 6;
                            int lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;

                            for (int row = headerRow + 1; row <= lastRow; row++)
                            {
                                string itemCode = ws.Cell(row, codeCol)
                                                   .GetValue<string>().Trim();

                                if (string.IsNullOrWhiteSpace(itemCode)) continue;

                                decimal? count1 = null;
                                decimal? count2 = null;
                                decimal? finalQty = null;

                                string c1Val = ws.Cell(row, count1Col).GetValue<string>().Trim();
                                string c2Val = ws.Cell(row, count2Col).GetValue<string>().Trim();
                                string fVal = ws.Cell(row, finalCol).GetValue<string>().Trim();

                                if (!string.IsNullOrWhiteSpace(c1Val) &&
                                    decimal.TryParse(c1Val, out decimal c1)) count1 = c1;

                                if (!string.IsNullOrWhiteSpace(c2Val) &&
                                    decimal.TryParse(c2Val, out decimal c2)) count2 = c2;

                                if (!string.IsNullOrWhiteSpace(fVal) &&
                                    decimal.TryParse(fVal, out decimal fq)) finalQty = fq;

                                // ── FinalQty logic ────────────────────────────
                                // If not manually set: Count2 wins if both entered,
                                // otherwise whichever was entered
                                if (!finalQty.HasValue)
                                {
                                    if (count2.HasValue) finalQty = count2;
                                    else if (count1.HasValue) finalQty = count1;
                                }

                                // Skip if nothing was entered at all
                                if (count1 == null && count2 == null && finalQty == null)
                                {
                                    skippedLines++;
                                    continue;
                                }

                                var line = _db.StockCountLines.FirstOrDefault(l =>
                                    l.CompanyID == CurrentUser.CoID &&
                                    l.CountID == countID &&
                                    l.StoreCode == storeCode &&
                                    l.ItemCode == itemCode);

                                if (line == null)
                                {
                                    unmatchedLines++;
                                    warnings.Add($"Row {row} on tab '{storeCode}': " +
                                                 $"Item '{itemCode}' not found — skipped.");
                                    continue;
                                }

                                if (count1.HasValue) line.Count1Qty = count1;
                                if (count2.HasValue) line.Count2Qty = count2;
                                if (finalQty.HasValue) line.FinalQty = finalQty;

                                updatedLines++;
                            }
                        }

                        _db.SaveChanges();
                    }

                    // ── Show results ──────────────────────────────────────────
                    pnlResults.Visible = true;
                    lblCountInfo.Text = $"Count ID: {countID} — {fileUpload.FileName}";

                    string resultHtml =
                        $"<p style='color:green'>&#10003; {updatedLines} line(s) updated successfully.</p>";

                    if (skippedLines > 0)
                        resultHtml +=
                            $"<p style='color:#888'>&#8212; {skippedLines} blank row(s) skipped.</p>";

                    if (unmatchedLines > 0)
                    {
                        resultHtml +=
                            $"<p style='color:orange'>&#9888; {unmatchedLines} row(s) could not be matched.</p>";
                        resultHtml += "<ul style='color:orange; font-size:0.85em'>";
                        foreach (var w in warnings)
                            resultHtml += $"<li>{w}</li>";
                        resultHtml += "</ul>";
                    }

                    lblUploadResult.Text = resultHtml;
                }
            }
            catch (Exception ex)
            {
                lblerr.Text = $"Upload failed: {ex.Message}";
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

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockCounts.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
    }  
}