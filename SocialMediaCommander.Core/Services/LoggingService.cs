using Serilog;
using Serilog.Events;

namespace SocialMediaCommander.Core.Services;

/// <summary>
/// Centralized logging service that configures Serilog for the entire application
/// </summary>
public static class LoggingService
{
    private static ILogger? _logger;
    
    /// <summary>
    /// Gets the configured logger instance
    /// </summary>
    public static ILogger Logger => _logger ?? throw new InvalidOperationException("Logger not initialized. Call Initialize() first.");

    /// <summary>
    /// Initializes the logging system with file-based logging
    /// </summary>
    public static void Initialize()
    {
        // Get the application data directory for logs
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDirectory = Path.Combine(appDataPath, "SocialMediaCommander", "Logs");
        
        // Ensure log directory exists
        Directory.CreateDirectory(logDirectory);
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SocialMediaCommander")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10_000_000, // 10MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDirectory, "errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                fileSizeLimitBytes: 10_000_000, // 10MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _logger = Log.Logger;
        
        // Log initialization
        _logger.Information("Logging system initialized. Log directory: {LogDirectory}", logDirectory);
    }

    /// <summary>
    /// Creates a logger for a specific context (typically a class)
    /// </summary>
    public static ILogger ForContext<T>() => Logger.ForContext<T>();

    /// <summary>
    /// Creates a logger for a specific context with a string name
    /// </summary>
    public static ILogger ForContext(string sourceContext) => Logger.ForContext("SourceContext", sourceContext);

    /// <summary>
    /// Closes and flushes the logging system
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Gets the application version for logging context
    /// </summary>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
} 