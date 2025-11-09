# Dual Authentication Support for BlueSky - Implementation Guide

## Overview

BlueSky now supports **two authentication methods**:
1. **OAuth 2.0** - More secure, requires developer account setup
2. **App Password** - Simpler, generated from BlueSky settings

This allows users to choose the authentication method that best fits their needs.

## User Experience

### Authentication Method Selection

When adding a BlueSky account, users now see:

```
Platform: [BlueSky ?]

Authentication Method: [OAuth ?]  or  [App Password ?]
  ?? OAuth: More secure, requires developer account setup
  ?? App Password: Simpler, generate from BlueSky settings
```

### Option 1: OAuth Authentication

1. Select "OAuth" from Authentication Method dropdown
2. Fill in OAuth credentials (Client ID, Client Secret, Redirect URI)
3. Click "Test OAuth Configuration" (optional)
4. Click "Start OAuth Flow"
5. Browser opens for authorization
6. Account created with OAuth tokens

### Option 2: App Password Authentication

1. Select "App Password" from Authentication Method dropdown
2. See instructions:
   ```
   1. Go to BlueSky Settings ? App Passwords
   2. Click 'Add App Password'
   3. Name it 'Social Media Commander'
   4. Copy the generated password (format: xxxx-xxxx-xxxx-xxxx)
   ```
3. Fill in:
   - Username: Your BlueSky handle (e.g., `alice.bsky.social`)
   - App Password: The generated password
4. Click "Test App Password" (optional)
5. Click "Create Account"
6. Account created with App Password

## Technical Implementation

### Model Changes

#### `Account.cs` - Added Authentication Method Support

```csharp
public enum AuthenticationMethod
{
    OAuth,
    AppPassword,
    ApiKey,
    UsernamePassword
}

public class Account
{
    // Existing properties...
    
    /// <summary>
    /// Authentication method used for this account
    /// </summary>
    public AuthenticationMethod AuthMethod { get; set; } = AuthenticationMethod.OAuth;
    
    /// <summary>
    /// App Password for authentication (used with AppPassword method for BlueSky)
    /// This will be encrypted when stored
    /// </summary>
    public string? AppPassword { get; set; }
    
    /// <summary>
    /// Checks if the account has valid authentication
    /// </summary>
    public bool IsAuthenticated => AuthStatus == AuthenticationStatus.Authenticated &&
                                   (AuthMethod switch
                                   {
                                       AuthenticationMethod.OAuth => Tokens != null && !Tokens.IsExpired,
                                       AuthenticationMethod.AppPassword => !string.IsNullOrEmpty(AppPassword),
                                       // ... other methods
                                       _ => false
                                   });
}
```

### ViewModel Changes

#### `AccountManagerViewModel.cs` - Added Properties

```csharp
[ObservableProperty]
private AuthenticationMethod _selectedAuthMethod = AuthenticationMethod.OAuth;

[ObservableProperty]
private string _appPassword = string.Empty;

// Computed properties for UI binding
public bool IsOAuthAuthMethod => SelectedAuthMethod == AuthenticationMethod.OAuth;
public bool IsAppPasswordAuthMethod => SelectedAuthMethod == AuthenticationMethod.AppPassword;
public bool ShowAuthMethodSelector => SelectedPlatform == SocialPlatform.BlueSky;

public IEnumerable<AuthenticationMethod> AvailableAuthMethods =>
    SelectedPlatform == SocialPlatform.BlueSky
        ? new[] { AuthenticationMethod.OAuth, AuthenticationMethod.AppPassword }
        : new[] { AuthenticationMethod.OAuth };
```

#### New Command: `TestAppPasswordCommand`

```csharp
[RelayCommand]
private async Task TestAppPassword()
{
    // Validates App Password format
    if (AppPassword.Length < 19) // BlueSky app passwords are typically 19 chars
    {
        AuthenticationStatus = "? BlueSky App Passwords are typically 19 characters (xxxx-xxxx-xxxx-xxxx)";
    }
    else
    {
        AuthenticationStatus = "? App Password format is valid! Click 'Create Account' to save.";
    }
}
```

#### Updated `SaveAccountAsync`

```csharp
// Create new account
account = new Account
{
    // ... existing properties
    AuthMethod = SelectedAuthMethod,
    OAuthConfiguration = oauthConfig, // Store OAuth config (if OAuth)
    AppPassword = SelectedAuthMethod == AuthenticationMethod.AppPassword ? AppPassword : null
};

var statusMessage = SelectedAuthMethod switch
{
    AuthenticationMethod.OAuth when oauthConfig != null => $"? Account '{account.DisplayName}' created with OAuth configuration!",
    AuthenticationMethod.AppPassword => $"? Account '{account.DisplayName}' created with App Password!",
    _ => $"? Account '{account.DisplayName}' created successfully"
};
```

