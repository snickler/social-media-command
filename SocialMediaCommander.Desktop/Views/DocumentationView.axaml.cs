using System;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using SocialMediaCommander.Desktop.ViewModels;

namespace SocialMediaCommander.Desktop.Views;

public partial class DocumentationView : UserControl
{
    private TextBlock? _documentContentBlock;

    public DocumentationView()
    {
        InitializeComponent();

        // Get the TextBlock reference
        _documentContentBlock = this.FindControl<TextBlock>("DocumentContentBlock");

        // Subscribe to DataContext changes
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DocumentationViewModel viewModel)
        {
            // Subscribe to content changes
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentationViewModel.DocumentContent) &&
            DataContext is DocumentationViewModel viewModel)
        {
            UpdateDocumentContent(viewModel.DocumentContent);
        }
    }

    private void UpdateDocumentContent(string htmlContent)
    {
        if (_documentContentBlock != null && !string.IsNullOrEmpty(htmlContent))
        {
            try
            {
                // Convert HTML to plain text for now (simple approach)
                var plainText = ConvertHtmlToPlainText(htmlContent);
                _documentContentBlock.Text = plainText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating document content: {ex.Message}");
                _documentContentBlock.Text = $"Error loading documentation: {ex.Message}";
            }
        }
    }

    private string ConvertHtmlToPlainText(string htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return string.Empty;

        // Simple HTML to text conversion - remove HTML tags and decode entities
        var text = Regex.Replace(htmlContent, @"<[^>]*>", "");
        text = text.Replace("&lt;", "<")
                  .Replace("&gt;", ">")
                  .Replace("&amp;", "&")
                  .Replace("&quot;", "\"")
                  .Replace("&nbsp;", " ");

        // Clean up extra whitespace
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        return text;
    }
}