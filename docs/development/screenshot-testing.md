# Screenshot Testing Guide

## Overview

Screenshot testing enables visual regression testing by capturing and comparing UI renderings. This guide explains how to use the screenshot testing infrastructure in Social Media Commander.

## Why Screenshot Testing?

Visual regression testing helps catch:
- ✅ Layout changes that break UI
- ✅ Unintended style modifications
- ✅ Platform-specific rendering issues
- ✅ Control sizing/positioning problems
- ✅ Visual bugs that unit tests miss

## Getting Started

### 1. Basic Screenshot Capture

```csharp
using Avalonia.Headless.XUnit;
using SocialMediaCommander.Tests.Helpers;

[AvaloniaFact]
public async Task MyView_ShouldRender()
{
    // Arrange
    var view = new MyView();
    var window = new Window { Content = view, Width = 800, Height = 600 };
    window.Show();
    await Task.Delay(100); // Allow rendering
    
    // Act - Capture screenshot
    var screenshot = ScreenshotHelper.Capture(view, width: 800, height: 600);
    ScreenshotHelper.Save(screenshot, "Screenshots/TestRun/MyView.png");
    
    // Assert
    screenshot.Should().NotBeNull();
    
    // Cleanup
    window.Close();
}
```

### 2. Screenshot with Baseline Comparison

```csharp
[AvaloniaFact]
public async Task MyView_ShouldMatch_Baseline()
{
    // Arrange
    var view = new MyView();
    var window = new Window { Content = view, Width = 800, Height = 600 };
    window.Show();
    await Task.Delay(100);
    
    // Act - Capture and compare
    var (testPath, baselineExists, matches) = ScreenshotHelper.CaptureAndCompare(
        view,
        nameof(MyView_ShouldMatch_Baseline),
        width: 800,
        height: 600,
        tolerance: 0.01  // 1% pixel difference allowed
    );
    
    // Assert
    if (baselineExists)
    {
        matches.Should().BeTrue("View should match baseline rendering");
    }
    else
    {
        // First run - manually create baseline
        this.WriteLine($"No baseline exists. Test screenshot saved to: {testPath}");
    }
    
    // Cleanup
    window.Close();
}
```

## ScreenshotHelper API

### Capture Methods

#### `Capture(Control, width, height)`
Captures a screenshot of a control.

```csharp
var screenshot = ScreenshotHelper.Capture(view, 1024, 768);
```

#### `CaptureAndSave(Control, fileName, width, height)`
Captures and saves to test run directory.

```csharp
var path = ScreenshotHelper.CaptureAndSave(view, "MyView_Test", 800, 600);
// Saves to: Screenshots/TestRun/MyView_Test.png
```

#### `CaptureAndCompare(Control, testName, width, height, tolerance)`
Captures, saves, and compares with baseline.

```csharp
var (testPath, baselineExists, matches) = ScreenshotHelper.CaptureAndCompare(
    view,
    "MyView_Comparison",
    tolerance: 0.02  // 2% tolerance
);

if (baselineExists && matches.HasValue)
{
    if (matches.Value)
    {
        Console.WriteLine("✅ View matches baseline");
    }
    else
    {
        Console.WriteLine($"❌ View differs from baseline");
        Console.WriteLine($"   Baseline: Screenshots/Baselines/MyView_Comparison.png");
        Console.WriteLine($"   Current:  {testPath}");
    }
}
```

### Save Methods

#### `Save(bitmap, filePath)`
Saves a bitmap to a specific path.

```csharp
var screenshot = ScreenshotHelper.Capture(view);
ScreenshotHelper.Save(screenshot, "custom/path/screenshot.png");
```

#### `CreateBaseline(Control, fileName, width, height)`
Creates or updates a baseline screenshot.

```csharp
var path = ScreenshotHelper.CreateBaseline(view, "MyView_Baseline", 800, 600);
// Saves to: Screenshots/Baselines/MyView_Baseline.png
```

### Comparison Methods

#### `Compare(baselinePath, currentPath, tolerance)`
Compares two screenshots.

