using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SocialMediaCommander.Services.Implementation;

namespace SocialMediaCommander.Tests.UnitTests;

/// <summary>
/// Unit tests for DocumentationService functionality
/// </summary>
public class DocumentationServiceTests : IDisposable
{
    private readonly DocumentationService _documentationService;
    private readonly string _tempDocumentationPath;

    public DocumentationServiceTests()
    {
        // Create a temporary documentation directory for testing
        _tempDocumentationPath = Path.Combine(Path.GetTempPath(), $"SMC_DocsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDocumentationPath);

        // Create the documentation service with custom path for testing
        _documentationService = new DocumentationService(_tempDocumentationPath);
    }

    [Fact]
    public void GetCategories_ShouldReturnAllCategories()
    {
        // Act
        var categories = _documentationService.GetCategories();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().NotBeEmpty();
        categories.Should().Contain(c => c.Name == "user-guides");
        categories.Should().Contain(c => c.Name == "technical");
        categories.Should().Contain(c => c.Name == "security");
        categories.Should().Contain(c => c.Name == "development");
        categories.Should().Contain(c => c.Name == "operations");
    }

    [Fact]
    public void GetCategories_ShouldReturnCategoriesInSortOrder()
    {
        // Act
        var categories = _documentationService.GetCategories().ToList();

        // Assert
        categories.Should().NotBeEmpty();
        for (int i = 1; i < categories.Count; i++)
        {
            categories[i].SortOrder.Should().BeGreaterThanOrEqualTo(categories[i - 1].SortOrder);
        }
    }

    [Fact]
    public void GetCategories_ShouldHaveValidDisplayNames()
    {
        // Act
        var categories = _documentationService.GetCategories();

        // Assert
        categories.Should().NotBeNull();
        foreach (var category in categories)
        {
            category.Name.Should().NotBeNullOrEmpty();
            category.DisplayName.Should().NotBeNullOrEmpty();
            category.Description.Should().NotBeNullOrEmpty();
            category.SortOrder.Should().BeGreaterThan(0);
        }
    }

    [Theory]
    [InlineData("user-guides")]
    [InlineData("technical")]
    [InlineData("security")]
    [InlineData("development")]
    [InlineData("operations")]
    public void GetFilesByCategory_WithValidCategory_ShouldReturnFiles(string categoryName)
    {
        // Act
        var files = _documentationService.GetFilesByCategory(categoryName);

        // Assert
        files.Should().NotBeNull();
        files.Should().NotBeEmpty();
        files.Should().OnlyContain(f => f.Category == categoryName);
    }

    [Fact]
    public void GetFilesByCategory_WithInvalidCategory_ShouldReturnEmpty()
    {
        // Act
        var files = _documentationService.GetFilesByCategory("nonexistent-category");

        // Assert
        files.Should().NotBeNull();
        files.Should().BeEmpty();
    }

    [Fact]
    public void GetFilesByCategory_ShouldReturnFilesInSortOrder()
    {
        // Act
        var files = _documentationService.GetFilesByCategory("user-guides").ToList();

        // Assert
        files.Should().NotBeEmpty();
        for (int i = 1; i < files.Count; i++)
        {
            files[i].SortOrder.Should().BeGreaterThanOrEqualTo(files[i - 1].SortOrder);
        }
    }

    [Theory]
    [InlineData("user-guide")]
    [InlineData("getting-started")]
    [InlineData("technical-documentation")]
    [InlineData("implementation-summary")]
    [InlineData("security-overview")]
    [InlineData("enhanced-features")]
    public void GetFileById_WithValidId_ShouldReturnFile(string fileId)
    {
        // Act
        var file = _documentationService.GetFileById(fileId);

        // Assert
        file.Should().NotBeNull();
        file!.Id.Should().Be(fileId);
        file.Title.Should().NotBeNullOrEmpty();
        file.Category.Should().NotBeNullOrEmpty();
        file.FilePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetFileById_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var file = _documentationService.GetFileById("nonexistent-file");

        // Assert
        file.Should().BeNull();
    }

    [Fact]
    public void GetFileById_WithNullId_ShouldReturnNull()
    {
        // Act
        var file = _documentationService.GetFileById(null!);

        // Assert
        file.Should().BeNull();
    }

    [Fact]
    public void GetFileById_WithEmptyId_ShouldReturnNull()
    {
        // Act
        var file = _documentationService.GetFileById("");

        // Assert
        file.Should().BeNull();
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_WithNonexistentFile_ShouldReturnNotFoundHtml()
    {
        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync("nonexistent/file.md");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("Documentation Not Found");
        result.Should().Contain("nonexistent/file.md");
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_WithValidMarkdown_ShouldReturnHtml()
    {
        // Arrange
        var markdownContent = @"# Test Document

This is a **bold** text and this is *italic*.

## Section 2

- List item 1
- List item 2

```csharp
var example = ""Hello World"";
```";

        var testFilePath = CreateTempMarkdownFile("test-doc.md", markdownContent);
        var relativePath = Path.GetRelativePath(_tempDocumentationPath, testFilePath);

        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync(relativePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("Test Document");  // Header text without exact HTML tags
        result.Should().Contain("<strong>bold</strong>");
        result.Should().Contain("<em>italic</em>");
        result.Should().Contain("Section 2");  // Header text without exact HTML tags
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>List item 1</li>");
        result.Should().Contain("language-csharp");  // Check for code highlighting instead
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_WithComplexMarkdown_ShouldReturnStyledHtml()
    {
        // Arrange
        var markdownContent = @"# Complex Document

> This is a blockquote

| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |

## Code Example

```javascript
function example() {
    return ""Hello World"";
}
```

### Links and Images

[Link example](https://example.com)

---

**Note:** This is a test document.";

        var testFilePath = CreateTempMarkdownFile("complex-doc.md", markdownContent);
        var relativePath = Path.GetRelativePath(_tempDocumentationPath, testFilePath);

        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync(relativePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("<title>Documentation</title>");
        result.Should().Contain("<style>");
        result.Should().Contain("font-family:");
        result.Should().Contain("<blockquote>");
        result.Should().Contain("<table>");
        result.Should().Contain("<th>");
        result.Should().Contain("<td>");
        result.Should().Contain("<pre>");
        result.Should().Contain("<a href=\"https://example.com\">");
        result.Should().Contain("<hr");
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_WithEmptyFile_ShouldReturnValidHtml()
    {
        // Arrange
        var testFilePath = CreateTempMarkdownFile("empty-doc.md", "");
        var relativePath = Path.GetRelativePath(_tempDocumentationPath, testFilePath);

        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync(relativePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("<title>Documentation</title>");
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var markdownContent = @"# Special Characters Test

This document contains special characters: é, ñ, ü, 中文, 🚀

## Code with Special Characters

```
var emoji = ""🎉"";
var chinese = ""你好"";
var spanish = ""niño"";
```";

        var testFilePath = CreateTempMarkdownFile("special-chars.md", markdownContent);
        var relativePath = Path.GetRelativePath(_tempDocumentationPath, testFilePath);

        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync(relativePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("charset=\"utf-8\"");
        result.Should().Contain("Special Characters Test");
        result.Should().Contain("é, ñ, ü, 中文, 🚀");
        result.Should().Contain("🎉");
        result.Should().Contain("你好");
        result.Should().Contain("niño");
    }

    [Fact]
    public async Task LoadAndParseDocumentationAsync_HtmlShouldHaveValidStructure()
    {
        // Arrange
        var markdownContent = "# Test";
        var testFilePath = CreateTempMarkdownFile("structure-test.md", markdownContent);
        var relativePath = Path.GetRelativePath(_tempDocumentationPath, testFilePath);

        // Act
        var result = await _documentationService.LoadAndParseDocumentationAsync(relativePath);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("<!DOCTYPE html>");
        result.Should().Contain("<html>");
        result.Should().Contain("<head>");
        result.Should().Contain("<title>Documentation</title>");
        result.Should().Contain("<meta charset=\"utf-8\">");
        result.Should().Contain("<style>");
        result.Should().Contain("</head>");
        result.Should().Contain("<body>");
        result.Should().Contain("</body>");
        result.Should().Contain("</html>");
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act & Assert - Constructor should not throw
        var service = new DocumentationService();
        service.Should().NotBeNull();
    }

    [Fact]
    public void GetCategories_MultipleCallsShouldReturnConsistentResults()
    {
        // Act
        var categories1 = _documentationService.GetCategories().ToList();
        var categories2 = _documentationService.GetCategories().ToList();

        // Assert
        categories1.Should().HaveCount(categories2.Count);
        categories1.Should().BeEquivalentTo(categories2);
    }

    [Fact]
    public void GetFilesByCategory_MultipleCallsShouldReturnConsistentResults()
    {
        // Act
        var files1 = _documentationService.GetFilesByCategory("user-guides").ToList();
        var files2 = _documentationService.GetFilesByCategory("user-guides").ToList();

        // Assert
        files1.Should().HaveCount(files2.Count);
        files1.Should().BeEquivalentTo(files2);
    }

    private string CreateTempMarkdownFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDocumentationPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_tempDocumentationPath))
        {
            try
            {
                Directory.Delete(_tempDocumentationPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}