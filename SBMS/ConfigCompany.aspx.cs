using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;

namespace SBMS
{
    public partial class ConfigCompany : BasePage
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            
            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            if (File.Exists(Server.MapPath(imgPath)))
            {
                imgCoImg.ImageUrl = ResolveUrl(imgPath);
                imgCoImgD.ImageUrl = ResolveUrl(imgPath);
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                imgCoImgD.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            if (!IsPostBack)
            {
                GetCompanyDetails();
                ApiUrlCall api = new ApiUrlCall();
                await api.GetTaxType(CurrentUser);
                if (ApiUrlCall.dbName.ToLower().Contains("demo"))
                {
                    chkMod2.Enabled = true;
                    chkMod3.Enabled = true;
                    chkMobileModule.Enabled = true;
                }
                else
                {
                    chkMod2.Enabled = false;
                    chkMod3.Enabled = false;
                    chkMobileModule.Enabled = false;
                }
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
           if (CurrentUser != null)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>Delivery note settings - columns outside the EF model (SQL/Add_DeliveryNoteLayout.sql).</summary>
        public class DnConfigRow
        {
            public int DNLayout { get; set; }
            public string CoRegNo { get; set; }
            public string CoVatNo { get; set; }
            public string DocFooter { get; set; }
            public string PODPrefix { get; set; }
            public int PODNextNumber { get; set; }
        }

        private void GetCompanyDetails()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
               if (Comp.CompanyName!= null) lblCoName.Text = Comp.CompanyName ?? "";
               if(Comp.SBCACoID != null)   lblCoID.Text = Comp.SBCACoID.ToString();
               txtGenEmail.Text = Comp.CoGenericLoginEmail ?? "";
               // Password is never shown; the box stays blank and blank-on-save means "keep it".
               lblGenPwdSaved.Text = string.IsNullOrEmpty(Comp.CoGenericLoginPwd) ? "" : " (saved)";
               if (Comp.Contact!= null) txtContPerson.Text = Comp.Contact ?? "";
               if (Comp.Contactemail != null) txtContemail.Text = Comp.Contactemail ?? "";
               chkNotifs.Checked = (bool)Comp.SendMessages;
               chkUAT.Checked = (bool)Comp.UATMode;
               chkPickSlip.Checked = (bool)Comp.UsePickSlipTracking;
               chkMobileModule.Checked = Comp.MobileModule;
               // Mobile Picking tab is only shown to companies that have the Mobile Module
               // (same gating idea as Modules 2/3). The toggle itself lives in General > Modules.
               phMobileTab.Visible = Comp.MobileModule;
               phMobileTabBtn.Visible = Comp.MobileModule;

                // LPNPickMode lives outside the EF model (raw SQL, like PickSlipLineLPNs).
                // Defaults to 'off' if the column is not there yet (script not run).
                try
                {
                    string lpnMode = _db.Database.SqlQuery<string>(
                            "SELECT LPNPickMode FROM dbo.CompanyMaster WHERE SBCACoID = @p0",
                            CurrentUser.CoID)
                        .FirstOrDefault() ?? "off";
                    try { DDLPNMode.SelectedValue = lpnMode; } catch { DDLPNMode.SelectedValue = "off"; }
                }
                catch { DDLPNMode.SelectedValue = "off"; }

                // PickByBin lives outside the EF model too (raw SQL). Default off if absent.
                try
                {
                    chkPickByBin.Checked = _db.Database.SqlQuery<bool>(
                            "SELECT PickByBin FROM dbo.CompanyMaster WHERE SBCACoID = @p0",
                            CurrentUser.CoID)
                        .FirstOrDefault();
                }
                catch { chkPickByBin.Checked = false; }

                // Cost of Sales account for job card materials (job cards that keep their added
                // lines internal). Raw SQL too; "not set" when the column is absent.
                DDJobCos.Items.Clear();
                DDJobCos.Items.Add(new System.Web.UI.WebControls.ListItem("- Not set -", "0"));
                long jcCo = CurrentUser.CoID;
                foreach (var acc in _db.AccountsMasters.Where(x => x.CompanyID == jcCo && x.AccountID != null)
                                       .OrderBy(x => x.AccountName).Select(x => new { x.AccountID, x.AccountName }).ToList())
                    DDJobCos.Items.Add(new System.Web.UI.WebControls.ListItem(acc.AccountName ?? acc.AccountID.ToString(), acc.AccountID.ToString()));
                string jcCos = JobCardHide.CosAccountId(_db, jcCo).ToString();
                if (DDJobCos.Items.FindByValue(jcCos) != null) DDJobCos.SelectedValue = jcCos;

                // Delivery note layout - raw SQL too. Standard / blank when the columns are absent.
                try
                {
                    var dn = _db.Database.SqlQuery<DnConfigRow>(
                            "SELECT DNLayout, CoRegNo, CoVatNo, DocFooter, PODPrefix, PODNextNumber FROM dbo.CompanyMaster WHERE SBCACoID = @p0",
                            CurrentUser.CoID)
                        .FirstOrDefault();
                    if (dn != null)
                    {
                        DDDnLayout.SelectedValue = dn.DNLayout == 1 ? "1" : "0";
                        txtCoRegNo.Text = dn.CoRegNo ?? "";
                        txtCoVatNo.Text = dn.CoVatNo ?? "";
                        txtDocFooter.Text = dn.DocFooter ?? "";
                        txtPodPrefix.Text = dn.PODPrefix ?? "";
                        txtPodNext.Text = dn.PODNextNumber.ToString();
                        ViewState["PodNextLoaded"] = dn.PODNextNumber;
                    }
                }
                catch { }
               chkPSAuto.Checked = (bool)Comp.AutoUpdateSageSOs;
               chkTaxInvAuto.Checked = (bool)Comp.AutoGenTaxInvoice;
               chkRecPriceEdit.Checked = Comp.AllowRecPriceEdit;
               chkInvWhenComplete.Checked = Comp.InvoiceWhenSOComplete;
               chkUsePacks.Checked = (bool)Comp.UsePacks;
                chkManfCosts.Checked = (bool)Comp.ShowManfCosts;

               chkLotTrack.Checked = (bool)Comp.UseLotTracking;
                chkSysLot.Checked = Comp.AllowSystemLotNumbers == true;
                chkSerialTrack.Checked = Comp.UseSerialNumbers;
                ApplyLotTrackDependants();
                chkScanCount.Checked = Comp.AllowScannerCount == true;
                chkScanReceive.Checked = Comp.AllowScannerReceive == true;
                chkScanPutAway.Checked = Comp.AllowScannerPutAway == true;
                txtBinSegments.Text = Comp.BinSegments ?? "";
                txtBinDelim.Text = string.IsNullOrEmpty(Comp.BinDelimiter) ? "-" : Comp.BinDelimiter;
                chkAutoManf.Checked = (bool)Comp.UseAutoManf;
                chkAutoManfConf.Checked = false;
                if (chkAutoManf.Checked)
                {
                    chkAutoManfConf.Checked = true;
                }
                DDecPlaces.Text = Comp.ItemQtyDecPlaces.ToString();
                chkMod2.Checked = Convert.ToBoolean(Comp.UseModule2);
                chkMod3.Checked = Convert.ToBoolean(Comp.UseModule3);
                if (Comp.SageWeightField != null)
                {
                    txtSageWght.Text = Comp.SageWeightField.ToString();
                    chkweight.Checked = true;
                }
            }
         }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            bool uselotTrack = false;
            if (chkAutoManfConf.Checked && chkAutoManf.Checked)
            {
                // Auto Manufacture only switches lot tracking off. It used to delete every
                // store except FG as well - Auto Manufacture now draws from whichever store
                // is chosen on the works order, so multiple stores are supported and there
                // is nothing to remove.
                uselotTrack = false;
            } else
            {
                uselotTrack = chkLotTrack.Checked;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
                {
                    Comp.CompanyName = lblCoName.Text;
                    // Generic Sage login (Basic auth) for users flagged "Use Generic Login".
                    // Same TripleDES key/iv as onboarding uses for SBCApwd; Login decrypts it.
                    Comp.CoGenericLoginEmail = txtGenEmail.Text.Trim();
                    if (txtGenPwd.Text.Length > 0)
                        Comp.CoGenericLoginPwd = new cTripleDES(Login.key, Login.iv).Encrypt(txtGenPwd.Text);
                    Comp.Contact = txtContPerson.Text.ToString();
                    Comp.Contactemail = txtContemail.Text.ToString();
                    Comp.SendMessages = chkNotifs.Checked;
                    CurrentUser.SendMessages = Comp.SendMessages;
                    Comp.UATMode = chkUAT.Checked;
                    Comp.UseLotTracking = uselotTrack;
                    // Serials are lots of quantity 1, so the module cannot outlive lot
                    // tracking - switching lots off switches serials off with them.
                    Comp.UseSerialNumbers = uselotTrack && chkSerialTrack.Checked;
                    CurrentUser.CompanyUseSerialNumbers = Comp.UseSerialNumbers;
                    CurrentUser.CompanyUseLotNumbers = uselotTrack;
                    Comp.AllowSystemLotNumbers = chkSysLot.Checked;
                    CurrentUser.CompanyAllowSystemLotNumbers = chkSysLot.Checked;
                    Comp.AllowScannerCount = chkScanCount.Checked;
                    CurrentUser.AllowScannerCount = chkScanCount.Checked;
                    Comp.AllowScannerReceive = chkScanReceive.Checked;
                    CurrentUser.AllowScannerReceive = chkScanReceive.Checked;
                    Comp.AllowScannerPutAway = chkScanPutAway.Checked;
                    CurrentUser.AllowScannerPutAway = chkScanPutAway.Checked;
                    Comp.BinSegments = (txtBinSegments.Text ?? "").Trim();
                    CurrentUser.BinSegments = Comp.BinSegments;
                    Comp.BinDelimiter = string.IsNullOrWhiteSpace(txtBinDelim.Text) ? "-" : txtBinDelim.Text.Trim();
                    CurrentUser.BinDelimiter = Comp.BinDelimiter;
                    Comp.ItemQtyDecPlaces = Convert.ToInt32(DDecPlaces.Text.ToString());
                    CurrentUser.CompanyDecPlaces = Convert.ToInt32(DDecPlaces.Text.ToString());
                    Comp.UseModule2 = chkMod2.Checked;
                    CurrentUser.UseModule2 = Comp.UseModule2;
                    Comp.UseModule3 = chkMod3.Checked;
                    CurrentUser.UseModule3 = Comp.UseModule3;
                    Comp.UseAutoManf = chkAutoManf.Checked;
                    CurrentUser.UseAutoManf = Comp.UseAutoManf;
                    Comp.UsePickSlipTracking = chkPickSlip.Checked;
                    CurrentUser.UsePickSlipTracking = Comp.UsePickSlipTracking;
                    Comp.MobileModule = chkMobileModule.Checked;
                    CurrentUser.MobileModule = Comp.MobileModule;
                    phMobileTab.Visible = chkMobileModule.Checked;
                    phMobileTabBtn.Visible = chkMobileModule.Checked;
                    Comp.AutoUpdateSageSOs = chkPSAuto.Checked;
                    CurrentUser.AutoUpdateSageSOs = Comp.AutoUpdateSageSOs;
                    Comp.AutoGenTaxInvoice = chkTaxInvAuto.Checked;
                    Comp.AllowRecPriceEdit = chkRecPriceEdit.Checked;
                    CurrentUser.AllowRecPriceEdit = Comp.AllowRecPriceEdit;
                    // Part deliveries cannot be switched OFF while an order delivered in part is
                    // still open: it would then complete under the back-order rules, and what was
                    // already delivered on its earlier slips would never be invoiced.
                    long pdCo = CurrentUser.CoID;
                    bool partOrdersOpen = Comp.InvoiceWhenSOComplete && !chkInvWhenComplete.Checked
                        && _db.DocHeaders.Any(h => h.CompanyID == pdCo && h.DocType == 5 && h.Complete != true
                            && _db.PickingSlipMasters.Any(p => p.CustomerID == pdCo && p.LinkedSOrdID == h.DocID && p.PSComplete == true));
                    if (!partOrdersOpen) Comp.InvoiceWhenSOComplete = chkInvWhenComplete.Checked;
                    CurrentUser.InvoiceWhenSOComplete = Comp.InvoiceWhenSOComplete;
                    CurrentUser.AutoGenTaxInvoice = Comp.AutoGenTaxInvoice;
                    Comp.UsePacks = chkUsePacks.Checked;
                    CurrentUser.UsePacks = Comp.UsePacks;
                    Comp.ShowManfCosts = chkManfCosts.Checked;
                    CurrentUser.ShowManfCosts = Comp.ShowManfCosts;
                    if (chkweight.Checked)
                    {
                        if (txtSageWght.Text.ToLower().Contains("userfield"))
                        {
                            Comp.SageWeightField = txtSageWght.Text;
                            CurrentUser.SageWeightField = Comp.SageWeightField;
                        }
                    }
                    else
                    {
                        CurrentUser.SageWeightField = null;
                    }
                        _db.SaveChanges();
                string message = "Successfully Saved";
                string icon = "success";

                // LPNPickMode is outside the EF model - saved with raw SQL. Also
                // refresh this session's cached mode so testing picks it up without
                // a re-login (pickers on other sessions see it at their next login).
                try
                {
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.CompanyMaster SET LPNPickMode = @p0 WHERE SBCACoID = @p1",
                        DDLPNMode.SelectedValue, CurrentUser.CoID);
                    Session["PSLPNMode"] = DDLPNMode.SelectedValue;
                }
                catch
                {
                    message = "Saved - but LPN Boxing mode was NOT saved. Run Add_CompanyLPNPickMode.sql on this database first.";
                    icon = "warning";
                }

                // PickByBin - raw SQL; refresh this session's cache too.
                try
                {
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.CompanyMaster SET PickByBin = @p0 WHERE SBCACoID = @p1",
                        chkPickByBin.Checked, CurrentUser.CoID);
                    Session["PSPickByBin"] = chkPickByBin.Checked;
                }
                catch
                {
                    // Don't clobber an existing LPN warning - append instead.
                    message = (icon == "warning" ? message + " " : "Saved - but ")
                              + "Pick by Bin was NOT saved. Run Add_PickByBin.sql on this database first.";
                    icon = "warning";
                }

