using System;
using System.Web.UI;

namespace SBMS.Classes
{
    public static class AlertHelper
    {
        public static void ShowSweetAlert(Page page, string message, string alertType = "info", string title = "")
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                switch (alertType.ToLower())
                {
                    case "success": title = "Success"; break;
                    case "error": title = "Error"; break;
                    case "warning": title = "Warning"; break;
                    default: title = "Information"; break;
                }
            }

            // Escape single quotes so JS doesn't break
            message = message.Replace("'", "\\'");

            string script = $"Swal.fire({{ icon: '{alertType}', title: '{title}', text: '{message}' }});";

            ScriptManager.RegisterStartupScript(
                page,
                page.GetType(),
                Guid.NewGuid().ToString(),
                script,
                true
            );
        }

        public static void ShowProcessingAlert(Page page, string message = "This may take up to 30 seconds. Please do not close this page.", string title = "Processing Transfer")
        {
            string script = $"Swal.fire({{ title: '{title.Replace("'", "\\'")}', text: '{message.Replace("'", "\\'")}', icon: 'info', showConfirmButton: false, allowOutsideClick: false }});";

            ScriptManager.RegisterStartupScript(
                page,
                page.GetType(),
                Guid.NewGuid().ToString(),
                script,
                true
            );
        }
    }
}