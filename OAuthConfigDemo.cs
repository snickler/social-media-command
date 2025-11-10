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
        Console.WriteLine("2. Saving custom OAuth configuration for BlueSky:");
        var customConfig = new OAuthConfig
        {
            ClientId = "your-bluesky-client-id",
            ClientSecret = "your-bluesky-client-secret",
            AuthorizationEndpoint = "https://bsky.social/oauth/authorize",
            TokenEndpoint = "https://bsky.social/oauth/token",
            UserInfoEndpoint = "https://bsky.social/xrpc/com.atproto.server.getSession",
            RedirectUri = "http://localhost:8080/oauth/callback",
            Scopes = new[] { "atproto", "transition:generic" }
        };

        await configService.SaveConfigurationAsync(SocialPlatform.BlueSky, customConfig);
        Console.WriteLine("   Custom configuration saved for BlueSky!");

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

            // Try to start authentication for BlueSky (which has a configuration)
            var result = await authService.StartAuthenticationAsync(SocialPlatform.BlueSky);
            if (result.IsSuccess)
            {
                Console.WriteLine($"   ✓ Authentication URL generated for BlueSky: {result.AuthorizationUrl}");
            }
            else
            {
                Console.WriteLine($"   ✗ Authentication failed for BlueSky: {result.ErrorMessage}");
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