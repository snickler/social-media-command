using SocialMediaCommander.Core.Models;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for OAuthConfig and ConfigurationStatus models
/// </summary>
public class OAuthConfigModelTests
{
    [Fact]
    public void OAuthConfig_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var config = new OAuthConfig();

        // Assert
        config.ClientId.Should().BeEmpty();
        config.ClientSecret.Should().BeEmpty();
        config.AuthorizationEndpoint.Should().BeEmpty();
        config.TokenEndpoint.Should().BeEmpty();
        config.RevokeEndpoint.Should().BeNull();
        config.UserInfoEndpoint.Should().BeNull();
        config.RedirectUri.Should().BeEmpty();
        config.Scopes.Should().NotBeNull().And.BeEmpty();
        config.AdditionalParameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void OAuthConfig_WithFullConfiguration_ShouldStoreAllValues()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            AuthorizationEndpoint = "https://example.com/oauth/authorize",
            TokenEndpoint = "https://example.com/oauth/token",
            RevokeEndpoint = "https://example.com/oauth/revoke",
            UserInfoEndpoint = "https://example.com/oauth/userinfo",
            RedirectUri = "https://localhost:8080/callback",
            Scopes = new[] { "read", "write", "profile" },
            AdditionalParameters = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "state", "random_state" }
            }
        };

        // Assert
        config.ClientId.Should().Be("test_client_id");
        config.ClientSecret.Should().Be("test_client_secret");
        config.AuthorizationEndpoint.Should().Be("https://example.com/oauth/authorize");
        config.TokenEndpoint.Should().Be("https://example.com/oauth/token");
        config.RevokeEndpoint.Should().Be("https://example.com/oauth/revoke");
        config.UserInfoEndpoint.Should().Be("https://example.com/oauth/userinfo");
        config.RedirectUri.Should().Be("https://localhost:8080/callback");
        config.Scopes.Should().BeEquivalentTo("read", "write", "profile");
        config.AdditionalParameters.Should().Contain("response_type", "code");
        config.AdditionalParameters.Should().Contain("state", "random_state");
    }

    [Fact]
    public void OAuthConfig_WithMinimalConfiguration_ShouldWork()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            ClientId = "minimal_client",
            ClientSecret = "minimal_secret",
            AuthorizationEndpoint = "https://auth.example.com",
            TokenEndpoint = "https://token.example.com",
            RedirectUri = "https://callback.example.com"
        };

        // Assert
        config.ClientId.Should().Be("minimal_client");
        config.ClientSecret.Should().Be("minimal_secret");
        config.AuthorizationEndpoint.Should().Be("https://auth.example.com");
        config.TokenEndpoint.Should().Be("https://token.example.com");
        config.RedirectUri.Should().Be("https://callback.example.com");
        config.RevokeEndpoint.Should().BeNull();
        config.UserInfoEndpoint.Should().BeNull();
    }

    [Fact]
    public void OAuthConfig_ScopesArray_ShouldSupportMultipleValues()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            Scopes = new[] { "scope1", "scope2", "scope3", "scope:special" }
        };

        // Assert
        config.Scopes.Should().HaveCount(4);
        config.Scopes.Should().Contain("scope1");
        config.Scopes.Should().Contain("scope2");
        config.Scopes.Should().Contain("scope3");
        config.Scopes.Should().Contain("scope:special");
    }

    [Fact]
    public void OAuthConfig_AdditionalParameters_ShouldSupportVariousTypes()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            AdditionalParameters = new Dictionary<string, string>
            {
                { "string_param", "string_value" },
                { "number_param", "123" },
                { "boolean_param", "true" },
                { "url_param", "https://example.com/endpoint" },
                { "empty_param", "" }
            }
        };

        // Assert
        config.AdditionalParameters.Should().HaveCount(5);
        config.AdditionalParameters["string_param"].Should().Be("string_value");
        config.AdditionalParameters["number_param"].Should().Be("123");
        config.AdditionalParameters["boolean_param"].Should().Be("true");
        config.AdditionalParameters["url_param"].Should().Be("https://example.com/endpoint");
        config.AdditionalParameters["empty_param"].Should().BeEmpty();
    }

    [Fact]
    public void OAuthConfig_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            ClientId = "client@example.com",
            ClientSecret = "secret_with_!@#$%^&*()_+",
            AuthorizationEndpoint = "https://example.com/auth?param=value&other=test",
            TokenEndpoint = "https://example.com/token",
            RedirectUri = "https://localhost:8080/callback?app=test"
        };

        // Assert
        config.ClientId.Should().Be("client@example.com");
        config.ClientSecret.Should().Be("secret_with_!@#$%^&*()_+");
        config.AuthorizationEndpoint.Should().Be("https://example.com/auth?param=value&other=test");
        config.RedirectUri.Should().Be("https://localhost:8080/callback?app=test");
    }
}

