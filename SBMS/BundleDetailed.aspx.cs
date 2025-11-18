using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class BundleDetailed : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        string bundcode;
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
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
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
                bundcode = Request.QueryString["bund"].ToString();
                LoadBundle();
            }
        }

        protected void LoadBundle()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BundH = _db.BundlesHeaders.Where(x => x.CompanyID == CoID && x.BundCode == bundcode).FirstOrDefault();
                if (BundH != null)
                {
                    lblFGCode.Text = BundH.BundCode;
                    lblFGDescript.Text = BundH.BundDescription;

                    var BundL = _db.BundlesLines.Where(x=>x.CompanyID == CoID  && x.BundCode == bundcode).ToList();
                    GridBundleLines.DataSource = BundL;
                    GridBundleLines.DataBind();
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
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnDeleteBundle_Click(object sender, EventArgs e)
        {

        }
    }
}