using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SkiaSharp;
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

        System.Diagnostics.Debug.WriteLine("[PostEditorView] ========== CONSTRUCTOR CALLED ==========");
        System.Diagnostics.Debug.WriteLine("[PostEditorView] Drag-drop handlers added");
    }

    private void OnTextBoxLoaded(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] ========== OnTextBoxLoaded FIRED ==========");
        if (sender is TextBox textBox)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] TextBox found: Name={textBox.Name}");

            // Wire up PreviewKeyDown handler with tunneling for Ctrl+V detection
            // This ensures we intercept before the TextBox processes the key
            textBox.AddHandler(InputElement.KeyDownEvent, OnTextBoxKeyDown, RoutingStrategies.Tunnel);
            System.Diagnostics.Debug.WriteLine("[PostEditorView] *** PreviewKeyDown handler ATTACHED with TUNNELING to TextBox ***");

            // Create custom context menu with image-aware paste
            textBox.ContextFlyout = CreateCustomContextMenu(textBox);
            System.Diagnostics.Debug.WriteLine("[PostEditorView] *** Custom context menu ATTACHED to TextBox ***");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] ERROR: Sender is not TextBox, it's {sender?.GetType().Name ?? "null"}");
        }
    }

    private MenuFlyout CreateCustomContextMenu(TextBox textBox)
    {
        var menu = new MenuFlyout();

        // Paste menu item
        var pasteItem = new MenuItem { Header = "Paste" };
        pasteItem.Click += async (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("[PostEditorView] ========== CONTEXT MENU PASTE CLICKED ==========");
            await HandleContextMenuPasteAsync(textBox);
        };
        menu.Items.Add(pasteItem);

        // Cut menu item
        var cutItem = new MenuItem { Header = "Cut" };
        cutItem.Click += (s, e) => textBox.Cut();
        menu.Items.Add(cutItem);

        // Copy menu item
        var copyItem = new MenuItem { Header = "Copy" };
        copyItem.Click += (s, e) => textBox.Copy();
        menu.Items.Add(copyItem);

        return menu;
    }

    private async Task HandleContextMenuPasteAsync(TextBox textBox)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            System.Diagnostics.Debug.WriteLine("[PostEditorView] Clipboard not available");
            return;
        }

        try
        {
#pragma warning disable CS0618
            var formats = await clipboard.GetFormatsAsync();
#pragma warning restore CS0618

            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Checking clipboard, {formats.Count()} formats available:");
            foreach (var fmt in formats)
            {
                System.Diagnostics.Debug.WriteLine($"  - {fmt}");
            }

            var hasImage = formats.Any(f =>
                f.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("Bitmap", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("PNG", StringComparison.OrdinalIgnoreCase) ||
                f.Contains("DIB", StringComparison.OrdinalIgnoreCase));

            if (hasImage)
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] IMAGE DETECTED - Handling as image paste");
                await HandleClipboardPasteAsync(textBox);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] No image detected, pasting as text");
                // Paste text normally
                textBox.Paste();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Error in context menu paste: {ex.Message}");
        }
    }

    private async void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[PostEditorView] KeyDown: Key={e.Key}, Modifiers={e.KeyModifiers}");

        if ((e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) && e.Key == Key.V)
        {
            System.Diagnostics.Debug.WriteLine("========== PASTE DETECTED ==========");
            System.Diagnostics.Debug.WriteLine("[PostEditorView] Ctrl+V or Cmd+V detected");

            // CRITICAL: Mark as handled immediately to prevent default paste behavior
            // We'll manually paste text later if no image is found
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("[PostEditorView] *** Event marked as HANDLED to intercept default paste ***");

            if (sender is not TextBox textBox)
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] ERROR: Sender is not TextBox");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] TopLevel: {topLevel?.GetType().Name ?? "null"}");

            var clipboard = topLevel?.Clipboard;
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Clipboard: {(clipboard != null ? "Available" : "NULL")}");

            if (clipboard != null)
            {
                try
                {
#pragma warning disable CS0618
                    var formats = await clipboard.GetFormatsAsync();
#pragma warning restore CS0618
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Clipboard has {formats.Count()} formats:");
                    foreach (var format in formats)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {format}");
                    }

                    var hasImage = formats.Any(f =>
                        f.Contains("image", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("Bitmap", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("PNG", StringComparison.OrdinalIgnoreCase) ||
                        f.Contains("DIB", StringComparison.OrdinalIgnoreCase));

                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Has image format: {hasImage}");

                    if (hasImage)
                    {
                        System.Diagnostics.Debug.WriteLine("[PostEditorView] *** ATTEMPTING TO HANDLE IMAGE PASTE ***");
                        await HandleClipboardPasteAsync(textBox);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[PostEditorView] No image format detected, pasting text manually");
                        // No image found, manually paste the text
                        textBox.Paste();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] ERROR in KeyDown handler: {ex.GetType().Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Stack trace: {ex.StackTrace}");
                    // On error, try to paste text as fallback
                    textBox.Paste();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] Clipboard is NULL, cannot handle paste");
            }
            System.Diagnostics.Debug.WriteLine("========== END PASTE DETECTION ==========");
        }
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

    private void Drop(object? sender, DragEventArgs e)
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



    private async Task HandleClipboardPasteAsync(object? sender)
    {
        System.Diagnostics.Debug.WriteLine("[PostEditorView] ===== HandleClipboardPasteAsync START =====");
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
            {
                System.Diagnostics.Debug.WriteLine("[PostEditorView] ERROR: Clipboard not available");
                return;
            }

#pragma warning disable CS0618
            var formats = await clipboard.GetFormatsAsync();
#pragma warning restore CS0618
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Processing {formats.Count()} clipboard formats");

            Bitmap? bitmap = null;
            string? successfulFormat = null;

            var imageFormats = new[] {
                "image/png", "image/bmp", "image/jpeg", "image/jpg",
                "Bitmap", "PNG", "BMP", "JPEG", "JPG",
                "DeviceIndependentBitmap", "CF_DIB", "CF_DIBV5"
            };

            foreach (var format in imageFormats)
            {
                if (formats.Contains(format))
                {
                    System.Diagnostics.Debug.WriteLine($"[PostEditorView] Attempting to read format: {format}");
                    try
                    {
#pragma warning disable CS0618
                        var imageData = await clipboard.GetDataAsync(format);
#pragma warning restore CS0618

                        System.Diagnostics.Debug.WriteLine($"[PostEditorView] GetDataAsync returned: {imageData?.GetType().Name ?? "null"}");

                        if (imageData is Bitmap bmp)
                        {
                            bitmap = bmp;
                            successfulFormat = format;
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] ✓ Successfully got Bitmap from format: {format}");
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView]   Bitmap size: {bmp.PixelSize.Width}x{bmp.PixelSize.Height}");
                            break;
                        }
                        else if (imageData is byte[] bytes)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Got byte array ({bytes.Length} bytes), trying SkiaSharp...");
                            bitmap = await TryCreateBitmapFromBytesAsync(bytes);
                            if (bitmap != null)
                            {
                                successfulFormat = format;
                                System.Diagnostics.Debug.WriteLine($"[PostEditorView] ✓ Successfully created Bitmap from bytes using SkiaSharp");
                                break;
                            }
                        }
                        else if (imageData is Stream stream)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Got stream, trying to read...");
                            var memStream = new MemoryStream();
                            await stream.CopyToAsync(memStream);
                            var streamBytes = memStream.ToArray();
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Stream contained {streamBytes.Length} bytes, trying SkiaSharp...");
                            bitmap = await TryCreateBitmapFromBytesAsync(streamBytes);
                            if (bitmap != null)
                            {
                                successfulFormat = format;
                                System.Diagnostics.Debug.WriteLine($"[PostEditorView] ✓ Successfully created Bitmap from stream using SkiaSharp");
                                break;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Unhandled data type from clipboard");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PostEditorView] Exception reading format {format}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            if (bitmap != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] ✓✓✓ SUCCESS: Got bitmap from clipboard (format: {successfulFormat})");
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] Bitmap details: {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}, DPI: {bitmap.Dpi}");

                // Save bitmap to temporary file
                var tempPath = await SaveBitmapToTempFileAsync(bitmap);

                if (!string.IsNullOrEmpty(tempPath))
                {
                    // Determine target ViewModel
                    var targetViewModel = FindTargetViewModelForDrop(sender);

                    if (targetViewModel is PostEditorViewModel mainViewModel)
                    {
                        mainViewModel.AddMediaFile(tempPath);
                        System.Diagnostics.Debug.WriteLine($"[PostEditorView] Added pasted image to main post: {tempPath}");
                    }
                    else if (targetViewModel is ThreadPostViewModel threadPostViewModel)
                    {
                        threadPostViewModel.AddMediaFile(tempPath);
                        System.Diagnostics.Debug.WriteLine($"[PostEditorView] Added pasted image to thread post: {tempPath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] ✗✗✗ EXCEPTION in HandleClipboardPasteAsync:");
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Stack: {ex.StackTrace}");
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("[PostEditorView] ===== HandleClipboardPasteAsync END =====");
        }
    }

    private async Task<Bitmap?> TryCreateBitmapFromBytesAsync(byte[] bytes)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] TryCreateBitmapFromBytesAsync: {bytes.Length} bytes");

            // Try using SkiaSharp to decode the image
            using var skImage = SkiaSharp.SKImage.FromEncodedData(bytes);
            if (skImage != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] SkiaSharp decoded image: {skImage.Width}x{skImage.Height}");

                // Convert to Avalonia Bitmap via memory stream
                using var encoded = skImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var stream = encoded.AsStream();
                var bitmap = await Task.Run(() => new Bitmap(stream));

                System.Diagnostics.Debug.WriteLine($"[PostEditorView] Successfully converted to Avalonia Bitmap");
                return bitmap;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PostEditorView] SkiaSharp could not decode image data");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] TryCreateBitmapFromBytesAsync failed: {ex.Message}");
        }

        return null;
    }

    private async Task<string?> SaveBitmapToTempFileAsync(Bitmap bitmap)
    {
        try
        {
            // Create temp directory if it doesn't exist
            var tempDir = Path.Combine(Path.GetTempPath(), "SocialMediaCommander", "ClipboardImages");
            Directory.CreateDirectory(tempDir);

            // Generate unique filename
            var fileName = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            var filePath = Path.Combine(tempDir, fileName);

            // Save bitmap as PNG
            await Task.Run(() =>
            {
                using var fileStream = File.Create(filePath);
                bitmap.Save(fileStream);
            });

            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Saved clipboard image to: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostEditorView] Error saving clipboard image: {ex.Message}");
            return null;
        }
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