using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCountLineM : BasePage
    {
        // ── Session/ViewState helpers ──────────────────────────────────────────────
        private new UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private int CountID
        {
            get { return ViewState["CountID"] != null ? (int)ViewState["CountID"] : 0; }
            set { ViewState["CountID"] = value; }
        }

        private string SelectedStore
        {
            get { return ViewState["SelectedStore"] as string ?? ""; }
            set { ViewState["SelectedStore"] = value; }
        }

        public long MatchedLineID
        {
            get { return ViewState["MatchedLineID"] != null ? (long)ViewState["MatchedLineID"] : 0L; }
            set { ViewState["MatchedLineID"] = value; }
        }

        private HashSet<long> CountedLineIDs
        {
            get
            {
                if (Session["SCLineCountedIDs"] == null)
                    Session["SCLineCountedIDs"] = new HashSet<long>();
                return (HashSet<long>)Session["SCLineCountedIDs"];
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────
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
                Session.Remove("SCLineCountedIDs");

                if (!int.TryParse(Request.QueryString["cntid"], out int cntid) || cntid == 0)
                {
                    Response.Redirect("~/SBMSMobile/StockCountsM.aspx", false);
                    return;
                }

                CountID       = cntid;
                SelectedStore = Request.QueryString["store"] ?? "";

                if (string.IsNullOrEmpty(SelectedStore))
                {
                    Response.Redirect("~/SBMSMobile/StockCountsM.aspx", false);
                    return;
                }

                LoadCountHeader();
                LoadCategoryDropdown();
                BindLines();
            }

            lblStoreBadge.Text  = SelectedStore;
            lblStoreName.Text   = SelectedStore;
        }

        // ── Count header ───────────────────────────────────────────────────────────
        private void LoadCountHeader()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = db.StockCountMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == CountID);

                if (count == null) return;

                lblCountRefTop.Text = count.CtDescription ?? "-";
                lblCountRef.Text    = count.CtDescription ?? "-";
                lblCountDate.Text   = count.CtCreateDate?.ToString("dd MMM yyyy") ?? "-";
                lblCountStatus.Text = count.ClosedOff == true ? "(Closed)" : "(Open)";
                lblCountStatus.ForeColor = count.ClosedOff == true
                    ? System.Drawing.Color.Gray : System.Drawing.Color.Green;
            }
        }

        private void LoadCategoryDropdown()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var categs = db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true)
                    .Select(x => x.CategoryDescript)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                ddCategory.Items.Clear();
                ddCategory.Items.Add(new ListItem("— Category —", ""));
                foreach (var cat in categs)
                    ddCategory.Items.Add(new ListItem(cat, cat));
            }
        }

        // ── Line data ──────────────────────────────────────────────────────────────
        private List<GetStckCountDetails_Result> GetFilteredLines()
        {
            if (CountID == 0) return new List<GetStckCountDetails_Result>();

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = db.GetStckCountDetails(CurrentUser.CoID, CountID)
                    .Where(x => x.StoreCode == SelectedStore)
                    .ToList();

                string search = (txtSearch.Text ?? "").Trim().ToLower();

                if (ddCategory.SelectedIndex > 0)
                {
                    string cat = ddCategory.SelectedValue;
                    lines = lines.Where(x => x.CategoryDescript == cat).ToList();
                }

                if (search.Length > 0)
                {
                    lines = lines.Where(x =>
                        (x.ItemCode != null && x.ItemCode.ToLower().Contains(search)) ||
                        (x.ItemDescription != null && x.ItemDescription.ToLower().Contains(search)))
                        .ToList();
                }

                // Load CountExpression from StockCountLines for each line
                var lineIds = lines.Select(l => l.CtLineID).ToList();
                var expressions = db.StockCountLines
                    .Where(x => lineIds.Contains(x.CtLineID))
                    .ToDictionary(x => x.CtLineID, x => x.CountExpression ?? "");

                ViewState["LineExpressions"] = expressions;

                // Load barcodes for all items on this page
                var itemCodes = lines.Select(l => l.ItemCode).Distinct().ToList();
                var itemIdToCode = db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && itemCodes.Contains(x.Code))
                    .ToDictionary(x => x.ID, x => x.Code);

                var allItemIds = itemIdToCode.Keys.ToList();
                var barcodeLinks = new Dictionary<string, List<string>>();
                if (allItemIds.Count > 0)
                {
                    var links = db.ItemBarCodeLinks
                        .Where(x => x.CompanyID == CurrentUser.CoID && allItemIds.Contains(x.ItemID.Value))
                        .ToList();

                    foreach (var link in links)
                    {
                        if (link.ItemID.HasValue && itemIdToCode.TryGetValue(link.ItemID.Value, out string code))
                        {
                            if (!barcodeLinks.ContainsKey(code))
                                barcodeLinks[code] = new List<string>();
                            barcodeLinks[code].Add(link.BarCode ?? "");
                        }
                    }
                }

                ViewState["LineBarcodes"] = barcodeLinks;

                return lines
                    .OrderBy(x => x.LineFinished == true ? 1 : 0)
                    .ThenBy(x => x.ItemCode)
                    .ToList();
            }
        }

        private void BindLines()
        {
            if (CountID == 0) return;

            var lines = GetFilteredLines();
            int unfinished = lines.Count(l => l.LineFinished != true && !CountedLineIDs.Contains(l.CtLineID));

            lblLineCount.Text = unfinished.ToString();
            lblEmpty.Visible  = lines.Count == 0;
            if (lines.Count == 0)
                lblEmpty.Text = "No items found for store " + SelectedStore + ".";

            rptLines.DataSource = lines;
            rptLines.DataBind();
        }

        // ── Repeater binding ───────────────────────────────────────────────────────
        protected void rptLines_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var line = (GetStckCountDetails_Result)e.Item.DataItem;

            // Get fresh data from DB for this line (Count1Qty, Count2Qty, FinalQty, Expression, LineFinished)
            long ctLineId = line.CtLineID;
            StockCountLine dbLine = null;
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                dbLine = db.StockCountLines.FirstOrDefault(x => x.CtLineID == ctLineId);

            bool isFinished    = dbLine != null && dbLine.LineFinished;
            bool hasCount1     = dbLine != null && dbLine.Count1Qty.HasValue && dbLine.Count1Qty.Value > 0;
            bool hasCount2     = dbLine != null && dbLine.Count2Qty.HasValue && dbLine.Count2Qty.Value > 0;
            bool needsRecount  = hasCount1 && !isFinished;

            // Green "Counted" badge when finished
            var countedBadge = (Label)e.Item.FindControl("lblCountedBadge");
            if (countedBadge != null) countedBadge.Visible = isFinished;

            // Amber "Count Again" badge when first count done but not finished
            var againBadge = (Label)e.Item.FindControl("lblCountAgainBadge");
            if (againBadge != null) againBadge.Visible = needsRecount;

            // Red "Final Count" badge when both counts 1 & 2 missed QOH (forced count 3)
            var finalBadge = (Label)e.Item.FindControl("lblFinalCountBadge");
            if (finalBadge != null)
            {
                decimal qoh = dbLine.QtyOnHand ?? 0;
                finalBadge.Visible = isFinished && hasCount1 && hasCount2 && dbLine.Count1Qty != qoh && dbLine.Count2Qty != qoh;
            }

            // Count 1 info panel
            var pnlC1 = (Panel)e.Item.FindControl("pnlCount1Info");
            var lblC1 = (Label)e.Item.FindControl("lblCount1Qty");
            if (pnlC1 != null && hasCount1)
            {
                pnlC1.Visible = true;
                if (lblC1 != null) lblC1.Text = (dbLine.Count1Qty ?? 0).ToString("0.##");
            }

            // Count 2 info panel
            var pnlC2 = (Panel)e.Item.FindControl("pnlCount2Info");
            var lblC2 = (Label)e.Item.FindControl("lblCount2Qty");
            if (pnlC2 != null && hasCount2)
            {
                pnlC2.Visible = true;
                if (lblC2 != null) lblC2.Text = (dbLine.Count2Qty ?? 0).ToString("0.##");
            }

            // Expression
            var lblExpr = (Label)e.Item.FindControl("lblCountExpr");
            if (lblExpr != null && dbLine != null && !string.IsNullOrEmpty(dbLine.CountExpression))
                lblExpr.Text = "Count: " + dbLine.CountExpression;

            // Recount button — always visible once any count has been entered
            var recountBtn = (LinkButton)e.Item.FindControl("lbtnRecount");
            if (recountBtn != null)
                recountBtn.Visible = hasCount1;

            // Barcode chips
            var lblBarcodes = (Label)e.Item.FindControl("lblBarcodes");
            if (lblBarcodes != null)
            {
                var barcodeDict = ViewState["LineBarcodes"] as Dictionary<string, List<string>>;
                string itemCode = line.ItemCode ?? "";
                if (barcodeDict != null && barcodeDict.TryGetValue(itemCode, out var barcodes) && barcodes.Count > 0)
                {
                    var chips = new System.Text.StringBuilder();
                    foreach (var barcode in barcodes)
                    {
                        string chip = "<span class='mob-barcode-chip'>" +
                            System.Web.HttpUtility.HtmlEncode(barcode) + "</span>";
                        chips.Append(chip);
                    }
                    lblBarcodes.Text = chips.ToString();
                }
            }
        }

        protected void rptLines_ItemCommand(object source, RepeaterCommandEventArgs e) { }

        // ── Filters ────────────────────────────────────────────────────────────────
        protected void ddCategory_SelectedIndexChanged(object sender, EventArgs e) => BindLines();

        protected void lbtnSearch_Click(object sender, EventArgs e) => BindLines();

        // ── Barcode scan ───────────────────────────────────────────────────────────
        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            string raw = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(raw)) { ClearFeedback(); return; }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                GetOneItemFromBarcode_Result barcodeItem = null;
                try
                {
                    barcodeItem = db.GetOneItemFromBarcode(CurrentUser.CoID, raw).FirstOrDefault();
                }
                catch { }

                if (barcodeItem == null)
                {
                    SetFeedback(false, "&#128683; Barcode not recognised: " + raw);
                    MatchedLineID = 0;
                    txtBarcode.Text = string.Empty;
                    BindLines();
                    return;
                }

                var matchedLine = db.StockCountLines
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID
                                      && x.CountID == CountID
                                      && x.ItemCode == barcodeItem.Code
                                      && x.StoreCode == SelectedStore);

                if (matchedLine == null)
                {
                    SetFeedback(false,
                        "&#9888; <strong>" + barcodeItem.Code + "</strong> (" +
                        barcodeItem.Description + ") is not on this count for store " + SelectedStore + ".");
                    MatchedLineID = 0;
                    txtBarcode.Text = string.Empty;
                    BindLines();
                    return;
                }

                // Block finished lines
                if (matchedLine.LineFinished == true)
                {
                    SetFeedback(false,
                        "&#128683; <strong>" + barcodeItem.Code + "</strong> has already been counted by " +
                        (matchedLine.CountBy ?? "another user") + ".");
                    MatchedLineID = 0;
                    txtBarcode.Text = string.Empty;
                    BindLines();
                    return;
                }

                // Block lines started by another user
                bool hasExistingCount = matchedLine.Count1Qty.HasValue && matchedLine.Count1Qty.Value > 0;
                if (hasExistingCount && !string.IsNullOrEmpty(matchedLine.CountBy) &&
                    matchedLine.CountBy != CurrentUser.UserName)
                {
                    SetFeedback(false,
                        "&#128683; <strong>" + barcodeItem.Code + "</strong> is already being counted by " +
                        matchedLine.CountBy + ".");
                    MatchedLineID = 0;
                    txtBarcode.Text = string.Empty;
                    BindLines();
                    return;
                }

                // Determine count round from current DB state
                bool hasCount1 = matchedLine.Count1Qty.HasValue && matchedLine.Count1Qty.Value > 0;
                bool hasCount2 = matchedLine.Count2Qty.HasValue && matchedLine.Count2Qty.Value > 0;
                string roundLabel = !hasCount1 ? "(Count 1)" : hasCount2 ? "(Count 3)" : "(Count 2)";
                int initQty = barcodeItem.QtyPerBarcode > 0 ? barcodeItem.QtyPerBarcode : 0;

                // If line already has a count, pre-fill with existing expression
                string existingExpr = matchedLine.CountExpression ?? "";
                if (!string.IsNullOrEmpty(existingExpr) && !hasCount1)
                {
                    hfCalcInitQty.Value = "0";
                    hfCalcExpression.Value = existingExpr;
                }
                else
                {
                    hfCalcExpression.Value = "";
                    hfCalcInitQty.Value = initQty.ToString();
                }

                hfCalcCtLineID.Value      = matchedLine.CtLineID.ToString();
                hfCalcRoundText.Value      = roundLabel;
                lblCalcRound.Text          = roundLabel;
                lblCalcItemCode.Text       = barcodeItem.Code;
                lblCalcItemDesc.Text       = barcodeItem.Description;
                lblCalcQtyHint.Visible     = barcodeItem.QtyPerBarcode > 1;
                lblCalcQtyHint.Text        = "1 barcode = " + barcodeItem.QtyPerBarcode + " units";

                MatchedLineID   = matchedLine.CtLineID;
                txtBarcode.Text = string.Empty;

                ShowCalculator();
            }
        }

        protected void lbtnClearScan_Click(object sender, EventArgs e)
        {
            txtBarcode.Text = string.Empty;
            MatchedLineID   = 0;
            ClearFeedback();
            BindLines();
        }

        // ── Recount (tap line card) ────────────────────────────────────────────────
        protected void lbtnRecount_Click(object sender, EventArgs e)
        {
            var btn  = (LinkButton)sender;
            var item = btn.NamingContainer as RepeaterItem;
            if (item == null) return;

            var hf = (HiddenField)item.FindControl("hfCtLineID");
            if (!long.TryParse(hf?.Value, out long ctLineID)) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.StockCountLines
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.CtLineID == ctLineID);

                if (line == null) return;

                // Block finished lines
                if (line.LineFinished == true)
                {
                    SetFeedback(false,
                        "&#128683; <strong>" + (line.ItemCode ?? "") + "</strong> has already been counted.");
                    BindLines();
                    return;
                }

                // Block lines started by another user
                bool hasExistingCount = line.Count1Qty.HasValue && line.Count1Qty.Value > 0;
                if (hasExistingCount && !string.IsNullOrEmpty(line.CountBy) &&
                    line.CountBy != CurrentUser.UserName)
                {
                    SetFeedback(false,
                        "&#128683; <strong>" + (line.ItemCode ?? "") + "</strong> is already being counted by " +
                        line.CountBy + ".");
                    BindLines();
                    return;
                }

                hfCalcCtLineID.Value      = line.CtLineID.ToString();
                hfCalcExpression.Value    = line.CountExpression ?? "";
                hfCalcInitQty.Value       = "0";
                lblCalcItemCode.Text       = line.ItemCode ?? "";
                lblCalcItemDesc.Text       = line.ItemDescription ?? "";
                lblCalcQtyHint.Visible     = false;

                bool hasCount1 = line.Count1Qty.HasValue && line.Count1Qty.Value > 0;
                bool hasCount2 = line.Count2Qty.HasValue && line.Count2Qty.Value > 0;
                string roundLabel = !hasCount1 ? "(Count 1)" : hasCount2 ? "(Count 3)" : "(Count 2)";
                hfCalcRoundText.Value      = roundLabel;
                lblCalcRound.Text          = roundLabel;

                MatchedLineID              = ctLineID;

                ShowCalculator();
            }
        }

        // ── Calculator ─────────────────────────────────────────────────────────────
        private void ShowCalculator()
        {
            pnlCalculator.Visible = true;
            pnlScanBar.Visible    = false;
            // Register startup script to init calculator after partial postback
            string initQty   = hfCalcInitQty.Value ?? "0";
            string expr      = hfCalcExpression.Value ?? "";
            string roundText = hfCalcRoundText.Value ?? "";

            ScriptManager.RegisterStartupScript(this, GetType(), "calcInit",
                "calcExpr = '" + expr.Replace("'", "\\'") + "';" +
                "if (!calcExpr && " + initQty + " > 0) calcExpr = '" + initQty + "';" +
                "renderCalc();", true);
        }

        private void HideCalculator()
        {
            pnlCalculator.Visible = false;
            pnlScanBar.Visible    = true;
            MatchedLineID         = 0;
        }

        protected void lbtnCalcSave_Click(object sender, EventArgs e)
        {
            if (!long.TryParse(hfCalcCtLineID.Value, out long ctLineID)) return;

            string expression = hfCalcExpression.Value ?? "";
            string totalStr   = hfCalcTotal.Value ?? "0";

            if (!decimal.TryParse(totalStr, out decimal total) || total <= 0)
            {
                SetFeedback(false, "&#9888; Please enter a valid count quantity.");
                return;
            }

            string itemCode = lblCalcItemCode.Text;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = db.StockCountLines
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.CtLineID == ctLineID);

                if (line == null)
                {
                    SetFeedback(false, "&#9888; Line not found.");
                    HideCalculator();
                    BindLines();
                    return;
                }

                decimal qoh = line.QtyOnHand ?? 0;
                bool isFirstCount  = line.Count1Qty == null || line.Count1Qty == 0;
                bool isSecondCount = !isFirstCount && (line.Count2Qty == null || line.Count2Qty == 0);

                if (isFirstCount)
                {
                    // ── First count ──────────────────────────────────────────
                    line.CountExpression = expression;
                    line.Count1Qty       = total;
                    line.CountDate       = DateTime.Now;
                    line.CountBy         = CurrentUser.UserName;

                    if (total == qoh)
                    {
                        line.FinalQty     = total;
                        line.LineFinished = true;
                        db.SaveChanges();

                        CountedLineIDs.Add(ctLineID);
                        SetFeedback(true,
                            "&#10003; <strong>" + itemCode + "</strong> — " + total +
                            " matches On Hand. Accepted as Final Qty.");
                    }
                    else
                    {
                        db.SaveChanges();

                        CountedLineIDs.Add(ctLineID);
                        SetFeedback(false,
                            "&#9888; <strong>" + itemCode + "</strong> — " + total +
                            " recorded as Count 1. Qty differs from On Hand — please count again.");
                    }
                }
                else if (isSecondCount)
                {
                    // ── Second count ─────────────────────────────────────────
                    string prevExpr = line.CountExpression ?? "";
                    line.CountExpression = string.IsNullOrEmpty(prevExpr)
                        ? expression
                        : prevExpr + " | " + expression;

                    line.Count2Qty = total;
                    line.CountDate = DateTime.Now;
                    line.CountBy   = CurrentUser.UserName;

                    if (total == qoh)
                    {
                        line.FinalQty     = total;
                        line.LineFinished = true;
                        db.SaveChanges();

                        CountedLineIDs.Add(ctLineID);
                        SetFeedback(true,
                            "&#10003; <strong>" + itemCode + "</strong> — Count 2 (" + total +
                            ") matches On Hand. Accepted as Final Qty.");
                    }
                    else
                    {
                        db.SaveChanges();

                        CountedLineIDs.Add(ctLineID);
                        SetFeedback(false,
                            "&#9888; <strong>" + itemCode + "</strong> — " + total +
                            " recorded as Count 2. Final count required — please count a 3rd time.");
                    }
                }
                else
                {
                    // ── Third count (forced final) ───────────────────────────
                    string prevExpr = line.CountExpression ?? "";
                    line.CountExpression = string.IsNullOrEmpty(prevExpr)
                        ? expression
                        : prevExpr + " | " + expression;

                    line.FinalQty     = total;
                    line.LineFinished = true;
                    line.CountDate    = DateTime.Now;
                    line.CountBy      = CurrentUser.UserName;
                    db.SaveChanges();

                    CountedLineIDs.Add(ctLineID);

                    if (total == qoh)
                    {
                        SetFeedback(true,
                            "&#10003; <strong>" + itemCode + "</strong> — Count 3 (" + total +
                            ") matches On Hand. Accepted as Final Qty.");
                    }
                    else
                    {
                        SetFeedback(true,
                            "&#9878; <strong>" + itemCode + "</strong> — Count 3 (" + total +
                            ") saved as Final Qty (differs from On Hand). Line complete.");
                    }
                }
            }

            HideCalculator();
            BindLines();
        }

        protected void lbtnCalcCancel_Click(object sender, EventArgs e)
        {
            HideCalculator();
            ClearFeedback();
            BindLines();
        }

        // ── Close off ──────────────────────────────────────────────────────────────
        protected void lbtnCloseOff_Click(object sender, EventArgs e)
        {
            if (CountID == 0) return;

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = db.StockCountMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == CountID);

                if (count != null && count.ClosedOff == true)
                {
                    SetFeedback(false, "&#9888; This count is already closed off.");
                    return;
                }

                var unfinished = db.StockCountLines
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.CountID == CountID
                             && x.LineFinished == false)
                    .Count();

                if (unfinished > 0)
                {
                    SetFeedback(false,
                        "&#9888; " + unfinished + " line(s) across all stores have not been counted. " +
                        "Count all lines before closing off.");
                    return;
                }
            }

            lblCloseOffError.Visible = false;
            pnlCloseOff.Visible      = true;
        }

        protected void lbtnCancelCloseOff_Click(object sender, EventArgs e)
        {
            pnlCloseOff.Visible = false;
        }

        protected void lbtnConfirmCloseOff_Click(object sender, EventArgs e)
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = db.StockCountMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == CountID);

                if (count == null)
                {
                    SetCloseOffError("Count not found.");
                    return;
                }

                count.ClosedOff  = true;
                count.ClosedBy   = CurrentUser.RoleID;
                count.ClosedDate = DateTime.Now;
                db.SaveChanges();
            }

            pnlCloseOff.Visible = false;
            Session.Remove("SCLineCountedIDs");

            SetFeedback(true, "&#10003; Stock count closed off successfully.");
            BindLines();
            LoadCountHeader();
        }

        // ── Navigation ─────────────────────────────────────────────────────────────
        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect(
                CurrentUser != null
                    ? "~/SBMSMobile/StockCountsM.aspx?user=" + CurrentUser.UserGuiD
                    : "~/SBMSMobile/StockCountsM.aspx", false);
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

        // ── Feedback helpers ───────────────────────────────────────────────────────
        private void SetFeedback(bool found, string html)
        {
            lblScanFeedback.Text     = html;
            lblScanFeedback.CssClass = found ? "mob-feedback found" : "mob-feedback notfound";
        }

        private void ClearFeedback()
        {
            lblScanFeedback.Text     = string.Empty;
            lblScanFeedback.CssClass = "mob-feedback";
        }

        private void SetCloseOffError(string msg)
        {
            lblCloseOffError.Text    = "&#9888; " + msg;
            lblCloseOffError.Visible = true;
            pnlCloseOff.Visible      = true;
        }
    }
}
