using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SBMS.Classes
{
    /// <summary>
    /// Tokens from "Login with Sage Account" (Sage ID). Lives on UserDetails for the session.
    /// </summary>
    [Serializable]
    public class SageOAuthToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public string Email { get; set; }
    }

    /// <summary>
    /// Sage ID OAuth 2.0 Authorization Code grant (confidential web client).
    /// Settings are in the sageOAuthSettings section of Web.config. Each instance has its
    /// OWN Sage client registration: /za = live, /demo = resellers profile.
    /// </summary>
    public static class SageOAuth
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private static NameValueCollection Settings =>
            ConfigurationManager.GetSection("sageOAuthSettings") as NameValueCollection
            ?? new NameValueCollection();

        private static string Setting(string key) => ConfigEncryptionHelper.DecryptValue(Settings[key] ?? "").Trim();

        private static string Authority
        {
            get
            {
                string a = Setting("Authority");
                return (a == "" ? "https://id.sage.com" : a).TrimEnd('/');
            }
        }

        private static string Scope
        {
            get
            {
                string s = Setting("Scope");
                return s == "" ? "openid profile email offline_access" : s;
            }
        }

        /// <summary>No ClientId/secret for this instance = the Sage Account button stays hidden.</summary>
        public static bool IsConfigured => Setting("ClientId") != "" && Setting("ClientSecret") != "";

        /// <summary>Built from the current instance, so /za and /demo each call back to themselves.</summary>
        public static string CallbackUrl(HttpRequest req) =>
            req.Url.GetLeftPart(UriPartial.Authority) + req.ApplicationPath.TrimEnd('/') + "/SageCallback.aspx";

        public static string NewState()
        {
            byte[] b = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string AuthorizeUrl(string state, string redirectUri)
        {
            var sb = new StringBuilder(Authority + "/authorize?response_type=code");
            sb.Append("&client_id=").Append(Uri.EscapeDataString(Setting("ClientId")));
            sb.Append("&redirect_uri=").Append(Uri.EscapeDataString(redirectUri));
            sb.Append("&scope=").Append(Uri.EscapeDataString(Scope));
            string audience = Setting("Audience");
            if (audience != "") sb.Append("&audience=").Append(Uri.EscapeDataString(audience));
            sb.Append("&state=").Append(Uri.EscapeDataString(state));
            return sb.ToString();
        }

        public static Task<SageOAuthToken> ExchangeCodeAsync(string code, string redirectUri)
        {
            return PostTokenAsync(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", Setting("ClientId") },
                { "client_secret", Setting("ClientSecret") },
                { "code", code },
                { "redirect_uri", redirectUri }
            }, null);
        }

        /// <summary>Sage requires rotating refresh tokens - always keep the NEW one returned.</summary>
        public static Task<SageOAuthToken> RefreshAsync(SageOAuthToken current)
        {
            return PostTokenAsync(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", Setting("ClientId") },
                { "client_secret", Setting("ClientSecret") },
                { "refresh_token", current.RefreshToken }
            }, current);
        }

        private static async Task<SageOAuthToken> PostTokenAsync(Dictionary<string, string> form, SageOAuthToken current)
        {
            HttpResponseMessage resp = await Http.PostAsync(Authority + "/oauth/token", new FormUrlEncodedContent(form)).ConfigureAwait(false);
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Sage ID token request failed ({(int)resp.StatusCode}): {body}");

            JObject j = JObject.Parse(body);
            int expiresIn = (int?)j["expires_in"] ?? 3600;
            return new SageOAuthToken
            {
                AccessToken = (string)j["access_token"],
                RefreshToken = (string)j["refresh_token"] ?? current?.RefreshToken,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn),
                Email = EmailFromIdToken((string)j["id_token"]) ?? current?.Email
            };
        }

        // The id_token came straight from Sage's token endpoint over TLS, authenticated with our
        // client secret, so its signature need not be re-checked (OIDC Core 3.1.3.7).
        private static string EmailFromIdToken(string idToken)
        {
            if (string.IsNullOrEmpty(idToken)) return null;
            string[] parts = idToken.Split('.');
            if (parts.Length < 2) return null;

            string p = parts[1].Replace('-', '+').Replace('_', '/');
            p = p.PadRight(p.Length + (4 - p.Length % 4) % 4, '=');
            JObject claims = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(p)));

            if (claims["email_verified"] != null && (bool)claims["email_verified"] == false) return null;
            return (string)claims["email"];
        }
    }
}
