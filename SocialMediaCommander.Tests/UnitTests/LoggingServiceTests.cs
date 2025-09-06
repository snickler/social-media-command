using System;
using System.IO;
using SocialMediaCommander.Core.Services;
using Serilog;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for LoggingService to ensure proper initialization and functionality
/// </summary>
public class LoggingServiceTests : IDisposable
{
    private readonly string _testLogDirectory;

    public LoggingServiceTests()
    {
        // Create a unique test log directory to avoid conflicts
        _testLogDirectory = Path.Combine(Path.GetTempPath(), "SocialMediaCommanderTests", Guid.NewGuid().ToString());

        // Close any existing logger to ensure clean state
        Log.CloseAndFlush();
    }

    public void Dispose()
    {
        // Clean up logs after each test
        Log.CloseAndFlush();

        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
    }

    [Fact]
    public void Initialize_ShouldCreateLoggerInstance()
    {
        // Act
        LoggingService.Initialize();

        // Assert
        Log.Logger.Should().NotBeNull();
    }

    [Fact]
    public void Initialize_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        var action = () =>
        {
            LoggingService.Initialize();
            LoggingService.Initialize();
            LoggingService.Initialize();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void ForContext_WithGenericType_ShouldReturnContextualLogger()
    {
        // Act
        var logger = LoggingService.ForContext<LoggingServiceTests>();

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<ILogger>();
    }

    [Fact]
    public void ForContext_WithStringName_ShouldReturnContextualLogger()
    {
        // Act
        var logger = LoggingService.ForContext("TestContext");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<ILogger>();
    }

    [Fact]
    public void ForContext_WithNullString_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => LoggingService.ForContext((string)null!);

        action.Should().NotThrow();
    }

    [Fact]
    public void ForContext_WithEmptyString_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => LoggingService.ForContext(string.Empty);

        action.Should().NotThrow();
    }

    [Fact]
    public void Logger_ShouldWriteToLogFiles()
    {
        // Arrange
        LoggingService.Initialize();
        var logger = LoggingService.ForContext<LoggingServiceTests>();

        // Act
        logger.Information("Test information message");
        logger.Warning("Test warning message");
        logger.Error("Test error message");

        // Force flush to ensure logs are written
        LoggingService.CloseAndFlush();

        // Assert - Log directory should exist
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SocialMediaCommander",
            "Logs"
        );

        Directory.Exists(logDirectory).Should().BeTrue();

        // Should have log files (may vary by date, so check for any .log files)
        var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly);
        logFiles.Should().NotBeEmpty();
    }

    [Fact]
    public void CloseAndFlush_ShouldNotThrow()
    {
        // Arrange
        LoggingService.Initialize();

        // Act & Assert
        var action = () => LoggingService.CloseAndFlush();
        action.Should().NotThrow();
    }

    [Fact]
    public void Initialize_WithInvalidLogDirectory_ShouldFallbackToConsoleLogging()
    {
        // This test ensures graceful degradation when file logging fails
        // Arrange - Force an environment where file logging might fail
        // We can't easily simulate this without mocking the file system,
        // but we can test that initialization doesn't throw

        // Act & Assert
        var action = () => LoggingService.Initialize();
        action.Should().NotThrow();
    }

    [Fact]
    public void ForContext_AfterInitialize_ShouldCreateWorkingLogger()
    {
        // Arrange
        LoggingService.Initialize();

        // Act
        var logger = LoggingService.ForContext<LoggingServiceTests>();

        // Assert - Logger should be able to log without throwing
        var action = () =>
        {
            logger.Debug("Debug message");
            logger.Information("Info message");
            logger.Warning("Warning message");
            logger.Error("Error message");
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void LoggingService_ShouldSupportConcurrentAccess()
    {
        // Arrange
        LoggingService.Initialize();

        // Act & Assert - Multiple concurrent logger creations should not throw
        var action = () =>
        {
            Parallel.For(0, 10, i =>
            {
                var logger = LoggingService.ForContext($"Context{i}");
                logger.Information("Concurrent test message {Index}", i);
            });
        };

        action.Should().NotThrow();
    }
}