# Quick Start: Encrypt Your Claude API Key

## The Process (3 Simple Steps)

### Step 1?? Get Your Encrypted Key

Open the file: `SBMS\Classes\EncryptApiKeyUtility.cs`

Replace line 15:
```csharp
// BEFORE:
string plainApiKey = "sk-ant-YOUR_API_KEY_HERE";

// AFTER (your actual Claude API key):
string plainApiKey = "sk-ant-v7m8x9y2z1a3b4c5d6e7f8g9h0i1j2k3";
```

Build the project. In the **Build Output**, you'll see something like:
```
Encrypted Key: enc:AgvRm4kcN3p9X2m7pL9wK1...
```

**Copy this entire encrypted value** (including the `enc:` prefix)

### Step 2?? Update Web.config

Open: `SBMS\Web.config`

Find this line:
```xml
<add key="ClaudeApiKey" value="sk-ant-YOUR_API_KEY_HERE" />
```

Replace it with your encrypted value:
```xml
<add key="ClaudeApiKey" value="enc:AgvRm4kcN3p9X2m7pL9wK1..." />
```

Save the file.

### Step 3?? Done! ?

Your app now:
- ? Stores the encrypted key in Web.config
- ? Automatically decrypts it at runtime
- ? Uses the plain key to call Claude

## What Gets Encrypted?

| Setting | Encrypted | At Runtime |
|---------|-----------|-----------|
| ClaudeApiKey | `enc:AgvRm...` | Decrypted ? |
| ClaudeModel | Plain text | Used as-is |
| ClaudeMaxTokens | Plain text | Used as-is |
| ClaudeTemperature | Plain text | Used as-is |

## Example Web.config (Before & After)

**BEFORE:**
```xml
<claudeSettings>
    <add key="ClaudeApiKey" value="sk-ant-YOUR_API_KEY_HERE" />
    <add key="ClaudeModel" value="claude-sonnet-4-6" />
    <add key="ClaudeMaxTokens" value="2048" />
    <add key="ClaudeTemperature" value="0.3" />
</claudeSettings>
```

**AFTER:**
```xml
<claudeSettings>
    <add key="ClaudeApiKey" value="enc:AgvRm4kcN3p9X2m7pL9wK1bzHqmN8vP0qR2sT3uV4wX5yZ6" />
    <add key="ClaudeModel" value="claude-sonnet-4-6" />
    <add key="ClaudeMaxTokens" value="2048" />
    <add key="ClaudeTemperature" value="0.3" />
</claudeSettings>
```

## How to Update the Key Later

If you need to change your API key:

1. Open `EncryptApiKeyUtility.cs`
2. Replace with your new API key
3. Build the project
4. Copy the new encrypted value
5. Update Web.config
6. Restart your app

That's it!

---

**Security:** ? Plain text API key hidden in Web.config  
**Runtime:** ? Decrypted automatically when needed  
**Easy:** ? Simple copy-paste process
