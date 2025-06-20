using Avalonia;
using System;
using SocialMediaCommander.Core.Services;
using System.Threading;
using System.Diagnostics;

namespace SocialMediaCommander.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Initialize logging system first
            LoggingService.Initialize();
            
            var logger = LoggingService.ForContext<Program>();
            logger.Information("=== APPLICATION STARTUP ===");
            logger.Information("Process ID: {ProcessId}", Process.GetCurrentProcess().Id);
            logger.Information("Thread ID: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            logger.Information("Arguments: {@Args}", args);
            logger.Information("Current Directory: {CurrentDirectory}", Environment.CurrentDirectory);
            logger.Information("OS Version: {OSVersion}", Environment.OSVersion);
            logger.Information(".NET Version: {NetVersion}", Environment.Version);
            
            // Log before building Avalonia app
            logger.Information("Building Avalonia app...");
            var appBuilder = BuildAvaloniaApp();
            logger.Information("Avalonia app built successfully");
            
            // Log before starting the application
            logger.Information("Starting application with classic desktop lifetime...");
            appBuilder.StartWithClassicDesktopLifetime(args);
            logger.Information("Application startup completed");
        }
        catch (Exception ex)
        {
            // Log any unhandled exceptions
            var logger = LoggingService.ForContext<Program>();
            logger.Fatal(ex, "Application failed to start");
            
            // Also write to console as fallback
            Console.WriteLine($"FATAL ERROR: {ex}");
            throw;
        }
        finally
        {
            // Ensure logs are flushed before exit
            var logger = LoggingService.ForContext<Program>();
            logger.Information("Application exiting - flushing logs");
            LoggingService.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var logger = LoggingService.ForContext<Program>();
        logger.Information("Configuring Avalonia AppBuilder...");
        
        var builder = AppBuilder.Configure<App>();
        logger.Information("App configured");
        
        builder = builder.UsePlatformDetect();
        logger.Information("Platform detection enabled");
        
        builder = builder.WithInterFont();
        logger.Information("InterFont configured");
        
        builder = builder.LogToTrace();
        logger.Information("Trace logging enabled");
        
        logger.Information("AppBuilder configuration complete");
        return builder;
    }
}
