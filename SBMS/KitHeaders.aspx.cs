using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class KitHeaders : BasePage
    {
        long CoID;
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

            CoID = CurrentUser.CoID;
            if (!IsPostBack)
            {
                LoadKits();
            }
        }

        protected void LoadKits()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BOMs = _db.KitHeaders.Where(x => x.CompanyID == CoID).ToList();
                if (BOMs.Count > 0)
                {
                    foreach (var item in BOMs)
                    {
                        if (item.KitActive == null)
                        {
                            item.KitActive = true;
                        }
                    }
                    GridBOM.DataSource = BOMs;
                    GridBOM.DataBind();
                }
            }
        }

        protected void lbtnBOM_Click(object sender, EventArgs e)
        {
            LinkButton lbtnBOM = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnBOM.NamingContainer;
            int bomid = Convert.ToInt32(lbtnBOM.CommandArgument);
            Response.Redirect("~/KitDetailed.aspx?kitid=" + lbtnBOM.CommandArgument);
        }

        protected void GridBOM_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }
        protected void lbtnCreateNew_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                KitHeader NewKH = new KitHeader();
                NewKH.CompanyID = (int)CoID;
                NewKH.KitCode = "NEW";
                NewKH.KitActive = true;
                _db.KitHeaders.Add(NewKH);
                _db.SaveChanges();
                int newbhid = NewKH.KitHID;
                Response.Redirect("~/KitCreate.aspx?kitid=" + newbhid, false);
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

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BH = _db.KitHeaders.Where(x => x.KitHID == Lid);
                _db.KitHeaders.RemoveRange(BH);

                var BL = _db.KitLines.Where(x => x.KitHID == Lid);
                _db.KitLines.RemoveRange(BL);
                _db.SaveChanges();

                LoadKits();
            }
        }
    }
}