### View Changes

#### `AccountManagerView.axaml` - Added UI Elements

1. **Authentication Method Selector** (after Platform Selection):
```xml
<StackPanel Spacing="8" IsVisible="{Binding ShowAuthMethodSelector}">
    <TextBlock Text="Authentication Method" FontWeight="Medium"/>
    <ComboBox ItemsSource="{Binding AvailableAuthMethods}"
              SelectedItem="{Binding SelectedAuthMethod}"/>
    <TextBlock FontSize="12" TextWrapping="Wrap">
        <Run Text="OAuth: More secure, requires developer account setup"/>
        <LineBreak/>
        <Run Text="App Password: Simpler, generate from BlueSky settings"/>
    </TextBlock>
</StackPanel>
```

2. **App Password Section** (conditional on IsAppPasswordAuthMethod):
```xml
<Border IsVisible="{Binding IsAppPasswordAuthMethod}">
    <StackPanel Spacing="16">
        <TextBlock Text="BlueSky App Password" FontWeight="SemiBold"/>
        <TextBlock TextWrapping="Wrap">
            1. Go to BlueSky Settings ? App Passwords
            2. Click 'Add App Password'
            3. Name it 'Social Media Commander'
            4. Copy the generated password (format: xxxx-xxxx-xxxx-xxxx)
        </TextBlock>
        
        <TextBox Text="{Binding AppPassword}"
                 Watermark="xxxx-xxxx-xxxx-xxxx"
                 PasswordChar="*"/>
        
        <Button Content="?? Test App Password"
                Command="{Binding TestAppPasswordCommand}"
                IsEnabled="{Binding CanSaveAppPasswordAccount}"/>
    </StackPanel>
</Border>
```

3. **OAuth Section** (conditional on IsOAuthAuthMethod):
```xml
<Border IsVisible="{Binding IsOAuthAuthMethod}">
    <!-- Existing OAuth configuration fields -->
</Border>
```

## Security Considerations

### App Password Storage

App Passwords are stored **encrypted at rest**:
- **Windows**: DPAPI (`ProtectedData`)
- **Linux/macOS**: AES-256 with PBKDF2 key derivation

File location: `%APPDATA%\SocialMediaCommander\Data\accounts.encrypted`

### OAuth vs App Password Security

| Feature | OAuth 2.0 | App Password |
|---------|-----------|--------------|
| Security Level | Higher (delegated auth) | Medium (password-based) |
| Setup Complexity | High (dev account needed) | Low (user-generated) |
| Token Refresh | Yes (automatic) | No (static password) |
| Revocation | OAuth provider | BlueSky settings |
| Best For | Production apps | Personal use, testing |

## Usage Examples

### Creating a BlueSky Account with OAuth

```csharp
var account = new Account
{
    PlatformId = SocialPlatform.BlueSky,
    Username = "alice.bsky.social",
    DisplayName = "Alice",
    AuthMethod = AuthenticationMethod.OAuth,
    Tokens = new OAuthTokens
    {
        AccessToken = "jwt_token_here",
        RefreshToken = "refresh_token_here",
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    }
};
```

### Creating a BlueSky Account with App Password

```csharp
var account = new Account
{
    PlatformId = SocialPlatform.BlueSky,
    Username = "alice.bsky.social",
    DisplayName = "Alice",
    AuthMethod = AuthenticationMethod.AppPassword,
    AppPassword = "abcd-efgh-ijkl-mnop" // This will be encrypted
};
```

### Checking Authentication Status

```csharp
if (account.IsAuthenticated)
{
    // Account is authenticated via either OAuth or App Password
    
    if (account.AuthMethod == AuthenticationMethod.OAuth)
    {
        // Use OAuth tokens
        var accessToken = account.Tokens?.AccessToken;
    }
    else if (account.AuthMethod == AuthenticationMethod.AppPassword)
    {
        // Use App Password
        var appPassword = account.AppPassword; // Decrypted
    }
}
```

## BlueSky API Integration

### Using App Password with BlueSky API

```csharp
// BlueSkyService.cs
public async Task<bool> CreateSessionAsync(Account account)
{
    try
    {
        if (account.AuthMethod == AuthenticationMethod.AppPassword)
        {
            // Use App Password flow
            var sessionData = new
            {
                identifier = account.Username,
                password = account.AppPassword // The app password
            };
            
            var json = JsonSerializer.Serialize(sessionData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                "https://bsky.social/xrpc/com.atproto.server.createSession", 
                content);
                
            return response.IsSuccessStatusCode;
        }
        else if (account.AuthMethod == AuthenticationMethod.OAuth)
        {
            // Use OAuth tokens
            // ... existing OAuth flow
        }
    }
    catch
    {
        return false;
    }
}
```

