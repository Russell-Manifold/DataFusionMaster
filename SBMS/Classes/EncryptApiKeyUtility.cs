using SBMS.Classes;
using System;

namespace SBMS.Classes
{
    /// <summary>
    /// Simple utility to encrypt API key for Web.config
    /// 
    /// Usage:
    /// 1. Replace "sk-ant-YOUR_API_KEY_HERE" with your actual Claude API key
    /// 2. Call: string encrypted = EncryptApiKeyUtility.GetEncryptedKey("sk-ant-your-key");
    /// 3. Copy the encrypted value (starting with "enc:")
    /// 4. Paste it in Web.config as the ClaudeApiKey value
    /// </summary>
    public static class EncryptApiKeyUtility
    {
        /// <summary>
        /// Encrypts an API key and returns the encrypted value for Web.config
        /// </summary>
        public static string GetEncryptedKey(string plainApiKey)
        {
            if (string.IsNullOrEmpty(plainApiKey))
                return null;

            return ConfigEncryptionHelper.EncryptValue(plainApiKey);
        }

        /// <summary>
        /// Test method - returns the encrypted version of your API key
        /// </summary>
        public static string EncryptMyKey()
        {
            // Replace with your plain API key, run once to get the encrypted value,
            // then paste the result into Web.config and clear this value again.
            string plainApiKey = "";

            if (string.IsNullOrEmpty(plainApiKey))
                return "No key provided. Add your plain key here temporarily to encrypt it.";

            return ConfigEncryptionHelper.EncryptValue(plainApiKey);
        }
    }
}