```csharp
bool matches = ScreenshotHelper.Compare(
    "Screenshots/Baselines/MyView.png",
    "Screenshots/TestRun/MyView.png",
    tolerance: 0.01
);
```

**Tolerance Parameter**:
- `0.0` = Exact pixel match required
- `0.01` = 1% pixel difference allowed (recommended)
- `0.05` = 5% pixel difference allowed (lenient)
- `1.0` = Any difference accepted (not useful)

### Utility Methods

#### `GetTestRunDirectory()`
Returns the full path to test run directory.

```csharp
var dir = ScreenshotHelper.GetTestRunDirectory();
// Returns: /path/to/Screenshots/TestRun
```

#### `GetBaselineDirectory()`
Returns the full path to baseline directory.

```csharp
var dir = ScreenshotHelper.GetBaselineDirectory();
// Returns: /path/to/Screenshots/Baselines
```

#### `CleanupTestRun()`
Deletes all screenshots from test run directory.

```csharp
ScreenshotHelper.CleanupTestRun();
// Useful in test setup/teardown
```

## Baseline Management Workflow

### Initial Baseline Creation

**Step 1**: Run test to generate screenshot
```bash
dotnet test --filter "FullyQualifiedName~MyView_ShouldMatch_Baseline"
```

**Step 2**: Inspect the screenshot
```bash
# Screenshot saved to:
# SocialMediaCommander.Tests/bin/Debug/net9.0/Screenshots/TestRun/MyView_ShouldMatch_Baseline.png
```

**Step 3**: If correct, create baseline
```bash
# Option A: Copy manually
cp Screenshots/TestRun/MyView_ShouldMatch_Baseline.png Screenshots/Baselines/

# Option B: Use CreateBaseline in test (one-time)
ScreenshotHelper.CreateBaseline(view, "MyView_ShouldMatch_Baseline");
```

**Step 4**: Future test runs compare against baseline
```bash
dotnet test --filter "FullyQualifiedName~MyView_ShouldMatch_Baseline"
# Now compares with baseline and asserts match
```

### Updating Baselines

When intentional UI changes are made:

**Step 1**: Run tests (will fail if UI changed)
```bash
dotnet test --filter "FullyQualifiedName~UI"
```

**Step 2**: Review differences
- Check `Screenshots/TestRun/` for new screenshots
- Compare visually with `Screenshots/Baselines/`

**Step 3**: If changes are correct, update baselines
```bash
# Copy all updated screenshots
cp Screenshots/TestRun/*.png Screenshots/Baselines/
```

**Step 4**: Commit updated baselines
```bash
git add SocialMediaCommander.Tests/Screenshots/Baselines/
git commit -m "chore: Update UI screenshot baselines"
```

## Directory Structure

```
SocialMediaCommander.Tests/
├── bin/Debug/net9.0/
│   ├── Screenshots/
│   │   ├── Baselines/          ← Committed to repository
│   │   │   ├── AccountManagerView_Rendering.png
│   │   │   ├── PostEditorView_Layout.png
│   │   │   └── ...
│   │   └── TestRun/            ← Generated during tests (not committed)
│   │       ├── AccountManagerView_Rendering.png
│   │       ├── PostEditorView_Layout.png
│   │       └── ...
```

**.gitignore Configuration**:
```gitignore
# Ignore test run screenshots
SocialMediaCommander.Tests/Screenshots/TestRun/

# Keep baselines (comment out to not commit them)
# SocialMediaCommander.Tests/Screenshots/Baselines/
```

## Best Practices

### 1. Descriptive Test Names

Use clear names that describe what's being verified:

```csharp
// ✅ Good
[AvaloniaFact]
public async Task AccountManagerView_ShouldDisplay_AccountList()

[AvaloniaFact]
public async Task PostEditor_ShouldShow_CharacterCount()

// ❌ Bad
[AvaloniaFact]
public async Task Test1()

[AvaloniaFact]
public async Task Screenshot()
```

### 2. Consistent Sizing

Use consistent window sizes for comparable screenshots:

