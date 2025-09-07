using System;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Desktop.Helpers;

/// <summary>
/// Manages account persistence operations including creating, updating, and deleting accounts
/// Extracted from AccountManagerViewModel to reduce cyclic complexity
/// </summary>
public class AccountPersistenceManager
{
    private readonly IAccountService _accountService;
    private readonly IOAuthConfigurationService _oauthConfigService;
    private readonly ILogger _logger;

    public AccountPersistenceManager(
        IAccountService accountService,
        IOAuthConfigurationService oauthConfigService,
        ILogger logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _oauthConfigService = oauthConfigService ?? throw new ArgumentNullException(nameof(oauthConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new account with the provided details
    /// </summary>
    public async Task<AccountPersistenceResult> CreateAccountAsync(
        SocialPlatform platform,
        string username,
        string displayName,
        string avatar,
        bool isFirstAccountForPlatform,
        string? oauthClientId = null,
        string? oauthClientSecret = null,
        string? oauthRedirectUri = null)
    {
        try
        {
            _logger.Information("Creating new account for platform: {Platform}", platform);

            // Create OAuth configuration if provided
            OAuthConfig? oauthConfig = null;
            if (!string.IsNullOrWhiteSpace(oauthClientId) && !string.IsNullOrWhiteSpace(oauthClientSecret))
            {
                oauthConfig = await CreateOAuthConfigurationAsync(platform, oauthClientId, oauthClientSecret, oauthRedirectUri);
            }

            // Create new account with safe DateTime handling
            var account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                PlatformId = platform,
                Username = username.Trim(),
                DisplayName = displayName.Trim(),
                Avatar = avatar,
                IsDefault = isFirstAccountForPlatform, // First account for platform is default
                CreatedAt = GetSafeUtcNow(),
                LastUsed = GetSafeUtcNow(),
                OAuthConfiguration = oauthConfig,
                Metadata = new Dictionary<string, string>()
            };

            await _accountService.CreateAccountAsync(account);

            var statusMessage = oauthConfig != null
                ? $"✅ Account '{account.DisplayName}' created with OAuth configuration!"
                : $"✅ Account '{account.DisplayName}' created successfully";

            _logger.Information("New account created: {AccountId}", account.Id);

            return new AccountPersistenceResult
            {
                Success = true,
                Account = account,
                StatusMessage = statusMessage
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create account for platform: {Platform}, Username: {Username}", platform, username);
            return new AccountPersistenceResult
            {
                Success = false,
                ErrorMessage = $"Failed to create account: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Updates an existing account with new details
    /// </summary>
    public async Task<AccountPersistenceResult> UpdateAccountAsync(
        Account existingAccount,
        string username,
        string displayName,
        string avatar,
        string? oauthClientId = null,
        string? oauthClientSecret = null,
        string? oauthRedirectUri = null)
    {
        try
        {
            _logger.Information("Updating existing account: {AccountId} ({DisplayName})", existingAccount.Id, existingAccount.DisplayName);

            // Update OAuth configuration if provided
            OAuthConfig? updatedOAuthConfig = existingAccount.OAuthConfiguration;
            if (!string.IsNullOrWhiteSpace(oauthClientId) && !string.IsNullOrWhiteSpace(oauthClientSecret))
            {
                updatedOAuthConfig = await CreateOAuthConfigurationAsync(
                    existingAccount.PlatformId, oauthClientId, oauthClientSecret, oauthRedirectUri);
            }

            // Create updated account
            var updatedAccount = new Account
            {
                Id = existingAccount.Id,
                PlatformId = existingAccount.PlatformId,
                Username = username.Trim(),
                DisplayName = displayName.Trim(),
                Avatar = avatar,
                IsDefault = existingAccount.IsDefault,
                CreatedAt = existingAccount.CreatedAt,
                LastUsed = GetSafeUtcNow(),
                Tokens = existingAccount.Tokens,
                AuthStatus = existingAccount.AuthStatus,
                OAuthConfiguration = updatedOAuthConfig,
                Metadata = existingAccount.Metadata ?? new Dictionary<string, string>()
            };

            await _accountService.UpdateAccountAsync(updatedAccount);

            _logger.Information("Account updated successfully: {AccountId}", updatedAccount.Id);

            return new AccountPersistenceResult
            {
                Success = true,
                Account = updatedAccount,
                StatusMessage = $"✅ Account '{updatedAccount.DisplayName}' updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update account: {AccountId}", existingAccount.Id);
            return new AccountPersistenceResult
            {
                Success = false,
                ErrorMessage = $"Failed to update account: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Deletes an account
    /// </summary>
    public async Task<AccountPersistenceResult> DeleteAccountAsync(Account account)
    {
        try
        {
            _logger.Information("Deleting account: {AccountId} ({DisplayName})", account.Id, account.DisplayName);

            await _accountService.DeleteAccountAsync(account.Id);

            return new AccountPersistenceResult
            {
                Success = true,
                StatusMessage = $"✅ Account '{account.DisplayName}' deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete account: {AccountId}", account.Id);
            return new AccountPersistenceResult
            {
                Success = false,
                ErrorMessage = $"Failed to delete account: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Sets an account as the default for its platform
    /// </summary>
    public async Task<AccountPersistenceResult> SetAsDefaultAsync(Account account)
    {
        try
        {
            _logger.Information("Setting account as default: {AccountId} ({DisplayName})", account.Id, account.DisplayName);

            await _accountService.SetDefaultAccountAsync(account.Id);

            return new AccountPersistenceResult
            {
                Success = true,
                StatusMessage = $"✅ '{account.DisplayName}' set as default for {account.PlatformId}"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set default account: {AccountId}", account.Id);
            return new AccountPersistenceResult
            {
                Success = false,
                ErrorMessage = $"Failed to set default account: {ex.Message}",
                Exception = ex
            };
        }
    }

    private async Task<OAuthConfig> CreateOAuthConfigurationAsync(
        SocialPlatform platform,
        string clientId,
        string clientSecret,
        string? redirectUri)
    {
        var oauthConfig = _oauthConfigService.GetDefaultConfiguration(platform);
        oauthConfig.ClientId = clientId;
        oauthConfig.ClientSecret = clientSecret;
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            oauthConfig.RedirectUri = redirectUri;
        }

        // Save OAuth configuration globally for the platform
        await _oauthConfigService.SaveConfigurationAsync(platform, oauthConfig);
        _logger.Information("OAuth configuration saved for platform: {Platform}", platform);

        return oauthConfig;
    }

    private static DateTime GetSafeUtcNow()
    {
        try
        {
            return DateTime.UtcNow;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Fallback to a safe date if DateTime.UtcNow overflows
            return new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}

/// <summary>
/// Result of account persistence operation
/// </summary>
public class AccountPersistenceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }
    public Exception? Exception { get; set; }
    public Account? Account { get; set; }
}