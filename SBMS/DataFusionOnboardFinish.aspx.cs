using SBMS.Classes;
using SBMS.Models;
using System;
using System.Linq;
using System.Web.UI;

namespace SBMS
{
    public partial class DataFusionOnboardFinish : System.Web.UI.Page
    {
        long coid;
        protected void Page_Load(object sender, EventArgs e)
            {
            
            try
                {
                    coid = Convert.ToInt64(Request.QueryString["id"].ToString());
                }
                catch { }
                if (IsPostBack)
                {

                }
           }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            if ( rbagree.SelectedItem == null ||  rbagree.SelectedItem.Value != "Yes")
            {
                lblSuccess.Text = "Please agree before continuing";
                return;
            }
            Guid useguid;
            try
            {
                useguid = new Guid(txtguid.Text);
            }
            catch
            {
                lblSuccess.Text = "Invalid Unlock Code. Unable to continue";
                return;
            } 
           
            // update user and company status
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Comp = _db.CompanyMasters.Where(x => x.SBCACoID == coid).FirstOrDefault();
                if (Comp != null)
                {
                    Comp.ProfileStatus = "Trial";
                    Comp.Modified = DateTime.Now;
                    Comp.Active = true;
                }
                else
                {
                    lblSuccess.Text = "Error loading your profile, unable to continue.";
                    return;
                }
            
            var user = _db.UsersMasters.Where(x=>x.UserGUID == useguid).FirstOrDefault();
                if (user != null)
                {
                user.Active = true;
                }
                _db.SaveChanges();
            }
            lbtnSave.Style.Add("display", "none");
            lblSuccess.Text = "Successfully Created, proceed to Log In";
            lbtnlogin.Style.Add("display", "inline-block");
            
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }
    }
}
