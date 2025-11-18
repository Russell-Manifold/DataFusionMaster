using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigAccounts : BasePage
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
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            if (!IsPostBack)
            {
                ApiUrlCall api = new ApiUrlCall();
                await api.LoadGLAccounts(CurrentUser);
                LoadAccts();
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

        protected void LoadAccts()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var delivs = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID).OrderByDescending(x => x.AcctCategDescr).ToList();
                GridAccounts.DataSource = delivs.OrderByDescending(x => x.JCUse);
                GridAccounts.DataBind();
            }
        }

       protected void chkJCUse_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            int AcctID = Convert.ToInt32(row.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                AccountsMaster NewAcct = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == AcctID).FirstOrDefault();
                NewAcct.JCUse = chk.Checked;
                _db.SaveChanges();
                
                string message = "alert('" + "Successfully Saved" + "')";
                ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                return;
            }
        }

        protected void chkADCUse_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            int AcctID = Convert.ToInt32(row.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                AccountsMaster NewAcct = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == AcctID).FirstOrDefault();
                NewAcct.AccountAddCosts = chk.Checked;
                _db.SaveChanges();

                string message = "alert('" + "Successfully Saved" + "')";
                ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                return;
            }
        }
    }
}