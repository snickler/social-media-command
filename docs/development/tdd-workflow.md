# TDD Workflow Guide

## Overview

This guide explains how to practice Test-Driven Development (TDD) in the Social Media Commander project. TDD is a development methodology where tests are written before the implementation code.

## The Red-Green-Refactor Cycle

TDD follows a simple three-step cycle:

```
┌──────────────────────────────────────────────┐
│          TDD Red-Green-Refactor              │
└──────────────────────────────────────────────┘

1. 🔴 RED: Write a failing test
   ↓
2. 🟢 GREEN: Make the test pass
   ↓
3. 🔵 REFACTOR: Improve the code
   ↓
   Repeat ↺
```

### Step 1: RED - Write a Failing Test

**Goal**: Define the expected behavior before writing code.

```csharp
[Fact]
public void PostViewModel_ShouldValidate_WhenTitleEmpty()
{
    // Arrange
    var vm = new PostViewModel();
    
    // Act
    vm.Title = "";
    var isValid = vm.Validate();
    
    // Assert
    isValid.Should().BeFalse("Empty title should be invalid");
    vm.Errors.Should().Contain("Title is required");
}
```

**Run the test**: ❌ It will FAIL because the feature doesn't exist yet.

### Step 2: GREEN - Make the Test Pass

**Goal**: Write the minimum code to make the test pass.

```csharp
public class PostViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<string> Errors { get; } = new();
    
    public bool Validate()
    {
        Errors.Clear();
        
        if (string.IsNullOrWhiteSpace(Title))
        {
            Errors.Add("Title is required");
            return false;
        }
        
        return true;
    }
}
```

**Run the test**: ✅ It should PASS now.

### Step 3: REFACTOR - Improve the Code

**Goal**: Clean up the code while keeping tests green.

```csharp
public class PostViewModel
{
    private const string TitleRequiredMessage = "Title is required";
    
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
    private readonly List<string> _errors = new();
    
    public bool Validate()
    {
        _errors.Clear();
        
        if (string.IsNullOrWhiteSpace(Title))
        {
            _errors.Add(TitleRequiredMessage);
            return false;
        }
        
        return true;
    }
}
```

**Run all tests**: ✅ All tests should still PASS.

## Best Practices

### 1. AAA Pattern (Arrange-Act-Assert)

Always structure tests with three clear sections:

```csharp
[Fact]
public void MyTest()
{
    // Arrange - Set up test data and dependencies
    var service = new MyService();
    var input = "test";
    
    // Act - Execute the behavior being tested
    var result = service.Process(input);
    
    // Assert - Verify the expected outcome
    result.Should().NotBeNull();
    result.Should().Be("PROCESSED: test");
}
```

### 2. Descriptive Test Names

Use clear, behavior-focused names:

```csharp
// ✅ Good
[Fact]
public void PostService_ShouldThrowException_WhenContentExceedsLimit()

// ❌ Bad
[Fact]
public void Test1()
```

**Naming Convention**: `MethodName_ShouldExpectedBehavior_WhenCondition`

### 3. One Assertion Per Logical Concept

Focus each test on a single behavior:

```csharp
// ✅ Good - Tests one concept
[Fact]
public void User_ShouldBeActive_WhenEmailVerified()
{
    var user = new User { EmailVerified = true };
    user.IsActive.Should().BeTrue();
}

// ❌ Bad - Tests multiple unrelated things
[Fact]
public void User_Tests()
{
    var user = new User();
    user.Email.Should().NotBeNull();
    user.Name.Should().NotBeNull();
    user.CreatedDate.Should().BeAfter(DateTime.MinValue);
}
```

### 4. Use Meaningful Test Data

Avoid "magic values" that obscure intent:

```csharp
// ✅ Good
[Fact]
public void TwitterPost_ShouldFail_WhenExceedingCharacterLimit()
{
    const int TwitterCharacterLimit = 280;
    var longContent = new string('x', TwitterCharacterLimit + 1);
    
    var post = new Post { Content = longContent };
    var result = post.ValidateForTwitter();
    
    result.IsValid.Should().BeFalse();
}

// ❌ Bad
[Fact]
public void Test()
{
    var post = new Post { Content = new string('x', 281) };
    post.ValidateForTwitter().IsValid.Should().BeFalse();
}
```

### 5. Test Edge Cases

