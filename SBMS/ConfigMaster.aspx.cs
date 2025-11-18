using SBMS.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigMaster : BasePage
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
                if (!CurrentUser.UseModule2)
                {
                    lbtnKit.Style.Add("display", "none");
                    lbtnAccts.Style.Add("display", "none");
                }
                if (!CurrentUser.UseModule3)
                {
                    lbtnBOM.Style.Add("display", "none");
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

        protected void lbtnOpenBals_Click(object sender, EventArgs e)
        {

        }

        protected async void lbtnItems_Click(object sender, EventArgs e)
        {
            ApiUrlCall api = new ApiUrlCall();
            var errors = await api.LoadItems(CurrentUser);
           
            if (errors.Count > 0)
            {
                string message = string.Join("\n", errors);
                ScriptManager.RegisterStartupScript(
                    this,
                    this.GetType(),
                    "warnAndRedirect",
                    "Swal.fire({ icon: 'warning', title: 'Warning', text: 'There was an error refreshing items from Sage:- '" +message + "' }).then(() => { window.location='ItemsHeaders.aspx'; });",
                    true);
            }
            else
            {
                ScriptManager.RegisterStartupScript(
                         this,
                         this.GetType(),
                         "alertSuccess",
                         "Swal.fire({ icon: 'success', title: 'Success', text: 'Items successfully synchronised with Sage' }).then(() => { window.location='ItemsHeaders.aspx'; });",
                         true
                 );
            }
        }
    }
}