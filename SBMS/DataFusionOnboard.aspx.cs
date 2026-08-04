using MimeKit;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Web.UI;
using System.Linq;

namespace SBMS
{
    public partial class DataFusionOnboard : System.Web.UI.Page
    {

        public static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 15, 11, 12, 77, 14, 15, 16, 17, 18, 91, 20, 21, 22, 23, 24 };
        public static byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };
        protected void Page_Load(object sender, EventArgs e)
            {
           
            if (!IsPostBack)
            {

            }
          }

        protected async void lbtnNext_Click(object sender, EventArgs e)
        {
            if (
      txtCoName.Text.Trim().Length > 0 &&
      txtContact.Text.Trim().Length > 0 &&
      txtsagemail.Text.Trim().Length > 0 &&
      txtSagePwd.Text.Trim().Length > 0)
            {
                ApiUrlCall api = new ApiUrlCall();
                JObject result = await api.GetCompaniesEnrollAsync(txtsagemail.Text.Trim(), txtSagePwd.Text.Trim());

                if (result != null && result.ContainsKey("success") && (bool)result["success"])
                {
                    JArray items = (JArray)result["data"]?["Results"];

                    if (items != null && items.Count > 0)
                    {
                        List<Company> CompList = new List<Company>();
                        foreach (JObject item in items)
                        {
                            Company comp = new Company
                            {
                                CoID = item["ID"]?.ToString(),
                                CoName = item["Name"]?.ToString()
                            };
                            CompList.Add(comp);
                        }

                        DDCompany.DataSource = CompList;
                        DDCompany.DataTextField = "CoName";
                        DDCompany.DataValueField = "CoID";
                        DDCompany.DataBind();

                        // show company panel
                        PnlPrimary.Style.Add("display", "none");
                        PnlAdds.Style.Add("display", "none");
                        PnlSelectCompany.Style.Add("display", "inline-block");
                        PnlSelectCompany.Style.Add("width", "100%");
                    }
                    else
                    {
                        ShowMessage(sender, EventArgs.Empty, "No companies returned from Sage with these credentials.");
                    }
                }
                else
                {
                    string errorMsg = result?["error"]?["reason"]?.ToString()
                                    ?? "Unknown error occurred.";
                    ShowMessage(sender, EventArgs.Empty, "Sage login failed: " + errorMsg);
                    PnlPrimary.Style.Add("display", "none");
                    pnlTroubleshoot.Style.Add("display", "inline-block");
                }
            }
            else
            {
                ShowMessage(sender, EventArgs.Empty, "Please complete all compulsory fields before continuing");
            }
        }
        protected void lbtnNext2_Click(object sender, EventArgs e)
        {
            if (!chkWarning.Checked)
            {
                ShowMessage(sender, EventArgs.Empty, "Please indicate you understand User Testing Mode before continung.");
                return;
            }
            PnlPrimary.Style.Add("display", "none");
            PnlSelectCompany.Style.Add("display", "none");
            PnlAdds.Style.Add("display", "inline-block");
            PnlAdds.Style.Add("width", "100%");
        }

        protected void lbtnBack2_Click(object sender, EventArgs e)
        {
            PnlSelectCompany.Style.Add("display", "none");
            PnlAdds.Style.Add("display", "none");
            PnlPrimary.Style.Add("display", "inline-block");
            PnlPrimary.Style.Add("width", "100%");
        }
        protected void lbtnBack3_Click(object sender, EventArgs e)
        {
            PnlPrimary.Style.Add("display", "none");
            PnlAdds.Style.Add("display", "none");
            PnlSelectCompany.Style.Add("display", "inline-block");
            PnlSelectCompany.Style.Add("width", "100%");
        }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            // inser record into company master - set status to pending and Active to false
            long CoId = Convert.ToInt64(DDCompany.SelectedValue);
            Guid newUserguid = Guid.NewGuid();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.Where(x => x.SBCACoID == CoId).FirstOrDefault();
                if (Comp != null)
                {
                    ShowMessage(sender, EventArgs.Empty, "Company already registered, unable to continue");
                    return;
                }
                // encryp password
                cTripleDES des = new cTripleDES(key, iv);
                string EncryptedPwd = des.Encrypt(txtSagePwd.Text.Trim().ToString());
                CompanyMaster compm = new CompanyMaster
                {
                    SBCACoID = CoId,
                    SBCAemail = txtsagemail.Text.ToString().Trim(),
                    SBCApwd = EncryptedPwd,
                    CompanyName = DDCompany.SelectedItem.Text,
                    Contact = txtContact.Text.ToString(),
                    Contactemail = txtsagemail.Text.ToString().Trim(),
                    UseStartDate = DateTime.Now,
                    UseEndDate = DateTime.Now.AddMonths(1),
                    BillAddress1 = txtAdd1.Text.ToString().Trim(),
                    BillAddress2 = txtAdd2.Text.ToString().Trim(),
                    BillAddress3 = txtAdd3.Text.ToString().Trim(),
                    BillAddress4 = txtAdd4.Text.ToString().Trim(),
                    UseModule2 = chkMod2.Checked,
                    UseModule3 = chkMod3.Checked,
                    UATMode = true,
                    SendMessages = true,
                    UseLotTracking = chkLotTrack.Checked,
                    UseLotAddDetails = true,
                    ItemQtyDecPlaces = Convert.ToInt16(txtDecPlaces.Text.Trim()),
                    SageConsultRNum = txtConsultRNum.Text.Trim(),
                    ProfileStatus = "Pending",
                    // Non-nullable in the EDMX, so EF marks it Required and rejects the entity
                    // before the INSERT is ever sent - a database DEFAULT can never fire for it.
                    LPNPickMode = "off",
                    Active = false,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                    ModifiedBy = txtsagemail.Text.ToString().Trim(),
                    CreatedBy = txtsagemail.Text.ToString().Trim(),
                };

                // insert user into Users master
                UsersMaster userM = new UsersMaster
                {
                    Useremail = txtsagemail.Text.ToString().Trim(),
                    FirstName = txtContact.Text.ToString().Trim(),
                    CompanyID = CoId,
                    Active=false,
                    RoleId = 0,
                    userpwd = EncryptedPwd,
                    IsSuperUser = true,
                    IsLoggedIn = false,
                    UserGUID = newUserguid
                };
                _db.CompanyMasters.Add(compm);
                _db.UsersMasters.Add(userM);
                _db.SaveChanges();
            }

            emailer mailsend = new emailer();
            MimeMessage message = new MimeMessage();
            message.To.Add(MailboxAddress.Parse(txtsagemail.Text.ToString().Trim()));
            message.ReplyTo.Add(MailboxAddress.Parse("hello@syncflo.co.za"));
            message.Cc.Add(MailboxAddress.Parse("hello@syncflo.co.za"));
            message.From.Add(new MailboxAddress("Data Fusion Registration", "enquiries@syncflo.co.za"));

            message.Subject = $"My Data Fusion, New Registration";
            BodyBuilder Bdy = new BodyBuilder();
            Bdy.HtmlBody = $" Hi {txtContact.Text}" +
            "<br/><br/>" +
            $"Thanks so much for creating a profile on My Data Fusion. To finish off your onboarding, please copy your unique code below, and then follow the link to paste it:" +
            $"<br/><br/>" +
            $"Your unique code to copy: {newUserguid}" +
            $"<br/><br/>" +
            $" Open this link to paste: https://mydatafusion.online/za/DataFusionOnboardFinish.aspx?id={CoId} " +
            // $"Open this link to paste: http://localhost:55060/DataFusionOnboardFinish.aspx?id={CoId}" +
            "<br/><br/>" +
            "My data Fusion Team" +
            "<br/><br/>" +  // Separator line
            "<b>Email Disclaimer:</b><br/>" +
            "<span style='font-size: smaller;'>" +
            "This email and any attachments are confidential and intended solely for the recipient. If you are not the intended recipient, please delete this email and notify the sender immediately.Any unauthorized use, disclosure, or distribution of this email is prohibited." +
            "<br/>" +
            "While My Data Fusion and Syncflo(Pty) Ltd take reasonable steps to ensure the accuracy and security of our communications, we do not accept liability for errors, data discrepancies, or any damage resulting from email transmission.Please verify critical information independently." +
             "<br/>" +
             "<strong>This email is subject to and incorporates the <a href='https://mydatafusion.online/documents/Disclaimer.pdf' target='_blank'>Disclaimer</a> and <a href='https://mydatafusion.online/documents/Privacy_Policy.pdf' target='_blank'>Privacy Policy</a>Syncflo (Pty) Ltd.</strong>";
            message.Body = Bdy.ToMessageBody();
            string mailstr = mailsend.SendEmail(message);
            
            lbtnBack3.Style.Add("display", "none");
            lbtnSave.Style.Add("display", "none");
            if (mailstr == "OK")
            {
                PnlSuccess.Style.Add("display", "inline-block");
                PnlAdds.Style.Add("display", "none");
            }
            else
            {
                lblerr.Text =  "Ooops sorry, unable to sent mail message." + Environment.NewLine + Environment.NewLine + " Error Message:" + mailstr;
                return;
            }
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

        public class Company
        {
            public string CoID { get; set; }
            public string CoName { get; set; }

        }

        protected void PopMessage(string retmsg)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<script type = 'text/javascript'>");
            sb.Append("window.onload=function(){");
            sb.Append("alert('");
            sb.Append(retmsg);
            sb.Append("')};");
            sb.Append("</script>");
            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", sb.ToString());
        }
    }
}

