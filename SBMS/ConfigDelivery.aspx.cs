using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigDelivery : BasePage
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
                LoadDelivs();
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

        protected void LoadDelivs()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var delivs = _db.DelivMethods.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.DelivMethod1).ToList();
                GridDelivery.DataSource = delivs;
                GridDelivery.DataBind();
            }
        }
       
       protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            txtDelMName.Text = string.Empty;
            lblDelMID.Text = "0";
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
               if (txtDelMName.Text.Length < 2)
                {
                    string message = "alert('" + "Please capture valid Delivery Method." + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }
                try
                {
                    DelivMethod NewDelMthod = new DelivMethod();
                    NewDelMthod.DelivMethod1 = txtDelMName.Text;
                    NewDelMthod.CompanyID = CurrentUser.CoID;
                    NewDelMthod.DelActive = true;

                    _db.DelivMethods.Add(NewDelMthod);
                    _db.SaveChanges();
                    LoadDelivs();
                    string message = "alert('" + "Successfully Saved" + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }
                catch (Exception ex)
                {
                    string message = "alert('" + ex.Message + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }
            }
        }

        protected void GridDelivery_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            int DelivId = Convert.ToInt32(row.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                DelivMethod NewDel = _db.DelivMethods.Where(x => x.CompanyID == CurrentUser.CoID && x.DelID == DelivId).FirstOrDefault();
                NewDel.DelActive = chk.Checked;
                _db.SaveChanges();
                LoadDelivs();
                string message = "alert('" + "Successfully Saved" + "')";
                ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                return;
            }
        }
    }
}