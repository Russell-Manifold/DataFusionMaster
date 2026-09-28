using SBMS.Classes;
using System;

namespace SBMS
{
    /// <summary>
    /// Sage ID redirects here after "Login with Sage Account". Swaps the code for tokens,
    /// then hands back to Login.aspx?sso=1, which runs the normal login for that email.
    /// </summary>
    public partial class SageCallback : System.Web.UI.Page
    {
        // Where to hand back to: Login (default) or Onboarding ("onboard").
        private string ReturnPage = "~/Login.aspx";

        protected async void Page_Load(object sender, EventArgs e)
        {
            string expectedState = Session["SageOAuthState"] as string;
            Session.Remove("SageOAuthState");
            if (Session["SageOAuthReturn"] as string == "onboard") ReturnPage = "~/DataFusionOnboard.aspx";
            Session.Remove("SageOAuthReturn");

            string error = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(error))
            {
                Fail("Sage sign-in was cancelled or refused: " + (Request.QueryString["error_description"] ?? error));
                return;
            }

            string code = Request.QueryString["code"];
            if (string.IsNullOrEmpty(expectedState) || Request.QueryString["state"] != expectedState || string.IsNullOrEmpty(code))
            {
                Fail("Sage sign-in could not be verified (session expired). Please try again.");
                return;
            }

            try
            {
                SageOAuthToken token = await SageOAuth.ExchangeCodeAsync(code, SageOAuth.CallbackUrl(Request));
                if (string.IsNullOrEmpty(token.AccessToken) || string.IsNullOrEmpty(token.Email))
                {
                    Fail("Sage did not return a verified email address for this account.");
                    return;
                }
                Session["SageOAuth"] = token;
                Response.Redirect(ReturnPage + "?sso=1", false);
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile("Sage OAuth callback failed. " + ex.Message);
                Fail("Sage sign-in failed: " + ex.Message);
            }
        }

        private void Fail(string message)
        {
            Session["SageOAuthError"] = message;
            Response.Redirect(ReturnPage, false);
        }
    }
}
