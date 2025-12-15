using System.Collections.Generic;
using SmartDesktop.Core.Models;

namespace SmartDesktop.Core.Interfaces;

/// <summary>
/// Service for generating intelligent folder names based on content analysis.
/// Provides on-device inference for privacy-first naming suggestions.
/// </summary>
public interface IFolderNamingService
{
    /// <summary>
    /// Suggests a folder name based on analyzing the provided items.
    /// Returns the dominant category name or a fallback if unknown.
    /// </summary>
    /// <param name="items">The items to be placed in the folder</param>
    /// <returns>A suggested folder name based on content analysis</returns>
    string SuggestFolderName(IEnumerable<DesktopItem> items);

    /// <summary>
    /// Gets the category for an application based on its name and path.
    /// </summary>
    /// <param name="appName">The display name of the application</param>
    /// <param name="fullPath">The full path to the application or shortcut</param>
    /// <returns>Category name (e.g., "Productivity", "Games")</returns>
    string GetAppCategory(string appName, string fullPath);

    /// <summary>
    /// Gets the category for a file based on its name and extension.
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <param name="extension">The file extension (with or without dot)</param>
    /// <returns>Category name (e.g., "Documents", "Images")</returns>
    string GetFileCategory(string fileName, string extension);
}
