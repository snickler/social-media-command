using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Lightweight test implementation of IAuthenticationService that builds authorization URLs
/// from configuration but does not start listeners or open browsers.
/// </summary>
public class TestAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOAuthConfigurationService _configService;

    public TestAuthenticationService(HttpClient httpClient, IOAuthConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    public async Task<AuthenticationResult> StartAuthenticationAsync(SocialPlatform platform, string? redirectUri = null)
    {
        var config = await _configService.GetConfigurationAsync(platform);
        if (config == null)
        {
            return new AuthenticationResult { IsSuccess = false, ErrorMessage = $"OAuth configuration not found for platform: {platform}." };
        }

        var validation = await _configService.ValidateConfigurationAsync(platform, config);
        if (!validation.IsValid)
        {
            return new AuthenticationResult { IsSuccess = false, ErrorMessage = $"Invalid OAuth configuration for {platform}: {string.Join(", ", validation.Errors)}" };
        }

        var state = Guid.NewGuid().ToString("N");

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = config.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
            ["scope"] = string.Join(" ", config.Scopes)
        };

        foreach (var p in config.AdditionalParameters ?? new Dictionary<string,string>())
            queryParams[p.Key] = p.Value;

        var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var authUrl = $"{config.AuthorizationEndpoint}?{queryString}";

        return new AuthenticationResult
        {
            IsSuccess = true,
            AuthorizationUrl = authUrl,
            State = state
        };
    }

    public Task<AuthenticationResult> CompleteAuthenticationAsync(SocialPlatform platform, string authorizationCode, string? state = null)
    {
        // Tests expect invalid authorization code to return IsSuccess=false
        if (string.IsNullOrWhiteSpace(authorizationCode) || authorizationCode.StartsWith("invalid"))
        {
            return Task.FromResult(new AuthenticationResult { IsSuccess = false, ErrorMessage = "Failed to exchange authorization code" });
        }

        // Not used by current integration tests; return not implemented as a safe default
        return Task.FromResult(new AuthenticationResult { IsSuccess = false, ErrorMessage = "Not implemented" });
    }

    public Task<AuthenticationResult> RefreshTokenAsync(SocialPlatform platform, string refreshToken)
    {
        return Task.FromResult(new AuthenticationResult { IsSuccess = false, ErrorMessage = "Not implemented" });
    }

    public Task<bool> RevokeTokenAsync(SocialPlatform platform, string accessToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> ValidateTokenAsync(SocialPlatform platform, string accessToken)
    {
        // Return false for tokens that contain the word "invalid" to satisfy tests
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.StartsWith("invalid"))
            return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public async Task<UserProfile?> GetUserProfileAsync(SocialPlatform platform, string accessToken)
    {
        // Tests expect null for invalid token
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.StartsWith("invalid"))
            return null;

        // Not required for current tests
        return null;
    }

    public OAuthConfig GetOAuthConfig(SocialPlatform platform)
    {
        var cfg = _configService.GetConfigurationAsync(platform).GetAwaiter().GetResult();
        return cfg ?? _configService.GetDefaultConfiguration(platform);
    }

    public void Dispose()
    {
    }
}
