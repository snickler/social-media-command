using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace SocialMediaCommander.Core.Services;

/// <summary>
/// Centralized logging service using Serilog with file-based output
/// </summary>
public static class LoggingService
{
    private static bool _isInitialized = false;

    /// <summary>
    /// Initialize the logging system with file-based output
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            // Determine log directory
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SocialMediaCommander",
                "Logs"
            );

            // Ensure log directory exists
            Directory.CreateDirectory(logDirectory);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithProperty("Application", "SocialMediaCommander")
                .Enrich.WithProperty("Version", "1.0.0")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "errors-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Warning,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    retainedFileCountLimit: 90,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            Log.Information("Logging system initialized. Log directory: {LogDirectory}", logDirectory);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            // Fallback to minimal logging if file logging fails
            Console.WriteLine($"Failed to initialize file logging: {ex.Message}");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .CreateLogger();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Create a logger with context for a specific type
    /// </summary>
    public static ILogger ForContext<T>()
    {
        Initialize();
        return Log.ForContext<T>();
    }

    /// <summary>
    /// Create a logger with context for a specific name
    /// </summary>
    public static ILogger ForContext(string name)
    {
        Initialize();
        return Log.ForContext("SourceContext", name);
    }

    /// <summary>
    /// Flush and close all loggers
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
