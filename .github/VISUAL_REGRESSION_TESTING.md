# Visual Regression Testing Guide

## Overview

This project uses **automated visual regression testing** to detect unintended UI changes. Screenshots are captured during tests and compared with baseline images to ensure visual consistency across releases.

## How It Works

### Architecture

1. **Screenshot Capture**: Tests use `ScreenshotHelper.cs` to capture rendered UI components using Avalonia's headless Skia renderer
2. **Baseline Comparison**: Current screenshots are compared with approved baseline images
3. **Manual Review**: Developers review differences and approve visual changes
4. **Automated Testing**: GitHub Actions runs visual regression tests on every PR that modifies views

### Key Components

- **`ScreenshotHelper.cs`**: Core screenshot capture and comparison logic
- **`VisualRegressionTests.cs`**: Comprehensive test suite covering all views
- **`TestAppBuilder.cs`**: Configures Avalonia headless platform with Skia rendering
- **`visual-regression.yml`**: GitHub Actions workflow for automated testing

## Directory Structure

```
SocialMediaCommander.Tests/
├── Screenshots/
│   ├── Baselines/          # Approved baseline screenshots (committed to repo)
│   │   ├── AccountManagerView_VisualRegression_DefaultState.png
│   │   ├── PostEditorView_VisualRegression_DefaultState.png
│   │   └── ...
│   └── TestRun/            # Current test run screenshots (gitignored)
│       ├── AccountManagerView_VisualRegression_DefaultState.png
│       └── ...
└── UI/
    ├── VisualRegressionTests.cs
    └── ...
```

## Running Visual Regression Tests

### Locally

```bash
# Run all visual regression tests
dotnet test --filter "FullyQualifiedName~VisualRegressionTests"

# Run specific view test
dotnet test --filter "FullyQualifiedName~AccountManagerView_VisualRegression"

# Run tests with verbose output
dotnet test --filter "FullyQualifiedName~VisualRegressionTests" --logger "console;verbosity=detailed"
```

### In CI/CD

Visual regression tests run automatically on:
- **Pull Requests** that modify views, styles, or UI tests
- **Manual workflow dispatch** via GitHub Actions

## Screenshot Workflow

### First Run (No Baseline)

1. Test captures screenshot → saves to `Screenshots/TestRun/`
2. Test passes (no baseline to compare)
3. Developer reviews screenshot manually
4. If approved, copy to `Screenshots/Baselines/`
5. Commit baseline to repository

### Subsequent Runs (Baseline Exists)

1. Test captures screenshot → saves to `Screenshots/TestRun/`
2. Screenshot compared with baseline in `Screenshots/Baselines/`
3. **If Match**: Test passes ✓
4. **If Different**: 
   - Test reports difference (does NOT fail automatically)
   - Developer reviews both screenshots
   - If intentional change: update baseline
   - If bug: fix code and re-run

## Updating Baselines

### Method 1: Manual Local Update

```bash
# 1. Run visual regression tests
dotnet test --filter "FullyQualifiedName~VisualRegressionTests"

# 2. Review screenshots in Screenshots/TestRun/
# (Use image viewer to inspect each screenshot)

# 3. Copy approved screenshots to baselines
# Windows (PowerShell)
Copy-Item SocialMediaCommander.Tests\Screenshots\TestRun\*.png `
          SocialMediaCommander.Tests\Screenshots\Baselines\

