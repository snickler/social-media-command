# Quick Guide: Adding a BlueSky Account

## Step-by-Step Instructions

### 1. Get BlueSky OAuth Credentials

**Option A: Use BlueSky Developer Portal**
1. Visit the [BlueSky Developer Portal](https://bsky.social/settings/developers)
2. Create a new OAuth application
3. Set Redirect URI to: `http://localhost:8080/callback`
4. Copy your `Client ID` and `Client Secret`

**Option B: Use Placeholder for Testing (Will Fail)**
- Client ID: `YOUR_CLIENT_ID_HERE`
- Client Secret: `YOUR_CLIENT_SECRET_HERE`
- Redirect URI: `http://localhost:8080/callback`

### 2. Open Social Media Commander

1. Launch the application
2. Navigate to **Account Manager**

### 3. Add BlueSky Account

1. Click **"Add Account"** button (top right)
2. You'll see a form with two sections:

#### OAuth Configuration Section

```
Platform: [BlueSky ?]

OAuth Configuration:
?? Client ID: [Paste your BlueSky client ID here]
?? Client Secret: [Paste your BlueSky client secret here]
?? Redirect URI: http://localhost:8080/callback (pre-filled)

[?? Test OAuth Configuration]  [?? Start OAuth Flow]
```

3. **Fill in the OAuth fields**:
   - **Client ID**: Paste your BlueSky OAuth Client ID
   - **Client Secret**: Paste your BlueSky OAuth Client Secret  
   - **Redirect URI**: Already filled with `http://localhost:8080/callback`

4. **Optional: Click "?? Test OAuth Configuration"**
   - This validates your credentials before starting OAuth
   - You'll see ? "OAuth configuration is valid!" if correct
   - Or ? error messages if there's an issue

5. **Click "?? Start OAuth Flow"**
   - Your default browser will open
   - You'll be taken to BlueSky's authorization page

### 4. Authorize in Browser

1. **Log in** to your BlueSky account (if not already logged in)
2. **Review permissions** being requested
3. **Click "Authorize"** or "Allow"
4. Browser redirects to `http://localhost:8080/callback`
5. You'll see a success message in the browser
6. Browser window can be closed (it will auto-close after 3 seconds)

### 5. Account Created!

Back in Social Media Commander:
- ? You'll see: "Successfully connected [Your BlueSky Name]!"
- Your account appears in the accounts list
- Account shows:
  - Your real BlueSky avatar
  - Your real BlueSky display name
  - Your real BlueSky username
  - "Connected" status badge
  - "Default" badge (if first account for BlueSky)

## Troubleshooting

### "OAuth configuration not found" error

**Cause**: No OAuth credentials configured yet
**Solution**: Click "Add Account" again and fill in OAuth credentials

### "Invalid OAuth configuration" error

**Cause**: Incorrect Client ID, Client Secret, or Redirect URI
**Solution**: 
1. Double-check credentials from BlueSky Developer Portal
2. Ensure Redirect URI is exactly: `http://localhost:8080/callback`
3. Click "Test OAuth Configuration" to see specific errors

### "Authentication failed" error

**Cause**: OAuth flow was not completed or was denied
**Solution**:
1. Try clicking "Add Account" again
2. Make sure you click "Authorize" in the browser
3. Check that browser can reach `localhost:8080`

### Browser doesn't open

**Cause**: Default browser not configured or permissions issue
**Solution**:
1. Manually copy the authorization URL from logs
2. Paste into browser
3. Complete authorization there

### Form doesn't appear

**Cause**: OAuth is already configured globally for BlueSky
**Solution**: This is normal! The OAuth flow starts immediately without showing the form

### Account appears then disappears (the "flash")

**Cause**: This was the bug that's now fixed
**Solution**: Update to latest version with the fix applied

## What Happens Behind the Scenes

1. **You click "Add Account"**
   - App checks if OAuth is configured for BlueSky
   - If not, shows OAuth configuration form
   - If yes, starts OAuth flow immediately

2. **You fill in OAuth credentials**
   - Client ID and Secret are stored temporarily
   - Not saved until you click "Start OAuth Flow"

3. **You click "Start OAuth Flow"**
   - OAuth config is saved globally for BlueSky
   - Authorization URL is generated
   - Browser is opened to that URL
   - HTTP listener starts on `localhost:8080`

4. **You authorize in browser**
   - BlueSky asks for permissions
   - You click "Authorize"
   - BlueSky redirects to `http://localhost:8080/callback?code=...`

5. **App receives callback**
   - HTTP listener receives the authorization code
   - App exchanges code for OAuth tokens
   - App fetches your BlueSky profile
   - Account is created with:
     - Real OAuth access token
     - Real OAuth refresh token
     - Your profile data (name, username, avatar)

6. **Account appears in list**
   - Encrypted account data saved to disk
   - OAuth tokens stored securely
   - Account ready to use for posting

## Security Notes

? **OAuth tokens encrypted at rest**:
- Windows: DPAPI encryption
- Linux/macOS: AES-256 with PBKDF2

? **Client Secret never stored in plaintext**:
- Stored in encrypted configuration file
- Located in `%APPDATA%\SocialMediaCommander\Config\`

? **OAuth 2.0 Authorization Code Flow**:
- Industry-standard security
- State parameter for CSRF protection
- Tokens never exposed in URL

## Next Account

To add another BlueSky account (or account from another platform):

1. Click "Add Account" again
2. If OAuth is already configured for that platform:
   - OAuth flow starts immediately (no form)
3. If OAuth is not configured:
   - Form appears to enter credentials
4. Complete OAuth flow as before
5. Multiple accounts supported per platform!

## Reference Files

- **Complete Fix Documentation**: `docs/operations/add-account-form-fixed.md`
- **User Guide (Full)**: `docs/user-guide/adding-accounts-oauth.md`
- **OAuth Configuration Guide**: See OAuthConfigurationViewModel
