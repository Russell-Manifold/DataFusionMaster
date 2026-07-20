using SBMS.Classes;
using SBMS.Models;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCountsM : MobileBasePage
    {
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private int CountID
        {
            get { return ViewState["CountID"] != null ? (int)ViewState["CountID"] : 0; }
            set { ViewState["CountID"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            SessionValidator.ValidateUserSession(CurrentUser);

            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false);
                return;
            }

            lblUsername.Text = CurrentUser.UserName;

            if (!IsPostBack)
            {
                LoadCountDropdown();
            }
        }

        private void LoadCountDropdown()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var counts = db.StockCountMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID)
                    .OrderByDescending(x => x.CtCreateDate)
                    .ToList();

                if (chkShowClosed.Checked)
                    counts = counts.Where(x => x.ClosedOff == true).ToList();
                else
                    counts = counts.Where(x => x.ClosedOff == false || x.ClosedOff == null).ToList();

                ddCounts.Items.Clear();
                ddCounts.Items.Add(new ListItem("— Select Count —", ""));
                foreach (var c in counts)
                    ddCounts.Items.Add(new ListItem(
                        $"{c.CtDescription} ({c.CtCreateDate:dd MMM yyyy})", c.StCntID.ToString()));

                if (CountID > 0)
                {
                    var item = ddCounts.Items.FindByValue(CountID.ToString());
                    if (item != null) item.Selected = true;
                }
            }
        }

        private void LoadStores()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = db.Stores
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true
                             && x.StoreCode != "CoR" && x.StoreCode != "CoD")
                    .OrderBy(x => x.StoreDescript)
                    .ToList();

                ddStore.Items.Clear();
                ddStore.Items.Add(new ListItem("— Select Store —", ""));
                foreach (var s in stores)
                    ddStore.Items.Add(new ListItem($"{s.StoreCode} — {s.StoreDescript}", s.StoreCode));
            }
        }

        protected void lbtnLoadCount_Click(object sender, EventArgs e)
        {
            if (ddCounts.SelectedIndex <= 0 || string.IsNullOrEmpty(ddCounts.SelectedValue))
            {
                pnlCountHeader.Visible = false;
                return;
            }

            if (int.TryParse(ddCounts.SelectedValue, out int id))
            {
                CountID = id;
                LoadCountHeader();
                LoadStores();
                pnlCountHeader.Visible = true;
            }
        }

        protected void chkShowClosed_CheckedChanged(object sender, EventArgs e)
        {
            CountID = 0;
            pnlCountHeader.Visible = false;
            LoadCountDropdown();
        }

        private void LoadCountHeader()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = db.StockCountMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == CountID);

                if (count == null) return;

                lblCountRef.Text    = count.CtDescription ?? "-";
                lblCountDate.Text   = count.CtCreateDate?.ToString("dd MMM yyyy") ?? "-";
                lblCountStatus.Text = count.ClosedOff == true ? "(Closed)" : "(Open)";
                lblCountStatus.ForeColor = count.ClosedOff == true
                    ? System.Drawing.Color.Gray : System.Drawing.Color.Green;

                if (count.CreatedBy.HasValue)
                {
                    var role = db.RolesMasters
                        .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == count.CreatedBy.Value);
                    lblCreatedBy.Text = role?.RoleName ?? count.CreatedBy.ToString();
                }
                else
                {
                    lblCreatedBy.Text = "-";
                }

                int totalLines = db.StockCountLines
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.CountID == CountID)
                    .Count();

                int finishedLines = db.StockCountLines
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.CountID == CountID && x.LineFinished == true)
                    .Count();

                lblLineCount.Text = finishedLines + " / " + totalLines;
            }
        }

        protected void lbtnStartCounting_Click(object sender, EventArgs e)
        {
            if (ddStore.SelectedIndex <= 0 || string.IsNullOrEmpty(ddStore.SelectedValue))
            {
                AlertHelper.ShowSweetAlert(this, "Please select a store before counting.");
                return;
            }

            // Closed-off counts have already posted their variances - they can be
            // viewed via the Closed filter but never counted into again.
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                bool closedOff = db.StockCountMasters.Any(x =>
                    x.CompanyID == CurrentUser.CoID && x.StCntID == CountID && x.ClosedOff == true);
                if (closedOff)
                {
                    AlertHelper.ShowSweetAlert(this, "This count is closed off - it can no longer be counted.");
                    return;
                }
            }

            string store = ddStore.SelectedValue;

            if (CurrentUser != null)
            {
                Response.Redirect(
                    "~/SBMSMobile/StockCountLineM.aspx?cntid=" + CountID + "&store=" + store, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        // ── Navigation ─────────────────────────────────────────────────────────────
        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/DashboardM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/DashboardM.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
