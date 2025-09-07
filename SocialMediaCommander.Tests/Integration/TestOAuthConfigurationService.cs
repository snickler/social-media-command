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

    public Task<ValidationResult> ValidateConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        // Simple validation for test purposes
        var isValid = !string.IsNullOrEmpty(config.ClientId) &&
                     !string.IsNullOrEmpty(config.ClientSecret) &&
                     !string.IsNullOrEmpty(config.AuthorizationEndpoint) &&
                     !string.IsNullOrEmpty(config.TokenEndpoint);

        var result = new ValidationResult(isValid ? [] : ["Invalid test configuration"]);

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
            },
            [SocialPlatform.X] = new OAuthConfig
            {
                ClientId = "test_twitter_client_id",
                ClientSecret = "test_twitter_client_secret",
                AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["tweet.read", "tweet.write", "users.read", "offline.access"]
            },
            [SocialPlatform.LinkedIn] = new OAuthConfig
            {
                ClientId = "test_linkedin_client_id",
                ClientSecret = "test_linkedin_client_secret",
                AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                UserInfoEndpoint = "https://api.linkedin.com/v2/people/~",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["r_liteprofile", "w_member_social"]
            },
            [SocialPlatform.Threads] = new OAuthConfig
            {
                ClientId = "test_threads_client_id",
                ClientSecret = "test_threads_client_secret",
                AuthorizationEndpoint = "https://threads.net/oauth/authorize",
                TokenEndpoint = "https://graph.threads.net/oauth/access_token",
                UserInfoEndpoint = "https://graph.threads.net/v1.0/me",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["threads_basic", "threads_content_publish"]
            },
            [SocialPlatform.Facebook] = new OAuthConfig
            {
                ClientId = "test_facebook_client_id",
                ClientSecret = "test_facebook_client_secret",
                AuthorizationEndpoint = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v18.0/oauth/access_token",
                UserInfoEndpoint = "https://graph.facebook.com/v18.0/me",
                RedirectUri = "http://localhost:8080/callback",
                Scopes = ["pages_manage_posts", "pages_read_engagement", "pages_read_user_content"]
            }
        };
    }
}