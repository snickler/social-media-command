using System;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Demo;

/// <summary>
/// Demo program showing OAuth configuration functionality
/// </summary>
public class OAuthConfigDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Social Media Commander - OAuth Configuration Demo ===\n");

        // Create OAuth configuration service
        var configService = new OAuthConfigurationService();

        // Demo 1: Get default configurations
        Console.WriteLine("1. Getting default OAuth configurations for all platforms:");
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            var defaultConfig = configService.GetDefaultConfiguration(platform);
            Console.WriteLine($"   {platform}:");
            Console.WriteLine($"     Authorization Endpoint: {defaultConfig.AuthorizationEndpoint}");
            Console.WriteLine($"     Token Endpoint: {defaultConfig.TokenEndpoint}");
            Console.WriteLine($"     Scopes: {string.Join(", ", defaultConfig.Scopes)}");
            Console.WriteLine();
        }

        // Demo 2: Save a custom configuration
        Console.WriteLine("2. Saving custom OAuth configuration for Twitter/X:");
        var customConfig = new OAuthConfig
        {
            ClientId = "your-twitter-client-id",
            ClientSecret = "your-twitter-client-secret",
            AuthorizationEndpoint = "https://twitter.com/i/oauth2/authorize",
            TokenEndpoint = "https://api.twitter.com/2/oauth2/token",
            UserInfoEndpoint = "https://api.twitter.com/2/users/me",
            RedirectUri = "http://localhost:8080/oauth/callback",
            Scopes = new[] { "tweet.read", "tweet.write", "users.read" }
        };

        await configService.SaveConfigurationAsync(SocialPlatform.X, customConfig);
        Console.WriteLine("   Custom configuration saved for Twitter/X!");

        // Demo 3: Validate configuration
        Console.WriteLine("\n3. Validating configurations:");
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            var hasValid = await configService.HasValidConfigurationAsync(platform);
            var config = await configService.GetConfigurationAsync(platform);
            
            if (config != null)
            {
                var validation = await configService.ValidateConfigurationAsync(platform, config);
                Console.WriteLine($"   {platform}: {(validation.IsValid ? "✓ Valid" : "✗ Invalid")}");
                
                if (!validation.IsValid)
                {
                    foreach (var error in validation.Errors)
                    {
                        Console.WriteLine($"     - {error}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"   {platform}: No configuration found");
            }
        }

        // Demo 4: Show how to use with authentication service
        Console.WriteLine("\n4. OAuth Authentication Service Integration:");
        try
        {
            var httpClient = new System.Net.Http.HttpClient();
            var authService = new OAuthAuthenticationService(httpClient, configService);

            // Try to start authentication for Twitter/X (which has a configuration)
            var result = await authService.StartAuthenticationAsync(SocialPlatform.X);
            if (result.IsSuccess)
            {
                Console.WriteLine($"   ✓ Authentication URL generated for Twitter/X: {result.AuthorizationUrl}");
            }
            else
            {
                Console.WriteLine($"   ✗ Authentication failed for Twitter/X: {result.ErrorMessage}");
            }

            // Try to start authentication for BlueSky (which doesn't have a configuration)
            var blueSkyResult = await authService.StartAuthenticationAsync(SocialPlatform.BlueSky);
            if (blueSkyResult.IsSuccess)
            {
                Console.WriteLine($"   ✓ Authentication URL generated for BlueSky: {blueSkyResult.AuthorizationUrl}");
            }
            else
            {
                Console.WriteLine($"   ✗ Authentication failed for BlueSky: {blueSkyResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Error: {ex.Message}");
        }

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("\nTo use OAuth configuration in your application:");
        Console.WriteLine("1. Create an OAuth application on each social media platform");
        Console.WriteLine("2. Set the redirect URI to: http://localhost:8080/oauth/callback");
        Console.WriteLine("3. Use the OAuthConfigurationService to save your client credentials");
        Console.WriteLine("4. The OAuthAuthenticationService will automatically use your configurations");
    }
} 