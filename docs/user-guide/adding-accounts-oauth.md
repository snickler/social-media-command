# How to Add Social Media Accounts - User Guide

## Prerequisites

Before you can add accounts to Social Media Commander, you need to set up OAuth credentials for each platform you want to use.

---

## Part 1: Configure OAuth Credentials

### BlueSky Configuration

1. **Get BlueSky OAuth Credentials**:
   - Visit [BlueSky Developer Portal](https://bsky.social)
   - Create a new OAuth Application
   - Copy your `Client ID` and `Client Secret`

2. **Configure in Social Media Commander**:
   - Open Social Media Commander
   - Navigate to **Settings** ? **OAuth Configuration**
   - Select **BlueSky** from the platform dropdown
   - Fill in the form:
     - **Client ID**: Paste your BlueSky Client ID
     - **Client Secret**: Paste your BlueSky Client Secret
     - **Redirect URI**: `http://localhost:8080/oauth/callback` (default)
   - Click **Save Configuration**
   - You should see: ? "Configuration saved successfully!"

---

## Part 2: Add Your Account via OAuth

Once OAuth is configured, you can add accounts:

### Adding a BlueSky Account

1. **Navigate to Account Manager**:
   - Click on the **Account Manager** tab/page

2. **Start OAuth Flow**:
   - Click the **Add Account** button
   - Status message should show: "?? Starting OAuth authentication..."

3. **Authorize in Browser**:
   - Your default browser will open automatically
   - You'll see BlueSky's authorization page
   - Log in to your BlueSky account
   - Click **Authorize** to grant permissions

4. **Automatic Completion**:
   - Browser redirects to `http://localhost:8080/oauth/callback`
   - You'll see a success message in the browser
   - The browser window will close automatically after 3 seconds

5. **Account Created**:
   - Back in Social Media Commander:
   - Status shows: "? Successfully connected [Your Name]!"
   - Your account appears in the accounts list
   - Avatar, username, and display name are automatically populated

---

## What You'll See in the Application

### OAuth Configuration Page

```
Platform: [BlueSky ?]

OAuth Configuration:
?? Client ID: [your_client_id_here________]
?? Client Secret: [???????????????????????]
?? Redirect URI: [http://localhost:8080/oauth/callback]
?? Scopes: read, write

[Load Defaults] [Test Configuration] [Save Configuration]

Status: ? Configuration saved successfully!
```

### Account Manager - After Successful OAuth

```
Platform: BlueSky

Accounts:
??????????????????????????????????????????
? ?? BlueSky Account                     ?
??????????????????????????????????????????
? @yourhandle.bsky.social                ?
? Your Display Name                      ?
?                                        ?
? [Edit] [Reconnect] [Remove]  [Default]?
??????????????????????????????????????????

[Add Account]
```

---

## Troubleshooting

### Problem: "OAuth configuration not found" error

**Solution**:
1. Go to Settings ? OAuth Configuration
2. Configure OAuth credentials first (see Part 1 above)
3. Save the configuration
4. Then try adding an account again

### Problem: Browser opens but shows error

**Possible Causes**:
- Invalid Client ID or Client Secret
- Redirect URI mismatch
- Application not approved in developer portal

**Solution**:
1. Double-check OAuth credentials in developer portal
2. Ensure Redirect URI is exactly: `http://localhost:8080/oauth/callback`
3. Verify your app is approved/active in BlueSky developer console

### Problem: "Placeholder values" warning

**Cause**: You haven't configured OAuth credentials yet

**Solution**:
1. The default configuration uses placeholder values (`YOUR_CLIENT_ID_HERE`)
2. Replace these with real credentials from your developer account
3. Save the configuration

### Problem: Account appears briefly then disappears

**Possible Causes**:
- OAuth flow failed
- No valid authorization code received
- Network/connectivity issue

**Solution**:
1. Check console output for error messages
2. Verify OAuth configuration is valid
3. Test OAuth configuration using "Test Configuration" button
4. Check application logs in `%LOCALAPPDATA%\SocialMediaCommander\Logs\`

---

## OAuth Flow Diagram

```
[Social Media Commander]
        |
        | 1. Click "Add Account"
        ?
[Check OAuth Config]
        |
        | 2. Open Browser
        ?
[BlueSky Authorization Page]
        |
        | 3. User logs in & authorizes
        ?
[Redirect to localhost:8080/oauth/callback?code=...]
        |
        | 4. App receives auth code
        ?
[Exchange code for tokens]
        |
        | 5. Get user profile
        ?
[Create account with real tokens]
        |
        ?
[Account appears in list] ?
```

---

## Security Notes

? **All OAuth tokens are encrypted at rest**
- Windows: Uses DPAPI encryption
- Linux/macOS: Uses AES-256 with PBKDF2

? **Credentials never stored in plaintext**
- Encrypted files in `%APPDATA%\SocialMediaCommander\`

? **OAuth 2.0 Authorization Code Flow**
- Industry-standard security
- State parameter for CSRF protection
- PKCE support for enhanced security

---

## Getting OAuth Credentials

### BlueSky Developer Setup

1. Visit: https://bsky.social
2. Navigate to Developer Settings
3. Create New Application:
   - **Name**: Social Media Commander
   - **Redirect URI**: `http://localhost:8080/oauth/callback`
   - **Scopes**: read, write
4. Copy Client ID and Client Secret
5. Paste into Social Media Commander OAuth Configuration

### Twitter/X Developer Setup

1. Visit: https://developer.twitter.com
2. Create Project & App
3. Generate OAuth 2.0 Client ID & Secret
4. Configure Redirect URI: `http://localhost:8080/oauth/callback`
5. Copy credentials to Social Media Commander

### LinkedIn Developer Setup

1. Visit: https://www.linkedin.com/developers/
2. Create New Application
3. Get Client ID & Client Secret
4. Configure OAuth settings
5. Copy credentials to Social Media Commander

---

## FAQ

**Q: Do I need to configure OAuth for every platform?**
A: Yes, each platform requires its own OAuth credentials from that platform's developer portal.

**Q: Can I use the same redirect URI for all platforms?**
A: Yes, `http://localhost:8080/oauth/callback` is used for all platforms.

**Q: What happens if I click "Add Account" without configuring OAuth?**
A: You'll see an error message: "OAuth configuration not found" or "Please configure OAuth settings first."

**Q: How do I know if OAuth is configured correctly?**
A: Click the "Test Configuration" button in OAuth Configuration page. You should see: "? OAuth configuration is valid!"

**Q: Can I add multiple accounts for the same platform?**
A: Yes! Each account goes through its own OAuth flow.

**Q: What if I see placeholder values in OAuth config?**
A: Replace `YOUR_CLIENT_ID_HERE` and `YOUR_CLIENT_SECRET_HERE` with your actual credentials from the developer portal.

---

## Next Steps

Once you've successfully added accounts:
1. ? Select default account for each platform
2. ? Test posting to ensure OAuth works
3. ? Configure post scheduling (optional)
4. ? Set up automation rules (optional)

Happy posting! ??
