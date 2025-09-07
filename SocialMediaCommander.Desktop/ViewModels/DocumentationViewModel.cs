using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for the documentation viewer
/// </summary>
public partial class DocumentationViewModel : ObservableObject
{
    private readonly IDocumentationService _documentationService;
    private readonly ILogger _logger;

    [ObservableProperty]
    private ObservableCollection<DocumentationCategory> _categories = new();

    [ObservableProperty]
    private DocumentationCategory? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<DocumentationFile> _filesInCategory = new();

    [ObservableProperty]
    private DocumentationFile? _selectedFile;

    [ObservableProperty]
    private string _documentContent = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _loadingMessage = string.Empty;

    public DocumentationViewModel(IDocumentationService documentationService)
    {
        _documentationService = documentationService;
        _logger = Log.ForContext<DocumentationViewModel>();

        LoadCategories();
    }

    private void LoadCategories()
    {
        try
        {
            var categories = _documentationService.GetCategories().ToList();
            Categories.Clear();

            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            // Select first category by default
            if (Categories.Count > 0)
            {
                SelectedCategory = Categories[0];
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load documentation categories");
        }
    }

    partial void OnSelectedCategoryChanged(DocumentationCategory? value)
    {
        if (value != null)
        {
            LoadFilesForCategory(value.Name);
        }
    }

    private void LoadFilesForCategory(string categoryName)
    {
        try
        {
            var files = _documentationService.GetFilesByCategory(categoryName).ToList();
            FilesInCategory.Clear();

            foreach (var file in files)
            {
                FilesInCategory.Add(file);
            }

            // Select first file by default
            if (FilesInCategory.Count > 0)
            {
                SelectedFile = FilesInCategory[0];
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load files for category: {Category}", categoryName);
        }
    }

    partial void OnSelectedFileChanged(DocumentationFile? value)
    {
        if (value != null)
        {
            _ = LoadDocumentContentAsync(value);
        }
    }

    private async Task LoadDocumentContentAsync(DocumentationFile file)
    {
        try
        {
            IsLoading = true;
            LoadingMessage = $"Loading {file.Title}...";

            var content = await _documentationService.LoadAndParseDocumentationAsync(file.FilePath);
            DocumentContent = content;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load document content for: {File}", file.Title);
            DocumentContent = GenerateErrorContent(ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void RefreshDocumentation()
    {
        LoadCategories();
    }

    [RelayCommand]
    private void SelectCategory(DocumentationCategory category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    private async Task LoadFileByIdAsync(string fileId)
    {
        try
        {
            var file = _documentationService.GetFileById(fileId);
            if (file != null)
            {
                // Find and select the appropriate category
                var category = Categories.FirstOrDefault(c => c.Name == file.Category);
                if (category != null)
                {
                    SelectedCategory = category;

                    // Wait for files to load, then select the file
                    await Task.Delay(100); // Small delay to ensure files are loaded
                    SelectedFile = FilesInCategory.FirstOrDefault(f => f.Id == fileId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load file by ID: {FileId}", fileId);
        }
    }

    private string GenerateErrorContent(string error)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Error</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 20px; }}
        .error {{ background-color: #fee; border: 1px solid #fcc; border-radius: 4px; padding: 16px; }}
    </style>
</head>
<body>
    <div class=""error"">
        <h2>❌ Error Loading Documentation</h2>
        <p>{error}</p>
    </div>
</body>
</html>";
    }
}