# Linux/macOS (bash)
cp SocialMediaCommander.Tests/Screenshots/TestRun/*.png \
   SocialMediaCommander.Tests/Screenshots/Baselines/

# 4. Commit baseline updates
git add SocialMediaCommander.Tests/Screenshots/Baselines/
git commit -m "chore: update visual regression baselines"
```

### Method 2: GitHub Actions Workflow

```bash
# Trigger via GitHub Actions UI:
# 1. Go to Actions → Visual Regression Testing
# 2. Click "Run workflow"
# 3. Check "Update baseline screenshots"
# 4. Run workflow
# 5. Baselines automatically committed
```

## Best Practices

### Writing Visual Regression Tests

✅ **DO:**
- Capture screenshots for every major view
- Test multiple window sizes (small, default, large)
- Test different states (empty, populated, error states)
- Use descriptive test names that include view name and state
- Allow sufficient render time (`await Task.Delay(150)`)
- Clean up windows after tests (`window.Close()`)

❌ **DON'T:**
- Fail tests automatically on baseline mismatch (requires manual review)
- Test extremely large screenshots (>2000x2000) - slow and unnecessary
- Capture screenshots of rapidly changing data (timestamps, random IDs)
- Compare screenshots pixel-by-pixel without tolerance

### Example Test Pattern

```csharp
[AvaloniaFact]
public async Task MyView_VisualRegression_SpecificState()
{
    // Arrange
    Record("Creating MyView for visual regression");
    var view = new MyView();
    var window = new Window { Content = view, Width = 1000, Height = 800 };

    // Set specific state for testing
    view.DataContext = new MyViewModel { /* test data */ };

    Record("Showing window and allowing render");
    await Dispatcher.UIThread.InvokeAsync(() => window.Show());
    await Task.Delay(150); // Allow rendering

    // Act - Capture and compare
    Record("Capturing screenshot for comparison");
    var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
        .CaptureAndCompare(view, nameof(MyView_VisualRegression_SpecificState), 1000, 800)
        .ConfigureAwait(false);

    await Dispatcher.UIThread.InvokeAsync(() => window.Close());

    // Assert
    view.Should().NotBeNull();
    testPath.Should().NotBeNullOrEmpty();
    File.Exists(testPath).Should().BeTrue();

    var fileInfo = new FileInfo(testPath);
    fileInfo.Length.Should().BeGreaterThan(1000, "Screenshot should contain actual image data");

    LogVisualRegressionResult(baselineExists, matchesBaseline, testPath, 
        nameof(MyView_VisualRegression_SpecificState));
    SaveRecording(nameof(MyView_VisualRegression_SpecificState));
}
```

## Reviewing Screenshot Differences

### Manual Review Process

1. **Download artifacts** from GitHub Actions workflow run
2. **Compare screenshots** side-by-side:
   - `Baselines/` = expected (old)
   - `TestRun/` = actual (new)
3. **Identify changes**:
   - Layout shifts
   - Color changes
   - Missing/added elements
   - Font rendering differences
4. **Make decision**:
   - **Intentional change** → Update baseline
   - **Unintended regression** → Fix code
   - **Platform rendering difference** → Adjust tolerance or test expectations

### Tools for Comparison

- **Visual Studio Code**: Install extension "Image Preview" to view PNGs side-by-side
- **GitHub**: View images in PR file changes
- **ImageMagick** (CLI): `compare baseline.png testrun.png diff.png`
- **Online tools**: https://www.diffchecker.com/image-diff/

## CI/CD Integration

### GitHub Actions Workflow

The `visual-regression.yml` workflow:

1. **Triggers**:
   - Pull requests modifying views/styles/UI tests
   - Manual workflow dispatch

2. **Steps**:
   - Build solution
   - Run visual regression tests
   - Upload screenshots as artifacts
   - Generate markdown report
   - Comment on PR with results

3. **Artifacts**:
   - `test-screenshots`: Current test run screenshots
   - `baseline-screenshots`: Baseline screenshots for reference
   - `visual-regression-report`: Markdown summary
   - `visual-regression-test-results`: Test result files

### PR Comments

Workflow automatically comments on PRs with:
- Test run summary
- Screenshot count
- Links to artifacts
- Instructions for review

## Troubleshooting

### Screenshots are zero bytes

**Problem**: Screenshots saved but contain no image data (0 bytes)

**Solution**: Ensure Skia renderer is enabled:
```csharp
// In TestAppBuilder.cs or test setup
AppBuilder.Configure<App>()
    .UseSkia()  // ← Required for screenshot capture
    .UseHeadless(new AvaloniaHeadlessPlatform
    {
        UseHeadlessDrawing = false  // ← Must be false for actual rendering
    });
