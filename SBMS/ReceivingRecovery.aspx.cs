using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    // Recovery screen for receipts stuck on DocHeader.RecStatus = 1.
    //
    // RecStatus 1 means "the supplier invoice posted to Sage, but finalising did not
    // finish" - it is written the instant Sage confirms the invoice, then normally
    // overwritten to 0 (partial) or 2 (complete) once the receipt completes. If the
    // request dies in between (historically an async timeout mid-Sage-loop), the flag
    // is left on 1 and lbtnReceiveFinish_Click refuses every further receive on that
    // PO - correctly, to stop a SECOND supplier invoice going out.
    //
    // Until now the only way out was manual SQL. This screen gives a controlled one.
    //
    // The important thing it does NOT do is decide for you. An invoice almost certainly
    // exists in Sage for every row here, so the operator must confirm what happened
    // before the PO is released.
    public partial class ReceivingRecovery : BasePage
    {
        private UserDetails CurrentUser
        {
            get { return Session["UserDetails"] as UserDetails; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Releasing a PO here can let a second supplier invoice be posted. Super users only.
            if (!CurrentUser.isSuperUser)
            {
                Response.Redirect("~/Dashboard.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack) BindStuck();
        }

        private void BindStuck()
        {
            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                long coId = CurrentUser.CoID;

                var heads = db.DocHeaders
                    .Where(h => h.CompanyID == coId && h.RecStatus == 1)
                    .ToList();

                var rows = heads.Select(h => new
                {
                    h.DocID,
                    PONumber = h.DocumentNumber,
                    Supplier = h.CustSupName,
                    Total    = h.Total ?? 0,
                    Recorded = string.IsNullOrWhiteSpace(h.SupplierInvNum) ? "(none)" : h.SupplierInvNum,
                    // Did the stock actually land locally? GRN rows are written per line as the
                    // receipt runs, BEFORE the point where it died - so this tells the operator
                    // whether re-receiving would duplicate the stock or is genuinely needed.
                    StockIn  = db.ItemTransactions.Any(t => t.DocumentID == h.DocID
                                                         && t.CompanyID == coId
                                                         && t.TransactionType == "GRN")
                                   ? "Yes" : "No",
                    LastActivity = h.CompleteDate
                })
                .OrderByDescending(r => r.LastActivity)
                .ToList();

                gvStuck.DataSource = rows;
                gvStuck.DataBind();

                pnlNone.Visible  = rows.Count == 0;
                gvStuck.Visible  = rows.Count > 0;
                lblCount.Text    = rows.Count.ToString();
            }
        }

        protected void gvStuck_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RecordUnlock" && e.CommandName != "UnlockOnly") return;

            long docId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument), out docId)) return;

            string invNum = "";
            if (e.CommandName == "RecordUnlock")
            {
                var row = ((Control)e.CommandSource).NamingContainer as GridViewRow;
                var txt = row?.FindControl("txtInvNum") as TextBox;
                invNum = (txt?.Text ?? "").Trim();
                if (invNum.Length == 0)
                {
                    SetMsg(false, "Enter the Sage invoice number, or use \"No invoice was posted\" if nothing reached Sage.");
                    return;
                }
            }

            using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
            {
                long coId = CurrentUser.CoID;
                var head = db.DocHeaders.FirstOrDefault(h => h.DocID == docId && h.CompanyID == coId);

                if (head == null)
                {
                    SetMsg(false, "That receipt could not be found.");
                    BindStuck();
                    return;
                }

                // Re-check inside the write context. If someone else already released it,
                // do nothing rather than re-appending an invoice number to a live PO.
                if (head.RecStatus != 1)
                {
                    SetMsg(false, "Receipt " + head.DocumentNumber + " has already been released by someone else. Nothing changed.");
                    BindStuck();
                    return;
                }

                string before = head.SupplierInvNum ?? "(none)";

                if (invNum.Length > 0)
                {
                    head.SupplierInvNum = string.IsNullOrWhiteSpace(head.SupplierInvNum)
                        ? invNum
                        : head.SupplierInvNum + "-" + invNum;
                    if (head.SupplierInvNum.Length > 50)
                        head.SupplierInvNum = head.SupplierInvNum.Substring(0, 50);
                }

                // 0 = Started. The receipt is open again and the outstanding lines can be
                // received normally. We never set 2 here - this screen releases, it does
                // not declare a receipt finished.
                head.RecStatus = 0;
                db.SaveChanges();

                // Audit trail. This alters what the business believes it has been invoiced,
                // so who did it and what they recorded must be recoverable afterwards.
                try
                {
                    new ApiUrlCall().LogErrorToFile(
                        "RECEIPT RELEASED - PO " + head.DocumentNumber +
                        " (DocID " + head.DocID + ", CoID " + coId + ")" +
                        " by " + CurrentUser.UserName +
                        " | invoice recorded: " + (invNum.Length > 0 ? invNum : "NONE - operator confirmed nothing posted to Sage") +
                        " | SupplierInvNum before: " + before);
                }
                catch { }

                SetMsg(true, "Receipt " + head.DocumentNumber + " released."
                     + (invNum.Length > 0 ? " Invoice " + invNum + " recorded." : " No invoice recorded."));
            }

            BindStuck();
        }

        private void SetMsg(bool ok, string text)
        {
            lblMsg.Text = text;
            lblMsg.CssClass = ok ? "msg ok" : "msg bad";
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
