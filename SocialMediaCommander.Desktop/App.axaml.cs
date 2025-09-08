using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.IO;
using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Desktop.Views;
using SocialMediaCommander.Core.Services;
using SocialMediaCommander.Services.Interfaces;
using Serilog;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SocialMediaCommander.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private Serilog.ILogger? _logger;
    private ILoggerFactory? _loggerFactory;

    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);

            // Initialize logging as early as possible
            LoggingService.Initialize();
            _logger = LoggingService.ForContext<App>();
            _logger.Information("=== APP.INITIALIZE STARTED ===");
            _logger.Information("XAML loaded successfully");

            // Log platform information
            _logger.Information("Platform: {Platform}", RuntimeInformation.OSDescription);
            _logger.Information("Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
            _logger.Information("Framework: {Framework}", RuntimeInformation.FrameworkDescription);
            _logger.Information("Process Architecture: {ProcessArch}", RuntimeInformation.ProcessArchitecture);

            // Create logger factory for Avalonia Developer Tools integration
            _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder
                .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                .AddConsole()
                .AddDebug());
            _logger.Information("Logger factory created");

            // Attach Developer Tools for debugging with enhanced logging
#if DEBUG
            this.AttachDeveloperTools(options =>
            {
                // Add Microsoft Extensions Logging integration
                options.AddMicrosoftLoggerObservable(_loggerFactory);
            });
            _logger.Information("Developer Tools attached with enhanced diagnostics");