Don't just test the happy path:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void PostValidator_ShouldReject_EmptyOrWhitespaceTitle(string title)
{
    var validator = new PostValidator();
    var result = validator.ValidateTitle(title);
    result.Should().BeFalse();
}
```

## Project-Specific Patterns

### Unit Tests

**Location**: `SocialMediaCommander.Tests/UnitTests/`

**Purpose**: Test individual classes/methods in isolation

**Example**:
```csharp
namespace SocialMediaCommander.Tests.UnitTests;

public class BackupServiceTests
{
    [Fact]
    public void BackupService_ShouldCreateBackup_WhenDataProvided()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var service = new BackupService(mockFileSystem.Object);
        var data = new BackupData { /* ... */ };
        
        // Act
        var result = service.CreateBackup(data);
        
        // Assert
        result.Should().NotBeNull();
        mockFileSystem.Verify(fs => fs.WriteFile(
            It.IsAny<string>(), 
            It.IsAny<byte[]>()
        ), Times.Once);
    }
}
```

### Integration Tests

**Location**: `SocialMediaCommander.Tests/Integration/`

**Purpose**: Test multiple components working together

**Example**:
```csharp
namespace SocialMediaCommander.Tests.Integration;

public class OAuthFlowIntegrationTests
{
    [Fact]
    public async Task OAuthFlow_ShouldComplete_ForValidCredentials()
    {
        // Arrange - Use real services with test configuration
        var services = new ServiceCollection();
        services.AddSocialMediaCommanderServices(GetTestConfiguration());
        var provider = services.BuildServiceProvider();
        
        var authService = provider.GetRequiredService<IOAuthAuthenticationService>();
        var accountService = provider.GetRequiredService<IAccountService>();
        
        // Act
        var authResult = await authService.AuthenticateAsync("twitter", "test-code");
        var account = await accountService.GetAccountByIdAsync(authResult.AccountId);
        
        // Assert
        account.Should().NotBeNull();
        account.IsAuthenticated.Should().BeTrue();
    }
}
```

### UI Tests with Screenshots

**Location**: `SocialMediaCommander.Tests/UI/`

**Purpose**: Test Avalonia UI components

**Example**:
```csharp
using SocialMediaCommander.Tests.Helpers;

namespace SocialMediaCommander.Tests.UI;

public class MyViewTests : RecordedTestBase
{
    [AvaloniaFact]
    public async Task MyView_ShouldRender_Correctly()
    {
        // Arrange
        Record("Creating view instance");
        var view = new MyView();
        var window = new Window { Content = view };
        
        // Act
        Record("Showing window");
        window.Show();
        await Task.Delay(100);
        
        Record("Capturing screenshot");
        var (testPath, baselineExists, matches) = 
            ScreenshotHelper.CaptureAndCompare(view, nameof(MyView_ShouldRender_Correctly));
        
        // Assert
        view.Should().NotBeNull();
        
        if (baselineExists)
        {
            matches.Should().BeTrue("View should match baseline rendering");
        }
        
        // Save interaction log
        SaveRecording(nameof(MyView_ShouldRender_Correctly));
        
        // Cleanup
        window.Close();
    }
}
```

## Using Test Helpers

### Screenshot Helper

The `ScreenshotHelper` class provides utilities for visual testing:

```csharp
// Capture and save screenshot
var screenshot = ScreenshotHelper.Capture(view, width: 800, height: 600);
ScreenshotHelper.Save(screenshot, "Screenshots/TestRun/MyView.png");

// Capture and compare with baseline
var (testPath, baselineExists, matches) = ScreenshotHelper.CaptureAndCompare(
    view, 
    "MyView_Test",
    tolerance: 0.01  // 1% difference allowed
);

// Create baseline for first time
ScreenshotHelper.CreateBaseline(view, "MyView_Baseline");
```

**Baseline Workflow**:
1. Run test - screenshot saved to `Screenshots/TestRun/`
2. Manually inspect screenshot
3. If correct, copy to `Screenshots/Baselines/`
4. Future test runs compare against baseline

### Interaction Recorder

The `TestInteractionRecorder` logs all test actions:

```csharp
public class MyTests : RecordedTestBase
{
    [Fact]
    public void MyTest()
    {
        // Record setup
        Record("Creating service");
        var service = new MyService();
        
        // Record commands
        RecordCommand("ProcessCommand", "parameter");
        service.ProcessCommand.Execute("parameter");
        
        // Record property changes
        RecordPropertyChange("Status", "Idle", "Processing");
        service.Status = "Processing";
        
        // Record navigation
        RecordNavigation("HomeView", "SettingsView");
        
        // Record assertions
        RecordAssertion("Service should be processing", service.IsProcessing);
        
        // Save recording
        SaveRecording(nameof(MyTest));
    }
}
```

**Recording Output** (`test-recordings/MyTest.log`):
```
Test Recording: MyTest
Started: 2025-10-05 21:17:39.712 UTC
Duration: 0.515s
================================================================================

