using System.Collections.Generic;
using System.Threading.Tasks;
using SocialMediaCommander.Core.Models;

namespace SocialMediaCommander.Services.Interfaces;

/// <summary>
/// Service for loading and managing documentation content
/// </summary>
public interface IDocumentationService
{
    /// <summary>
    /// Gets all available documentation categories
    /// </summary>
    /// <returns>List of documentation categories</returns>
    IEnumerable<DocumentationCategory> GetCategories();

    /// <summary>
    /// Gets documentation files for a specific category
    /// </summary>
    /// <param name="categoryName">Name of the category</param>
    /// <returns>List of documentation files in the category</returns>
    IEnumerable<DocumentationFile> GetFilesByCategory(string categoryName);

    /// <summary>
    /// Loads and parses markdown content for a documentation file
    /// </summary>
    /// <param name="filePath">Path to the documentation file</param>
    /// <returns>Parsed HTML content</returns>
    Task<string> LoadAndParseDocumentationAsync(string filePath);

    /// <summary>
    /// Gets a specific documentation file by ID
    /// </summary>
    /// <param name="fileId">ID of the documentation file</param>
    /// <returns>Documentation file information</returns>
    DocumentationFile? GetFileById(string fileId);
}