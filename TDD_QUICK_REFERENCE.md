# TDD Quick Reference

**Quick guide for practicing Test-Driven Development in Social Media Commander**

## The TDD Cycle

```
🔴 RED    → Write a failing test
🟢 GREEN  → Make it pass (minimal code)
🔵 REFACTOR → Improve the code
↺ REPEAT
```

## Common Test Commands

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName~Integration"
dotnet test --filter "FullyQualifiedName~UI"

# Run specific test
dotnet test --filter "MyTest_ShouldWork"

# With detailed output
dotnet test --verbosity detailed

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Structure (AAA Pattern)

```csharp
[Fact]
public void MyFeature_ShouldBehave_WhenCondition()
{
    // Arrange - Setup
    var service = new MyService();
    var input = "test";
    
    // Act - Execute
    var result = service.Process(input);
    
    // Assert - Verify
    result.Should().Be("expected");
}
```

## Screenshot Testing

```csharp
using SocialMediaCommander.Tests.Helpers;

[AvaloniaFact]
public async Task MyView_ShouldMatch_Baseline()
{
    var view = new MyView();
    var window = new Window { Content = view };
    window.Show();
    await Task.Delay(100);
    
    // Capture and compare
    var (path, exists, matches) = ScreenshotHelper.CaptureAndCompare(
        view, 
        nameof(MyView_ShouldMatch_Baseline),
        tolerance: 0.01
    );
    
    if (exists)
    {
        matches.Should().BeTrue();
    }
    
    window.Close();
}
```

## Interaction Recording

```csharp
public class MyTests : RecordedTestBase
{
    [Fact]
    public void MyTest()
    {
        Record("Starting test");
        RecordCommand("MyCommand", "parameter");
        RecordPropertyChange("Status", "old", "new");
        RecordAssertion("Check passed", true);
        
        SaveRecording(nameof(MyTest));
    }
}
```

## FluentAssertions Examples

```csharp
// Basic
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().BeTrue();

// Strings
text.Should().Contain("substring");
text.Should().StartWith("prefix");
text.Should().NotBeNullOrEmpty();

// Collections
list.Should().HaveCount(5);
list.Should().Contain(item);
list.Should().BeEmpty();

// Exceptions
Action act = () => service.ThrowError();
act.Should().Throw<InvalidOperationException>()
   .WithMessage("error message");

// Async
await asyncFunc.Should().ThrowAsync<Exception>();
```

## Moq Examples

```csharp
// Setup mock
var mock = new Mock<IMyService>();
mock.Setup(s => s.GetData()).Returns("test");
mock.Setup(s => s.ProcessAsync(It.IsAny<string>()))
    .ReturnsAsync("result");

// Use mock
var service = mock.Object;

// Verify calls
mock.Verify(s => s.GetData(), Times.Once);
mock.Verify(s => s.ProcessAsync("input"), Times.Exactly(2));
```

## Test Categories

| Category | Location | Purpose |
|----------|----------|---------|
| Unit | `UnitTests/` | Test individual classes |
| Integration | `Integration/` | Test component interaction |
| UI | `UI/` | Test Avalonia views |
| Performance | `Performance/` | Test async patterns |

## Common Patterns

### Test Setup
```csharp
// Use constructor for common setup
public class MyTests
{
    private readonly MyService _service;
    
    public MyTests()
    {
        _service = new MyService();
    }
}
```

### Test Data
```csharp
// Theory for multiple inputs
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void MyTest(string input, string expected)
{
    var result = Process(input);
    result.Should().Be(expected);
}
```

### Async Tests
```csharp
[Fact]
public async Task MyAsyncTest()
{
    var result = await service.GetDataAsync();
    result.Should().NotBeNull();
}
```

## File Locations

- **Test Infrastructure**: `SocialMediaCommander.Tests/`
- **Screenshot Helper**: `Helpers/ScreenshotHelper.cs`
- **Recorder Helper**: `Helpers/TestInteractionRecorder.cs`
- **Example Tests**: `UI/TddExampleTests.cs`
- **Screenshots**: `bin/Debug/net9.0/Screenshots/`
- **Recordings**: `bin/Debug/net9.0/test-recordings/`

## Best Practices

✅ **DO:**
- Write test before code (Red-Green-Refactor)
- Use descriptive test names
- Test one behavior per test
- Keep tests fast and independent
- Use AAA pattern (Arrange-Act-Assert)
- Mock external dependencies

❌ **DON'T:**
- Test implementation details
- Create test interdependencies
- Use random/changing test data
- Skip the refactor step
- Ignore failing tests

## Documentation

- **Workflow Guide**: [docs/development/tdd-workflow.md](docs/development/tdd-workflow.md)
- **Screenshot Guide**: [docs/development/screenshot-testing.md](docs/development/screenshot-testing.md)
- **Full Assessment**: [TDD_IMPLEMENTATION_GUIDE.md](TDD_IMPLEMENTATION_GUIDE.md)
- **Summary**: [TDD_IMPLEMENTATION_SUMMARY.md](TDD_IMPLEMENTATION_SUMMARY.md)

## Getting Help

1. Check the documentation guides above
2. Look at example tests in `TddExampleTests.cs`
3. Review existing tests in `SocialMediaCommander.Tests/`
4. Ask team for test review before committing

---

**Remember**: Good tests are the foundation of maintainable code! 🚀
