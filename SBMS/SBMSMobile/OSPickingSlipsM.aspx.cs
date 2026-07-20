using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class OSPickingSlipsM : MobileBasePage
    {
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private bool SortAsc
        {
            get { return ViewState["SortAsc"] == null || (bool)ViewState["SortAsc"]; }
            set { ViewState["SortAsc"] = value; }
        }

        private class PSListRow
        {
            public int      PSID           { get; set; }
            public string   PSIntNumber    { get; set; }
            public string   CustSupName    { get; set; }
            public string   DocumentNumber { get; set; }
            public DateTime? DueDelDate    { get; set; }
            public string   PSStatus       { get; set; }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────
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
                LoadStatusDropdown();
                BindCards();
            }
        }

        // ── Data ───────────────────────────────────────────────────────────────
        private List<PSListRow> GetFilteredSlips()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // GetAllActivePickingSlips is a stored proc (ObjectResult), so join in memory
                var active = db.GetAllActivePickingSlips(CurrentUser.CoID).ToList();

                var psIds    = active.Select(x => x.PSID).ToList();
                var statuses = db.PickingSlipMasters
                    .Where(x => psIds.Contains(x.PSID))
                    .ToDictionary(x => x.PSID, x => x.PSStatus ?? "");

                string find         = txtfind.Text.Trim().ToLower();
                string statusFilter = ddStatus.SelectedIndex > 0 ? ddStatus.SelectedValue : null;

                IEnumerable<PSListRow> rows = active.Select(a => new PSListRow
                {
                    PSID           = a.PSID,
                    PSIntNumber    = a.PSIntNumber,
                    CustSupName    = a.CustSupName,
                    DocumentNumber = a.DocumentNumber,
                    DueDelDate     = a.DueDelDate,
                    PSStatus       = statuses.TryGetValue(a.PSID, out string st) ? st : ""
                });

                if (find.Length > 1)
                    rows = rows.Where(x =>
                        (x.PSIntNumber    != null && x.PSIntNumber.ToLower().Contains(find))    ||
                        (x.CustSupName    != null && x.CustSupName.ToLower().Contains(find))    ||
                        (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(find)));

                if (!string.IsNullOrEmpty(statusFilter))
                    rows = rows.Where(x => x.PSStatus == statusFilter);

                return (SortAsc
                    ? rows.OrderBy(x => x.PSIntNumber)
                    : rows.OrderByDescending(x => x.PSIntNumber)).ToList();
            }
        }

        private void BindCards()
        {
            lbtnSort.Text = SortAsc ? "&#8593;" : "&#8595;";
            var data = GetFilteredSlips();
            rptSlips.DataSource = data;
            rptSlips.DataBind();
            lblEmpty.Visible = data.Count == 0;
        }

        private void LoadStatusDropdown()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var statuses = db.PickSlipProcesses
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.PSActive == true)
                    .OrderBy(x => x.Seq)
                    .Select(x => x.PSName)
                    .ToList();

                ddStatus.DataSource = statuses;
                ddStatus.DataBind();
                ddStatus.Items.Insert(0, "-All-");
            }
        }

        // ── Events ─────────────────────────────────────────────────────────────
        protected void lbtnFind_Click(object sender, EventArgs e) => BindCards();

        protected void lbtnSort_Click(object sender, EventArgs e)
        {
            SortAsc = !SortAsc;
            BindCards();
        }

        protected void ddStatus_SelectedIndexChanged(object sender, EventArgs e) => BindCards();

        protected void lbtnPS_Click(object sender, EventArgs e)
        {
            LinkButton btn  = (LinkButton)sender;
            string     psid = btn.CommandArgument;
            Response.Redirect("~/SBMSMobile/PickingSlipM.aspx?psid=" + psid, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/DashboardM.aspx", false);
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/DashboardM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/DashboardM.aspx", false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD);
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ── Badge colour per PS status ──────────────────────────────────────────
        protected void rptSlips_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var row   = (PSListRow)e.Item.DataItem;
            var badge = (HtmlGenericControl)e.Item.FindControl("spnBadge");
            badge.InnerText = row.PSStatus ?? "";

            string css = "mob-badge ";
            switch (row.PSStatus?.ToLower())
            {
                case "complete":               css += "mob-badge-complete";  break;
                case "picking":                css += "mob-badge-open";      break;
                case "packing":                css += "mob-badge-started";   break;
                case "delivery":
                case "dispatched":             css += "mob-badge-invoiced";  break;
                default:                       css += "mob-badge-other";     break;
            }
            badge.Attributes["class"] = css;
        }
    }
}
