using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class PostEditorView : UserControl
{
    public PostEditorView()
    {
        InitializeComponent();

        // Subscribe to DataContext changes to wire up event handlers
        DataContextChanged += OnDataContextChanged;

        // Set up drag-and-drop event handlers
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DragLeaveEvent, DragLeave);
    }

    public PostEditorView(PostEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Subscribe to new ViewModel events
        if (DataContext is PostEditorViewModel viewModel)
        {
            viewModel.OnMediaUploadRequested += () => HandleMediaUploadRequested(viewModel);

            // Subscribe to thread posts collection changes
            viewModel.ThreadPosts.CollectionChanged += ThreadPosts_CollectionChanged;

            // Wire up existing thread posts
            foreach (var threadPost in viewModel.ThreadPosts)
            {
                threadPost.OnMediaUploadRequested += () => HandleMediaUploadRequested(threadPost);
            }
        }
    }

    private void ThreadPosts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Subscribe to new thread posts
        if (e.NewItems != null)
        {
            foreach (ThreadPostViewModel threadPost in e.NewItems)
            {
                threadPost.OnMediaUploadRequested += () => HandleMediaUploadRequested(threadPost);
            }
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        // Only allow file drops
#pragma warning disable CS0618 // Type or member is obsolete
        var dataTransfer = e.Data;
#pragma warning restore CS0618
        e.DragEffects = (dataTransfer.GetFiles() != null && dataTransfer.GetFiles()?.Any() == true)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void DragEnter(object? sender, DragEventArgs e)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var dataTransfer = e.Data;
#pragma warning restore CS0618
        if (dataTransfer.GetFiles() != null && dataTransfer.GetFiles()?.Any() == true)
        {
            // Show visual feedback (handled in XAML via triggers)
            System.Diagnostics.Debug.WriteLine("[PostEditorView] Drag enter with files");
        }
    }

    private void DragLeave(object? sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Drag leave");
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Drop event triggered");

        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var dataTransfer = e.Data;
#pragma warning restore CS0618
            var files = dataTransfer.GetFiles();

            if (files != null && files.Any())
            {
                var filePaths = new List<string>();

                foreach (var file in files)
                {
                    var path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();

                        // Only accept image and video files
                        if (extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or
                            ".mp4" or ".mov" or ".avi" or ".webm" or ".mkv")
                        {
                            filePaths.Add(path);
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Accepted file: {path}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Rejected file (unsupported): {path}");
                        }
                    }
                }

                if (filePaths.Any())
                {
                    // Determine which ViewModel to target based on the drop source
                    var targetViewModel = FindTargetViewModelForDrop(e.Source);

                    if (targetViewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PostEditorView] Adding {filePaths.Count} media files to target ViewModel");

                        if (targetViewModel is PostEditorViewModel mainViewModel)
                        {
                            foreach (var filePath in filePaths)
                            {
                                mainViewModel.AddMediaFile(filePath);
                            }
                        }
                        else if (targetViewModel is ThreadPostViewModel threadPostViewModel)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Targeting thread post {threadPostViewModel.OrderIndex}");
                            foreach (var filePath in filePaths)
                            {
                                threadPostViewModel.AddMediaFile(filePath);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Error handling drop: {ex.Message}");
        }

        e.Handled = true;
    }

    private object? FindTargetViewModelForDrop(object? source)
    {
        // Walk up the visual tree from the drop source to find the Border with DataContext
        if (source is not Visual visual)
            return DataContext;

        var current = visual;
        while (current != null)
        {
            // Check if this is a Border that's part of a thread post (has ThreadPostViewModel as DataContext)
            if (current is Border border && border.DataContext is ThreadPostViewModel threadPost)
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] Found thread post DataContext: {threadPost.DisplayIndex}");
                return threadPost;
            }

            // Move up the visual tree
            current = current.GetVisualParent();

            // Stop if we've reached the root UserControl
            if (current == this)
                break;
        }

        // Default to main post ViewModel
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Defaulting to main post");
        return DataContext;
    }

    private async void HandleMediaUploadRequested(PostEditorViewModel viewModel)
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Media upload requested for main post");
        await HandleMediaUploadAsync(viewModel);
    }

    private async void HandleMediaUploadRequested(ThreadPostViewModel threadPost)
    {
        System.Diagnostics.Debug.WriteLine($"[PostEditorView] Media upload requested for thread post {threadPost.OrderIndex}");
        await HandleMediaUploadAsync(threadPost);
    }

    private async Task HandleMediaUploadAsync(object targetViewModel)
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

                if (filePaths.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Processing {filePaths.Length} file path(s)");

                    // Route to correct ViewModel
                    if (targetViewModel is PostEditorViewModel mainViewModel)
                    {
                        foreach (var filePath in filePaths)
                        {
                            mainViewModel.AddMediaFile(filePath);
                        }
                    }
                    else if (targetViewModel is ThreadPostViewModel threadPostViewModel)
                    {
                        foreach (var filePath in filePaths)
                        {
                            threadPostViewModel.AddMediaFile(filePath);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PostEditorView] No valid file paths");
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