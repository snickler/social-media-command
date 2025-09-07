using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Desktop.Helpers;

/// <summary>
/// Handles OAuth authentication flows for social media platforms
/// Extracted from AccountManagerViewModel to reduce cyclic complexity
/// </summary>
public class OAuthAuthenticationHandler
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountService _accountService;
    private readonly ILogger _logger;

    public OAuthAuthenticationHandler(
        IAuthenticationService authenticationService,
        IAccountService accountService,
        ILogger logger)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts the OAuth authentication process for the specified platform
    /// </summary>
    public async Task<OAuthAuthenticationResult> StartAuthenticationAsync(
        SocialPlatform platform,
        Account? existingAccount = null,
        Action<string>? statusCallback = null)
    {
        try
        {
            statusCallback?.Invoke($"Starting authentication for {PlatformConfigurations.GetPlatformConfig(platform).Name}...");

            var result = await _authenticationService.StartAuthenticationAsync(platform);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.AuthorizationUrl))
            {
                statusCallback?.Invoke("Opening browser for authentication...");

                // Open the authorization URL in the default browser
                var startInfo = new ProcessStartInfo
                {
                    FileName = result.AuthorizationUrl,
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                statusCallback?.Invoke("Waiting for authorization...");

                // TODO: In a real implementation, listen for the redirect callback
                // For now, simulate a successful authentication after a delay
                await Task.Delay(3000);

                // Simulate completing the OAuth flow
                var completeResult = await _authenticationService.CompleteAuthenticationAsync(
                    platform, "mock_auth_code", result.State ?? "");

                if (completeResult.IsSuccess && completeResult.Tokens != null)
                {
                    statusCallback?.Invoke("Authentication successful! Creating account...");

                    if (existingAccount != null)
                    {
                        return await UpdateExistingAccountAsync(existingAccount, completeResult.Tokens, statusCallback);
                    }
                    else
                    {
                        return await CreateNewAccountAsync(platform, completeResult, statusCallback);
                    }
                }
                else
                {
                    var errorMessage = $"Authentication failed: {completeResult.ErrorMessage}";
                    statusCallback?.Invoke(errorMessage);
                    return new OAuthAuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }
            }
            else
            {
                var errorMessage = $"Failed to start authentication: {result.ErrorMessage}";
                statusCallback?.Invoke(errorMessage);
                return new OAuthAuthenticationResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Authentication error: {ex.Message}";
            statusCallback?.Invoke(errorMessage);
            _logger.Error(ex, "OAuth authentication failed for platform: {Platform}", platform);

            return new OAuthAuthenticationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = ex
            };
        }
    }

    private async Task<OAuthAuthenticationResult> UpdateExistingAccountAsync(
        Account existingAccount,
        OAuthTokens tokens,
        Action<string>? statusCallback)
    {
        try
        {
            // Update existing account with new tokens
            existingAccount.Tokens = tokens;
            existingAccount.LastUsed = DateTime.UtcNow;
            existingAccount.AuthStatus = AuthenticationStatus.Authenticated;

            await _accountService.UpdateAccountAsync(existingAccount);

            statusCallback?.Invoke("Account reconnected successfully!");

            return new OAuthAuthenticationResult
            {
                Success = true,
                Account = existingAccount,
                IsExistingAccount = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update existing account: {AccountId}", existingAccount.Id);
            throw;
        }
    }

    private async Task<OAuthAuthenticationResult> CreateNewAccountAsync(
        SocialPlatform platform,
        AuthenticationResult completeResult,
        Action<string>? statusCallback)
    {
        try
        {
            // Create new account with safe DateTime handling
            var newAccount = new Account
            {
                Id = Guid.NewGuid().ToString(),
                PlatformId = platform,
                Username = completeResult.UserProfile?.Username ?? $"user_{DateTime.Now.Ticks}",
                DisplayName = completeResult.UserProfile?.DisplayName ?? $"User {DateTime.Now:HH:mm}",
                Avatar = completeResult.UserProfile?.Avatar ??
                    $"https://api.dicebear.com/7.x/personas/svg?seed={platform}-{DateTime.Now.Ticks}",
                IsDefault = false, // Will be set by caller based on existing accounts
                CreatedAt = GetSafeUtcNow(),
                LastUsed = GetSafeUtcNow(),
                Tokens = completeResult.Tokens,
                AuthStatus = AuthenticationStatus.Authenticated,
                Metadata = new Dictionary<string, string>()
            };

            await _accountService.CreateAccountAsync(newAccount);

            statusCallback?.Invoke("Account connected successfully!");

            return new OAuthAuthenticationResult
            {
                Success = true,
                Account = newAccount,
                IsExistingAccount = false
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create new account for platform: {Platform}", platform);
            throw;
        }
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
/// Result of OAuth authentication operation
/// </summary>
public class OAuthAuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public Account? Account { get; set; }
    public bool IsExistingAccount { get; set; }
}