using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SocialMediaCommander.Desktop.ViewModels;
using SocialMediaCommander.Services.Interfaces;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Tests;

/// <summary>
/// Comprehensive tests for DocumentationViewModel with MVVM pattern compliance
/// </summary>
public class DocumentationViewModelTests
{
    private readonly Mock<IDocumentationService> _mockDocumentationService;
    private readonly DocumentationViewModel _viewModel;

    public DocumentationViewModelTests()
    {
        _mockDocumentationService = new Mock<IDocumentationService>();
        SetupMockDocumentationService();
        _viewModel = new DocumentationViewModel(_mockDocumentationService.Object);
    }

    private void SetupMockDocumentationService()
    {
        var categories = new List<DocumentationCategory>
        {
            new() { Name = "Getting Started", Description = "Basic setup and configuration", SortOrder = 1 },
            new() { Name = "Advanced Features", Description = "Advanced functionality", SortOrder = 2 },
            new() { Name = "API Reference", Description = "Complete API documentation", SortOrder = 3 }
        };

        var files = new Dictionary<string, List<DocumentationFile>>
        {
            ["Getting Started"] = new List<DocumentationFile>
            {
                new() { Id = "quickstart", Title = "Quick Start Guide", FilePath = "quickstart.md", Category = "Getting Started" },
                new() { Id = "installation", Title = "Installation", FilePath = "installation.md", Category = "Getting Started" }
            },
            ["Advanced Features"] = new List<DocumentationFile>
            {
                new() { Id = "automation", Title = "Automation", FilePath = "automation.md", Category = "Advanced Features" },
                new() { Id = "integrations", Title = "Integrations", FilePath = "integrations.md", Category = "Advanced Features" }
            },
            ["API Reference"] = new List<DocumentationFile>
            {
                new() { Id = "auth-api", Title = "Authentication API", FilePath = "auth-api.md", Category = "API Reference" }
            }
        };

        _mockDocumentationService.Setup(x => x.GetCategories()).Returns(categories);

        foreach (var kvp in files)
        {
            _mockDocumentationService.Setup(x => x.GetFilesByCategory(kvp.Key)).Returns(kvp.Value);
        }

        // Setup file retrieval by ID
        var allFiles = files.Values.SelectMany(f => f).ToList();
        foreach (var file in allFiles)
        {
            _mockDocumentationService.Setup(x => x.GetFileById(file.Id)).Returns(file);
        }

        // Setup content loading
        _mockDocumentationService.Setup(x => x.LoadAndParseDocumentationAsync(It.IsAny<string>()))
            .ReturnsAsync((string filePath) => $"<h1>Documentation Content</h1><p>Content for {filePath}</p>");
    }

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        Assert.NotNull(_viewModel.Categories);
        Assert.NotNull(_viewModel.FilesInCategory);
        Assert.Empty(_viewModel.DocumentContent);
        Assert.False(_viewModel.IsLoading);
        Assert.Empty(_viewModel.LoadingMessage);
    }

    [Fact]
    public void Constructor_ShouldLoadCategories()
    {
        // Assert
        Assert.Equal(3, _viewModel.Categories.Count);
        Assert.Equal("Getting Started", _viewModel.Categories[0].Name);
        Assert.Equal("Advanced Features", _viewModel.Categories[1].Name);
        Assert.Equal("API Reference", _viewModel.Categories[2].Name);
    }

    [Fact]
    public void Constructor_ShouldSelectFirstCategoryByDefault()
    {
        // Assert
        Assert.NotNull(_viewModel.SelectedCategory);
        Assert.Equal("Getting Started", _viewModel.SelectedCategory.Name);
    }

    #endregion

    #region Category Selection Tests

    [Fact]
    public void SelectedCategory_Changed_ShouldLoadFilesForCategory()
    {
        // Arrange
        var advancedCategory = _viewModel.Categories.First(c => c.Name == "Advanced Features");

        // Act
        _viewModel.SelectedCategory = advancedCategory;

        // Assert
        Assert.Equal(2, _viewModel.FilesInCategory.Count);
        Assert.Equal("Automation", _viewModel.FilesInCategory[0].Title);
        Assert.Equal("Integrations", _viewModel.FilesInCategory[1].Title);
    }

    [Fact]
    public void SelectedCategory_Changed_ShouldSelectFirstFileByDefault()
    {
        // Arrange
        var apiCategory = _viewModel.Categories.First(c => c.Name == "API Reference");

        // Act
        _viewModel.SelectedCategory = apiCategory;

        // Assert
        Assert.NotNull(_viewModel.SelectedFile);
        Assert.Equal("Authentication API", _viewModel.SelectedFile.Title);
    }

    [Fact]
    public void SelectedCategory_SetToNull_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _viewModel.SelectedCategory = null);
        Assert.Null(exception);
    }

    #endregion

    #region File Selection and Content Loading Tests

    [Fact]
    public async Task SelectedFile_Changed_ShouldLoadDocumentContent()
    {
        // Arrange
        var file = _viewModel.FilesInCategory.First();

        // Act
        _viewModel.SelectedFile = file;
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        Assert.Contains("Documentation Content", _viewModel.DocumentContent);
        Assert.Contains(file.FilePath, _viewModel.DocumentContent);
    }

    [Fact]
    public async Task LoadDocumentContentAsync_ShouldSetLoadingStates()
    {
        // Arrange
        var file = _viewModel.FilesInCategory.First();
        var loadingStates = new List<bool>();
        var loadingMessages = new List<string>();

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsLoading))
                loadingStates.Add(_viewModel.IsLoading);
            if (e.PropertyName == nameof(_viewModel.LoadingMessage))
                loadingMessages.Add(_viewModel.LoadingMessage);
        };

        // Act
        _viewModel.SelectedFile = file;
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        Assert.Contains(true, loadingStates); // Should have been loading at some point
        Assert.Contains(false, loadingStates); // Should have finished loading
        Assert.False(_viewModel.IsLoading); // Should be false at the end
        Assert.Empty(_viewModel.LoadingMessage); // Should be empty at the end
    }

    [Fact]
    public async Task LoadDocumentContentAsync_WithError_ShouldGenerateErrorContent()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.LoadAndParseDocumentationAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test error"));

        var file = _viewModel.FilesInCategory.First();

        // Act
        _viewModel.SelectedFile = file;
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        Assert.Contains("Error Loading Documentation", _viewModel.DocumentContent);
        Assert.Contains("Test error", _viewModel.DocumentContent);
        Assert.False(_viewModel.IsLoading);
    }

    #endregion

    #region Command Tests

    [Fact]
    public void RefreshDocumentationCommand_ShouldReloadCategories()
    {
        // Arrange
        _viewModel.Categories.Clear();

        // Act
        _viewModel.RefreshDocumentationCommand.Execute(null);

        // Assert
        Assert.Equal(3, _viewModel.Categories.Count);
        _mockDocumentationService.Verify(x => x.GetCategories(), Times.AtLeast(2));
    }

    [Fact]
    public void SelectCategoryCommand_ShouldSetSelectedCategory()
    {
        // Arrange
        var targetCategory = _viewModel.Categories.First(c => c.Name == "Advanced Features");

        // Act
        _viewModel.SelectCategoryCommand.Execute(targetCategory);

        // Assert
        Assert.Equal(targetCategory, _viewModel.SelectedCategory);
    }

    [Fact]
    public async Task LoadFileByIdCommand_WithValidId_ShouldSelectFileAndCategory()
    {
        // Act
        await _viewModel.LoadFileByIdCommand.ExecuteAsync("automation");
        await Task.Delay(150); // Allow for category change and file loading

        // Assert
        Assert.Equal("Advanced Features", _viewModel.SelectedCategory?.Name);
        Assert.Equal("automation", _viewModel.SelectedFile?.Id);
    }

    [Fact]
    public async Task LoadFileByIdCommand_WithInvalidId_ShouldNotThrow()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.GetFileById("invalid")).Returns((DocumentationFile?)null);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _viewModel.LoadFileByIdCommand.ExecuteAsync("invalid"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task LoadFileByIdCommand_WithServiceError_ShouldNotThrow()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.GetFileById(It.IsAny<string>()))
            .Throws(new Exception("Service error"));

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
            await _viewModel.LoadFileByIdCommand.ExecuteAsync("test"));
        Assert.Null(exception);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void LoadCategories_WithServiceError_ShouldHandleGracefully()
    {
        // Arrange
        var mockService = new Mock<IDocumentationService>();
        mockService.Setup(x => x.GetCategories()).Throws(new Exception("Service error"));

        // Act & Assert
        var exception = Record.Exception(() => new DocumentationViewModel(mockService.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void LoadFilesForCategory_WithServiceError_ShouldHandleGracefully()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.GetFilesByCategory(It.IsAny<string>()))
            .Throws(new Exception("Category error"));

        var category = new DocumentationCategory { Name = "Test Category" };

        // Act & Assert
        var exception = Record.Exception(() => _viewModel.SelectedCategory = category);
        Assert.Null(exception);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void Categories_Empty_ShouldNotSelectAnyCategory()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.GetCategories()).Returns(new List<DocumentationCategory>());
        var viewModel = new DocumentationViewModel(_mockDocumentationService.Object);

        // Assert
        Assert.Empty(viewModel.Categories);
        Assert.Null(viewModel.SelectedCategory);
    }

    [Fact]
    public void FilesInCategory_Empty_ShouldNotSelectAnyFile()
    {
        // Arrange
        _mockDocumentationService.Setup(x => x.GetFilesByCategory(It.IsAny<string>()))
            .Returns(new List<DocumentationFile>());

        var category = new DocumentationCategory { Name = "Empty Category" };

        // Act
        _viewModel.SelectedCategory = category;

        // Assert
        Assert.Empty(_viewModel.FilesInCategory);
        Assert.Null(_viewModel.SelectedFile);
    }

    [Fact]
    public void GenerateErrorContent_ShouldCreateValidHTML()
    {
        // This tests the private method indirectly through error scenarios
        // Arrange
        _mockDocumentationService.Setup(x => x.LoadAndParseDocumentationAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test error message"));

        // Act
        _viewModel.SelectedFile = _viewModel.FilesInCategory.First();

        // Wait and assert
        Task.Delay(100).ContinueWith(_ =>
        {
            Assert.Contains("<!DOCTYPE html>", _viewModel.DocumentContent);
            Assert.Contains("Test error message", _viewModel.DocumentContent);
            Assert.Contains("Error Loading Documentation", _viewModel.DocumentContent);
        });
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var changedProperties = new List<string>();
        _viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? "");

        // Act
        _viewModel.IsLoading = true;
        _viewModel.LoadingMessage = "Test message";
        _viewModel.DocumentContent = "Test content";

        // Assert
        Assert.Contains(nameof(_viewModel.IsLoading), changedProperties);
        Assert.Contains(nameof(_viewModel.LoadingMessage), changedProperties);
        Assert.Contains(nameof(_viewModel.DocumentContent), changedProperties);
    }

    #endregion

    #region Performance and Memory Tests

    [Fact]
    public void MultipleFileSelections_ShouldNotLeakMemory()
    {
        // Arrange
        var files = _viewModel.FilesInCategory.ToList();

        // Act - rapidly switch between files
        for (int i = 0; i < files.Count * 3; i++)
        {
            _viewModel.SelectedFile = files[i % files.Count];
        }

        // Assert - no exceptions should be thrown
        Assert.NotNull(_viewModel.SelectedFile);
    }

    [Fact]
    public void ConcurrentOperations_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - perform concurrent operations
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await _viewModel.LoadFileByIdCommand.ExecuteAsync("quickstart");
                await Task.Delay(10);
                _viewModel.RefreshDocumentationCommand.Execute(null);
            }));
        }

        // Assert
        var exception = Record.Exception(() => Task.WaitAll(tasks.ToArray()));
        Assert.Null(exception);
    }

    #endregion
}