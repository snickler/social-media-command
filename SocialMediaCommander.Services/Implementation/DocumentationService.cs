using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;
using Serilog;

namespace SocialMediaCommander.Services.Implementation;

/// <summary>
/// Service for loading and managing documentation content
/// </summary>
public class DocumentationService : IDocumentationService
{
    private readonly ILogger _logger;
    private readonly MarkdownPipeline _markdownPipeline;
    private readonly string _documentationBasePath;

    // Documentation structure definition
    private readonly List<DocumentationCategory> _categories = new()
    {
        new() { Name = "user-guides", DisplayName = "User Guides", Description = "Getting started and user documentation", SortOrder = 1 },
        new() { Name = "technical", DisplayName = "Technical", Description = "Technical documentation and implementation details", SortOrder = 2 },
        new() { Name = "security", DisplayName = "Security", Description = "Security policies and implementation", SortOrder = 3 },
        new() { Name = "development", DisplayName = "Development", Description = "Development guides and enhanced features", SortOrder = 4 },
        new() { Name = "operations", DisplayName = "Operations", Description = "Logging, monitoring, and operations", SortOrder = 5 }
    };

    private readonly List<DocumentationFile> _files = new()
    {
        // User Guides
        new() { Id = "user-guide", Title = "User Guide", Category = "user-guides", FilePath = "user-guides/user-guide.md", SortOrder = 1 },
        new() { Id = "getting-started", Title = "Getting Started", Category = "user-guides", FilePath = "user-guides/getting-started.md", SortOrder = 2 },
        
        // Technical Documentation
        new() { Id = "technical-documentation", Title = "Technical Documentation", Category = "technical", FilePath = "technical/technical-documentation.md", SortOrder = 1 },
        new() { Id = "implementation-summary", Title = "Implementation Summary", Category = "technical", FilePath = "technical/implementation-summary.md", SortOrder = 2 },
        new() { Id = "performance-optimizations", Title = "Performance Optimizations", Category = "technical", FilePath = "technical/performance-optimizations.md", SortOrder = 3 },
        
        // Security Documentation
        new() { Id = "security-overview", Title = "Security Overview", Category = "security", FilePath = "security/security-overview.md", SortOrder = 1 },
        new() { Id = "secure-storage", Title = "Secure Storage Implementation", Category = "security", FilePath = "security/secure-storage-implementation.md", SortOrder = 2 },
        
        // Development Documentation
        new() { Id = "enhanced-features", Title = "Enhanced Features", Category = "development", FilePath = "development/enhanced-features.md", SortOrder = 1 },
        new() { Id = "enhanced-features-final", Title = "Enhanced Features Final", Category = "development", FilePath = "development/enhanced-features-final.md", SortOrder = 2 },
        new() { Id = "oauth-config-demo", Title = "OAuth Configuration Demo", Category = "development", FilePath = "development/oauth-config-demo.md", SortOrder = 3 },
        new() { Id = "prd", Title = "Product Requirements Document", Category = "development", FilePath = "development/prd.md", SortOrder = 4 },
        
        // Operations Documentation
        new() { Id = "logging-implementation", Title = "Logging Implementation", Category = "operations", FilePath = "operations/logging-implementation.md", SortOrder = 1 },
        new() { Id = "log-analysis-fixes", Title = "Log Analysis Fixes", Category = "operations", FilePath = "operations/log-analysis-fixes.md", SortOrder = 2 }
    };

    public DocumentationService()
    {
        _logger = Log.ForContext<DocumentationService>();
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // Find the documentation base path relative to the application
        _documentationBasePath = FindDocumentationPath();
    }

    private string FindDocumentationPath()
    {
        // Try different possible locations for the docs folder
        var possiblePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "docs"),
            Path.Combine(Environment.CurrentDirectory, "docs"),
            Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "docs")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                _logger.Information("Found documentation path at: {Path}", fullPath);
                return fullPath;
            }
        }

        _logger.Warning("Documentation path not found. Checked paths: {Paths}", string.Join(", ", possiblePaths));
        return possiblePaths[0]; // Return first path as fallback
    }

    public IEnumerable<DocumentationCategory> GetCategories()
    {
        return _categories.OrderBy(c => c.SortOrder);
    }

    public IEnumerable<DocumentationFile> GetFilesByCategory(string categoryName)
    {
        return _files.Where(f => f.Category == categoryName).OrderBy(f => f.SortOrder);
    }

    public DocumentationFile? GetFileById(string fileId)
    {
        return _files.FirstOrDefault(f => f.Id == fileId);
    }

    public async Task<string> LoadAndParseDocumentationAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_documentationBasePath, filePath);

            if (!File.Exists(fullPath))
            {
                _logger.Warning("Documentation file not found: {Path}", fullPath);
                return GenerateNotFoundHtml(filePath);
            }

            var markdownContent = await File.ReadAllTextAsync(fullPath);
            var htmlContent = Markdown.ToHtml(markdownContent, _markdownPipeline);

            // Wrap in a styled HTML document
            return GenerateStyledHtml(htmlContent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading documentation file: {FilePath}", filePath);
            return GenerateErrorHtml(ex.Message);
        }
    }

    private string GenerateStyledHtml(string content)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Documentation</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #ffffff;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #2c3e50;
            margin-top: 24px;
            margin-bottom: 16px;
        }}
        h1 {{
            border-bottom: 1px solid #eaecef;
            padding-bottom: 10px;
        }}
        code {{
            background-color: #f6f8fa;
            border-radius: 3px;
            padding: 2px 4px;
            font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
        }}
        pre {{
            background-color: #f6f8fa;
            border-radius: 6px;
            padding: 16px;
            overflow: auto;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{
            border-left: 4px solid #dfe2e5;
            padding: 0 16px;
            color: #6a737d;
            margin: 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 16px 0;
        }}
        th, td {{
            border: 1px solid #dfe2e5;
            padding: 8px 12px;
            text-align: left;
        }}
        th {{
            background-color: #f6f8fa;
            font-weight: 600;
        }}
        a {{
            color: #0366d6;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        .highlight {{
            background-color: #fff3cd;
            border: 1px solid #ffeaa7;
            border-radius: 4px;
            padding: 12px;
            margin: 16px 0;
        }}
    </style>
</head>
<body>
{content}
</body>
</html>";
    }

    private string GenerateNotFoundHtml(string filePath)
    {
        return GenerateStyledHtml($@"
<div class=""highlight"">
    <h2>📄 Documentation Not Found</h2>
    <p>The requested documentation file <code>{filePath}</code> could not be found.</p>
    <p>Please ensure the documentation files are properly installed in the application directory.</p>
</div>");
    }

    private string GenerateErrorHtml(string error)
    {
        return GenerateStyledHtml($@"
<div class=""highlight"">
    <h2>❌ Error Loading Documentation</h2>
    <p>An error occurred while loading the documentation:</p>
    <code>{error}</code>
</div>");
    }
}