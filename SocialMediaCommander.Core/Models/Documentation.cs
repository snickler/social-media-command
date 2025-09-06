namespace SocialMediaCommander.Core.Models;

/// <summary>
/// Represents a documentation category
/// </summary>
public class DocumentationCategory
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>
/// Represents a documentation file
/// </summary>
public class DocumentationFile
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int SortOrder { get; set; }
}