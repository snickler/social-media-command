using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for OAuthConfigurationService functionality
/// </summary>
public class OAuthConfigurationServiceTests : IDisposable
{
    private readonly OAuthConfigurationService _oauthConfigService;
    private readonly string _tempDirectory;

    public OAuthConfigurationServiceTests()
    {
        // Create unique temp directory for this test instance
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SMC_OAuthTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        // Use the new constructor that accepts a custom directory
        _oauthConfigService = new OAuthConfigurationService(_tempDirectory);
    }

    [Fact]
    public void GetDefaultConfiguration_AllPlatforms_ShouldReturnValidConfigs()
    {
        // Test each platform
        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            // Act
            var config = _oauthConfigService.GetDefaultConfiguration(platform);

            // Assert
            config.Should().NotBeNull();
            // Note: ClientId and ClientSecret are empty in templates - that's expected
            config.AuthorizationEndpoint.Should().NotBeNullOrEmpty();
            config.TokenEndpoint.Should().NotBeNullOrEmpty();
            config.RedirectUri.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetConfigurationAsync_NonExistentConfig_ShouldReturnNull()
    {
        // Act
        var config = await _oauthConfigService.GetConfigurationAsync(SocialPlatform.X);

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public async Task SaveConfigurationAsync_ValidConfig_ShouldPersistConfiguration()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var testConfig = CreateTestOAuthConfig();

        // Act
        await _oauthConfigService.SaveConfigurationAsync(platform, testConfig);
        var retrievedConfig = await _oauthConfigService.GetConfigurationAsync(platform);

        // Assert
        retrievedConfig.Should().NotBeNull();
        retrievedConfig!.ClientId.Should().Be(testConfig.ClientId);
        retrievedConfig.ClientSecret.Should().Be(testConfig.ClientSecret);
        retrievedConfig.AuthorizationEndpoint.Should().Be(testConfig.AuthorizationEndpoint);
        retrievedConfig.TokenEndpoint.Should().Be(testConfig.TokenEndpoint);
        retrievedConfig.RedirectUri.Should().Be(testConfig.RedirectUri);
    }

    [Fact]
    public async Task SaveConfigurationAsync_NullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _oauthConfigService.SaveConfigurationAsync(SocialPlatform.X, null!));
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_WithMultipleConfigs_ShouldReturnAllConfigs()
    {
        // Arrange
        var xConfig = CreateTestOAuthConfig();
        var blueskyConfig = CreateTestOAuthConfig();
        blueskyConfig.ClientId = "bluesky_client";

        await _oauthConfigService.SaveConfigurationAsync(SocialPlatform.X, xConfig);
        await _oauthConfigService.SaveConfigurationAsync(SocialPlatform.BlueSky, blueskyConfig);

        // Act
        var allConfigs = await _oauthConfigService.GetAllConfigurationsAsync();

        // Assert
        allConfigs.Should().HaveCount(2);
        allConfigs.Should().ContainKey(SocialPlatform.X);
        allConfigs.Should().ContainKey(SocialPlatform.BlueSky);
        allConfigs[SocialPlatform.X].ClientId.Should().Be(xConfig.ClientId);
        allConfigs[SocialPlatform.BlueSky].ClientId.Should().Be(blueskyConfig.ClientId);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ExistingConfig_ShouldRemoveConfiguration()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var testConfig = CreateTestOAuthConfig();
        await _oauthConfigService.SaveConfigurationAsync(platform, testConfig);

        // Verify it exists
        var configBeforeDelete = await _oauthConfigService.GetConfigurationAsync(platform);
        configBeforeDelete.Should().NotBeNull();

        // Act
        await _oauthConfigService.DeleteConfigurationAsync(platform);

        // Assert
        var configAfterDelete = await _oauthConfigService.GetConfigurationAsync(platform);
        configAfterDelete.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfigurationAsync_NonExistentConfig_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _oauthConfigService.DeleteConfigurationAsync(SocialPlatform.X);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ValidConfig_ShouldReturnSuccess()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateTestOAuthConfig();

        // Act
        var result = await _oauthConfigService.ValidateConfigurationAsync(platform, validConfig);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateConfigurationAsync_InvalidConfig_ShouldReturnErrors()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var invalidConfig = new OAuthConfig
        {
            ClientId = "", // Invalid: empty
            ClientSecret = "secret",
            AuthorizationEndpoint = "invalid-url", // Invalid: not a valid URL
            TokenEndpoint = "",  // Invalid: empty
            RedirectUri = "http://localhost:8080/callback"
        };

        // Act
        var result = await _oauthConfigService.ValidateConfigurationAsync(platform, invalidConfig);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Client ID"));
        result.Errors.Should().Contain(e => e.Contains("Token Endpoint"));
    }

    [Fact]
    public async Task HasValidConfigurationAsync_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var validConfig = CreateTestOAuthConfig();
        await _oauthConfigService.SaveConfigurationAsync(platform, validConfig);

