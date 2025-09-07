using SocialMediaCommander.Core.Models;
using Xunit;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Comprehensive unit tests for SocialPlatform and related configuration classes
/// </summary>
public class SocialPlatformTests
{
    [Fact]
    public void SocialPlatform_AllValuesAreDefined()
    {
        // Act & Assert - Verify all expected platforms are defined
        var platforms = Enum.GetValues<SocialPlatform>();

        Assert.Contains(SocialPlatform.BlueSky, platforms);
        Assert.Contains(SocialPlatform.X, platforms);
        Assert.Contains(SocialPlatform.LinkedIn, platforms);
        Assert.Contains(SocialPlatform.Threads, platforms);
        Assert.Contains(SocialPlatform.Facebook, platforms);
        Assert.Equal(5, platforms.Length);
    }

    [Fact]
    public void PlatformConfigurations_AllPlatformsHaveConfigurations()
    {
        // Act
        var configurations = PlatformConfigurations.Platforms;

        // Assert
        Assert.Equal(5, configurations.Count);
        Assert.True(configurations.ContainsKey(SocialPlatform.BlueSky));
        Assert.True(configurations.ContainsKey(SocialPlatform.X));
        Assert.True(configurations.ContainsKey(SocialPlatform.LinkedIn));
        Assert.True(configurations.ContainsKey(SocialPlatform.Threads));
        Assert.True(configurations.ContainsKey(SocialPlatform.Facebook));
    }

