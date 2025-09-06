using System.Collections.Generic;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Tests.Integration;

/// <summary>
/// Simple in-memory OAuth configuration service for tests.
/// Provides valid default configurations without file IO or encryption.
/// </summary>
public class TestOAuthConfigurationService : IOAuthConfigurationService
{
    private readonly Dictionary<SocialPlatform, OAuthConfig> _configs;

    public TestOAuthConfigurationService()
    {
        _configs = new Dictionary<SocialPlatform, OAuthConfig>
        {
            [SocialPlatform.BlueSky] = new OAuthConfig
            {
                AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
                TokenEndpoint = "https://bsky.social/oauth/token",
                UserInfoEndpoint = "https://bsky.social/xrpc/com.atproto.server.getSession",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "read", "write" },
                ClientId = "test_client",
                ClientSecret = "test_secret",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code",
                    ["code_challenge_method"] = "S256"
                }
            },
            [SocialPlatform.X] = new OAuthConfig
            {
                AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
                TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
                UserInfoEndpoint = "https://api.twitter.com/2/users/me",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "tweet.read", "tweet.write", "users.read", "offline.access" },
                ClientId = "test_client",
                ClientSecret = "test_secret",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code",
                    ["code_challenge_method"] = "S256"
                }
            },
            [SocialPlatform.LinkedIn] = new OAuthConfig
            {
                AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization",
                TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken",
                UserInfoEndpoint = "https://api.linkedin.com/v2/people/~",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "r_liteprofile", "r_emailaddress", "w_member_social" },
                ClientId = "test_client",
                ClientSecret = "test_secret",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            },
            [SocialPlatform.Threads] = new OAuthConfig
            {
                AuthorizationEndpoint = "https://threads.net/oauth/authorize",
                TokenEndpoint = "https://graph.threads.net/oauth/access_token",
                UserInfoEndpoint = "https://graph.threads.net/v1.0/me",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "threads_basic", "threads_content_publish" },
                ClientId = "test_client",
                ClientSecret = "test_secret",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            },
            [SocialPlatform.Facebook] = new OAuthConfig
            {
                AuthorizationEndpoint = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenEndpoint = "https://graph.facebook.com/v18.0/oauth/access_token",
                UserInfoEndpoint = "https://graph.facebook.com/v18.0/me",
                RedirectUri = "http://localhost:8080/oauth/callback",
                Scopes = new[] { "pages_manage_posts", "pages_read_engagement", "public_profile" },
                ClientId = "test_client",
                ClientSecret = "test_secret",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["response_type"] = "code"
                }
            }
        };
    }

    public Task<OAuthConfig?> GetConfigurationAsync(SocialPlatform platform)
    {
        return Task.FromResult(_configs.TryGetValue(platform, out var cfg) ? cfg : null);
    }

    public Task SaveConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        _configs[platform] = config;
        return Task.CompletedTask;
    }

    public Task<Dictionary<SocialPlatform, OAuthConfig>> GetAllConfigurationsAsync()
    {
        return Task.FromResult(new Dictionary<SocialPlatform, OAuthConfig>(_configs));
    }

    public Task DeleteConfigurationAsync(SocialPlatform platform)
    {
        _configs.Remove(platform);
        return Task.CompletedTask;
    }

    public Task<ValidationResult> ValidateConfigurationAsync(SocialPlatform platform, OAuthConfig config)
    {
        // Consider configs valid for tests
        return Task.FromResult(new ValidationResult());
    }

    public OAuthConfig GetDefaultConfiguration(SocialPlatform platform)
    {
        return _configs.TryGetValue(platform, out var cfg) ? cfg : null!;
    }

    public Task<bool> HasValidConfigurationAsync(SocialPlatform platform)
    {
        return Task.FromResult(true);
    }

    public Task ImportConfigurationsAsync(string filePath)
    {
        return Task.CompletedTask;
    }

    public Task ExportConfigurationsAsync(string filePath)
    {
        return Task.CompletedTask;
    }
}