#endif

            _logger.Information("=== APP.INITIALIZE COMPLETED ===");
        }
        catch (Exception ex)
        {
            _logger?.Fatal(ex, "Fatal error during App.Initialize");
            Console.WriteLine($"FATAL ERROR in App.Initialize: {ex}");
            throw;
        }
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Avalonia UI binding plugins required for proper operation")]
    public override void OnFrameworkInitializationCompleted()
    {
        _logger?.Information("=== FRAMEWORK INITIALIZATION STARTED ===");

        try
        {
            _logger?.Information("ApplicationLifetime type: {LifetimeType}", ApplicationLifetime?.GetType().Name ?? "null");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _logger?.Information("Classic desktop lifetime detected - proceeding with setup");

                // Log current thread info
                _logger?.Information("Current thread: {ThreadId}, IsBackground: {IsBackground}, IsThreadPoolThread: {IsThreadPool}",
                    System.Threading.Thread.CurrentThread.ManagedThreadId,
                    System.Threading.Thread.CurrentThread.IsBackground,
                    System.Threading.Thread.CurrentThread.IsThreadPoolThread);

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                _logger?.Information("Disabling Avalonia data annotation validation...");
                DisableAvaloniaDataAnnotationValidation();
                _logger?.Information("Data annotation validation disabled");

                // Set up dependency injection
                _logger?.Information("Setting up dependency injection...");
                var services = new ServiceCollection();
                var configuration = BuildConfiguration();
                _logger?.Information("Configuration built successfully");

                services.AddSocialMediaCommanderServices(configuration);
                _serviceProvider = services.BuildServiceProvider();
                _logger?.Information("Service provider built with {ServiceCount} services", services.Count);

                // Create main window with DI - test each dependency individually
                _logger?.Information("Creating ViewModels individually to identify hanging dependency...");

                _logger?.Information("Creating PostEditorViewModel...");
                var postEditorViewModel = _serviceProvider.GetRequiredService<PostEditorViewModel>();
                _logger?.Information("PostEditorViewModel created successfully");

                _logger?.Information("Creating SocialFeedViewModel...");
                var socialFeedViewModel = _serviceProvider.GetRequiredService<SocialFeedViewModel>();
                _logger?.Information("SocialFeedViewModel created successfully");

                _logger?.Information("Creating AccountManagerViewModel...");
                var accountManagerViewModel = _serviceProvider.GetRequiredService<AccountManagerViewModel>();
                _logger?.Information("AccountManagerViewModel created successfully");

                _logger?.Information("Creating AnalyticsDashboardViewModel...");
                var analyticsDashboardViewModel = _serviceProvider.GetRequiredService<AnalyticsDashboardViewModel>();
                _logger?.Information("AnalyticsDashboardViewModel created successfully");

                _logger?.Information("Creating SettingsViewModel...");
                var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
                _logger?.Information("SettingsViewModel created successfully");

                _logger?.Information("Creating SchedulerViewModel...");
                var schedulerViewModel = _serviceProvider.GetRequiredService<SchedulerViewModel>();
                _logger?.Information("SchedulerViewModel created successfully");

                _logger?.Information("Creating AIAssistantViewModel...");
                var aiAssistantViewModel = _serviceProvider.GetRequiredService<AIAssistantViewModel>();
                _logger?.Information("AIAssistantViewModel created successfully");

                _logger?.Information("Creating DocumentationViewModel...");
                var documentationViewModel = _serviceProvider.GetRequiredService<DocumentationViewModel>();
                _logger?.Information("DocumentationViewModel created successfully");

                _logger?.Information("Getting IDataIntegrityService...");
                var dataIntegrityService = _serviceProvider.GetRequiredService<IDataIntegrityService>();
                _logger?.Information("IDataIntegrityService retrieved successfully");

                _logger?.Information("Getting ISettingsService...");
                var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
                _logger?.Information("ISettingsService retrieved successfully");

                _logger?.Information("Creating MainWindowViewModel with individual dependencies...");
                var mainWindowViewModel = new MainWindowViewModel(
                    postEditorViewModel,
                    socialFeedViewModel,
                    accountManagerViewModel,
                    analyticsDashboardViewModel,
                    settingsViewModel,
                    schedulerViewModel,
                    aiAssistantViewModel,
                    documentationViewModel,
                    dataIntegrityService,
                    settingsService);
                _logger?.Information("MainWindowViewModel created successfully");

                _logger?.Information("Creating MainWindow...");
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };
                _logger?.Information("MainWindow instance created");

                // Set window properties for debugging
                mainWindow.Title = "Social Media Commander - Debug Mode";
                mainWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen;
                _logger?.Information("Window properties set");

                desktop.MainWindow = mainWindow;
                _logger?.Information("MainWindow assigned to desktop.MainWindow");

                // Log window state
                _logger?.Information("Window Handle: {Handle}", mainWindow.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
                _logger?.Information("Window IsVisible: {IsVisible}", mainWindow.IsVisible);
                _logger?.Information("Window Width: {Width}, Height: {Height}", mainWindow.Width, mainWindow.Height);

                // Handle application shutdown
                desktop.ShutdownRequested += OnShutdownRequested;
                _logger?.Information("Shutdown handler registered");

                // Try to show window explicitly
                _logger?.Information("Attempting to show window explicitly...");
                try
                {
                    mainWindow.Show();
                    _logger?.Information("Window.Show() called successfully");
                }
                catch (Exception showEx)
                {
                    _logger?.Error(showEx, "Error calling Window.Show()");
                }
            }
            else
            {
                _logger?.Warning("ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime: {LifetimeType}",
                    ApplicationLifetime?.GetType().Name ?? "null");
            }

            _logger?.Information("Calling base.OnFrameworkInitializationCompleted()...");
            base.OnFrameworkInitializationCompleted();
            _logger?.Information("=== FRAMEWORK INITIALIZATION COMPLETED ===");
        }
        catch (Exception ex)
        {
            _logger?.Fatal(ex, "Fatal error during OnFrameworkInitializationCompleted");
            Console.WriteLine($"FATAL ERROR in OnFrameworkInitializationCompleted: {ex}");
            throw;
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.enhanced.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Avalonia UI binding plugins required for proper operation")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Avalonia framework requires access to DataValidators for UI binding")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger?.Information("Application shutdown requested");
        (_serviceProvider as IDisposable)?.Dispose();
    }
}