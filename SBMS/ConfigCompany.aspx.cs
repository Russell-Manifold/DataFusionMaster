using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;

namespace SBMS
{
    public partial class ConfigCompany : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

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

        private void GetCompanyDetails()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
               if (Comp.CompanyName!= null) lblCoName.Text = Comp.CompanyName ?? "";
               if(Comp.SBCACoID != null)   lblCoID.Text = Comp.SBCACoID.ToString();
               //if (Comp.CoGenericLoginEmail != null) lblGenEmail.Text = Comp.CoGenericLoginEmail ?? "";
              // if (Comp.CoGenericLoginPwd != null) lblGenPwd.Text = Comp.CoGenericLoginPwd ?? "";
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
               chkPSAuto.Checked = (bool)Comp.AutoUpdateSageSOs;
               chkTaxInvAuto.Checked = (bool)Comp.AutoGenTaxInvoice;
               chkUsePacks.Checked = (bool)Comp.UsePacks;
                chkManfCosts.Checked = (bool)Comp.ShowManfCosts;

               chkLotTrack.Checked = (bool)Comp.UseLotTracking;
                chkSysLot.Checked = Comp.AllowSystemLotNumbers == true;
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
                // Removed lot tracking 
                uselotTrack = false;
                // remove All Stored except FG
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode != "FG" && x.StoreCode != "CoR" && x.StoreCode != "CoD" && x.StoreCode.ToLower() != "scr").ToList(); 
                        _db.Stores.RemoveRange(Stores);
                        _db.SaveChanges();
                       
                    }
            } else
            {
                uselotTrack = chkLotTrack.Checked;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == CurrentUser.CoID);
                {
                    Comp.CompanyName = lblCoName.Text;
                    //Comp.CoGenericLoginEmail = lblGenEmail.Text.ToString();
                    //Comp.CoGenericLoginPwd = lblGenPwd.Text.ToString();
                    Comp.Contact = txtContPerson.Text.ToString();
                    Comp.Contactemail = txtContemail.Text.ToString();
                    Comp.SendMessages = chkNotifs.Checked;
                    CurrentUser.SendMessages = Comp.SendMessages;
                    Comp.UATMode = chkUAT.Checked;
                    Comp.UseLotTracking = uselotTrack;
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
        }

        protected void chkweight_CheckedChanged(object sender, EventArgs e)
        {
            if (chkweight.Checked = false) txtSageWght.Text = null;
        }
    }
}