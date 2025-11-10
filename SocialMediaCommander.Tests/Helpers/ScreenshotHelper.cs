using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace SocialMediaCommander.Tests.Helpers;

/// <summary>
/// Helper class for capturing screenshots of Avalonia UI controls in tests.
/// Enables visual regression testing and debugging of UI components.
/// </summary>
public static class ScreenshotHelper
{
    private const string BaselineDir = "Screenshots/Baselines";
    private const string TestRunDir = "Screenshots/TestRun";

    static ScreenshotHelper()
    {
        // Ensure directories exist
        Directory.CreateDirectory(BaselineDir);
        Directory.CreateDirectory(TestRunDir);
    }

    /// <summary>
    /// Captures a screenshot of the specified control using Avalonia's headless rendering.
    /// The control must be hosted in a Window for proper rendering with Skia backend.
    /// </summary>
    /// <param name="control">The control to capture (must be in a Window's visual tree)</param>
    /// <param name="width">Width of the render target</param>
    /// <param name="height">Height of the render target</param>
    /// <returns>WriteableBitmap containing the screenshot</returns>
    public static WriteableBitmap Capture(Control control, int width = 800, int height = 600)
    {
        if (control is null)
        {
            throw new ArgumentNullException(nameof(control));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            return CaptureInternal(control, width, height);
        }

        return Dispatcher.UIThread.InvokeAsync(() => CaptureInternal(control, width, height)).GetAwaiter().GetResult();
    }

    private static WriteableBitmap CaptureInternal(Control control, int width, int height)
    {
        // Find or create a window for the control
        var window = control as Window;
        if (window == null)
        {
            // Find parent window
            var visual = control.GetVisualRoot();
            window = visual as Window;

            if (window == null)
            {
                // Create a temporary window to host the control
                window = new Window
                {
                    Content = control,
                    Width = width,
                    Height = height
                };
                window.Show();
            }
        }

        // Ensure the control is measured and arranged
        control.Measure(new Size(width, height));
        control.Arrange(new Rect(0, 0, width, height));

        // Force render timer tick to ensure rendering is complete
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        // Capture the rendered frame using Avalonia's headless platform method
        var frame = window.CaptureRenderedFrame();
        if (frame == null)
        {
            throw new InvalidOperationException("Failed to capture rendered frame. Ensure Skia renderer is enabled in test configuration.");
        }

        return frame;
    }

    /// <summary>
    /// Captures a screenshot and saves it to the test run directory.
    /// </summary>
    /// <param name="control">The control to capture</param>
    /// <param name="fileName">Name of the file (without extension)</param>
    /// <param name="width">Width of the render target</param>
    /// <param name="height">Height of the render target</param>
    /// <returns>Path to the saved screenshot</returns>
    public static async ValueTask<string> CaptureAndSave(Control control, string fileName, int width = 800, int height = 600)
    {
        var bitmap = Capture(control, width, height);
        var filePath = Path.Combine(TestRunDir, $"{fileName}.png");

        await Save(bitmap, filePath).ConfigureAwait(false);

        return filePath;
    }

