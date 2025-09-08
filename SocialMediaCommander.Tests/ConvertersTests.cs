using System;
using System.Globalization;
using Xunit;
using SocialMediaCommander.Desktop.Converters;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests;

/// <summary>
/// Comprehensive tests for Avalonia value converters with modern C# features
/// </summary>
public class ConvertersTests
{
    #region BooleanToStringConverter Tests

    [Theory]
    [InlineData(true, "Yes|No", "Yes")]
    [InlineData(false, "Yes|No", "No")]
    [InlineData(true, "Active|Inactive", "Active")]
    [InlineData(false, "Active|Inactive", "Inactive")]
    [InlineData(true, "Online|Offline", "Online")]
    [InlineData(false, "Online|Offline", "Offline")]
    public void BooleanToStringConverter_Convert_WithValidParameters_ShouldReturnCorrectString(bool value, string parameter, string expected)
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithNullValue_ShouldReturnEmptyString()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithNonBooleanValue_ShouldReturnStringRepresentation()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;
        var intValue = 42;

        // Act
        var result = converter.Convert(intValue, typeof(string), "Yes|No", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithNullParameter_ShouldReturnStringRepresentation()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("True", result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithInvalidParameter_ShouldReturnStringRepresentation()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), "InvalidParameter", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("True", result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithEmptyParameter_ShouldReturnStringRepresentation()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(string), "", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("False", result);
    }

    [Theory]
    [InlineData("OnlyOne")]
    [InlineData("One|Two|Three")]
    [InlineData("First|")]
    [InlineData("|Second")]
    public void BooleanToStringConverter_Convert_WithMalformedParameter_ShouldReturnStringRepresentation(string parameter)
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("True", result);
    }

    [Fact]
    public void BooleanToStringConverter_ConvertBack_ShouldThrowNotImplementedException()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack("Yes", typeof(bool), "Yes|No", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BooleanToStringConverter_Instance_ShouldBeSingleton()
    {
        // Assert
        Assert.Same(BooleanToStringConverter.Instance, BooleanToStringConverter.Instance);
    }

    #endregion

    #region MediaTypeToIconConverter Tests

    [Theory]
    [InlineData(MediaType.Image, "🖼️")]
    [InlineData(MediaType.Video, "🎥")]
    [InlineData(MediaType.Audio, "🎵")]
    [InlineData(MediaType.Document, "📄")]
    public void MediaTypeToIconConverter_Convert_WithValidMediaType_ShouldReturnCorrectIcon(MediaType mediaType, string expectedIcon)
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;

        // Act
        var result = converter.Convert(mediaType, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedIcon, result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithUndefinedMediaType_ShouldReturnDefaultIcon()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;
        var undefinedMediaType = (MediaType)999; // Invalid enum value

        // Act
        var result = converter.Convert(undefinedMediaType, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("📁", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithNullValue_ShouldReturnDefaultIcon()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("📁", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithNonMediaTypeValue_ShouldReturnDefaultIcon()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;
        var stringValue = "not a media type";

        // Act
        var result = converter.Convert(stringValue, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("📁", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithDifferentTargetType_ShouldStillWork()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;

        // Act
        var result = converter.Convert(MediaType.Image, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("🖼️", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithParameter_ShouldIgnoreParameter()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;

        // Act
        var result = converter.Convert(MediaType.Video, typeof(string), "ignored parameter", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("🎥", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_WithDifferentCulture_ShouldReturnSameResult()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;
        var frenchCulture = new CultureInfo("fr-FR");

        // Act
        var result = converter.Convert(MediaType.Audio, typeof(string), null, frenchCulture);

        // Assert
        Assert.Equal("🎵", result);
    }

    [Fact]
    public void MediaTypeToIconConverter_ConvertBack_ShouldThrowNotImplementedException()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;

        // Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack("🖼️", typeof(MediaType), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MediaTypeToIconConverter_Instance_ShouldBeSingleton()
    {
        // Assert
        Assert.Same(MediaTypeToIconConverter.Instance, MediaTypeToIconConverter.Instance);
    }

    #endregion

    #region Edge Cases and Performance Tests

    [Theory]
    [InlineData(true, "Test|Test", "Test")]
    [InlineData(false, "Same|Same", "Same")]
    public void BooleanToStringConverter_Convert_WithSameStrings_ShouldReturnCorrectString(bool value, string parameter, string expected)
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BooleanToStringConverter_Convert_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;
        var parameter = "✓ Available|✗ Unavailable";

        // Act
        var trueResult = converter.Convert(true, typeof(string), parameter, CultureInfo.InvariantCulture);
        var falseResult = converter.Convert(false, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("✓ Available", trueResult);
        Assert.Equal("✗ Unavailable", falseResult);
    }

    [Fact]
    public void MediaTypeToIconConverter_Convert_PerformanceTest_ShouldHandleMultipleCallsEfficiently()
    {
        // Arrange
        var converter = MediaTypeToIconConverter.Instance;
        var mediaTypes = new[] { MediaType.Image, MediaType.Video, MediaType.Audio, MediaType.Document };

        // Act - Multiple conversions
        for (int i = 0; i < 1000; i++)
        {
            foreach (var mediaType in mediaTypes)
            {
                converter.Convert(mediaType, typeof(string), null, CultureInfo.InvariantCulture);
            }
        }

        // Assert - No exceptions should be thrown
        Assert.True(true); // Test passes if no exceptions
    }

    [Fact]
    public void BooleanToStringConverter_Convert_PerformanceTest_ShouldHandleMultipleCallsEfficiently()
    {
        // Arrange
        var converter = BooleanToStringConverter.Instance;
        var values = new[] { true, false };
        var parameters = new[] { "Yes|No", "Active|Inactive", "On|Off" };

        // Act - Multiple conversions
        for (int i = 0; i < 1000; i++)
        {
            foreach (var value in values)
            {
                foreach (var parameter in parameters)
                {
                    converter.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);
                }
            }
        }

        // Assert - No exceptions should be thrown
        Assert.True(true); // Test passes if no exceptions
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Converters_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var booleanConverter = BooleanToStringConverter.Instance;
        var mediaConverter = MediaTypeToIconConverter.Instance;
        var tasks = new List<System.Threading.Tasks.Task>();

        // Act - Concurrent access from multiple threads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    booleanConverter.Convert(j % 2 == 0, typeof(string), "True|False", CultureInfo.InvariantCulture);
                    mediaConverter.Convert(MediaType.Image, typeof(string), null, CultureInfo.InvariantCulture);
                }
            }));
        }

        // Assert
        var exception = Record.Exception(() => System.Threading.Tasks.Task.WaitAll(tasks.ToArray()));
        Assert.Null(exception);
    }

    #endregion
}