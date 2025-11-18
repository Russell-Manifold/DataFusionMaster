using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigUsers : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (GridUsers.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridUsers.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridUsers, "Select$" + row.RowIndex, true));
                    }
                }
            }
            base.Render(writer);
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
                LoadUsers();
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

        protected void GridUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            int userID = Convert.ToInt32(GridUsers.SelectedRow.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var selectedUser = _db.UsersMasters.FirstOrDefault(x => x.ID == userID && x.CompanyID == CurrentUser.CoID);
                lbluserID.Text = userID.ToString();
                txtUsername.Text = selectedUser.FirstName.ToString();
                txtemail.Text = selectedUser.Useremail.ToString();
                try
                {
                    DDRole.SelectedValue = selectedUser.RoleId.ToString();
                }
                catch { }
                chkIsActive.Checked = Convert.ToBoolean(selectedUser.Active);
                chkSuperUser.Checked = Convert.ToBoolean(selectedUser.IsSuperUser);
                Button25_ModalPopupExtender.Show();
            }
        }

        protected void LoadUsers()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Usrs = (from user in _db.UsersMasters
                                where user.CompanyID == CurrentUser.CoID
                                orderby user.Active, user.Surname
                                 select new
                                 {
                                     user.ID,
                                     user.FirstName,
                                     user.Useremail,
                                     RoleDescript = (from role in _db.RolesMasters
                                                     where role.RoleID == user.RoleId
                                                     select role.RoleName).FirstOrDefault(),
                                     user.Active,
                                     user.LastActivity
                                 }).ToList();

                GridUsers.DataSource = Usrs;
                GridUsers.DataBind();

                var Roles = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.RoleName).ToList();
                DDRole.DataSource = Roles;
                DDRole.DataTextField = "RoleName";
                DDRole.DataValueField = "RoleID";
                DDRole.DataBind();
                DDRole.Items.Insert(0, "-Select-");  
            }
        }

        protected void GridUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            txtUsername.Text = string.Empty;
            txtemail.Text = string.Empty;
            lbluserID.Text = string.Empty;
            DDRole.SelectedIndex = 0;
            chkSuperUser.Checked = false;
            chkIsActive.Checked = false;
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {

                if (ComparePwds(txtPwd1.Text, txtPwd2.Text) != "OK")
                {
                    string message = "alert('" + "Passwords Do Not Match" + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                };
                int userid = 0;
                try
                {
                    userid = Convert.ToInt32(lbluserID.Text);
                }
                catch{};
               
                if (txtUsername.Text.Length < 3 || txtemail.Text.Length < 3)
                {
                    string message = "alert('" + "Please capture valid username and email address" + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }
                
                if (DDRole.SelectedIndex == 0)
                {
                    string message = "alert('" + "Please select a valid role for this user." + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }

                if (userid > 0)
                {
                       
                    // is user - update
                    var User = _db.UsersMasters.FirstOrDefault(x => x.ID == userid && x.CompanyID == CurrentUser.CoID);
                    User.FirstName = txtUsername.Text.Replace("'", "''");
                    User.Useremail = txtemail.Text.ToString();
                    User.RoleId = Convert.ToInt32(DDRole.SelectedItem.Value.ToString());
                    User.Active = chkIsActive.Checked;
                    User.IsSuperUser = chkSuperUser.Checked;
                    
                }
                else
                {
                    // new user
                    UsersMaster NewUser = new UsersMaster();
                    NewUser.FirstName = txtUsername.Text.Replace("'", "''");
                    NewUser.Useremail = txtemail.Text.ToString();
                    NewUser.userpwd = txtPwd1.Text.ToString();
                    NewUser.IsSuperUser = chkSuperUser.Checked;
                    NewUser.Active = chkIsActive.Checked;
                    NewUser.RoleId = Convert.ToInt32(DDRole.SelectedItem.Value.ToString());
                    NewUser.UserGUID =Guid.NewGuid();
                    NewUser.CompanyID = CurrentUser.CoID;
                    _db.UsersMasters.Add(NewUser);
                }

                try
                {
                    _db.SaveChanges();
                    txtUsername.Text = string.Empty;
                    txtemail.Text = string.Empty;
                    lbluserID.Text = string.Empty;
                    DDRole.SelectedIndex = 0;
                    chkSuperUser.Checked = false;
                    chkIsActive.Checked = false;
                    LoadUsers();
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

        protected string ComparePwds(string pwd1, string pwd2)
        {
            string retStr = "OK";
            if (pwd1 != pwd2)
            {
                retStr = "Passwords Do Not Match";
            }
            return retStr;
        }
    }
}