```

### Tests fail with "Failed to capture rendered frame"

**Problem**: `CaptureRenderedFrame()` returns null

**Causes**:
1. Control not hosted in a Window
2. Skia renderer not enabled
3. Window not shown before capture
4. Insufficient render time

**Solution**:
```csharp
var window = new Window { Content = view, Width = 800, Height = 600 };
await Dispatcher.UIThread.InvokeAsync(() => window.Show());
await Task.Delay(150); // Allow rendering
// Now capture screenshot
```

### Baselines don't match but screenshots look identical

**Problem**: Baseline comparison returns false despite visual similarity

**Causes**:
1. Different PNG compression
2. Metadata differences
3. Rendering on different platforms (Windows vs Linux)

**Solution**:
- Increase comparison tolerance: `ScreenshotHelper.Compare(..., tolerance: 0.05)`
- Use platform-specific baselines if necessary
- Focus on significant visual differences, not byte-perfect matches

### Screenshots differ between local and CI

**Problem**: Tests pass locally but fail in CI (or vice versa)

**Causes**:
1. Font rendering differences (Windows vs Linux)
2. Different DPI settings
3. Graphics driver differences

**Solutions**:
- Use platform-specific baseline directories
- Increase comparison tolerance
- Mock platform-specific rendering (fonts, icons)
- Document expected platform differences

## Advanced Topics

### Custom Comparison Logic

For views with dynamic content, implement custom comparison:

```csharp
// Mask out dynamic regions before comparison
public static bool CompareWithMask(string baseline, string current, Rectangle maskRegion)
{
    var baselineImg = new Bitmap(baseline);
    var currentImg = new Bitmap(current);
    
    // Apply mask to ignore dynamic region
    // ... custom logic ...
    
    return pixelDifference < tolerance;
}
```

### Multi-Platform Baselines

For platform-specific rendering:

```
Screenshots/
├── Baselines/
│   ├── Windows/
│   │   └── MyView_VisualRegression.png
│   ├── Linux/
│   │   └── MyView_VisualRegression.png
│   └── macOS/
│       └── MyView_VisualRegression.png
└── TestRun/
    └── MyView_VisualRegression.png
```

### Threshold-Based Comparison

Configure acceptable difference percentage:

```csharp
// Allow up to 2% pixel difference
var (testPath, baselineExists, matchesBaseline) = await ScreenshotHelper
    .CaptureAndCompare(view, testName, 800, 600, tolerance: 0.02)
    .ConfigureAwait(false);
```

## Performance Considerations

- **Screenshot size**: Larger screenshots = slower tests (keep < 1600x1200)
- **Render delay**: Balance accuracy vs speed (100-200ms typical)
- **Parallel tests**: xUnit runs tests in parallel - ensure thread safety
- **Artifact storage**: GitHub has artifact retention limits (30 days default)

## Related Documentation

- [Avalonia Headless Testing](https://docs.avaloniaui.net/docs/concepts/headless/)
- [Screenshot Helper Implementation](../SocialMediaCommander.Tests/Helpers/ScreenshotHelper.cs)
- [Test App Builder](../SocialMediaCommander.Tests/TestAppBuilder.cs)
- [TDD Best Practices](./copilot-instructions.md#test-infrastructure)

## Contributing

When adding new views:

1. ✅ Add visual regression test in `VisualRegressionTests.cs`
2. ✅ Run test locally to generate screenshot
3. ✅ Review screenshot for correctness
4. ✅ Copy to baselines and commit
5. ✅ Include baseline in PR

When modifying existing views:

1. ✅ Run visual regression tests before changes (baseline)
2. ✅ Make changes to view
3. ✅ Run visual regression tests after changes
4. ✅ Review screenshot differences
5. ✅ Update baselines if changes are intentional
6. ✅ Include baseline updates in PR with explanation

---

**Questions?** See [CONTRIBUTING.md](../CONTRIBUTING.md) or open an issue.