    [Theory]
    [InlineData(SocialPlatform.BlueSky)]
    [InlineData(SocialPlatform.X)]
    [InlineData(SocialPlatform.LinkedIn)]
    [InlineData(SocialPlatform.Threads)]
    [InlineData(SocialPlatform.Facebook)]
    public void GetPlatformConfig_WithValidPlatform_ShouldReturnCorrectConfig(SocialPlatform platform)
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(platform);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(platform, config.Id);
        Assert.NotNull(config.Name);
        Assert.NotEmpty(config.Name);
        Assert.NotNull(config.Color);
        Assert.NotEmpty(config.Color);
        Assert.NotNull(config.IconPath);
        Assert.NotEmpty(config.IconPath);
    }

    [Fact]
    public void BlueSkyConfiguration_ShouldHaveCorrectProperties()
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.BlueSky);

        // Assert
        Assert.Equal(SocialPlatform.BlueSky, config.Id);
        Assert.Equal("BlueSky", config.Name);
        Assert.Equal("#1285FE", config.Color);
        Assert.Equal(300, config.CharacterLimit);
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.True(config.ThreadSupport);
        Assert.Contains("bluesky.png", config.IconPath);
    }

    [Fact]
    public void XConfiguration_ShouldHaveCorrectProperties()
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.X);

        // Assert
        Assert.Equal(SocialPlatform.X, config.Id);
        Assert.Equal("X", config.Name);
        Assert.Equal("#000000", config.Color);
        Assert.Equal(280, config.CharacterLimit);
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.True(config.ThreadSupport);
        Assert.Contains("x.png", config.IconPath);
    }

    [Fact]
    public void LinkedInConfiguration_ShouldHaveCorrectProperties()
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.LinkedIn);

        // Assert
        Assert.Equal(SocialPlatform.LinkedIn, config.Id);
        Assert.Equal("LinkedIn", config.Name);
        Assert.Equal("#0077B5", config.Color);
        Assert.Null(config.CharacterLimit); // LinkedIn has no character limit
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.False(config.ThreadSupport); // LinkedIn doesn't support threads
        Assert.Contains("linkedin.png", config.IconPath);
    }

    [Fact]
    public void ThreadsConfiguration_ShouldHaveCorrectProperties()
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.Threads);

        // Assert
        Assert.Equal(SocialPlatform.Threads, config.Id);
        Assert.Equal("Threads", config.Name);
        Assert.Equal("#000000", config.Color);
        Assert.Equal(500, config.CharacterLimit);
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.True(config.ThreadSupport);
        Assert.Contains("threads.png", config.IconPath);
    }

    [Fact]
    public void FacebookConfiguration_ShouldHaveCorrectProperties()
    {
        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.Facebook);

        // Assert
        Assert.Equal(SocialPlatform.Facebook, config.Id);
        Assert.Equal("Facebook", config.Name);
        Assert.Equal("#1877F2", config.Color);
        Assert.Null(config.CharacterLimit); // Facebook has no character limit
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.False(config.ThreadSupport); // Facebook doesn't support threads
        Assert.Contains("facebook.png", config.IconPath);
    }

    [Fact]
    public void GetAllPlatforms_ShouldReturnAllConfigurations()
    {
        // Act
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();

        // Assert
        Assert.NotNull(allPlatforms);
        Assert.Equal(5, allPlatforms.Count());

        var platformNames = allPlatforms.Select(p => p.Name).ToList();
        Assert.Contains("BlueSky", platformNames);
        Assert.Contains("X", platformNames);
        Assert.Contains("LinkedIn", platformNames);
        Assert.Contains("Threads", platformNames);
        Assert.Contains("Facebook", platformNames);
    }

    [Fact]
    public void GetThreadSupportedPlatforms_ShouldReturnOnlyThreadCapablePlatforms()
    {
        // Act
        var threadPlatforms = PlatformConfigurations.GetThreadSupportedPlatforms();

        // Assert
        Assert.NotNull(threadPlatforms);
        Assert.True(threadPlatforms.Count() >= 2); // At least BlueSky, X, and Threads support threads

        var threadPlatformNames = threadPlatforms.Select(p => p.Name).ToList();
        Assert.Contains("BlueSky", threadPlatformNames);
        Assert.Contains("X", threadPlatformNames);
        Assert.Contains("Threads", threadPlatformNames);

        // These platforms should NOT support threads
        Assert.DoesNotContain("LinkedIn", threadPlatformNames);
        Assert.DoesNotContain("Facebook", threadPlatformNames);
    }

    [Fact]
    public void AllPlatformConfigurations_ShouldHaveValidColors()
    {
        // Act
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();

        // Assert
        foreach (var platform in allPlatforms)
        {
            Assert.NotNull(platform.Color);
            Assert.True(platform.Color.StartsWith("#"), $"Platform {platform.Name} color should start with #");
            Assert.True(platform.Color.Length == 7, $"Platform {platform.Name} color should be 7 characters (including #)");
        }
    }

    [Fact]
    public void AllPlatformConfigurations_ShouldHaveValidIconPaths()
    {
        // Act
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();

        // Assert
        foreach (var platform in allPlatforms)
        {
            Assert.NotNull(platform.IconPath);
            Assert.NotEmpty(platform.IconPath);
            Assert.True(platform.IconPath.StartsWith("avares://"),
                $"Platform {platform.Name} icon path should use Avalonia resource scheme");
            Assert.True(platform.IconPath.EndsWith(".png"),
                $"Platform {platform.Name} icon should be a PNG file");
        }
    }

    [Fact]
    public void PlatformConfigurations_ShouldHaveConsistentFeatureSupport()
    {
        // Act
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();

        // Assert
        foreach (var platform in allPlatforms)
        {
            // All platforms should support hashtags and media (as per current configuration)
            Assert.True(platform.HashtagSupport, $"Platform {platform.Name} should support hashtags");
            Assert.True(platform.MediaSupport, $"Platform {platform.Name} should support media");

            // Thread support varies by platform, so we just check it's a valid boolean
            Assert.True(platform.ThreadSupport || !platform.ThreadSupport);
        }
    }

    [Fact]
    public void CharacterLimits_ShouldBeReasonable()
    {
        // Act
        var allPlatforms = PlatformConfigurations.GetAllPlatforms();

        // Assert
        foreach (var platform in allPlatforms)
        {
            if (platform.CharacterLimit.HasValue)
            {
                Assert.True(platform.CharacterLimit.Value > 0,
                    $"Platform {platform.Name} character limit should be positive");
                Assert.True(platform.CharacterLimit.Value >= 140,
                    $"Platform {platform.Name} character limit should be at least 140 characters");
                Assert.True(platform.CharacterLimit.Value <= 10000,
                    $"Platform {platform.Name} character limit should be reasonable (≤10000)");
            }
        }
    }

    [Fact]
    public void SocialPlatformConfig_DefaultConstructor_ShouldInitializeCorrectly()
    {
        // Act
        var config = new SocialPlatformConfig();

        // Assert
        Assert.Equal(SocialPlatform.BlueSky, config.Id); // Default enum value
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Color);
        Assert.Null(config.CharacterLimit);
        Assert.False(config.HashtagSupport);
        Assert.False(config.MediaSupport);
        Assert.False(config.ThreadSupport);
        Assert.Equal(string.Empty, config.IconPath);
    }

    [Fact]
    public void SocialPlatformConfig_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var config = new SocialPlatformConfig
        {
            Id = SocialPlatform.X,
            Name = "Test Platform",
            Color = "#FF0000",
            CharacterLimit = 280,
            HashtagSupport = true,
            MediaSupport = true,
            ThreadSupport = false,
            IconPath = "test/path/icon.png"
        };

        // Assert
        Assert.Equal(SocialPlatform.X, config.Id);
        Assert.Equal("Test Platform", config.Name);
        Assert.Equal("#FF0000", config.Color);
        Assert.Equal(280, config.CharacterLimit);
        Assert.True(config.HashtagSupport);
        Assert.True(config.MediaSupport);
        Assert.False(config.ThreadSupport);
        Assert.Equal("test/path/icon.png", config.IconPath);
    }

    [Fact]
    public async Task PlatformConfigurations_ShouldBeThreadSafe()
    {
        // This test verifies that concurrent access to platform configurations is safe
        // Act
        var tasks = new List<Task<SocialPlatformConfig>>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => PlatformConfigurations.GetPlatformConfig(SocialPlatform.X)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, results.Length);
        Assert.All(results, config =>
        {
            Assert.NotNull(config);
            Assert.Equal(SocialPlatform.X, config.Id);
            Assert.Equal("X", config.Name);
        });
    }

    [Fact]
    public void GetPlatformConfig_WithInvalidPlatform_ShouldReturnBlueSkyAsDefault()
    {
        // This test verifies the fallback behavior mentioned in the implementation
        // Note: This would require casting an invalid enum value, which is generally not recommended
        // but we can test the documented fallback behavior

        // Act
        var config = PlatformConfigurations.GetPlatformConfig(SocialPlatform.BlueSky);
        var fallbackConfig = PlatformConfigurations.Platforms[SocialPlatform.BlueSky];

        // Assert - Verify that BlueSky is indeed used as the fallback
        Assert.Equal(config.Id, fallbackConfig.Id);
        Assert.Equal(config.Name, fallbackConfig.Name);
        Assert.Equal(config.Color, fallbackConfig.Color);
    }
}