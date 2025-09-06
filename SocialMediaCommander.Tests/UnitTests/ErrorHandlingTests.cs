using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Implementation;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for error handling and edge cases across core services
/// </summary>
public class ErrorHandlingTests
{
    [Fact]
    public void Account_WithInvalidPlatformId_ShouldHandleGracefully()
    {
        // Arrange & Act
        var account = new Account
        {
            PlatformId = (SocialPlatform)999, // Invalid enum value
            Username = "testuser",
            DisplayName = "Test User"
        };

        // Assert - Should not throw during creation
        account.Should().NotBeNull();
        account.PlatformId.Should().Be((SocialPlatform)999);
    }

    [Fact]
    public void Account_GetAvatarUrl_WithInvalidPlatform_ShouldGenerateUrl()
    {
        // Arrange
        var account = new Account
        {
            PlatformId = (SocialPlatform)999, // Invalid enum value
            Id = "test-id"
        };

        // Act
        var avatarUrl = account.GetAvatarUrl();

        // Assert - Should still generate a URL even with invalid platform
        avatarUrl.Should().NotBeNullOrEmpty();
        avatarUrl.Should().StartWith("https://api.dicebear.com/7.x/personas/svg?seed=");
    }

    [Fact]
    public void OAuthTokens_WithFutureExpirationDate_ShouldNotBeExpired()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            AccessToken = "valid_token",
            ExpiresAt = DateTime.UtcNow.AddYears(1) // Far future
        };

        // Act
        var isExpired = tokens.IsExpired;

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void OAuthTokens_WithPastExpirationDate_ShouldBeExpired()
    {
        // Arrange
        var tokens = new OAuthTokens
        {
            AccessToken = "expired_token",
            ExpiresAt = DateTime.UtcNow.AddYears(-1) // Far past
        };

        // Act
        var isExpired = tokens.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void DefaultAccounts_AllPlatforms_ShouldHaveValidData()
    {
        // Act
        var allAccounts = DefaultAccounts.GetAllDefaultAccounts().ToList();

        // Assert
        foreach (var account in allAccounts)
        {
            account.Should().NotBeNull();
            account.Id.Should().NotBeNullOrEmpty();
            account.Username.Should().NotBeNullOrEmpty();
            account.DisplayName.Should().NotBeNullOrEmpty();
            account.IsDefault.Should().BeTrue();
            account.Avatar.Should().NotBeNullOrEmpty();
            
            // Verify the avatar URL is valid format
            account.Avatar.Should().StartWith("https://");
        }
    }

    [Fact]
    public void Account_Metadata_ShouldSupportComplexData()
    {
        // Arrange
        var account = new Account();

        // Act
        account.Metadata["string"] = "value";
        account.Metadata["number"] = "123";
        account.Metadata["json"] = "{\"nested\": \"data\"}";
        account.Metadata["empty"] = "";
        account.Metadata["special_chars"] = "!@#$%^&*()_+";

        // Assert
        account.Metadata.Should().HaveCount(5);
        account.Metadata["string"].Should().Be("value");
        account.Metadata["number"].Should().Be("123");
        account.Metadata["json"].Should().Be("{\"nested\": \"data\"}");
        account.Metadata["empty"].Should().BeEmpty();
        account.Metadata["special_chars"].Should().Be("!@#$%^&*()_+");
    }

    [Fact]
    public void OAuthConfig_WithEmptyValues_ShouldNotThrow()
    {
        // Arrange & Act
        var config = new OAuthConfig
        {
            ClientId = "",
            ClientSecret = "",
            AuthorizationEndpoint = "",
            TokenEndpoint = "",
            RedirectUri = "",
            Scopes = Array.Empty<string>(),
            AdditionalParameters = new Dictionary<string, string>()
        };

        // Assert - Should handle empty values gracefully
        config.Should().NotBeNull();
        config.ClientId.Should().BeEmpty();
        config.ClientSecret.Should().BeEmpty();
        config.Scopes.Should().BeEmpty();
        config.AdditionalParameters.Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationStatus_WithAllCombinations_ShouldWork()
    {
        // Test all valid combinations of configuration status
        var testCases = new[]
        {
            new { IsConfigured = true, HasPlaceholders = false, Status = "Ready", Expected = "Fully configured" },
            new { IsConfigured = false, HasPlaceholders = true, Status = "Needs setup", Expected = "Has placeholders" },
            new { IsConfigured = false, HasPlaceholders = false, Status = "Invalid", Expected = "Invalid configuration" },
            new { IsConfigured = true, HasPlaceholders = true, Status = "Partial", Expected = "Partially configured" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var status = new ConfigurationStatus
            {
                IsConfigured = testCase.IsConfigured,
                HasPlaceholders = testCase.HasPlaceholders,
                Status = testCase.Status,
                Message = testCase.Expected
            };

            // Assert
            status.IsConfigured.Should().Be(testCase.IsConfigured);
            status.HasPlaceholders.Should().Be(testCase.HasPlaceholders);
            status.Status.Should().Be(testCase.Status);
            status.Message.Should().Be(testCase.Expected);
        }
    }

    [Fact]
    public void Account_WithMaximumLengthFields_ShouldPassValidation()
    {
        // Arrange - Use maximum allowed lengths
        var account = new Account
        {
            PlatformId = SocialPlatform.BlueSky,
            Username = new string('a', 50), // Maximum 50 characters
            DisplayName = new string('b', 100) // Maximum 100 characters
        };

        // Act
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(account);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(account, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void OAuthTokens_EdgeCaseExpirationTimes_ShouldBehaveCorrectly()
    {
        var testCases = new[]
        {
            new { ExpiresAt = DateTime.UtcNow.AddMinutes(6), ExpectedExpired = false, Description = "6 minutes future" },
            new { ExpiresAt = DateTime.UtcNow.AddMinutes(5), ExpectedExpired = true, Description = "Exactly 5 minutes future (edge of buffer)" },
            new { ExpiresAt = DateTime.UtcNow.AddMinutes(4), ExpectedExpired = true, Description = "4 minutes future (within buffer)" },
            new { ExpiresAt = DateTime.UtcNow, ExpectedExpired = true, Description = "Current time" },
            new { ExpiresAt = DateTime.UtcNow.AddSeconds(30), ExpectedExpired = true, Description = "30 seconds future" },
            new { ExpiresAt = DateTime.UtcNow.AddMilliseconds(1), ExpectedExpired = true, Description = "1 millisecond future" }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            var tokens = new OAuthTokens
            {
                AccessToken = "test_token",
                ExpiresAt = testCase.ExpiresAt
            };

            // Act
            var isExpired = tokens.IsExpired;

            // Assert
            isExpired.Should().Be(testCase.ExpectedExpired, $"Token expiring at {testCase.ExpiresAt} ({testCase.Description}) should be expired: {testCase.ExpectedExpired}");
        }
    }

    [Fact]
    public void CrossPlatformEncryption_MethodInfo_ShouldBeValid()
    {
        // Act
        var method = CrossPlatformEncryption.GetEncryptionMethod();
        var isSupported = CrossPlatformEncryption.IsSecureEncryptionSupported();

        // Assert
        method.Should().NotBeNullOrEmpty();
        method.Should().ContainAny("DPAPI", "AES");
        isSupported.Should().BeTrue();
    }

    [Fact]
    public void Account_AuthenticationStates_ShouldCoverAllScenarios()
    {
        var testCases = new[]
        {
            new { Status = AuthenticationStatus.NotAuthenticated, HasTokens = false, TokenExpired = false, ExpectedAuth = false },
            new { Status = AuthenticationStatus.Authenticating, HasTokens = false, TokenExpired = false, ExpectedAuth = false },
            new { Status = AuthenticationStatus.Authenticated, HasTokens = true, TokenExpired = false, ExpectedAuth = true },
            new { Status = AuthenticationStatus.Authenticated, HasTokens = true, TokenExpired = true, ExpectedAuth = false },
            new { Status = AuthenticationStatus.Authenticated, HasTokens = false, TokenExpired = false, ExpectedAuth = false },
            new { Status = AuthenticationStatus.AuthenticationFailed, HasTokens = true, TokenExpired = false, ExpectedAuth = false },
            new { Status = AuthenticationStatus.TokenExpired, HasTokens = true, TokenExpired = true, ExpectedAuth = false },
            new { Status = AuthenticationStatus.Revoked, HasTokens = false, TokenExpired = false, ExpectedAuth = false }
        };

        foreach (var testCase in testCases)
        {
            // Arrange
            var account = new Account
            {
                AuthStatus = testCase.Status,
                Tokens = testCase.HasTokens ? new OAuthTokens
                {
                    AccessToken = "test_token",
                    ExpiresAt = testCase.TokenExpired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1)
                } : null
            };

            // Act
            var isAuthenticated = account.IsAuthenticated;

            // Assert
            isAuthenticated.Should().Be(testCase.ExpectedAuth, 
                $"Account with status {testCase.Status}, hasTokens: {testCase.HasTokens}, tokenExpired: {testCase.TokenExpired} should be authenticated: {testCase.ExpectedAuth}");
        }
    }
}