```csharp
// ✅ Good - Consistent sizes
const int StandardWidth = 800;
const int StandardHeight = 600;

var screenshot1 = ScreenshotHelper.Capture(view1, StandardWidth, StandardHeight);
var screenshot2 = ScreenshotHelper.Capture(view2, StandardWidth, StandardHeight);

// ❌ Bad - Varying sizes
var screenshot1 = ScreenshotHelper.Capture(view1, 800, 600);
var screenshot2 = ScreenshotHelper.Capture(view2, 1024, 768);  // Different size!
```

### 3. Wait for Rendering

Always allow time for views to render:

```csharp
// ✅ Good
window.Show();
await Task.Delay(100);  // Wait for rendering
var screenshot = ScreenshotHelper.Capture(view);

// ❌ Bad
window.Show();
var screenshot = ScreenshotHelper.Capture(view);  // May capture mid-render
```

### 4. Test Data Consistency

Use consistent test data for reproducible screenshots:

```csharp
// ✅ Good
var testData = new AccountViewModel
{
    Username = "testuser",
    DisplayName = "Test User",
    FollowerCount = 100
};
view.DataContext = testData;

// ❌ Bad
view.DataContext = GetRandomAccount();  // Screenshots vary each run
```

### 5. Appropriate Tolerance

Choose tolerance based on test goals:

```csharp
// Strict - For critical UI that shouldn't change
tolerance: 0.0  // Exact match

// Standard - For most UI tests
tolerance: 0.01  // 1% difference (recommended)

// Lenient - For dynamic content areas
tolerance: 0.05  // 5% difference
```

## Common Scenarios

### Testing Different States

```csharp
[Theory]
[InlineData("loading", "MyView_Loading")]
[InlineData("success", "MyView_Success")]
[InlineData("error", "MyView_Error")]
public async Task MyView_ShouldRender_InState(string state, string testName)
{
    // Arrange
    var view = new MyView();
    view.DataContext = new { State = state };
    
    var window = new Window { Content = view };
    window.Show();
    await Task.Delay(100);
    
    // Act & Assert
    var (path, exists, matches) = ScreenshotHelper.CaptureAndCompare(view, testName);
    
    if (exists)
    {
        matches.Should().BeTrue($"{state} state should match baseline");
    }
    
    window.Close();
}
```

### Testing Responsive Layout

```csharp
[Theory]
[InlineData(320, 568, "Mobile")]   // iPhone SE
[InlineData(768, 1024, "Tablet")]  // iPad
[InlineData(1920, 1080, "Desktop")]
public async Task MyView_ShouldBeResponsive_AtSize(int width, int height, string device)
{
    // Arrange
    var view = new MyView();
    var window = new Window { Content = view, Width = width, Height = height };
    window.Show();
    await Task.Delay(150);
    
    // Act
    var testName = $"MyView_Responsive_{device}";
    var screenshot = ScreenshotHelper.Capture(view, width, height);
    ScreenshotHelper.Save(screenshot, $"Screenshots/TestRun/{testName}.png");
    
    // Assert - Visual inspection required
    screenshot.Should().NotBeNull();
    
    window.Close();
}
```

### Testing Theme Variations

```csharp
[Theory]
[InlineData("Light", "MyView_LightTheme")]
[InlineData("Dark", "MyView_DarkTheme")]
public async Task MyView_ShouldRender_WithTheme(string theme, string testName)
{
    // Arrange
    var view = new MyView();
    ApplyTheme(view, theme);
    
    var window = new Window { Content = view };
    window.Show();
    await Task.Delay(100);
    
    // Act & Assert
    var (path, exists, matches) = ScreenshotHelper.CaptureAndCompare(view, testName);
    
    window.Close();
}
```

## Troubleshooting

### Screenshots Are Blank

**Problem**: Screenshot is completely white/transparent.

**Solutions**:
```csharp
// Ensure window is shown
window.Show();
await Task.Delay(100);  // Increase delay if needed

// Ensure control has valid bounds
view.Measure(new Size(800, 600));
view.Arrange(new Rect(0, 0, 800, 600));
```

### Comparisons Always Fail

**Problem**: Baseline comparison always returns false.

