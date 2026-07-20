using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;

namespace SBMS.Classes
{
    // Base for all SBMSMobile pages. BasePage's OnInit redirect uses
    // endResponse:false + CompleteRequest, which does NOT stop the WebForms
    // lifecycle - a postback's control event still fires afterwards, and every
    // mobile handler dereferences CurrentUser (and the scan handlers write
    // stock). This override runs before the Load event and TERMINATES the
    // request (endResponse: true) when the session is missing or expired, so
    // no event handler can run - and no stock can move - on a dead session.
    public class MobileBasePage : BasePage
    {
        protected override void OnLoad(EventArgs e)
        {
            var user = Session["UserDetails"] as UserDetails;
            if (user == null)
            {
                string returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, true);
                return; // unreachable - Redirect(endResponse: true) ends the request
            }
            if (user.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", true);
                return;
            }
            base.OnLoad(e);
        }

        // ── One-shot action token (double-tap guard) ───────────────────────────
        // A double-tap sends two postbacks of the SAME rendered page, so both
        // carry the same token: the first TryConsumeActionToken passes, the
        // second is rejected. A legitimate follow-up action arrives after a
        // re-render (which stamped a fresh token) and passes. Session state
        // serialises the two requests, so there is no race.
        // Usage: page renders an <asp:HiddenField ID="hfActionToken"> inside its
        // UpdatePanel, stamps it in OnPreRender via StampActionToken, and every
        // state-changing handler starts with: if (!TryConsumeActionToken(hfActionToken)) return;
        private const string ConsumedTokensKey = "MobileConsumedActionTokens";

        protected void StampActionToken(HiddenField hf)
        {
            if (hf != null) hf.Value = Guid.NewGuid().ToString("N");
        }

        // Invariant quantity text for <input type="number"> prefills. Server-culture
        // formatting ("2,5" on comma-decimal machines) is an invalid value for a
        // number input (the browser blanks it), and the pages parse these back with
        // InvariantCulture - so both legs must speak invariant.
        protected static string FormatQty(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            return Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture)
                .ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        protected bool TryConsumeActionToken(HiddenField hf)
        {
            string token = hf?.Value;
            if (string.IsNullOrEmpty(token)) return true; // page rendered before the guard existed

            var used = Session[ConsumedTokensKey] as Queue<string>;
            if (used == null) { used = new Queue<string>(); Session[ConsumedTokensKey] = used; }

            if (used.Contains(token)) return false;

            used.Enqueue(token);
            while (used.Count > 20) used.Dequeue();
            return true;
        }
    }
}
