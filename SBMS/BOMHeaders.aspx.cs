using SBMS.Classes;
using SBMS.Models;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class BOMHeaders : BasePage
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
            if (CurrentUser == null) Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();

            if (!IsPostBack)
            {
                //deleteNewUnused();
                LoadBoms();
            }
        }

        protected void LoadBoms()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BOMb = _db.BOMHeaders
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.FGCode == null).ToList();
                _db.BOMHeaders.RemoveRange(BOMb);
                _db.SaveChanges();

                string searchText = txtfind.Text.Trim();
                var BOMs = _db.BOMHeaders
                    .Where(x => x.CompanyID == CurrentUser.CoID &&
                                (string.IsNullOrEmpty(searchText) ||
                                 x.BOMCode.ToLower().Contains(searchText.ToLower()) ||
                                 x.FGCode.ToLower().Contains(searchText.ToLower()) ||
                                 x.BomDescript.ToLower().Contains(searchText.ToLower())))
                    .ToList();
                if (BOMs.Count > 0)
                {
                    if (BOMs.Count > 0)
                    {
                        foreach (var item in BOMs)
                        {
                            if (item.BomActive == null)
                            {
                                item.BomActive = true;
                            }
                        }

                        GridBOM.DataSource = BOMs;
                        GridBOM.DataBind();
                    }
                }
            }
        }

        protected void lbtnBOM_Click(object sender, EventArgs e)
        {
            LinkButton lbtnBOM = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnBOM.NamingContainer;
            int bomid = Convert.ToInt32(lbtnBOM.CommandArgument);
            Response.Redirect("~/BOMDetailed.aspx?bomid=" + lbtnBOM.CommandArgument);
        }

        protected void GridBOM_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void lbtnCreateNew_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                BOMHeader NewBH = new BOMHeader();
                NewBH.CompanyID = CurrentUser.CoID;
                NewBH.BOMCode = "NEW";
                NewBH.BomActive = true;
                _db.BOMHeaders.Add(NewBH);
                _db.SaveChanges();
                int newbhid = NewBH.BomHID;
                Response.Redirect("~/BOMCreate.aspx?bomid=" + newbhid, false);
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
                var BH = _db.BOMHeaders.Where(x => x.BomHID == Lid);
                _db.BOMHeaders.RemoveRange(BH);

                var BL = _db.BOMLines.Where(x => x.BomHID == Lid);
                _db.BOMLines.RemoveRange(BL);
                _db.SaveChanges();
                        
                LoadBoms();
            }
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            LoadBoms();
        }
    }
}