using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace SBMS.Classes
{
    /// <summary>
    /// Helper class to encrypt/decrypt individual configuration values
    /// Values are encrypted using AES and stored in Web.config as "enc:VALUE"
    /// At runtime, they are automatically decrypted when read
    /// </summary>
    public static class ConfigEncryptionHelper
    {
        // Static encryption key (you can change this)
        // For production, consider using a more secure key management approach
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("SBMSClaudeConfig2024SecurityKey!");

        /// <summary>
        /// Encrypts a string value for storage in Web.config
        /// Returns the encrypted value prefixed with "enc:"
        /// </summary>
        public static string EncryptValue(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Generate a random IV
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                        // Combine IV + encrypted data
                        byte[] combined = new byte[iv.Length + encryptedBytes.Length];
                        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
                        Buffer.BlockCopy(encryptedBytes, 0, combined, iv.Length, encryptedBytes.Length);

                        // Encode as base64 and prefix with "enc:"
                        return "enc:" + Convert.ToBase64String(combined);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    $"Failed to encrypt configuration value: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a value that was encrypted with EncryptValue()
        /// Automatically detects "enc:" prefix and decrypts if present
        /// </summary>
        public static string DecryptValue(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText) || !encryptedText.StartsWith("enc:"))
                return encryptedText;  // Not encrypted, return as-is

            try
            {
                // Remove "enc:" prefix
                string base64Data = encryptedText.Substring(4);
                byte[] combined = Convert.FromBase64String(base64Data);

                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Extract IV (first 16 bytes)
                    byte[] iv = new byte[aes.IV.Length];
                    Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);

                    // Extract encrypted data (remainder)
                    byte[] encryptedBytes = new byte[combined.Length - iv.Length];
                    Buffer.BlockCopy(combined, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException(
                    $"Failed to decrypt configuration value: {ex.Message}", ex);
            }
        }
    }
}