    /// <summary>
    /// Saves a bitmap to the specified path.
    /// Must be called from UI thread or within a Dispatcher.UIThread.InvokeAsync context.
    /// </summary>
    /// <param name="bitmap">Bitmap to save (WriteableBitmap from CaptureRenderedFrame)</param>
    /// <param name="filePath">Full path to save location</param>
    public static ValueTask Save(WriteableBitmap bitmap, string filePath)
    {
        if (bitmap is null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save directly - caller must ensure this runs on UI thread
        // WriteableBitmap.Save() works correctly with Skia backend enabled
        bitmap.Save(normalizedPath);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Compares two images and returns true if they match within the specified tolerance.
    /// </summary>
    /// <param name="baselinePath">Path to baseline image</param>
    /// <param name="currentPath">Path to current test image</param>
    /// <param name="tolerance">Tolerance for pixel differences (0.0 = exact match, 1.0 = any match)</param>
    /// <returns>True if images match within tolerance</returns>
    public static bool Compare(string baselinePath, string currentPath, double tolerance = 0.01)
    {
        if (!File.Exists(baselinePath))
        {
            // No baseline exists - cannot compare
            return false;
        }

        if (!File.Exists(currentPath))
        {
            throw new FileNotFoundException($"Current test image not found: {currentPath}");
        }

        // Load both images
        var baseline = new Bitmap(baselinePath);
        var current = new Bitmap(currentPath);

        // Check dimensions match
        if (baseline.PixelSize != current.PixelSize)
        {
            return false;
        }

        // Simple pixel comparison (for production, consider using ImageSharp or similar for better comparison)
        // This is a basic implementation - can be enhanced with perceptual difference algorithms
        var width = baseline.PixelSize.Width;
        var height = baseline.PixelSize.Height;

        // For this simple version, we just check if files match exactly
        // In production, you'd do pixel-by-pixel comparison with tolerance
        var baselineBytes = File.ReadAllBytes(baselinePath);
        var currentBytes = File.ReadAllBytes(currentPath);

        if (baselineBytes.Length != currentBytes.Length)
        {
            return false;
        }

        // Count different bytes
        int differences = 0;
        for (int i = 0; i < baselineBytes.Length; i++)
        {
            if (baselineBytes[i] != currentBytes[i])
            {
                differences++;
            }
        }

        // Calculate difference percentage
        double differencePercent = (double)differences / baselineBytes.Length;
        return differencePercent <= tolerance;
    }

    /// <summary>
    /// Creates or updates a baseline screenshot.
    /// Use this to establish the "correct" rendering that future tests will compare against.
    /// </summary>
    /// <param name="control">Control to capture</param>
    /// <param name="fileName">Baseline file name (without extension)</param>
    /// <param name="width">Width of render target</param>
    /// <param name="height">Height of render target</param>
    /// <returns>Path to the baseline screenshot</returns>
    public static async ValueTask<string> CreateBaseline(Control control, string fileName, int width = 800, int height = 600)
    {
        var bitmap = Capture(control, width, height);
        var filePath = Path.Combine(BaselineDir, $"{fileName}.png");

        await Save(bitmap, filePath).ConfigureAwait(false);

        return filePath;
    }

    /// <summary>
    /// Captures a screenshot, saves it to test run directory, and compares with baseline if it exists.
    /// </summary>
    /// <param name="control">Control to capture</param>
    /// <param name="testName">Name of the test (used for file naming)</param>
    /// <param name="width">Width of render target</param>
    /// <param name="height">Height of render target</param>
    /// <param name="tolerance">Tolerance for comparison</param>
    /// <returns>Tuple of (test screenshot path, baseline exists, matches baseline)</returns>
    public static async ValueTask<(string testPath, bool baselineExists, bool? matchesBaseline)> CaptureAndCompare(
        Control control,
        string testName,
        int width = 800,
        int height = 600,
        double tolerance = 0.01)
    {
        // Capture current test
        var testPath = await CaptureAndSave(control, testName, width, height).ConfigureAwait(false);

        // Check for baseline
        var baselinePath = Path.Combine(BaselineDir, $"{testName}.png");
        var baselineExists = File.Exists(baselinePath);

        // Compare if baseline exists
        bool? matchesBaseline = baselineExists
            ? Compare(baselinePath, testPath, tolerance)
            : null;

        return (testPath, baselineExists, matchesBaseline);
    }

    /// <summary>
    /// Gets the path to the test run directory.
    /// </summary>
    public static string GetTestRunDirectory() => Path.GetFullPath(TestRunDir);

    /// <summary>
    /// Gets the path to the baseline directory.
    /// </summary>
    public static string GetBaselineDirectory() => Path.GetFullPath(BaselineDir);

    /// <summary>
    /// Cleans up old test run screenshots (useful for cleanup between test runs).
    /// </summary>
    public static void CleanupTestRun()
    {
        if (Directory.Exists(TestRunDir))
        {
            foreach (var file in Directory.GetFiles(TestRunDir, "*.png"))
            {
                File.Delete(file);
            }
        }
    }
}
