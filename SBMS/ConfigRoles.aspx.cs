using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigRoles : BasePage
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
            if (GridRoles.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridRoles.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridRoles, "Select$" + row.RowIndex, true));
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
                LoadRoles();
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

        protected void LoadRoles()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Roles = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.RoleName).ToList();
                GridRoles.DataSource = Roles;
                GridRoles.DataBind();
            }
        }
        protected void GridRoles_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void GridRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            int RoleID = Convert.ToInt32(GridRoles.SelectedRow.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var SelectedRole = _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == RoleID);
                lblRoleID.Text = RoleID.ToString();
                txtRoleName.Text = SelectedRole.RoleName.ToString();
                chkCanReceiveS.Checked = Convert.ToBoolean(SelectedRole.CanReceive);
                chkCanTransferS.Checked = Convert.ToBoolean(SelectedRole.CanTransfer);
                chkCanInvoiceS.Checked = Convert.ToBoolean(SelectedRole.CanInvoice);
                chkNotifyGRNS.Checked = Convert.ToBoolean(SelectedRole.NotifyGRN);
                chkNotifyTransferS.Checked = Convert.ToBoolean(SelectedRole.NotifyTransfer);
                chkNotifyPSMoveS.Checked = Convert.ToBoolean(SelectedRole.NotifyPSMove);
                chkNotifyJCMoveS.Checked = Convert.ToBoolean(SelectedRole.NotifyJCMove);
                chkNotifyNewSOS.Checked = Convert.ToBoolean(SelectedRole.NotifyNewSO);
                chkNotifyPSCompleteS.Checked = Convert.ToBoolean(SelectedRole.NotifySOComplete);
                //chkUseGenericLoginS.Checked = Convert.ToBoolean(SelectedRole.UseGenericLogin);
                Button25_ModalPopupExtender.Show();
            }
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            txtRoleName.Text = string.Empty;
            lblRoleID.Text = "0";
            chkCanReceiveS.Checked = false;
            chkCanTransferS.Checked = false;
            chkCanInvoiceS.Checked = false;
            chkNotifyGRNS.Checked = false;
            chkNotifyTransferS.Checked = false;
            chkNotifyPSMoveS.Checked = false;
            chkNotifyJCMoveS.Checked = false;
            chkNotifyNewSOS.Checked = false;
            chkNotifyPSCompleteS.Checked = false;
            //chkUseGenericLoginS.Checked = false;
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            int RoleID = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                try
                {
                    RoleID = Convert.ToInt32(lblRoleID.Text);
                }
                catch { };

                if (txtRoleName.Text.Length < 2)
                {
                    string message = "alert('" + "Please capture valid Role Name." + "')";
                    ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                    return;
                }
   
                if (RoleID > 0)
                {
                    var ThisRole = _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == RoleID);
                    ThisRole.RoleName = txtRoleName.Text.ToString().Replace("'", "''");
                    ThisRole.CanReceive = chkCanReceiveS.Checked;
                    ThisRole.CanTransfer = chkCanTransferS.Checked;
                    ThisRole.CanInvoice = chkCanInvoiceS.Checked;
                    ThisRole.NotifyGRN = chkNotifyGRNS.Checked;
                    ThisRole.NotifyTransfer = chkNotifyTransferS.Checked;
                    ThisRole.NotifyPSMove = chkNotifyPSMoveS.Checked;
                    ThisRole.NotifyJCMove = chkNotifyJCMoveS.Checked;
                    ThisRole.NotifyNewSO = chkNotifyNewSOS.Checked;
                    ThisRole.NotifySOComplete = chkNotifyPSCompleteS.Checked;
                    //ThisRole.UseGenericLogin = chkUseGenericLoginS.Checked;
                }
                else
                {
                    RolesMaster ThisRole = new RolesMaster();
                    ThisRole.RoleName = txtRoleName.Text.ToString().Replace("'", "''");
                    ThisRole.CanReceive = chkCanReceiveS.Checked;
                    ThisRole.CanTransfer = chkCanTransferS.Checked;
                    ThisRole.CanInvoice = chkCanInvoiceS.Checked;
                    ThisRole.NotifyGRN = chkNotifyGRNS.Checked;
                    ThisRole.NotifyTransfer = chkNotifyTransferS.Checked;
                    ThisRole.NotifyPSMove = chkNotifyPSMoveS.Checked;
                    ThisRole.NotifyJCMove = chkNotifyJCMoveS.Checked;
                    ThisRole.NotifyNewSO = chkNotifyNewSOS.Checked;
                    ThisRole.NotifySOComplete = chkNotifyPSCompleteS.Checked;
                    //ThisRole.UseGenericLogin = chkUseGenericLoginS.Checked;
                    ThisRole.CompanyID = CurrentUser.CoID;
                    _db.RolesMasters.Add(ThisRole);
                }
                try
                {
                    _db.SaveChanges();
                    txtRoleName.Text = string.Empty;
                    lblRoleID.Text = "0";
                    chkCanReceiveS.Checked = false;
                    chkCanTransferS.Checked = false;
                    chkCanInvoiceS.Checked = false;
                    chkNotifyGRNS.Checked = false;
                    chkNotifyTransferS.Checked = false;
                    chkNotifyPSMoveS.Checked = false;
                    chkNotifyJCMoveS.Checked = false;
                    chkNotifyNewSOS.Checked = false;
                    chkNotifyPSCompleteS.Checked = false;
                    //chkUseGenericLoginS.Checked = false;
                    LoadRoles();
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
    }
}