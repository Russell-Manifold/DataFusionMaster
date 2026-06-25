using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StoresMaster : BasePage
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
            if (!IsPostBack)
            {
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

                LoadStores();
                SetBinFormatHint();
            }
        }

        // Show the company's configured location-code format (from Company Config) so the
        // user knows what to capture, e.g. Warehouse-Aisle-Row-Bin -> WH1-A03-R2-B05.
        private void SetBinFormatHint()
        {
            var labels = BinMask.Segments(CurrentUser);
            if (labels.Count == 0)
            {
                lblBinFormat.Text = "Free-text location codes &ndash; no fixed format is set in Company Config.";
                return;
            }
            string sep = BinMask.Separator(CurrentUser);
            lblBinFormat.Text = "Required format: <b>" + string.Join(sep, labels) + "</b></br>"
                + labels.Count + " parts separated by '" + sep + "', letters / numbers only </br>(e.g. "
                + string.Join(sep, labels.Select(l => l.Substring(0, System.Math.Min(2, l.Length)).ToUpper() + "1")) + ").";
        }

        protected void LoadStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode != "CoR" && x.StoreCode != "CoD").ToList();
                if (Stores != null)
                {
                    GridStores.DataSource = Stores;
                    GridStores.DataBind();
                }
            }
         }
        protected void GridStores_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                LinkButton lbtn = new LinkButton();
                lbtn = (LinkButton)e.Row.FindControl("lbtnDelete");
                //if (e.Row.Cells[0].Text == "Co")
                //{
                //    lbtn.Visible = false;
                //}
            }
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            if (txtstorecode.Text.Trim().Length < 2)
            {
                PopMessage("Please capture a store code of at least 2 characters");
                return;
            }
            if (txtStoreDesctript.Text.Trim().Length < 5)
            {
                PopMessage("Please capture a store description of at least 5 characters");
                return;
            }
            Store NewSt = new Store();
            NewSt.StoreActive = true;
            NewSt.StoreCode = txtstorecode.Text.Trim().ToString();
            NewSt.StoreDescript = txtStoreDesctript.Text.Trim().ToString();
            NewSt.CompanyID = CurrentUser.CoID;
            NewSt.AllowPicking = chkAllowP.Checked;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {     
                _db.Stores.Add(NewSt);
                _db.SaveChanges();
            }
            LoadStores();
            PopMessage("Store Successfully Added");
        }

        protected void PopMessage(string retmsg)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<script type = 'text/javascript'>");
            sb.Append("window.onload=function(){");
            sb.Append("alert('");
            sb.Append(retmsg);
            sb.Append("')};");
            sb.Append("</script>");
            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", sb.ToString());
        }

        protected void chkAllowPick_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                NewSt.AllowPicking = chk.Checked;
                _db.SaveChanges();
                LoadStores();
                PopMessage("Store Updated");
            }
        }

        protected void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            if (stCode == "Scr")
            {
                chk.Checked = true;
                PopMessage("Scrap store is mandatory, and cannot be de-activated.");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                NewSt.StoreActive = chk.Checked;
                string retstr = " de-activated";
                if (chk.Checked) { retstr = " activated"; }
                _db.SaveChanges();
                LoadStores();
                PopMessage("Store Successfully " + retstr);
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void chkAllowReceive_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (chk.Checked)
                {
                    // Single receiving store per company - clear it off every other store.
                    foreach (var st in _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.AllowReceiving && x.StoreCode != stCode))
                        st.AllowReceiving = false;
                }
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                NewSt.AllowReceiving = chk.Checked;
                _db.SaveChanges();
                LoadStores();
                PopMessage("Store Updated");
            }
        }

        protected void chkIsWip_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                NewSt.IsWip = chk.Checked;
                _db.SaveChanges();
                LoadStores();
                PopMessage("Store Updated");
            }
        }

        protected void chkIsReject_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (chk.Checked)
                {
                    // Single reject store per company - clear it off every other store.
                    foreach (var st in _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.IsRejectStore && x.StoreCode != stCode))
                        st.IsRejectStore = false;
                }
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                NewSt.IsRejectStore = chk.Checked;
                _db.SaveChanges();
                LoadStores();
                PopMessage("Store Updated");
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

    }
}