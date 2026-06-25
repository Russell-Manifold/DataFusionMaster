using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    // Mobile/scanner PUT-AWAY.
    // Receiving (the GRV → Sage) is done on the web. This page only relocates stock that has
    // already been received into the holding store (the single store flagged AllowReceiving)
    // out to its destination — any AllowPicking bin, IsWip store, or the IsRejectStore.
    // Each tap is an immediate internal transfer (TRF) — no Sage, no PO, no finalise.
    public partial class ReceivingM : BasePage
    {
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        // The single AllowReceiving store = the holding store we put away FROM.
        private string SourceStoreCode
        {
            get { return ViewState["SrcCode"] as string; }
            set { ViewState["SrcCode"] = value; }
        }
        private long SourceStoreID
        {
            get { return ViewState["SrcId"] != null ? (long)ViewState["SrcId"] : 0; }
            set { ViewState["SrcId"] = value; }
        }

        // Highlighted line after a scan: "<ItemID>|<LotNumber>".
        protected string MatchedKey
        {
            get { return ViewState["MatchedKey"] as string; }
            set { ViewState["MatchedKey"] = value; }
        }

        // When set, the worklist is narrowed to this item only (after a scan) so the operator
        // can put away just the scanned item; cleared on success / clear → full list returns.
        protected string MatchedItemCode
        {
            get { return ViewState["MatchedItemCode"] as string; }
            set { ViewState["MatchedItemCode"] = value; }
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

            // Resolve the holding store on every load so the put-away handlers have it.
            if (!ResolveSourceStore())
            {
                lblPONum.Text = "No receiving store configured";
                SetFeedback(false, "&#9888; No store is flagged 'Allow Receiving'. Set one up before put-away.");
                txtBarcode.Enabled = false;
                return;
            }
            lblPONum.Text = SourceStoreCode + " - " + (ViewState["SrcName"] as string ?? "");

            if (!IsPostBack)
            {
                // Fresh visit → start an empty "put away this session" list.
                Session["PutAwaySession"] = new List<PutAwayDone>();
                BindLines();
                BindDone();
            }
        }

        private bool ResolveSourceStore()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var s = db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID
                                                   && x.AllowReceiving == true
                                                   && x.StoreActive == true);
                if (s == null) return false;
                SourceStoreCode      = s.StoreCode;
                SourceStoreID        = s.StoreID;
                ViewState["SrcName"] = s.StoreDescript;
                return true;
            }
        }

        // ── Data ──────────────────────────────────────────────────────────────

        // Stock sitting in the holding store, by item + lot, qty > 0 = the put-away worklist.
        private List<GetOpeningBalancesAllStores_Result> GetHoldingLines()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                return db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                    .Where(r => r.StoreCode == SourceStoreCode && (r.QOH ?? 0) > 0)
                    .OrderBy(r => r.ItemCode).ThenBy(r => r.LotNumber)
                    .ToList();
            }
        }

        private void BindLines()
        {
            var all = GetHoldingLines();
            lblLineCount.Text = all.Count.ToString();   // total still in holding

            // After a scan, show only the scanned item so the operator can't put away the
            // wrong line by scanning a location onto a different card.
            var shown = string.IsNullOrEmpty(MatchedItemCode)
                ? all
                : all.Where(r => r.ItemCode == MatchedItemCode).ToList();

            lblEmpty.Visible    = all.Count == 0;
            rptLines.DataSource = shown;
            rptLines.DataBind();
        }

        // ── "Put away this session" running confirmation (held in Session, clears on revisit) ──

        [Serializable]
        public class PutAwayDone
        {
            public string ItemCode { get; set; }
            public decimal Qty { get; set; }
            public string Dest { get; set; }
            public string TimeText { get; set; }
        }

        private List<PutAwayDone> DoneList
        {
            get
            {
                var l = Session["PutAwaySession"] as List<PutAwayDone>;
                if (l == null) { l = new List<PutAwayDone>(); Session["PutAwaySession"] = l; }
                return l;
            }
        }

        private void RecordDone(string itemCode, decimal qty, string dest)
        {
            DoneList.Insert(0, new PutAwayDone
            {
                ItemCode = itemCode,
                Qty      = qty,
                Dest     = dest,
                TimeText = DateTime.Now.ToString("HH:mm")
            });
        }

        private void BindDone()
        {
            var l = DoneList;
            pnlDone.Visible    = l.Count > 0;
            lblDoneCount.Text  = l.Count.ToString();
            rptDone.DataSource = l;
            rptDone.DataBind();
        }

        // Used by the card markup to flag the scanned line.
        protected bool IsMatched(object itemId, object lot)
        {
            return (Convert.ToString(itemId) + "|" + Convert.ToString(lot)) == MatchedKey;
        }

        // ── Scan: highlight the holding line(s) for the scanned item ──

        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            string raw = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(raw)) { ClearFeedback(); return; }

            GetOneItemFromBarcode_Result item = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                item = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();

            if (item == null)
            {
                SetFeedback(false, $"&#128683; Barcode not recognised: {raw}");
                MatchedKey = null; MatchedItemCode = null; BindLines(); return;
            }

            var holding = GetHoldingLines().Where(r => r.ItemCode == item.Code).ToList();
            if (!holding.Any())
            {
                SetFeedback(false, $"&#9888; <strong>{item.Code}</strong> has nothing in {SourceStoreCode} to put away.");
                MatchedKey = null; MatchedItemCode = null; BindLines(); return;
            }

            var first = holding.First();
            MatchedKey = Convert.ToString(first.ItemID) + "|" + Convert.ToString(first.LotNumber);
            MatchedItemCode = item.Code;   // narrow the list to just this item
            SetFeedback(true, holding.Count > 1
                ? $"&#10003; <strong>{item.Code}</strong> &mdash; {holding.Count} lots in holding."
                : $"&#10003; <strong>{item.Code}</strong> &mdash; {item.Description}");
            txtBarcode.Text = string.Empty;
            BindLines();
        }

        protected void lbtnClearScan_Click(object sender, EventArgs e)
        {
            txtBarcode.Text = string.Empty;
            MatchedKey = null;
            MatchedItemCode = null;
            ClearFeedback();
            BindLines();
        }

        // ── Put-away one line: internal TRF holding → destination ──

        protected void lbtnPutAway_Click(object sender, EventArgs e)
        {
            string toastMsg = null;
            var item = ((LinkButton)sender).NamingContainer as RepeaterItem;
            if (item == null) return;

            var hfItemId = (HiddenField)item.FindControl("hfItemId");
            var hfLot    = (HiddenField)item.FindControl("hfLot");
            var txtQty   = (TextBox)item.FindControl("txtQty");
            var txtLoc   = (TextBox)item.FindControl("txtLoc");
            if (hfItemId == null) return;

            if (!long.TryParse(hfItemId.Value, out long itemId)) return;
            string lot = string.IsNullOrEmpty(hfLot?.Value) ? null : hfLot.Value;

            if (!decimal.TryParse((txtQty?.Text ?? "").Trim(), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
            {
                SetFeedback(false, "&#9888; Enter a valid quantity.");
                return;
            }

            string locCode = (txtLoc?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(locCode))
            {
                SetFeedback(false, "&#9888; Scan or enter a destination location.");
                return;
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Destination must be an active pick/WIP/reject store, not the holding store itself.
                var dest = db.Stores.FirstOrDefault(s => s.CompanyID == CurrentUser.CoID
                    && s.StoreCode == locCode
                    && s.StoreActive == true
                    && s.StoreCode != SourceStoreCode
                    && (s.AllowPicking || s.IsWip || s.IsRejectStore));
                if (dest == null)
                {
                    SetFeedback(false, $"&#9888; '{locCode}' is not a valid put-away location.");
                    return;
                }

                // Re-check current holding qty for this item + lot.
                var row = GetHoldingLines().FirstOrDefault(r => r.ItemID == itemId
                    && (r.LotNumber ?? "") == (lot ?? ""));
                if (row == null || (row.QOH ?? 0) <= 0)
                {
                    SetFeedback(false, "&#9888; That stock is no longer in holding.");
                    MatchedKey = null; MatchedItemCode = null; BindLines(); return;
                }
                if (qty > (row.QOH ?? 0))
                {
                    SetFeedback(false, $"&#9888; Only {(row.QOH ?? 0):0.##} in holding for that lot.");
                    return;
                }

                decimal cost = row.TotalUnitPriceExclInclAdd ?? row.PriceExclusive ?? 0m;

                // IN to destination
                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = 0,
                    DocumentType              = 4,
                    TransactionType           = "TRF",
                    ItemID                    = itemId,
                    ItemCode                  = row.ItemCode,
                    ItemDescription           = row.ItemDescription,
                    LotNumber                 = lot,
                    Unit                      = row.Unit,
                    FromID                    = SourceStoreID,
                    ToID                      = dest.StoreID,
                    Qty                       = qty,
                    PriceExclusive            = cost,
                    AdditionalCosts           = 0m,
                    TotalUnitPriceExclInclAdd = cost,
                    TotalLineValExcl          = cost * qty,
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = row.ItemCode + " Put-away " + qty + " to " + dest.StoreCode,
                    ExchRate                  = 1
                });
                db.SaveChanges();
                EnsureStoreLink(db, itemId, dest.StoreID);

                // OUT of holding (mirror)
                db.ItemTransactions.Add(new ItemTransaction
                {
                    CompanyID                 = CurrentUser.CoID,
                    DocumentID                = 0,
                    DocumentType              = 4,
                    TransactionType           = "TRF",
                    ItemID                    = itemId,
                    ItemCode                  = row.ItemCode,
                    ItemDescription           = row.ItemDescription,
                    LotNumber                 = lot,
                    Unit                      = row.Unit,
                    ToID                      = SourceStoreID,
                    FromID                    = dest.StoreID,
                    Qty                       = qty * -1,
                    PriceExclusive            = cost,
                    AdditionalCosts           = 0m,
                    TotalUnitPriceExclInclAdd = cost,
                    TotalLineValExcl          = cost * (qty * -1),
                    TransactionDate           = DateTime.Now,
                    ByRoleID                  = CurrentUser.RoleID,
                    TransactionReference      = row.ItemCode + " Put-away " + qty + " from " + SourceStoreCode,
                    ExchRate                  = 1
                });

                db.SaveChanges();

                RecordDone(row.ItemCode, qty, dest.StoreCode);
                toastMsg = $"&#10003; {qty:0.##} {row.ItemCode} &#8594; {dest.StoreCode}";
            }

            MatchedKey = null;
            MatchedItemCode = null;             // re-populate the full holding list
            SetFeedback(true, $"&#10003; {qty:0.##} put away to {locCode}.");
            BindLines();
            BindDone();

            if (!string.IsNullOrEmpty(toastMsg))
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "putToast",
                    $"showToast('{JsEscape(toastMsg)}');", true);
        }

        private static string JsEscape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'")
                            .Replace("\r", "").Replace("\n", "");
        }

        private void EnsureStoreLink(SBMSEntities db, long itemId, long storeId)
        {
            bool exists = db.ItemStoreLinkMasters.Any(x => x.CompanyID == CurrentUser.CoID
                && x.StoreID == (int?)storeId && x.ItemID == itemId);
            if (!exists)
                db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                {
                    CompanyID = CurrentUser.CoID,
                    ItemID    = itemId,
                    StoreID   = (int?)storeId,
                    Active    = true
                });
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        protected void lbtnTopBack_Click(object sender, EventArgs e) { GoDashboard(); }
        protected void lbtnTopHome_Click(object sender, EventArgs e) { GoDashboard(); }

        private void GoDashboard()
        {
            Response.Redirect(CurrentUser != null
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

        // ── Feedback ───────────────────────────────────────────────────────────

        private void SetFeedback(bool ok, string html)
        {
            lblScanFeedback.Text     = html;
            lblScanFeedback.CssClass = ok ? "mob-feedback found" : "mob-feedback notfound";
        }

        private void ClearFeedback()
        {
            lblScanFeedback.Text     = string.Empty;
            lblScanFeedback.CssClass = "mob-feedback";
        }
    }
}
