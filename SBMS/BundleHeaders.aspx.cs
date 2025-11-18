using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class BundleHeaders : BasePage
    {
        long CoID;
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

            CoID = CurrentUser.CoID;
            if (!IsPostBack)
            {
                ApiUrlCall api = new ApiUrlCall();
                await api.LoadBundles(false, CurrentUser);
                LoadBundles();
            }
        }

        protected void LoadBundles()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Bunds = _db.BundlesHeaders.Where(x => x.CompanyID == CoID && x.Active == true).ToList();
                GridBundles.DataSource = Bunds;
                GridBundles.DataBind();
            }
        }

        protected void lbtnBundle_Click(object sender, EventArgs e)
        {
            LinkButton lbtnBundle = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnBundle.NamingContainer;
            Response.Redirect("~/BundleDetailed.aspx?bund=" + lbtnBundle.CommandArgument);
        }

        protected void GridBundles_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
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
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}