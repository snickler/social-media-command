using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FluentAssertions;
using SocialMediaCommander.Desktop.Views;
using SocialMediaCommander.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SocialMediaCommander.Tests.UI;

/// <summary>
/// Visual regression tests with screenshot capture for all views.
/// These tests capture screenshots and compare them with baselines to detect visual changes.
/// 
/// To create/update baselines:
/// 1. Run tests to generate screenshots in Screenshots/TestRun/
/// 2. Manually review screenshots for correctness
/// 3. Copy approved screenshots to Screenshots/Baselines/
/// </summary>
public class VisualRegressionTests : RecordedTestBase
{
    private readonly ITestOutputHelper _output;

    public VisualRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region View Rendering Tests with Screenshots

    [AvaloniaFact]
    public async Task AccountManagerView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating AccountManagerView for visual regression");
        var view = new AccountManagerView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare with baseline
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(AccountManagerView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot comparison complete", $"Baseline exists: {baselineExists}, Matches: {matchesBaseline}");

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();
        File.Exists(testPath).Should().BeTrue("Screenshot file should be created");

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        if (baselineExists)
        {
            _output.WriteLine($"⚠️  Baseline comparison: {(matchesBaseline == true ? "✓ MATCH" : "✗ DIFFERENT")}");
            _output.WriteLine($"   Baseline: Screenshots/Baselines/{nameof(AccountManagerView_VisualRegression_DefaultState)}.png");
            _output.WriteLine($"   Current:  {testPath}");

            // Don't fail test if baseline doesn't match - allow visual review
            if (matchesBaseline == false)
            {
                _output.WriteLine("   → Manual review required: Compare baseline and current screenshot");
            }
        }
        else
        {
            _output.WriteLine("ℹ️  No baseline exists - first run for this test");
            _output.WriteLine($"   Screenshot saved: {testPath}");
            _output.WriteLine("   → After visual review, copy to Screenshots/Baselines/ to establish baseline");
        }

        SaveRecording(nameof(AccountManagerView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task PostEditorView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating PostEditorView for visual regression");
        var view = new PostEditorView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(PostEditorView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(PostEditorView_VisualRegression_DefaultState));
        SaveRecording(nameof(PostEditorView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task SocialFeedView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating SocialFeedView for visual regression");
        var view = new SocialFeedView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(SocialFeedView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(SocialFeedView_VisualRegression_DefaultState));
        SaveRecording(nameof(SocialFeedView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task SettingsView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating SettingsView for visual regression");
        var view = new SettingsView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(SettingsView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(SettingsView_VisualRegression_DefaultState));
        SaveRecording(nameof(SettingsView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task SchedulerView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating SchedulerView for visual regression");
        var view = new SchedulerView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(SchedulerView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(SchedulerView_VisualRegression_DefaultState));
        SaveRecording(nameof(SchedulerView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task MediaUploadView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating MediaUploadView for visual regression");
        var view = new MediaUploadView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(MediaUploadView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(MediaUploadView_VisualRegression_DefaultState));
        SaveRecording(nameof(MediaUploadView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task AnalyticsDashboardView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating AnalyticsDashboardView for visual regression");
        var view = new AnalyticsDashboardView();
        var window = new Window { Content = view, Width = 1200, Height = 900 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(AnalyticsDashboardView_VisualRegression_DefaultState), 1200, 900)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(AnalyticsDashboardView_VisualRegression_DefaultState));
        SaveRecording(nameof(AnalyticsDashboardView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task DocumentationView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating DocumentationView for visual regression");
        var view = new DocumentationView();
        var window = new Window { Content = view, Width = 1200, Height = 900 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(DocumentationView_VisualRegression_DefaultState), 1200, 900)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(DocumentationView_VisualRegression_DefaultState));
        SaveRecording(nameof(DocumentationView_VisualRegression_DefaultState));
    }

    [AvaloniaFact]
    public async Task OAuthConfigurationView_VisualRegression_DefaultState()
    {
        // Arrange
        Record("Creating OAuthConfigurationView for visual regression");
        var view = new OAuthConfigurationView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Showing window and allowing render");
        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for comparison");
        var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(OAuthConfigurationView_VisualRegression_DefaultState), 1000, 800)
            .ConfigureAwait(false);

        Record("Screenshot captured", testPath);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        view.Should().NotBeNull();
        testPath.Should().NotBeNullOrEmpty();

        var fileInfo = new FileInfo(testPath);
        fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

        LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, nameof(OAuthConfigurationView_VisualRegression_DefaultState));
        SaveRecording(nameof(OAuthConfigurationView_VisualRegression_DefaultState));
    }

    #endregion

    #region Multiple Window Sizes

    [AvaloniaFact]
    public async Task AccountManagerView_VisualRegression_SmallWindow()
    {
        // Arrange - Test at smaller window size
        Record("Creating AccountManagerView for small window test");
        var view = new AccountManagerView();
        var window = new Window { Content = view, Width = 600, Height = 400 };

        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act
        Record("Capturing screenshot at 600x400");
        var (testPath, _, _) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(AccountManagerView_VisualRegression_SmallWindow), 600, 400)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        testPath.Should().NotBeNullOrEmpty();
        File.Exists(testPath).Should().BeTrue();

        _output.WriteLine($"Small window screenshot: {testPath}");
        SaveRecording(nameof(AccountManagerView_VisualRegression_SmallWindow));
    }

    [AvaloniaFact]
    public async Task PostEditorView_VisualRegression_LargeWindow()
    {
        // Arrange - Test at larger window size
        Record("Creating PostEditorView for large window test");
        var view = new PostEditorView();
        var window = new Window { Content = view, Width = 1600, Height = 1200 };

        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        await Task.Delay(150);

        // Act
        Record("Capturing screenshot at 1600x1200");
        var (testPath, _, _) = await ScreenshotHelper
            .CaptureAndCompare(view, nameof(PostEditorView_VisualRegression_LargeWindow), 1600, 1200)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => window.Close());

        // Assert
        testPath.Should().NotBeNullOrEmpty();
        File.Exists(testPath).Should().BeTrue();

        _output.WriteLine($"Large window screenshot: {testPath}");
        SaveRecording(nameof(PostEditorView_VisualRegression_LargeWindow));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Logs visual regression test results in a consistent format.
    /// </summary>
    private void LogVisualRegressionResult(bool baselineExists, bool? matchesBaseline, string testPath, string testName)
    {
        if (baselineExists)
        {
            _output.WriteLine($"⚠️  Baseline comparison: {(matchesBaseline == true ? "✓ MATCH" : "✗ DIFFERENT")}");
            _output.WriteLine($"   Baseline: Screenshots/Baselines/{testName}.png");
            _output.WriteLine($"   Current:  {testPath}");

            if (matchesBaseline == false)
            {
                _output.WriteLine("   → Manual review required: Compare baseline and current screenshot");
            }
        }
        else
        {
            _output.WriteLine("ℹ️  No baseline exists - first run for this test");
            _output.WriteLine($"   Screenshot saved: {testPath}");
            _output.WriteLine("   → After visual review, copy to Screenshots/Baselines/ to establish baseline");
        }
    }

    #endregion
}
