using System;
using System.IO;
using System.Web;

namespace SBMS.Classes
{
    // Cache-busting version token for static assets, derived from the file's
    // last-write time. The token changes automatically whenever the file changes,
    // so stylesheet/script links never need a manual version bump.
    public static class Ver
    {
        /// <summary>Same token, for script tags - so a deployed JS change actually reaches
        /// the browser instead of the cached copy being kept.</summary>
        public static string Js(string virtualPath)
        {
            return Css(virtualPath);
        }

        public static string Css(string virtualPath)
        {
            try
            {
                string path = HttpContext.Current.Server.MapPath(virtualPath);
                return "?v=" + File.GetLastWriteTimeUtc(path).Ticks;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
