using Avalonia;
using System;
using SocialMediaCommander.Core.Services;

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
            logger.Information("Application starting with args: {@Args}", args);
            
            // Start the Avalonia application
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Log any unhandled exceptions
            var logger = LoggingService.ForContext<Program>();
            logger.Fatal(ex, "Application failed to start");
            throw;
        }
        finally
        {
            // Ensure logs are flushed before exit
            LoggingService.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