**Possible Causes**:
1. **Font rendering differences** between runs/machines
2. **Animation states** captured at different times
3. **Dynamic content** (timestamps, random data)

**Solutions**:
```csharp
// Increase tolerance
tolerance: 0.02  // From 0.01

// Disable animations in tests
view.DisableAnimations = true;

// Use static test data
view.DataContext = GetStaticTestData();
```

### Baselines Not Found

**Problem**: `baselineExists` is always false.

**Check**:
```csharp
// Verify baseline directory
var baselineDir = ScreenshotHelper.GetBaselineDirectory();
Console.WriteLine($"Baseline directory: {baselineDir}");

// Verify filename matches
var expectedPath = Path.Combine(baselineDir, $"{testName}.png");
Console.WriteLine($"Looking for: {expectedPath}");
```

## CI/CD Integration

### Uploading Screenshot Artifacts

Update `.github/workflows/ci-cd.yml`:

```yaml
- name: Upload UI Screenshots
  if: failure()
  uses: actions/upload-artifact@v4
  with:
    name: ui-screenshots-${{ matrix.os }}-${{ matrix.arch }}
    path: SocialMediaCommander.Tests/bin/Debug/net9.0/Screenshots/TestRun/
    retention-days: 30
```

### Baseline Storage Options

**Option 1: Commit to Repository** (Recommended for small projects)
- ✅ Simple workflow
- ✅ Version controlled
- ❌ Increases repository size

**Option 2: External Storage** (For large test suites)
- ✅ Smaller repository
- ✅ Faster clones
- ❌ More complex setup

## Example: Complete Screenshot Test

```csharp
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using SocialMediaCommander.Desktop.Views;
using SocialMediaCommander.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace SocialMediaCommander.Tests.UI;

public class AccountManagerViewScreenshotTests
{
    private readonly ITestOutputHelper _output;

    public AccountManagerViewScreenshotTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [AvaloniaFact]
    public async Task AccountManagerView_ShouldMatch_BaselineRendering()
    {
        // Arrange - Create view with consistent test data
        var view = new AccountManagerView();
        view.DataContext = GetTestAccountData();
        
        var window = new Window 
        { 
            Content = view,
            Width = 800,
            Height = 600
        };
        
        window.Show();
        await Task.Delay(150); // Allow rendering
        
        // Act - Capture and compare
        var testName = nameof(AccountManagerView_ShouldMatch_BaselineRendering);
        var (testPath, baselineExists, matches) = ScreenshotHelper.CaptureAndCompare(
            view,
            testName,
            width: 800,
            height: 600,
            tolerance: 0.01
        );
        
        // Assert
        view.Should().NotBeNull();
        
        if (baselineExists)
        {
            _output.WriteLine($"Comparing with baseline");
            matches.Should().BeTrue(
                "View rendering should match baseline. " +
                $"Test screenshot: {testPath}, " +
                $"Baseline: {ScreenshotHelper.GetBaselineDirectory()}/{testName}.png"
            );
        }
        else
        {
            _output.WriteLine($"⚠️  No baseline exists for {testName}");
            _output.WriteLine($"Test screenshot saved to: {testPath}");
            _output.WriteLine($"If rendering is correct, copy to baselines:");
            _output.WriteLine($"  cp {testPath} {ScreenshotHelper.GetBaselineDirectory()}/");
        }
        
        // Cleanup
        window.Close();
    }

    private static object GetTestAccountData()
    {
        return new
        {
            Accounts = new[]
            {
                new { Username = "user1", Platform = "Twitter", IsActive = true },
                new { Username = "user2", Platform = "LinkedIn", IsActive = true }
            }
        };
    }
}
```

## Resources

- [TDD Workflow Guide](tdd-workflow.md) - General TDD practices
- [TDD Implementation Guide](../../TDD_IMPLEMENTATION_GUIDE.md) - Comprehensive overview
- [UI Tests Summary](../../UI_TESTS_SUMMARY.md) - Current UI test status

---

**Pro Tip**: Start with screenshot tests for stable, critical UI components. Add visual regression testing incrementally as your test suite matures.
