using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SBMS
{
    public partial class Dashboard : BasePage
    {
       UserDetails userDets;
        Guid userGuid;
        protected void Page_Load(object sender, EventArgs e)
        {
            userDets = Session["UserDetails"] as UserDetails;
            SessionValidator.ValidateUserSession(userDets);
            if (userDets == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblUserName.Text = $":.. { userDets.UserName} ..:";
            string expUrl = Request.QueryString["exp"];
            if (expUrl != null) if (expUrl.ToString() == "true") AlertHelper.ShowSweetAlert(this, "Your subscription has expired, please contact support.", "error", "Subscription Expired");

            if (!IsPostBack)
            {
                string imgname = userDets.CoID + ".png";
                string imgPath = $"~/images/CoImages/{imgname}";
                if (File.Exists(Server.MapPath(imgPath)))
                {
                    imgCoImg.ImageUrl = ResolveUrl(imgPath);
                }
                else
                {
                    imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                }
                
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                   lblCoName.Text = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == userDets.CoID).CompanyName.ToString();
                }
                showhidebuttons();
                if (userDets.ExpiryDate < DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                if (userDets.ExpiryDate <= DateTime.Now.AddDays(60))
                 {
                   
                    lblWarn.Text = $"Your subscription expires in {(userDets.ExpiryDate - DateTime.Now).Days} days. Please contact support to renew";
                    lblWarn.Style.Add("display", "inline-block");
                    return;
                }
            }
        }

        private void showhidebuttons()
        {
            if (userDets.UATMode == false) lbluat.Style.Add("display", "none");
            if (userDets.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (userDets.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (userDets.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (userDets.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (userDets.UsePickSlipTracking != true)
            {
                ibtnPickTrack.Style.Add("display", "none");
                DDProgBoard.Style.Add("display", "none");
            }

            if (userDets.UseModule2 == true)
            {
                ibtnmrp2.Style.Add("display", "none");
                if (userDets.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (userDets.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (userDets.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                DDProgBoard.Items.Remove("Job Cards");
                PnlForecast.Style.Add("display", "none");
             }
            if (userDets.UseModule3 == true)
            {
                if (userDets.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (userDets.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (userDets.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
                DDProgBoard.Items.Remove("Production");
            }
            else
            {
                DDProgBoard.Items.Remove("Production");
                PnlProduction.Style.Add("display", "none");
            }
        }

        /// <summary>
        /// Mini dashboard (KPI tiles + charts). Added for the UI refresh: READ-ONLY
        /// aggregate queries — no writes, no Sage API calls, and no existing page
        /// behaviour is altered. Hooked in via OnLoadComplete so Page_Load is untouched.
        /// </summary>
        protected override void OnLoadComplete(EventArgs e)
        {
            base.OnLoadComplete(e);

            if (IsPostBack || userDets == null) return;
            if (userDets.ExpiryDate <= DateTime.Now) return;

            LoadDashboardMetrics();
        }

        private void LoadDashboardMetrics()
        {
            // Tile visibility mirrors the module/permission flags already used by showhidebuttons().
            bool showSales = userDets.CanViewPickSlips == true;
            bool showWorksOrders = userDets.UseModule3 == true;

            if (!showSales)
            {
                lnkKpiSO.Style.Add("display", "none");
                divCharts.Style.Add("display", "none");
            }
            if (!showWorksOrders) lnkKpiWO.Style.Add("display", "none");

            long coId = userDets.CoID;
            // 12 whole months, starting at the first of the month 11 months back.
            DateTime fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);

            List<string> monthLabels = new List<string>();
            List<decimal> salesSeries = new List<decimal>();
            List<decimal> gpSeries = new List<decimal>();
            List<string> custLabels = new List<string>();
            List<decimal> custSeries = new List<decimal>();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // "Open" definitions deliberately match each screen's own filter so the
                // tile totals agree with what the user sees when they click through:
                //   POs -> OSPurchaseOrders.aspx   SOs -> OSSalesOrders.aspx
                lblKpiPO.Text = _db.DocHeaders
                    .Count(x => x.CompanyID == coId && x.DocType == 1
                             && x.Complete == false && x.Status != "Cancelled")
                    .ToString();

                if (showSales)
                {
                    lblKpiSO.Text = _db.DocHeaders
                        .Count(x => x.CompanyID == coId && x.DocType == 5 && x.Active == true)
                        .ToString();
                }

                if (showWorksOrders)
                {
                    lblKpiWO.Text = _db.WorksOrderHeaders
                        .Count(x => x.CompanyID == coId && x.Active == true)
                        .ToString();
                }

                if (showSales)
                {
                    var monthly = _db.DocHeaders
                        .Where(x => x.CompanyID == coId && x.DocType == 5 && x.DocDate >= fromDate)
                        .GroupBy(x => new { Year = x.DocDate.Value.Year, Month = x.DocDate.Value.Month })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            Sales = g.Sum(x => x.Total),
                            GP = g.Sum(x => x.DocGP)
                        })
                        .ToList();

                    for (int i = 0; i < 12; i++)
                    {
                        DateTime m = fromDate.AddMonths(i);
                        var hit = monthly.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                        monthLabels.Add(m.ToString("MMM yy"));
                        salesSeries.Add(hit == null ? 0m : decimal.Round(hit.Sales ?? 0m, 2));
                        gpSeries.Add(hit == null ? 0m : decimal.Round(hit.GP ?? 0m, 2));
                    }

                    var topCust = _db.DocHeaders
                        .Where(x => x.CompanyID == coId && x.DocType == 5
                                 && x.DocDate >= fromDate && x.CustSupName != null)
                        .GroupBy(x => x.CustSupName)
                        .Select(g => new { Name = g.Key, Sales = g.Sum(x => x.Total) })
                        .OrderByDescending(x => x.Sales)
                        .Take(5)
                        .ToList();

                    foreach (var c in topCust)
                    {
                        custLabels.Add(c.Name);
                        custSeries.Add(decimal.Round(c.Sales ?? 0m, 2));
                    }
                }
            }

            string json = JsonConvert.SerializeObject(new
            {
                months = monthLabels,
                sales = salesSeries,
                gp = gpSeries,
                custLabels = custLabels,
                custSales = custSeries
            });

            ClientScript.RegisterStartupScript(GetType(), "dfChartData",
                "window.dfChartData = " + json + ";", true);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(userGuid);
            Session.Clear();
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void DDProgBoard_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDProgBoard.SelectedIndex == 1)
            {
                Response.Redirect("~/PickingSlipTrackStatic.aspx", true);
            }
            else
                if (DDProgBoard.SelectedIndex == 2)
                {
                    Response.Redirect("~/JobTrackingStatic.aspx", true);
                }
             else
                if (DDProgBoard.SelectedIndex == 3)
                {
                    Response.Redirect("~/ProductionTrackingStatic.aspx", true);
                }
        }

        protected async void LinkButton1_Click(object sender, EventArgs e)
        {
            UserDetails userDets = Session["UserDetails"] as UserDetails;
            //string requestUrl = $"https://resellers.accounting.sageone.co.za/api/2.0.0/PurchaseOrderAttachment/Download/544dd577-d6dd-406b-ac18-38befa54468e?apikey=934D4C3F-FF4D-4311-9380-F21ACB54DCBB&CompanyID=15240";
            string requestUrl = $"https://resellers.accounting.sageone.co.za/api/2.0.0/PurchaseOrderAttachment/Download/e8147ffa-da5b-4652-9e32-8bb3222de0da?apikey=934D4C3F-FF4D-4311-9380-F21ACB54DCBB&CompanyID=15240";
            // JObject parsedJSON = ApiUrlCall.ApiCall(requestUrl, userDets);
            //string requestUrl = "https://example.com/api/download-pdf";
            ApiUrlCall Api = new ApiUrlCall();
            byte[] pdfContent = await Api.DownloadPdfAsync(requestUrl, userDets);

            if (pdfContent != null)
            {
                // Serve the PDF to the user
                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "attachment;filename=document.pdf");
                Response.BinaryWrite(pdfContent);
                Response.End();
            }
            else
            {
                // Handle error, e.g., display a message to the user
                Response.Write("Failed to download the PDF.");
            }

        }

        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/OSSalesOrders.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/StockControl.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp2_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/FGDemands.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/JobTracking.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                if (userDets.ExpiryDate <= DateTime.Now)
                {
                    AlertHelper.ShowSweetAlert(this, "Your subscription has expired, unable to continue. Please contact support for renewal", "error", "Login Error");
                    return;
                }
                Response.Redirect("~/ProductionRMD.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        // 

    }
}