[0.003s] Creating service
[0.010s] Command invoked: ProcessCommand: with parameter: parameter
[0.012s] Property changed: Status: Idle → Processing
[0.015s] Navigation: HomeView → SettingsView
[0.017s] ✓ PASS: Service should be processing

================================================================================
Total Interactions: 5
```

## Running Tests

### Run All Tests
```bash
dotnet test SocialMediaCommander.sln
```

### Run Specific Test Category
```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~UnitTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"

# UI tests only
dotnet test --filter "FullyQualifiedName~UI"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~MyViewTests.MyView_ShouldRender_Correctly"
```

### Run with Detailed Output
```bash
dotnet test --verbosity detailed
```

## Common Pitfalls

### ❌ Testing Implementation Details

```csharp
// Bad - Tests internal implementation
[Fact]
public void MyService_ShouldCall_PrivateMethod()
{
    var service = new MyService();
    service.DoWork();
    // Checking private method was called
}
```

```csharp
// Good - Tests observable behavior
[Fact]
public void MyService_ShouldUpdateStatus_WhenWorkCompletes()
{
    var service = new MyService();
    service.DoWork();
    service.Status.Should().Be("Completed");
}
```

### ❌ Fragile Tests (Brittle)

```csharp
// Bad - Breaks if message changes
[Fact]
public void Validator_ShouldReturnExactMessage()
{
    var result = validator.Validate("");
    result.Error.Should().Be("The Title field is required.");
}
```

```csharp
// Good - Tests behavior, not exact wording
[Fact]
public void Validator_ShouldIndicateRequired_WhenEmpty()
{
    var result = validator.Validate("");
    result.IsValid.Should().BeFalse();
    result.Error.Should().Contain("Title");
    result.Error.Should().Contain("required");
}
```

### ❌ Test Interdependence

```csharp
// Bad - Test depends on order/state
private static User? _sharedUser;

[Fact]
public void Test1_CreateUser()
{
    _sharedUser = new User { Name = "Test" };
}

[Fact]
public void Test2_UseUser()
{
    _sharedUser!.Name.Should().Be("Test"); // Fails if Test1 doesn't run first
}
```

```csharp
// Good - Each test is independent
[Fact]
public void Test1_CreateUser()
{
    var user = new User { Name = "Test" };
    user.Should().NotBeNull();
}

[Fact]
public void Test2_UserHasName()
{
    var user = new User { Name = "Test" };
    user.Name.Should().Be("Test");
}
```

## CI/CD Integration

Tests run automatically in the CI/CD pipeline:

- **Build Step**: All tests run after successful build
- **UI Tests**: Run in headless mode (no display required)
- **Code Coverage**: Coverage reports generated and commented on PRs
- **Screenshot Artifacts**: UI screenshots uploaded on test failures

### Local Pre-Commit Checks

Git hooks automatically run before commits:
- ✅ Build verification
- ✅ Test execution
- ✅ Code formatting
- ✅ Secret detection

## Resources

- [TDD Implementation Guide](../../TDD_IMPLEMENTATION_GUIDE.md) - Comprehensive assessment
- [UI Tests Summary](../../UI_TESTS_SUMMARY.md) - UI testing details
- [Performance Optimizations](../technical/performance-optimizations.md) - Async patterns
- [Copilot Instructions](../../.github/copilot-instructions.md) - Project standards

## Next Steps

1. **Try the Red-Green-Refactor cycle** with a simple feature
2. **Write tests first** before implementing new functionality
3. **Use screenshot testing** for new UI components
4. **Record interactions** when debugging complex tests
5. **Review existing tests** in `SocialMediaCommander.Tests/` for examples

---

**Remember**: The goal of TDD is not just testing—it's designing better code through tests!
