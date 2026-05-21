# Claude Configuration Encryption Guide

## Overview
Your Claude API key and settings are now stored in `Web.config` under the `<claudeSettings>` section. For production, **encrypt this section** to protect your API key.

## How It Works

### Automatic Decryption
When your application runs, `ChatService.cs` automatically decrypts the configuration using `ConfigurationManager`. You don't need to do anything—just configure encryption once and forget about it.

### Manual Encryption Methods

#### Option 1: PowerShell (Recommended for IIS)

Run PowerShell **as Administrator** on your server:

```powershell
# For local IIS application
$webConfigPath = "C:\inetpub\wwwroot\YourApp\Web.config"
$config = [System.Configuration.ConfigurationManager]::OpenExeConfiguration($webConfigPath)
$section = $config.GetSection("claudeSettings")

if ($section -ne $null -and -not $section.SectionInformation.IsProtected) {
    $section.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider")
    $config.Save()
    Write-Host "? claudeSettings section encrypted successfully!"
} else {
    Write-Host "Section already encrypted or not found."
}
```

#### Option 2: ASPNET_REGIIS (For .NET Framework)

Run as Administrator in Command Prompt:

```cmd
cd C:\Windows\Microsoft.NET\Framework\v4.0.30319

# Encrypt
aspnet_regiis.exe -pef "claudeSettings" "C:\inetpub\wwwroot\YourApp"

# Decrypt (to edit)
aspnet_regiis.exe -pdf "claudeSettings" "C:\inetpub\wwwroot\YourApp"
```

#### Option 3: Programmatically via Page

Create an admin page that calls `ConfigEncryptionHelper` (available in the codebase):

```csharp
// Encrypt
ConfigEncryptionHelper.EncryptConfigSection("claudeSettings");

// Decrypt
ConfigEncryptionHelper.DecryptConfigSection("claudeSettings");

// Check status
bool isEncrypted = ConfigEncryptionHelper.IsConfigSectionEncrypted("claudeSettings");
```

## Step-by-Step Setup

### 1. First Time: Add Your API Key

Edit `Web.config` and fill in your Claude API key:

```xml
<claudeSettings>
    <add key="ClaudeApiKey" value="sk-ant-YOUR_ACTUAL_API_KEY_HERE" />
    <add key="ClaudeModel" value="claude-sonnet-4-6" />
    <add key="ClaudeMaxTokens" value="2048" />
    <add key="ClaudeTemperature" value="0.3" />
</claudeSettings>
```

### 2. Encrypt (Production)

Use **Option 1 (PowerShell)** or **Option 2 (ASPNET_REGIIS)** above to encrypt.

### 3. Edit Later (If Needed)

To edit encrypted settings:

```powershell
# Decrypt
aspnet_regiis.exe -pdf "claudeSettings" "C:\path\to\app"

# Edit Web.config...

# Re-encrypt
aspnet_regiis.exe -pef "claudeSettings" "C:\path\to\app"
```

## Security Notes

- ? The encrypted section can **only be decrypted by the IIS Application Pool Identity** that runs your app
- ? The key is encrypted with **RSA** (algorithm: `RsaProtectedConfigurationProvider`)
- ? Each server has its own machine key—encrypted configs **won't transfer** to another server
- ? Your API key is **never visible** in the encrypted section of Web.config
- ?? If you need to migrate to another server, decrypt first, then re-encrypt on the new server

## Troubleshooting

### "Access Denied" Error
- Run PowerShell/CMD **as Administrator**
- Ensure IIS Application Pool Identity has permission to the folder
- On Azure, use managed encryption through Azure Key Vault

### Can't Encrypt / Permission Denied
```powershell
# Make sure the app pool identity has access
icacls "C:\inetpub\wwwroot\YourApp" /grant "IIS AppPool\DefaultAppPool":(F) /t
```

### Need to See the API Key Again
Decrypt the section:
```powershell
aspnet_regiis.exe -pdf "claudeSettings" "C:\path\to\app"
```

Then the values become visible in Web.config.

## What's Encrypted?

Only the `<claudeSettings>` section:
- `ClaudeApiKey` ? Encrypted
- `ClaudeModel` ? Encrypted  
- `ClaudeMaxTokens` ? Encrypted
- `ClaudeTemperature` ? Encrypted

Your database connection strings remain under their own protection.

## No Action Required in Code

Your application code doesn't change. `ConfigurationManager.AppSettings` automatically decrypts on read:

```csharp
// This works the same whether encrypted or not
string apiKey = ConfigurationManager.AppSettings["ClaudeApiKey"];
```

---

**Questions?** Check that your IIS Application Pool Identity matches the user who encrypted the section.
