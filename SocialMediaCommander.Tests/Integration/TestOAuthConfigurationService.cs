using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Test implementation of IOAuthConfigurationService that provides deterministic test data
/// without performing file I/O or network operations
/// </summary>
public class TestOAuthConfigurationService : IOAuthConfigurationService
{
    private readonly Dictionary<SocialPlatform, OAuthConfig> _testConfigs;

    public TestOAuthConfigurationService()
    {
        _testConfigs = CreateTestConfigurations();
    }

    public OAuthConfig GetDefaultConfiguration(SocialPlatform platform)
    {
        return _testConfigs.TryGetValue(platform, out var config)
            ? config
            : throw new ArgumentException($"Test configuration not found for platform: {platform}");
    }

    public Task<OAuthConfig?> GetConfigurationAsync(SocialPlatform platform)
    {
        var config = _testConfigs.TryGetValue(platform, out var testConfig) ? testConfig : null;
        return Task.FromResult(config);
    }

    public Task SaveConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        // Store in memory for test purposes
        _testConfigs[platform] = config;
        return Task.CompletedTask;
    }

    public Task<Dictionary<SocialPlatform, OAuthConfig>> GetAllConfigurationsAsync()
    {
        return Task.FromResult(new Dictionary<SocialPlatform, OAuthConfig>(_testConfigs));
    }

    public Task DeleteConfigurationAsync(SocialPlatform platform)
    {
        _testConfigs.Remove(platform);
        return Task.CompletedTask;
    }

    public Task<OAuthValidationResult> ValidateConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        // Simple validation for test purposes
        var isValid = !string.IsNullOrEmpty(config.ClientId) &&
                     !string.IsNullOrEmpty(config.ClientSecret) &&
                     !string.IsNullOrEmpty(config.AuthorizationEndpoint) &&
                     !string.IsNullOrEmpty(config.TokenEndpoint);

        var result = new OAuthValidationResult
        {
            IsValid = isValid,
            Errors = isValid ? new List<string>() : new List<string> { "Invalid test configuration" }
        };

        return Task.FromResult(result);
    }

    public Task<bool> HasValidConfigurationAsync(SocialPlatform platform)
    {
        var hasConfig = _testConfigs.ContainsKey(platform);
        return Task.FromResult(hasConfig);
    }

    public Task ImportConfigurationsAsync(string filePath)
    {
        // No-op for test purposes (no actual file I/O)
        return Task.CompletedTask;
    }

    public Task ExportConfigurationsAsync(string filePath)
    {
        // No-op for test purposes (no actual file I/O)
        return Task.CompletedTask;
    }

    private static Dictionary<SocialPlatform, OAuthConfig> CreateTestConfigurations()
    {
        return new Dictionary<SocialPlatform, OAuthConfig>
        {
            [SocialPlatform.BlueSky] = new OAuthConfig
            {
                ClientId = "test_bluesky_client_id",
                ClientSecret = "test_bluesky_client_secret",
                AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
                TokenEndpoint = "https://bsky.social/oauth/token",
                UserInfoEndpoint = "https://bsky.social/oauth/userinfo",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["read", "write", "follow"]
            }
        };
    }
}
