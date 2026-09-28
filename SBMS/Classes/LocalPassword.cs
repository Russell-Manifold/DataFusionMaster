using System;
using System.Security.Cryptography;
using System.Text;

namespace SBMS.Classes
{
    /// <summary>
    /// Local Data Fusion password for "Use Generic Login" users (checked here, never sent
    /// to Sage). PBKDF2-SHA1, 10,000 rounds, salt = the user's UserGUID, stored in
    /// UsersMaster.userpwd as "$p$" + base64(32 bytes) = 47 chars (column is varchar(50)).
    /// A value without the "$p$" prefix is a legacy plain-text password: Verify still
    /// accepts it, and Login re-saves it hashed on the first successful match.
    /// </summary>
    public static class LocalPassword
    {
        private const string Prefix = "$p$";

        public static bool IsHashed(string stored) => stored != null && stored.StartsWith(Prefix, StringComparison.Ordinal);

        public static string Hash(string password, Guid salt)
        {
            using (var k = new Rfc2898DeriveBytes(password ?? "", salt.ToByteArray(), 10000))
                return Prefix + Convert.ToBase64String(k.GetBytes(32));
        }

        public static bool Verify(string password, string stored, Guid salt)
        {
            if (string.IsNullOrEmpty(stored) || password == null) return false;
            if (!IsHashed(stored)) return stored == password;          // legacy plain text
            byte[] a = Encoding.UTF8.GetBytes(Hash(password, salt));
            byte[] b = Encoding.UTF8.GetBytes(stored);
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
