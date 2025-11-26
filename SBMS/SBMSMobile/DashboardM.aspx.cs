using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;

namespace SBMS
{
    public partial class DashboardM : System.Web.UI.Page
    {
        UserDetails userDets;
        Guid userGuid;
        protected void Page_Load(object sender, EventArgs e)
        {
            userDets = Session["UserDetails"] as UserDetails;
            SessionValidator.ValidateUserSession(userDets);
            if (userDets == null)
            {
                Response.Redirect("~/LoginM.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            //lblUserName.Text = $":.. {userDets.UserName} ..:";
            if (!IsPostBack)
            {
                string imgname = userDets.CoID + ".png";
                string imgPath = $"~/images/CoImages/{imgname}";
                if (File.Exists(Server.MapPath(imgPath)))
                {
                  //  imgCoImg.ImageUrl = ResolveUrl(imgPath);
                }
                else
                {
                 //   imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
                }

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                  //  lblCoName.Text = _db.CompanyMasters.FirstOrDefault(x => x.SBCACoID == userDets.CoID).CompanyName.ToString();
                }
                showhidebuttons();
            }
        }

        private void showhidebuttons()
        {
        }
        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (userDets != null)
            {
                Response.Redirect("~/SBMSMobile/OSPurchaseOrdersM.aspx?user=" + userDets.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {

        }
    }
}