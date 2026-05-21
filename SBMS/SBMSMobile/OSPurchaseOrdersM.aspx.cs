using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class OSPurchaseOrdersM : BasePage
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

        // ── Lifecycle ──────────────────────────────────────────────────────────
        protected async void Page_Load(object sender, EventArgs e)
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
                ApiUrlCall api = new ApiUrlCall();
                JObject result = await api.LoadPurchaseOrders(CurrentUser);
                if (result != null && result["error"] != null)
                {
                    api.LogErrorToFile(
                        $"CoID: {CurrentUser.CoID} OSPurchaseOrdersM Error - {result["error"]}");
                    AlertHelper.ShowSweetAlert(this, result["error"].ToString(), "error");
                }

                LoadStatusDropdown();
                BindCards();
            }
        }

        // ── Data ───────────────────────────────────────────────────────────────
        private List<GetListOfDocHeadersByType_Result> GetFilteredPOs()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                string find = txtfind.Text.Trim();
                string selectedStatus = ddStatus.SelectedValue;
                var query = db.GetListOfDocHeadersByType(CurrentUser.CoID, 1).AsQueryable();

                if (find.Length > 1)
                    query = query.Where(x =>
                        x.DocumentNumber.Contains(find) ||
                        x.CustSupName.Contains(find));

                if (selectedStatus == "Completed")
                {
                    query = query.Where(x => x.Complete == true);
                }
                else if (ddStatus.SelectedIndex > 0)
                {
                    query = query.Where(x => x.Status == selectedStatus && x.Complete == false);
                }
                else if (find.Length <= 1)
                {
                    query = query.Where(x => x.Complete == false && x.Status != "Cancelled");
                }
                else
                {
                    query = query.Where(x => x.Status != "Cancelled");
                }

                return (SortAsc
                    ? query.OrderBy(x => x.DocumentNumber)
                    : query.OrderByDescending(x => x.DocumentNumber)).ToList();
            }
        }

        private void BindCards()
        {
            lbtnSort.Text = SortAsc ? "&#8593;" : "&#8595;";
            var data = GetFilteredPOs();
            rptPOs.DataSource = data;
            rptPOs.DataBind();
            lblEmpty.Visible = data.Count == 0;
        }

        private void LoadStatusDropdown()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var statuses = db.DocHeaders
                    .Where(d => d.DocType == 1 && d.CompanyID == CurrentUser.CoID)
                    .Select(d => d.Status)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                ddStatus.DataSource = statuses;
                ddStatus.DataBind();
                ddStatus.Items.Insert(0, "-All-");
                ddStatus.Items.Insert(1, "Completed");
            }
        }

        // ── Events ─────────────────────────────────────────────────────────────
        protected void lbtnFind_Click(object sender, EventArgs e) => BindCards();

        protected void ddStatus_SelectedIndexChanged(object sender, EventArgs e) => BindCards();

        protected void lbtnSort_Click(object sender, EventArgs e)
        {
            SortAsc = !SortAsc;
            BindCards();
        }

        protected void lbtnPO_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string docGuid = btn.CommandArgument;
            Response.Redirect("~/SBMSMobile/ReceivingM.aspx?docid=" + docGuid, false);
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

        // ── Badge colour per status ─────────────────────────────────────────────
        protected void rptPOs_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var row = (GetListOfDocHeadersByType_Result)e.Item.DataItem;
            HtmlGenericControl badge = (HtmlGenericControl)e.Item.FindControl("spnBadge");
            badge.InnerText = row.Status ?? "";

            string css = "mob-badge ";
            switch (row.Status?.ToLower())
            {
                case "invoiced":    css += "mob-badge-invoiced"; break;
                case "open":        css += "mob-badge-open";     break;
                case "confirmed":
                case "pending":
                case "overdue":
                    css += row.Started == true ? "mob-badge-started" : "mob-badge-open";
                    break;
                default:
                    css += row.Complete == true ? "mob-badge-complete" : "mob-badge-other";
                    break;
            }
            badge.Attributes["class"] = css;
        }
    }
}