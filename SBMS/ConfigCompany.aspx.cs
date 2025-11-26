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
                //if (ApiUrlCall.dbName.ToLower().Contains("demo"))
                //{
                //    chkMod2.Enabled = true;
                //    chkMod3.Enabled = true;
                //}
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
               if (Comp.SendMessages != null) chkNotifs.Checked = (bool)Comp.SendMessages;
               if (Comp.UATMode != null) chkUAT.Checked = (bool)Comp.UATMode;
               if (Comp.UsePickSlipTracking != null) chkPickSlip.Checked = (bool)Comp.UsePickSlipTracking;
               if (Comp.UseBarcodes != null) chkBarCodes.Checked = (bool)Comp.UseBarcodes;
               if (Comp.AutoUpdateSageSOs != null) chkPSAuto.Checked = (bool)Comp.AutoUpdateSageSOs;
               if (Comp.AutoGenTaxInvoice != null) chkTaxInvAuto.Checked = (bool)Comp.AutoGenTaxInvoice;
               if (Comp.UsePacks != null) chkUsePacks.Checked = (bool)Comp.UsePacks;

               chkLotTrack.Checked = (bool)Comp.UseLotTracking;
                chkAutoManf.Checked = (bool)Comp.UseAutoManf;
                chkAutoManfConf.Checked = false;
                if (chkAutoManf.Checked)
                {
                    chkAutoManfConf.Checked = true;
                }
                DDecPlaces.Text = Comp.ItemQtyDecPlaces.ToString();
                chkMod2.Checked = Convert.ToBoolean(Comp.UseModule2);
                chkMod3.Checked = Convert.ToBoolean(Comp.UseModule3);
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
                        var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode != "FG" && x.StoreCode != "CoR" && x.StoreCode != "CoD" && x.StoreCode != "Scr").ToList(); 
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
                    Comp.UseBarcodes = chkBarCodes.Checked;
                    CurrentUser.UseBarcodes = Comp.UseBarcodes;
                    Comp.AutoUpdateSageSOs = chkPSAuto.Checked;
                    CurrentUser.AutoUpdateSageSOs = Comp.AutoUpdateSageSOs;
                    Comp.AutoGenTaxInvoice = chkTaxInvAuto.Checked;
                    CurrentUser.AutoGenTaxInvoice = Comp.AutoGenTaxInvoice;
                    Comp.UsePacks = chkUsePacks.Checked;
                    CurrentUser.UsePacks = Comp.UsePacks;
                    _db.SaveChanges();
                string message = "Successfully Saved";
                AlertHelper.ShowSweetAlert(this, message, "success");
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
                        imgCoImgD.ImageUrl = ResolveUrl(imgPath);
                    }
                    else
                    {
                        imgCoImgD.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                    }
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
    }
}