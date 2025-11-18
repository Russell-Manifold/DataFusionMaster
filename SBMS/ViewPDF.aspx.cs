using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;

namespace SBMS
{
    public partial class ViewPDF : BasePage
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
                Uri referrer = Request.UrlReferrer;
                if (referrer != null)
                {
                    ViewState["ReferringUrl"] = referrer.ToString();
                }
                string doc = Request.QueryString["doc"];
                pnlpdfview.Attributes.Add("src", ($"PDFs\\" + doc + ".PDF"));
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
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            if (ViewState["ReferringUrl"] != null)
            {
                Response.Redirect(ViewState["ReferringUrl"].ToString());
            }
            else
            {
                // Handle the case where there is no referring URL, e.g., redirect to a default page
                Response.Redirect("~/Dashboard.aspx");
            }
        }
    }
}