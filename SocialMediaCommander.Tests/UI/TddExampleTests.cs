using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using SocialMediaCommander.Desktop.Views;
using SocialMediaCommander.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SocialMediaCommander.Tests.UI;

/// <summary>
/// Example tests demonstrating TDD best practices with screenshot capture and interaction recording.
/// These tests serve as a reference for implementing new UI tests.
/// </summary>
public class TddExampleTests : RecordedTestBase
{
    private readonly ITestOutputHelper _output;

    public TddExampleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [AvaloniaFact]
    public async Task AccountManagerView_ShouldRender_WithScreenshot()
    {
        // Arrange
        Record("Creating AccountManagerView instance");
        var view = new AccountManagerView();
        var window = new Window { Content = view, Width = 800, Height = 600 };

        Record("Showing window");
        window.Show();
        await Task.Delay(100); // Allow rendering

        // Act - Capture screenshot
        Record("Capturing screenshot");
        var (testPath, baselineExists, matchesBaseline) = ScreenshotHelper.CaptureAndCompare(
            view,
            nameof(AccountManagerView_ShouldRender_WithScreenshot)
        );

        Record("Screenshot captured", $"Path: {testPath}");
        
        if (baselineExists)
        {
            Record("Baseline comparison", $"Matches: {matchesBaseline}");
        }
        else
        {
            Record("No baseline exists", "This is the first run - baseline will be created manually");
        }

        // Assert
        view.Should().NotBeNull("View should be created successfully");
        testPath.Should().NotBeNullOrEmpty("Screenshot should be saved");

        // Save recording
        var recordingPath = SaveRecording(nameof(AccountManagerView_ShouldRender_WithScreenshot));
        _output.WriteLine($"Test recording saved to: {recordingPath}");
        _output.WriteLine($"Screenshot saved to: {testPath}");

        // Cleanup
        window.Close();
    }

    [AvaloniaFact]
    public async Task PostEditorView_ShouldLoad_WithoutErrors()
    {
        // Arrange
        AddSection("Setup Phase");
        Record("Creating PostEditorView");
        var view = new PostEditorView();

        // Act
        AddSection("Execution Phase");
        Record("Setting DataContext to test object");
        view.DataContext = new { Title = "Test Post", Content = "Test content" };

        Record("Measuring and arranging view");
        view.Measure(new Avalonia.Size(800, 600));
        view.Arrange(new Avalonia.Rect(0, 0, 800, 600));

        await Task.Delay(50);

        // Assert
        AddSection("Verification Phase");
        RecordAssertion("View is not null", view != null);
        RecordAssertion("DataContext is set", view.DataContext != null);

        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();

        // Save recording
        var recordingPath = SaveRecording(nameof(PostEditorView_ShouldLoad_WithoutErrors));
        _output.WriteLine($"Test recording saved to: {recordingPath}");
    }

    [AvaloniaFact]
    public void SettingsView_ShouldInstantiate_Successfully()
    {
        // Red-Green-Refactor Example
        // This test demonstrates the TDD cycle:

        // RED: Write test that fails (if feature doesn't exist yet)
        Record("Creating SettingsView instance");
        var view = new SettingsView();

        // GREEN: Make test pass (implement minimal code)
        Record("Verifying view instantiation");
        view.Should().NotBeNull("SettingsView should instantiate successfully");
        view.Should().BeAssignableTo<UserControl>("SettingsView should be a UserControl");

        // REFACTOR: Improve implementation while keeping tests green
        // (This step happens in the production code, not in tests)

        RecordAssertion("View instantiation successful", view != null);

        // Save recording
        SaveRecording(nameof(SettingsView_ShouldInstantiate_Successfully));
    }

    [AvaloniaFact]
    public async Task SocialFeedView_VisualRegression_Example()
    {
        // This test demonstrates visual regression testing workflow
        
        // Arrange
        Record("Setting up visual regression test");
        var view = new SocialFeedView();
        var window = new Window { Content = view, Width = 1000, Height = 800 };

        Record("Initializing view with test data");
        view.DataContext = new { Feed = "Test Feed Data" };

        window.Show();
        await Task.Delay(150);

        // Act - Capture and compare
        Record("Capturing screenshot for visual regression");
        var screenshot = ScreenshotHelper.Capture(view, 1000, 800);

        var testPath = $"Screenshots/TestRun/{nameof(SocialFeedView_VisualRegression_Example)}.png";
        ScreenshotHelper.Save(screenshot, testPath);

        Record("Screenshot saved", testPath);

        // Assert
        view.Should().NotBeNull();
        screenshot.Should().NotBeNull("Screenshot should be captured successfully");

        _output.WriteLine($"Visual regression baseline: Screenshots/Baselines/{nameof(SocialFeedView_VisualRegression_Example)}.png");
        _output.WriteLine($"Current test screenshot: {testPath}");
        _output.WriteLine("To create baseline, manually copy test screenshot to baseline directory after visual inspection");

        // Cleanup
        window.Close();

        // Save interaction recording
        SaveRecording(nameof(SocialFeedView_VisualRegression_Example));
    }

    [AvaloniaFact]
    public void TestInteractionRecorder_ShouldCapture_AllActions()
    {
        // This test demonstrates the recorder capabilities
        
        // Arrange
        AddSection("Test Recorder Demonstration");
        Record("Starting recorder demo");

        // Simulate various actions
        RecordCommand("TestCommand", "testParameter");
        RecordPropertyChange("TestProperty", "oldValue", "newValue");
        RecordNavigation("HomeView", "SettingsView");
        RecordAssertion("1 + 1 = 2", 1 + 1 == 2);

        // Act
        var interactions = Recorder.GetInteractions();
        var recordingText = Recorder.GetRecordingText();

        // Assert
        interactions.Should().NotBeEmpty("Recorder should capture interactions");
        interactions.Count.Should().BeGreaterThan(3, "Multiple interactions recorded");
        recordingText.Should().Contain("Test Recorder Demonstration");

        _output.WriteLine("=== Recorded Interactions ===");
        _output.WriteLine(recordingText);

        // Save recording
        var path = SaveRecording(nameof(TestInteractionRecorder_ShouldCapture_AllActions));
        _output.WriteLine($"\nFull recording saved to: {path}");
    }
}
