using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Test implementation of IAuthenticationService that provides deterministic test data
/// without starting HTTP listeners or performing network operations
/// </summary>
public class TestAuthenticationService : IAuthenticationService
{
    private readonly IOAuthConfigurationService _configService;

    public TestAuthenticationService(IOAuthConfigurationService configService)
    {
        _configService = configService;
    }

    public Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null)
    {
        try
        {
            var config = _configService.GetDefaultConfiguration(platform);
            var state = Guid.NewGuid().ToString("N")[..16]; // Generate deterministic test state
            
            var authUrl = $"{config.AuthorizationEndpoint}?" +
                         $"client_id={config.ClientId}&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri ?? config.RedirectUri)}&" +
                         $"response_type=code&" +
                         $"state={state}&" +
                         $"scope={string.Join(" ", config.Scopes)}";

            var result = new AuthenticationResult
            {
                IsSuccess = true,
                AuthorizationUrl = authUrl,
                State = state,
                ErrorMessage = null
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            var result = new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = $"OAuth configuration not found for platform {platform}: {ex.Message}"
            };

            return Task.FromResult(result);
        }
    }

    public Task<AuthenticationResult> CompleteAuthenticationAsync(SocialPlatform platform, string authorizationCode, string? state = null)
    {
        // Simulate token exchange failure for test purposes (no real network calls)
        var result = new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = "Failed to exchange authorization code for access token (test implementation)",
            Tokens = null,
            UserProfile = null
        };

        return Task.FromResult(result);
    }

    public Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken)
    {
        // For test purposes, return false for all tokens (no real validation)
        return Task.FromResult(false);
    }

    public Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken)
    {
        // For test purposes, return false (no real revocation)
        return Task.FromResult(false);
    }

    public Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken)
    {
        // Simulate not implemented for test purposes
        var result = new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = "Not implemented"
        };

        return Task.FromResult(result);
    }

    public Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken)
    {
        // For test purposes, return null (no real profile retrieval)
        return Task.FromResult<UserProfile?>(null);
    }

    public OAuthConfig GetOAuthConfig(SocialPlatform platform)
    {
        return _configService.GetDefaultConfiguration(platform);
    }
}