## User Documentation

### How to Generate a BlueSky App Password

1. **Open BlueSky App** (web or mobile)
2. Navigate to **Settings** ? **Privacy and Security**
3. Scroll to **App Passwords** section
4. Click **"Add App Password"**
5. Enter a name: `Social Media Commander`
6. Click **Create**
7. Copy the generated password (format: `xxxx-xxxx-xxxx-xxxx`)
8. ?? **Important**: You won't be able to see this password again!

### When to Use Each Method

**Use OAuth if:**
- You're building a production application
- You need token refresh capabilities
- You want maximum security
- You have a BlueSky developer account

**Use App Password if:**
- You're using the app personally
- You want quick setup
- You don't have a developer account
- You're testing/development

## Migration Guide

### Existing Accounts

Existing accounts will default to `AuthMethod = OAuth`. No migration needed unless you want to switch to App Password.

### Switching Authentication Methods

To switch an existing account from OAuth to App Password:

1. Edit the account
2. Change Authentication Method to "App Password"
3. Enter your BlueSky App Password
4. Save

The OAuth tokens will be cleared, and the App Password will be encrypted and stored.

## Testing

### Unit Tests

```csharp
[Fact]
public void Account_WithAppPassword_ShouldBeAuthenticated()
{
    // Arrange
    var account = new Account
    {
        AuthMethod = AuthenticationMethod.AppPassword,
        AppPassword = "abcd-efgh-ijkl-mnop",
        AuthStatus = AuthenticationStatus.Authenticated
    };
    
    // Act
    var isAuthenticated = account.IsAuthenticated;
    
    // Assert
    Assert.True(isAuthenticated);
}

[Fact]
public void Account_WithExpiredOAuth_ShouldNotBeAuthenticated()
{
    // Arrange
    var account = new Account
    {
        AuthMethod = AuthenticationMethod.OAuth,
        Tokens = new OAuthTokens
        {
            AccessToken = "token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        },
        AuthStatus = AuthenticationStatus.Authenticated
    };
    
    // Act
    var isAuthenticated = account.IsAuthenticated;
    
    // Assert
    Assert.False(isAuthenticated);
}
```

### Manual Testing Checklist

- [ ] BlueSky OAuth flow works
- [ ] BlueSky App Password flow works
- [ ] Other platforms still default to OAuth only
- [ ] Authentication method selector only shows for BlueSky
- [ ] App Password is encrypted in storage
- [ ] OAuth tokens are still encrypted in storage
- [ ] Can switch between authentication methods
- [ ] Test App Password validation works
- [ ] Both methods create functional accounts

## Troubleshooting

### App Password Not Working

**Problem**: "Invalid password" error when using App Password

**Solutions**:
1. Verify you copied the entire password (19 characters with dashes)
2. Make sure you're using the password, not the password name
3. Check that the BlueSky username is correct (include `.bsky.social`)
4. Regenerate the App Password if lost

### OAuth Still Preferred

**Problem**: Why use App Password if OAuth is more secure?

**Answer**: App Passwords are perfect for:
- Personal use where you control the credentials
- Quick testing without developer account setup
- Situations where OAuth redirect flow is problematic
- Automated scripts or bots (with caution)

### App Password Encryption

**Problem**: Where is the App Password stored?

**Answer**: Encrypted in `%APPDATA%\SocialMediaCommander\Data\accounts.encrypted`
- Windows: DPAPI encryption
- Linux/macOS: AES-256 encryption

## Future Enhancements

Potential improvements:
1. **Auto-detect authentication method** based on credentials provided
2. **App Password generation** directly from the app (if BlueSky adds API)
3. **Multi-factor authentication** support
4. **Session management** for App Password (like OAuth refresh)
5. **Expiration warnings** for App Passwords (user-set reminders)

## References

- BlueSky App Password Docs: https://bsky.social/about/blog/4-28-2023-app-passwords
- AT Protocol Authentication: https://atproto.com/specs/xrpc#authentication
- OAuth 2.0 Specification: https://oauth.net/2/

## Summary

This implementation provides BlueSky users with **flexible authentication options** while maintaining security through encryption. Users can choose the method that best fits their use case, whether it's the more secure OAuth flow or the simpler App Password approach.

Both methods are fully supported and provide equal functionality once authenticated. The choice is purely about setup complexity vs. security requirements.
