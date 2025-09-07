using Xunit;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests.UnitTests;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_ShouldInitializeWithDefaults()
    {
        // Act
        var userProfile = new UserProfile();

        // Assert
        Assert.Equal(string.Empty, userProfile.Id);
        Assert.Equal(string.Empty, userProfile.Username);
        Assert.Equal(string.Empty, userProfile.DisplayName);
        Assert.Null(userProfile.Bio);
        Assert.Null(userProfile.Avatar);
        Assert.Null(userProfile.Banner);
        Assert.Null(userProfile.ProfileUrl);
        Assert.Null(userProfile.Website);
        Assert.Null(userProfile.Location);
        Assert.Equal(0, userProfile.FollowerCount);
        Assert.Equal(0, userProfile.FollowingCount);
        Assert.Equal(0, userProfile.PostCount);
        Assert.False(userProfile.IsVerified);
        Assert.Equal(default(DateTime), userProfile.CreatedAt);
        Assert.Equal(default(SocialPlatform), userProfile.Platform);
        Assert.NotNull(userProfile.AdditionalData);
        Assert.Empty(userProfile.AdditionalData);
    }

    [Fact]
    public void UserProfile_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var additionalData = new Dictionary<string, object>
        {
            { "custom_field", "value" },
            { "follower_ratio", 2.5 }
        };

        // Act
        var userProfile = new UserProfile
        {
            Id = "user123",
            Username = "johndoe",
            DisplayName = "John Doe",
            Bio = "Software developer and tech enthusiast",
            Avatar = "https://example.com/avatar.jpg",
            Banner = "https://example.com/banner.jpg",
            ProfileUrl = "https://x.com/johndoe",
            Website = "https://johndoe.dev",
            Location = "San Francisco, CA",
            FollowerCount = 1500,
            FollowingCount = 750,
            PostCount = 250,
            IsVerified = true,
            CreatedAt = createdAt,
            Platform = SocialPlatform.X,
            AdditionalData = additionalData
        };

        // Assert
        Assert.Equal("user123", userProfile.Id);
        Assert.Equal("johndoe", userProfile.Username);
        Assert.Equal("John Doe", userProfile.DisplayName);
        Assert.Equal("Software developer and tech enthusiast", userProfile.Bio);
        Assert.Equal("https://example.com/avatar.jpg", userProfile.Avatar);
        Assert.Equal("https://example.com/banner.jpg", userProfile.Banner);
        Assert.Equal("https://x.com/johndoe", userProfile.ProfileUrl);
        Assert.Equal("https://johndoe.dev", userProfile.Website);
        Assert.Equal("San Francisco, CA", userProfile.Location);
        Assert.Equal(1500, userProfile.FollowerCount);
        Assert.Equal(750, userProfile.FollowingCount);
        Assert.Equal(250, userProfile.PostCount);
        Assert.True(userProfile.IsVerified);
        Assert.Equal(createdAt, userProfile.CreatedAt);
        Assert.Equal(SocialPlatform.X, userProfile.Platform);
        Assert.Equal(additionalData, userProfile.AdditionalData);
    }

    [Fact]
    public void UserProfile_AdditionalData_ShouldSupportVariousTypes()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act
        userProfile.AdditionalData["string_value"] = "test";
        userProfile.AdditionalData["int_value"] = 42;
        userProfile.AdditionalData["bool_value"] = true;
        userProfile.AdditionalData["double_value"] = 3.14;
        userProfile.AdditionalData["object_value"] = new { nested = "value" };

        // Assert
        Assert.Equal("test", userProfile.AdditionalData["string_value"]);
        Assert.Equal(42, userProfile.AdditionalData["int_value"]);
        Assert.Equal(true, userProfile.AdditionalData["bool_value"]);
        Assert.Equal(3.14, userProfile.AdditionalData["double_value"]);
        Assert.NotNull(userProfile.AdditionalData["object_value"]);
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.Threads)]
    [InlineData(SocialPlatform.Facebook)]
    public void UserProfile_Platform_ShouldAcceptAllSocialPlatforms(SocialPlatform platform)
    {
        // Act
        var userProfile = new UserProfile
        {
            Platform = platform
        };

        // Assert
        Assert.Equal(platform, userProfile.Platform);
    }

    [Fact]
    public void UserProfile_WithLargeFollowerCounts_ShouldHandleCorrectly()
    {
        // Act
        var userProfile = new UserProfile
        {
            FollowerCount = int.MaxValue,
            FollowingCount = int.MaxValue,
            PostCount = int.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, userProfile.FollowerCount);
        Assert.Equal(int.MaxValue, userProfile.FollowingCount);
        Assert.Equal(int.MaxValue, userProfile.PostCount);
    }

    [Fact]
    public void UserProfile_WithNullOptionalFields_ShouldHandleCorrectly()
    {
        // Act
        var userProfile = new UserProfile
        {
            Id = "test_user",
            Username = "testuser",
            DisplayName = "Test User",
            Bio = null,
            Avatar = null,
            Banner = null,
            ProfileUrl = null,
            Website = null,
            Location = null
        };

        // Assert
        Assert.Equal("test_user", userProfile.Id);
        Assert.Equal("testuser", userProfile.Username);
        Assert.Equal("Test User", userProfile.DisplayName);
        Assert.Null(userProfile.Bio);
        Assert.Null(userProfile.Avatar);
        Assert.Null(userProfile.Banner);
        Assert.Null(userProfile.ProfileUrl);
        Assert.Null(userProfile.Website);
        Assert.Null(userProfile.Location);
    }

    [Fact]
    public void UserProfile_WithEmptyAdditionalData_ShouldRemainEmpty()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Assert
        Assert.NotNull(userProfile.AdditionalData);
        Assert.Empty(userProfile.AdditionalData);
    }

    [Fact]
    public void UserProfile_CreatedAt_ShouldAcceptValidDateTimes()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddYears(-5);
        var futureDate = DateTime.UtcNow.AddDays(1);

        // Act
        var userProfile1 = new UserProfile { CreatedAt = pastDate };
        var userProfile2 = new UserProfile { CreatedAt = futureDate };

        // Assert
        Assert.Equal(pastDate, userProfile1.CreatedAt);
        Assert.Equal(futureDate, userProfile2.CreatedAt);
    }

    [Fact]
    public void UserProfile_IsVerified_ShouldToggleCorrectly()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act & Assert - Default
        Assert.False(userProfile.IsVerified);

        // Act & Assert - Set to true
        userProfile.IsVerified = true;
        Assert.True(userProfile.IsVerified);

        // Act & Assert - Set back to false
        userProfile.IsVerified = false;
        Assert.False(userProfile.IsVerified);
    }

    [Fact]
    public void UserProfile_AdditionalData_ShouldAllowModifications()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act
        userProfile.AdditionalData["initial"] = "value";
        userProfile.AdditionalData["initial"] = "updated_value";
        userProfile.AdditionalData.Remove("initial");

        // Assert
        Assert.False(userProfile.AdditionalData.ContainsKey("initial"));
    }
}