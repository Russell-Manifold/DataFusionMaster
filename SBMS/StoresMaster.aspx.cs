using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StoresMaster : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (!IsPostBack)
            {
                // Consignment column only when the company switch (Company Config) is on. The Sage
                // customer list is read once and kept in Session for 10 minutes (a company can have
                // thousands of customers; each read is several Sage calls against the per-company budget).
                ConsignColumn.Visible = CurrentUser.UseConsignment;
                var cached = _customerList;
                if (!CurrentUser.UseConsignment || (cached != null && (DateTime.Now - cached.ReadAt).TotalMinutes < 10))
                {
                    LoadStores();
                }
                else
                RegisterAsyncTask(new PageAsyncTask(async () =>
                {
                    SageCustomerList list;
                    try { list = await SageCustomers.GetAllAsync(CurrentUser); }
                    catch (Exception ex)
                    {
                        new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser.CoID} StoresMaster customer load failed - {ex}");
                        list = new SageCustomerList { Complete = false, ReadAt = DateTime.MinValue };
                    }
                    _customerList = list;
                    if (!list.Complete)
                        PopMessage(list.Customers.Count == 0
                            ? "Could not read the customer list from Sage right now, so consignment customers cannot be set. Existing links are kept. Refresh the page to try again."
                            : "Sage stopped answering part-way through the customer list, so some customers may be missing from the dropdowns. Refresh the page to try again.");
                    LoadStores();
                }));
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

                SetBinFormatHint();
            }
        }

        // Session, not ViewState: a company can have thousands of customers. Keyed by company so a
        // user who switches company never sees another company's customers.
        private SageCustomerList _customerList
        {
            get { return CurrentUser == null ? null : Session["SageCustomers_" + CurrentUser.CoID] as SageCustomerList; }
            set { Session["SageCustomers_" + CurrentUser.CoID] = value; }
        }
        private List<SageCustomerDto> _customers { get { return _customerList?.Customers ?? new List<SageCustomerDto>(); } }
        private DataControlField ConsignColumn
        {
            get { return GridStores.Columns.Cast<DataControlField>().First(c => c.HeaderText.StartsWith("Consignment")); }
        }

        // The dropdown is not kept in ViewState (thousands of options x every store row); its items are
        // rebuilt here on every request, including postbacks, so the posted selection still matches an item.
        private const string KeepMarker = " (saved - not in Sage list)";
        protected void GridStores_RowCreated(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow || CurrentUser == null) return;   // session gone: BasePage already redirected
            var ddl = (DropDownList)e.Row.FindControl("ddlConsignCust");
            if (ddl == null) return;
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem("- Not a consignment store -", ""));
            foreach (var c in _customers) ddl.Items.Add(new ListItem(c.Name, c.ID.ToString()));
            // On a postback the saved customer may not be in today's Sage list (inactive, or the list
            // failed to load). Keep it selectable from the posted value so Save never clears the link.
            if (IsPostBack)
            {
                string posted = Request.Form[ddl.UniqueID];
                if (!string.IsNullOrEmpty(posted) && ddl.Items.FindByValue(posted) == null)
                    ddl.Items.Add(new ListItem("Customer " + posted + KeepMarker, posted));
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
                var ddl = (DropDownList)e.Row.FindControl("ddlConsignCust");
                var hf = (HiddenField)e.Row.FindControl("hfConsignCustId");
                if (ddl != null)
                {
                    string cur = hf?.Value ?? "";
                    if (cur.Length > 0 && ddl.Items.FindByValue(cur) == null)
                    {
                        // Saved customer not in today's Sage list (inactive / list failed): keep it selectable.
                        var st = e.Row.DataItem as Store;
                        ddl.Items.Add(new ListItem((st?.ConsignCustomer ?? "Customer " + cur) + KeepMarker, cur));
                    }
                    if (cur.Length > 0) ddl.SelectedValue = cur;
                }
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

        // Consignment store = a store at a customer's premises. Name + address print on the
        // transfer slip as a Consignment Delivery Note. Blank name = ordinary store.
        // Picking a customer fills the address from Sage; it stays editable (a customer can have
        // several DCs). Nothing is saved until Save is clicked.
        protected void ddlConsignCust_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ddl = (DropDownList)sender;
            // The dropdown is rebuilt on every postback with nothing selected, so ASP.NET raises this
            // on EVERY postback for every linked row. Only act when the user actually changed THIS
            // dropdown; otherwise a Save (or any other click) would overwrite an edited address.
            if (Request.Form["__EVENTTARGET"] != ddl.UniqueID) return;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            var txtAddr = (TextBox)row.FindControl("txtConsignAddr");
            long id;
            var cust = long.TryParse(ddl.SelectedValue, out id) ? _customers.FirstOrDefault(c => c.ID == id) : null;
            if (txtAddr != null) txtAddr.Text = cust?.DeliveryAddress ?? "";
        }

        protected void lbtnConsignSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            string stCode = row.Cells[0].Text.ToString();
            var ddl = (DropDownList)row.FindControl("ddlConsignCust");
            string addr = ((TextBox)row.FindControl("txtConsignAddr")).Text.Trim();
            long custId;
            bool isConsign = long.TryParse(ddl.SelectedValue, out custId) && custId > 0;
            bool keepName = isConsign && ddl.SelectedItem.Text.EndsWith(KeepMarker);   // customer not in today's list: keep the saved name
            string name = isConsign ? ddl.SelectedItem.Text.Replace(KeepMarker, "").Trim() : null;
            if (name != null && name.Length > 100) name = name.Substring(0, 100);
            if (addr.Length > 400) addr = addr.Substring(0, 400);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                Store st = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                if (st == null) { PopMessage("Store not found"); return; }
                st.ConsignCustomerId = isConsign ? custId : (long?)null;
                if (!keepName || st.ConsignCustomerId != custId) st.ConsignCustomer = name;
                st.ConsignAddress = isConsign && addr.Length > 0 ? addr : null;
                _db.SaveChanges();
                LoadStores();
                PopMessage(isConsign ? "Consignment customer saved - transfers into this store print as a Consignment Delivery Note" : "Consignment customer cleared - normal store");
            }
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
                // A company may receive into as many stores as it likes - the web Receiving
                // screen has always offered them as a dropdown. This briefly cleared the flag
                // off every other store (added with the scanner work, so mobile could assume a
                // single holding store); that broke multi-warehouse customers, who could only
                // ever have one warehouse ticked at a time.
                Store NewSt = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stCode).FirstOrDefault();
                if (NewSt == null) { LoadStores(); return; }
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