        // Act
        var hasValid = await _oauthConfigService.HasValidConfigurationAsync(platform);

        // Assert
        hasValid.Should().BeTrue();
    }

    [Fact]
    public async Task HasValidConfigurationAsync_WithoutConfig_ShouldReturnFalse()
    {
        // Act
        var hasValid = await _oauthConfigService.HasValidConfigurationAsync(SocialPlatform.X);

        // Assert
        hasValid.Should().BeFalse();
    }

    [Fact]
    public async Task HasValidConfigurationAsync_WithInvalidConfig_ShouldReturnFalse()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var invalidConfig = new OAuthConfig
        {
            ClientId = "", // Invalid
            ClientSecret = "secret",
            AuthorizationEndpoint = "https://example.com/auth",
            TokenEndpoint = "https://example.com/token",
            RedirectUri = "http://localhost:8080/callback"
        };
        await _oauthConfigService.SaveConfigurationAsync(platform, invalidConfig);

        // Act
        var hasValid = await _oauthConfigService.HasValidConfigurationAsync(platform);

        // Assert
        hasValid.Should().BeFalse();
    }

    [Fact]
    public async Task ConfigurationPersistence_ShouldSurviveServiceRecreation()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var testConfig = CreateTestOAuthConfig();
        await _oauthConfigService.SaveConfigurationAsync(platform, testConfig);

        // Act - Create new service instance
        var newService = new OAuthConfigurationService(_tempDirectory);
        var loadedConfig = await newService.GetConfigurationAsync(platform);

        // Assert
        loadedConfig.Should().NotBeNull();
        loadedConfig!.ClientId.Should().Be(testConfig.ClientId);
        loadedConfig.ClientSecret.Should().Be(testConfig.ClientSecret);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_EmptyService_ShouldReturnEmptyDictionary()
    {
        // Act
        var allConfigs = await _oauthConfigService.GetAllConfigurationsAsync();

        // Assert
        allConfigs.Should().NotBeNull();
        allConfigs.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveConfigurationAsync_UpdateExistingConfig_ShouldOverwriteConfiguration()
    {
        // Arrange
        var platform = SocialPlatform.X;
        var originalConfig = CreateTestOAuthConfig();
        var updatedConfig = CreateTestOAuthConfig();
        updatedConfig.ClientId = "updated_client_id";
        updatedConfig.ClientSecret = "updated_secret";

        // Act
        await _oauthConfigService.SaveConfigurationAsync(platform, originalConfig);
        await _oauthConfigService.SaveConfigurationAsync(platform, updatedConfig);
        var retrievedConfig = await _oauthConfigService.GetConfigurationAsync(platform);

        // Assert
        retrievedConfig.Should().NotBeNull();
        retrievedConfig!.ClientId.Should().Be("updated_client_id");
        retrievedConfig.ClientSecret.Should().Be("updated_secret");
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var platforms = Enum.GetValues<SocialPlatform>();

        // Act - Save configurations concurrently
        foreach (var platform in platforms)
        {
            var config = CreateTestOAuthConfig();
            config.ClientId = $"client_{platform}";
            tasks.Add(_oauthConfigService.SaveConfigurationAsync(platform, config));
        }

        await Task.WhenAll(tasks);

        // Assert
        var allConfigs = await _oauthConfigService.GetAllConfigurationsAsync();
        allConfigs.Should().HaveCount(platforms.Length);

        foreach (var platform in platforms)
        {
            allConfigs.Should().ContainKey(platform);
            allConfigs[platform].ClientId.Should().Be($"client_{platform}");
        }
    }

    [Fact]
    public void GetDefaultConfiguration_BlueSky_ShouldHaveCorrectEndpoints()
    {
        // Act
        var config = _oauthConfigService.GetDefaultConfiguration(SocialPlatform.BlueSky);

        // Assert
        config.Should().NotBeNull();
        config.AuthorizationEndpoint.Should().Contain("bsky.social");
        config.TokenEndpoint.Should().Contain("bsky.social");
    }

    [Fact]
    public void GetDefaultConfiguration_X_ShouldHaveCorrectEndpoints()
    {
        // Act
        var config = _oauthConfigService.GetDefaultConfiguration(SocialPlatform.X);

        // Assert
        config.Should().NotBeNull();
        config.AuthorizationEndpoint.Should().Contain("twitter.com");
        config.TokenEndpoint.Should().Contain("twitter.com");
    }

    private static OAuthConfig CreateTestOAuthConfig()
    {
        return new OAuthConfig
        {
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            AuthorizationEndpoint = "https://api.example.com/oauth/authorize",
            TokenEndpoint = "https://api.example.com/oauth/token",
            UserInfoEndpoint = "https://api.example.com/oauth/userinfo",
            RedirectUri = "http://localhost:8080/callback",
            Scopes = ["read", "write"]
        };
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}