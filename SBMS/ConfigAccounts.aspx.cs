using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigAccounts : BasePage
    {
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

            if (!IsPostBack)
            {
                ApiUrlCall api = new ApiUrlCall();
                await api.LoadGLAccounts(CurrentUser);
                LoadAccts();
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

        protected void LoadAccts()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var delivs = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID).OrderByDescending(x => x.AcctCategDescr).ToList();
                GridAccounts.DataSource = delivs.OrderByDescending(x => x.JCUse);
                GridAccounts.DataBind();
            }
        }

       protected void chkJCUse_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            // AccountID is a long in the model and comes from the grid key: Convert.ToInt32
            // would overflow on a large Sage id, and Cells[0].Text breaks the moment that
            // column is reformatted, hidden or paged.
            long AcctID = Convert.ToInt64(GridAccounts.DataKeys[row.RowIndex].Value);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                AccountsMaster NewAcct = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == AcctID).FirstOrDefault();
                if (NewAcct == null) { LoadAccts(); return; }
                NewAcct.JCUse = chk.Checked;
                _db.SaveChanges();
                
                string message = "alert('" + "Successfully Saved" + "')";
                ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                return;
            }
        }

        protected void chkADCUse_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            // AccountID is a long in the model and comes from the grid key: Convert.ToInt32
            // would overflow on a large Sage id, and Cells[0].Text breaks the moment that
            // column is reformatted, hidden or paged.
            long AcctID = Convert.ToInt64(GridAccounts.DataKeys[row.RowIndex].Value);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                AccountsMaster NewAcct = _db.AccountsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == AcctID).FirstOrDefault();
                if (NewAcct == null) { LoadAccts(); return; }
                NewAcct.AccountAddCosts = chk.Checked;
                _db.SaveChanges();

                string message = "alert('" + "Successfully Saved" + "')";
                ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
                return;
            }
        }

        /// <summary>
        /// Marks the account Sage posts stock adjustments against - the DEBIT side of the
        /// journal raised when a works order carries BOM additional costs.
        ///
        /// Manufacturing already posts item adjustments that raise stock value by the
        /// additional cost, leaving an unexplained credit on Sage's own adjustment account.
        /// The journal clears it: debit THIS account, credit the account chosen on the BOM.
        ///
        /// Only one account per company should carry this flag, so ticking one clears the rest.
        /// </summary>
        protected void chkADCContra_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            // AccountID is a long in the model and comes from the grid key: Convert.ToInt32
            // would overflow on a large Sage id, and Cells[0].Text breaks the moment that
            // column is reformatted, hidden or paged.
            long AcctID = Convert.ToInt64(GridAccounts.DataKeys[row.RowIndex].Value);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Find the row FIRST. Clearing the old flag before knowing the new one exists
                // left the company with no Stock Adjustment Account at all, under a message
                // saying it had saved.
                AccountsMaster NewAcct = _db.AccountsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.AccountID == AcctID).FirstOrDefault();
                if (NewAcct == null) { LoadAccts(); return; }

                if (chk.Checked)
                {
                    // Single account only - clear any previous choice for this company. Saved on
                    // its own so the database only ever sees one account flagged at a time; the
                    // unique index would otherwise reject an unlucky statement order.
                    var others = _db.AccountsMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.AccountAddCostsContra == true).ToList();
                    foreach (var o in others) o.AccountAddCostsContra = false;
                    if (others.Count > 0) _db.SaveChanges();
                }

                NewAcct.AccountAddCostsContra = chk.Checked;
                _db.SaveChanges();
            }

            LoadAccts();   // rebind so the cleared flags show

            string msg = "alert('" + (chk.Checked
                ? "Saved. BOM additional costs will be journalled against this account."
                : "Saved.") + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", msg, true);
        }
    }
}