/// <summary>
/// Unit tests for ConfigurationStatus model
/// </summary>
public class ConfigurationStatusTests
{
    [Fact]
    public void ConfigurationStatus_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var status = new ConfigurationStatus();

        // Assert
        status.IsConfigured.Should().BeFalse();
        status.HasPlaceholders.Should().BeFalse();
        status.Status.Should().BeEmpty();
        status.Message.Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationStatus_WithConfiguredState_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            IsConfigured = true,
            HasPlaceholders = false,
            Status = "Ready",
            Message = "OAuth configuration is properly set up and ready to use."
        };

        // Assert
        status.IsConfigured.Should().BeTrue();
        status.HasPlaceholders.Should().BeFalse();
        status.Status.Should().Be("Ready");
        status.Message.Should().Be("OAuth configuration is properly set up and ready to use.");
    }

    [Fact]
    public void ConfigurationStatus_WithPlaceholderState_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            IsConfigured = false,
            HasPlaceholders = true,
            Status = "Needs setup",
            Message = "Please replace placeholder values with your actual OAuth credentials."
        };

        // Assert
        status.IsConfigured.Should().BeFalse();
        status.HasPlaceholders.Should().BeTrue();
        status.Status.Should().Be("Needs setup");
        status.Message.Should().Be("Please replace placeholder values with your actual OAuth credentials.");
    }

    [Fact]
    public void ConfigurationStatus_WithInvalidState_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            IsConfigured = false,
            HasPlaceholders = false,
            Status = "Invalid",
            Message = "OAuth configuration contains invalid endpoints or credentials."
        };

        // Assert
        status.IsConfigured.Should().BeFalse();
        status.HasPlaceholders.Should().BeFalse();
        status.Status.Should().Be("Invalid");
        status.Message.Should().Be("OAuth configuration contains invalid endpoints or credentials.");
    }

    [Theory]
    [InlineData("Ready")]
    [InlineData("Needs setup")]
    [InlineData("Invalid")]
    [InlineData("Partially configured")]
    [InlineData("Testing")]
    public void ConfigurationStatus_ShouldSupportVariousStatusValues(string statusValue)
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            Status = statusValue
        };

        // Assert
        status.Status.Should().Be(statusValue);
    }

    [Fact]
    public void ConfigurationStatus_WithLongMessage_ShouldStoreCorrectly()
    {
        // Arrange
        var longMessage = "This is a very long configuration status message that provides detailed information about the current state of the OAuth configuration, including specific issues that need to be resolved and steps the user should take to complete the setup process.";

        // Act
        var status = new ConfigurationStatus
        {
            Message = longMessage
        };

        // Assert
        status.Message.Should().Be(longMessage);
        status.Message.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void ConfigurationStatus_WithEmptyValues_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            Status = "",
            Message = ""
        };

        // Assert
        status.Status.Should().BeEmpty();
        status.Message.Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationStatus_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var status = new ConfigurationStatus
        {
            Status = null!,
            Message = null!
        };

        // Assert
        status.Status.Should().BeNull();
        status.Message.Should().BeNull();
    }

    [Fact]
    public void ConfigurationStatus_BooleanProperties_ShouldSupportAllCombinations()
    {
        // Test all combinations of boolean values
        var testCases = new[]
        {
            new { IsConfigured = true, HasPlaceholders = true },
            new { IsConfigured = true, HasPlaceholders = false },
            new { IsConfigured = false, HasPlaceholders = true },
            new { IsConfigured = false, HasPlaceholders = false }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var status = new ConfigurationStatus
            {
                IsConfigured = testCase.IsConfigured,
                HasPlaceholders = testCase.HasPlaceholders
            };

            // Assert
            status.IsConfigured.Should().Be(testCase.IsConfigured);
            status.HasPlaceholders.Should().Be(testCase.HasPlaceholders);
        }
    }
}
