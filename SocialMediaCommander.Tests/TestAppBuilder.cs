using Avalonia;
using Avalonia.Headless;
using SocialMediaCommander.Desktop;

[assembly: AvaloniaTestApplication(typeof(SocialMediaCommander.Tests.TestAppBuilder))]

namespace SocialMediaCommander.Tests;

/// <summary>
/// Test application builder for Avalonia headless tests with Skia rendering enabled.
/// This enables screenshot capture with actual pixel data.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure(() => new App())
        .UseSkia() // Enable Skia renderer for screenshot capture
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false // Disable headless drawing to enable real rendering
        });
}