                // Delivery note layout - raw SQL. The running number is written ONLY when it was
                // changed on this screen: it moves on every time a note is printed, so saving the
                // page with the figure it was loaded with would wind it back and repeat numbers.
                try
                {
                    int podNext;
                    if (!int.TryParse(txtPodNext.Text.Trim(), out podNext) || podNext < 0) podNext = 0;
                    string dnReg = txtCoRegNo.Text.Trim();       if (dnReg.Length > 50) dnReg = dnReg.Substring(0, 50);
                    string dnVat = txtCoVatNo.Text.Trim();       if (dnVat.Length > 50) dnVat = dnVat.Substring(0, 50);
                    string dnPrefix = txtPodPrefix.Text.Trim();  if (dnPrefix.Length > 10) dnPrefix = dnPrefix.Substring(0, 10);
                    string dnFooter = txtDocFooter.Text ?? "";   if (dnFooter.Length > 600) dnFooter = dnFooter.Substring(0, 600);
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.CompanyMaster SET DNLayout = @p0, CoRegNo = @p1, CoVatNo = @p2, DocFooter = @p3, PODPrefix = @p4 WHERE SBCACoID = @p5",
                        DDDnLayout.SelectedValue == "1" ? 1 : 0, dnReg, dnVat, dnFooter, dnPrefix, CurrentUser.CoID);
                    int podLoaded = ViewState["PodNextLoaded"] == null ? -1 : (int)ViewState["PodNextLoaded"];
                    if (podNext != podLoaded)
                    {
                        _db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.CompanyMaster SET PODNextNumber = @p0 WHERE SBCACoID = @p1", podNext, CurrentUser.CoID);
                        ViewState["PodNextLoaded"] = podNext;
                    }
                }
                catch
                {
                    message = (icon == "warning" ? message + " " : "Saved - but ")
                              + "the Delivery Note settings were NOT saved. Run Add_DeliveryNoteLayout.sql on this database first.";
                    icon = "warning";
                }

                // Cost of Sales account for job card materials - raw SQL.
                try
                {
                    long jcCosSel;
                    long.TryParse(DDJobCos.SelectedValue, out jcCosSel);
                    JobCardHide.SetCosAccountId(_db, CurrentUser.CoID, jcCosSel);
                }
                catch
                {
                    message = (icon == "warning" ? message + " " : "Saved - but ")
                              + "the job card Cost of Sales account was NOT saved. Run Add_JobCardHideLines.sql on this database first.";
                    icon = "warning";
                }

                if (partOrdersOpen)
                {
                    chkInvWhenComplete.Checked = true;
                    message = (icon == "warning" ? message + " " : "Saved - but ")
                              + "Part deliveries was left ON: there are Sales Orders delivered in part that are still open. Complete or close those first.";
                    icon = "warning";
                }

                AlertHelper.ShowSweetAlert(this, message, icon);
                }
            }
        }
        
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (fileUpload.HasFile)
            {
                // Validate file extension
                string fileExt = Path.GetExtension(fileUpload.FileName).ToLower();
                if (fileExt != ".png")
                {
                    lblMessage.Text = "Only .png files are allowed.";
                    return;
                }

                // Validate file size (max 200KB)
                if (fileUpload.PostedFile.ContentLength > 200 * 1024) // 200KB
                {
                    lblMessage.Text = "File size must be 200KB or less.";
                    return;
                }

                try
                {
                    // Define the target path
                    string uploadPath = Server.MapPath("~/images/CoImages/");

                    // Ensure the directory exists
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Save the file
                    string filePath = Path.Combine(uploadPath, CurrentUser.CoID + ".png");
                    fileUpload.SaveAs(filePath);

                    lblMessage.ForeColor = System.Drawing.Color.Green;
                    lblMessage.Text = "File uploaded successfully!";

                    string imgname = CurrentUser.CoID + ".png";
                    string imgPath = $"~/images/CoImages/{imgname}";
                    if (File.Exists(Server.MapPath(imgPath)))
                    {
                        imgCoImgD.ImageUrl = ResolveUrl(imgPath + "?v=" + DateTime.Now.Ticks);
                    }
                    else
                    {
                        imgCoImgD.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                    }

                    // Reload the page to refresh the image
                    Response.Redirect(Request.RawUrl);
                }
                catch (Exception ex)
                {
                    lblMessage.Text = "Error: " + ex.Message;
                }
            }
            else
            {
                lblMessage.Text = "Please select a file to upload.";
            }
        }

        protected void chkAutoManf_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoManf.Checked)
            {
                chkLotTrack.Checked = false;
                ApplyLotTrackDependants();   // clears/locks Advanced + System-Generated with it
            }
        }

        protected void chkLotTrack_CheckedChanged(object sender, EventArgs e)
        {
            if(chkLotTrack.Checked)
            {
                chkAutoManf.Checked = false;
                chkAutoManf.Enabled = false;
                chkAutoManfConf.Checked = false;
            } else
            {
                chkAutoManf.Enabled = true;
            }
            ApplyLotTrackDependants();
        }

        // The Advanced and System-Generated options only mean anything while lot tracking
        // is on, so switching it off clears and locks them.
        private void ApplyLotTrackDependants()
        {
            if (!chkLotTrack.Checked)
            {
                chkLotTrackAdd.Checked = false;
                chkSysLot.Checked = false;
                chkSerialTrack.Checked = false;   // serials are lots of qty 1 - no lots, no serials
            }
            chkLotTrackAdd.Enabled = chkLotTrack.Checked;
            chkSysLot.Enabled = chkLotTrack.Checked;
            chkSerialTrack.Enabled = chkLotTrack.Checked;
        }

        protected void chkweight_CheckedChanged(object sender, EventArgs e)
        {
            if (chkweight.Checked == false) txtSageWght.Text = null;
        }
    }
}