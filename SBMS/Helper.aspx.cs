using SBMS.Classes;
using SBMS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Helper : BasePage
    {
        private UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        private HelpService _helpService;
        private HelpService HelpSvc => _helpService ?? (_helpService = new HelpService());

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            SessionValidator.ValidateUserSession(CurrentUser);

            if (!IsPostBack)
            {
                pnlAdmin.Visible = CurrentUser.isSuperUser;
            }
        }

        protected async void lbtnAsk_Click(object sender, EventArgs e)
        {
            try
            {
                string question = (txtQuestion.Text ?? "").Trim();
                if (question.Length < 3)
                {
                    AlertHelper.ShowSweetAlert(this, "Please enter a question (at least 3 characters).", "warning");
                    return;
                }

                pnlEmpty.Visible = false;
                pnlAnswer.Visible = false;

                // ── Tier 1: Check cache ──────────────────────────────────────
                string currentModule = GetModuleFromReferrer();
                var cached = HelpSvc.Search(question, currentModule, CurrentUser.CoID, maxResults: 1);

                if (cached.Count > 0 && cached[0].UsageCount > 0)
                {
                    // Found a published match
                    var match = cached[0];
                    HelpSvc.IncrementUsage(match.Id);
                    DisplayAnswer(question, match.Answer, "cached &bull; matched: " + match.Question);
                    return;
                }

                // ── Tier 2: Check unpublished (exact match) ─────────────────
                var allResults = HelpSvc.Search(question, currentModule, CurrentUser.CoID, maxResults: 3);
                if (allResults.Count > 0)
                {
                    var best = allResults[0];
                    HelpSvc.IncrementUsage(best.Id);
                    DisplayAnswer(question, best.Answer, "library &bull; matched: " + best.Question);
                    return;
                }

                // ── Tier 3: Call Claude ─────────────────────────────────────
                pnlAnswer.Visible = true;
                lblAnswerQuestion.Text = question;
                litAnswer.Text = "<div class='help-answer-loading'>&#128640; Thinking&hellip;</div>";
                lblSource.Text = "";

                var chatSvc = new ChatService();
                string aiAnswer = await chatSvc.AskAsync(question);

                // Save as unpublished for admin review
                try
                {
                    HelpSvc.Insert(new HelpArticle
                    {
                        Question    = question,
                        Answer      = aiAnswer,
                        Keywords    = GenerateKeywords(question),
                        Module      = currentModule,
                        PageUrl     = Request.UrlReferrer?.AbsolutePath ?? "",
                        UsageCount  = 1,
                        CreatedBy   = CurrentUser.UserName,
                        CreatedDate = DateTime.Now,
                        Published   = false,
                        CompanyID   = CurrentUser.CoID
                    });
                }
                catch { }

                DisplayAnswer(question, aiAnswer, "Claude AI &bull; awaiting review");
            }
            catch (Exception ex)
            {
                try { new ApiUrlCall().LogErrorToFile($"CoID:{CurrentUser?.CoID} Helper lbtnAsk_Click – {ex}"); } catch { }
                AlertHelper.ShowSweetAlert(this, "Sorry, something went wrong getting your answer. Please try again.", "error");
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx", false);
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

        // ── Admin ──────────────────────────────────────────────────────────

        protected void lbtnToggleAdmin_Click(object sender, EventArgs e)
        {
            pnlAdminContent.Visible = !pnlAdminContent.Visible;
            if (pnlAdminContent.Visible) LoadAdminPanels();
        }

        protected void rptUnpublished_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse(e.CommandArgument?.ToString(), out int id)) return;

            switch (e.CommandName)
            {
                case "Publish":
                    var article = HelpSvc.GetById(id);
                    if (article != null)
                    {
                        article.Published = true;
                        HelpSvc.Update(article);
                    }
                    break;
                case "Delete":
                    HelpSvc.Delete(id);
                    break;
            }
            LoadAdminPanels();
        }

        protected void rptPublished_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (!int.TryParse(e.CommandArgument?.ToString(), out int id)) return;

            switch (e.CommandName)
            {
                case "Unpublish":
                    var article = HelpSvc.GetById(id);
                    if (article != null)
                    {
                        article.Published = false;
                        HelpSvc.Update(article);
                    }
                    break;
            }
            LoadAdminPanels();
        }

        private void LoadAdminPanels()
        {
            var unpub = HelpSvc.GetUnpublished();
            rptUnpublished.DataSource = unpub;
            rptUnpublished.DataBind();

            var pub = HelpSvc.GetAllPublished();
            rptPublished.DataSource = pub;
            rptPublished.DataBind();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void DisplayAnswer(string question, string answer, string source)
        {
            pnlAnswer.Visible    = true;
            lblAnswerQuestion.Text = question;
            litAnswer.Text       = FormatAnswer(answer);
            lblSource.Text        = "Source: " + source;
        }

        private string FormatAnswer(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // Basic markdown-like formatting: **bold**, bullet points, numbered lists
            raw = System.Web.HttpUtility.HtmlEncode(raw);
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            raw = raw.Replace("\n- ", "\n&bull; ");
            raw = raw.Replace("\r\n", "<br />").Replace("\n", "<br />");
            return raw;
        }

        private string GetModuleFromReferrer()
        {
            string path = (Request.UrlReferrer?.AbsolutePath ?? "").ToLower();
            if (path.Contains("receiving"))     return "Receiving";
            if (path.Contains("pickingslip"))   return "PickingSlips";
            if (path.Contains("stockcount"))    return "StockCounts";
            if (path.Contains("transfer"))      return "Transfers";
            if (path.Contains("bom"))           return "BOM";
            if (path.Contains("kit"))           return "Kits";
            if (path.Contains("production"))    return "Production";
            if (path.Contains("worksorder"))    return "WorksOrders";
            if (path.Contains("salesorder"))    return "SalesOrders";
            if (path.Contains("jobcard"))       return "JobCards";
            if (path.Contains("dashboard"))     return "Dashboard";
            return "";
        }

        private string GenerateKeywords(string question)
        {
            var words = question.ToLower()
                .Replace("?", "").Replace(".", "").Replace(",", "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var meaningful = words.Where(w => w.Length > 3);
            return string.Join(", ", meaningful);
        }
    }
}
