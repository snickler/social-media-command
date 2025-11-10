using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class PostEditorView : UserControl
{
    public PostEditorView()
    {
        InitializeComponent();

        // Subscribe to DataContext changes to wire up event handlers
        DataContextChanged += OnDataContextChanged;
    }

    public PostEditorView(PostEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old ViewModel events
        if (sender is Control control && control.DataContext is PostEditorViewModel oldViewModel)
        {
            oldViewModel.OnMediaUploadRequested -= HandleMediaUploadRequested;
        }

        // Subscribe to new ViewModel events
        if (DataContext is PostEditorViewModel viewModel)
        {
            viewModel.OnMediaUploadRequested += HandleMediaUploadRequested;
        }
    }

    private async void HandleMediaUploadRequested()
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Media upload requested event received");

        try
        {
            // Get the top-level window
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] Could not get top level window");
                return;
            }

            // Configure file picker options
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "Select Media Files",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp", "*.bmp" }
                    },
                    new FilePickerFileType("Videos")
                    {
                        Patterns = new[] { "*.mp4", "*.mov", "*.avi", "*.mkv", "*.webm" }
                    },
                    new FilePickerFileType("All Media")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp", "*.mp4", "*.mov", "*.avi" }
                    }
                }
            };

            System.Diagnostics.Debug.WriteLine("[PostEditorView] Opening file picker...");

            // Show file picker
            var selectedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

            if (selectedFiles != null && selectedFiles.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] User selected {selectedFiles.Count} file(s)");

                // Get file paths
                var filePaths = selectedFiles
                    .Where(f => f.TryGetLocalPath() != null)
                    .Select(f => f.TryGetLocalPath()!)
                    .ToArray();

                if (filePaths.Any() && DataContext is PostEditorViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Processing {filePaths.Length} file path(s)");
                    await viewModel.ProcessSelectedFilesAsync(filePaths);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PostEditorView] No valid file paths or ViewModel not found");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] User canceled file selection");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Error handling media upload: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Stack trace: {ex.StackTrace}");
        }
    }
}