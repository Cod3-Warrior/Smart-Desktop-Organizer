using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Models;

namespace SmartDesktop.Core.Services;

/// <summary>
/// Implements intelligent folder naming based on content analysis.
/// Uses on-device inference for privacy-first category detection.
/// </summary>
public class FolderNamingService : IFolderNamingService
{
    private const string DefaultFolderName = "Folder";
    private const string DefaultAppsName = "Apps";
    private const double MajorityThreshold = 0.5; // 50% threshold for dominant category

    /// <inheritdoc/>
    public string SuggestFolderName(IEnumerable<DesktopItem> items)
    {
        if (items == null)
            return DefaultFolderName;

        var itemList = items.ToList();
        
        // Edge case: No items
        if (itemList.Count == 0)
            return DefaultFolderName;

        // Analyze categories
        var categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalItems = 0;

        foreach (var item in itemList)
        {
            string category = GetItemCategory(item);
            if (!string.IsNullOrEmpty(category))
            {
                if (!categoryCounts.ContainsKey(category))
                    categoryCounts[category] = 0;
                categoryCounts[category]++;
                totalItems++;
            }
        }

        // Edge case: No recognizable items
        if (totalItems == 0 || categoryCounts.Count == 0)
            return DefaultAppsName;

        // Single item: Use its category directly
        if (totalItems == 1)
            return categoryCounts.Keys.First();

        // Find dominant category
        var sortedCategories = categoryCounts
            .OrderByDescending(kv => kv.Value)
            .ToList();

        var topCategory = sortedCategories[0];
        double topRatio = (double)topCategory.Value / totalItems;

        // Clear majority (>50%): Use that category
        if (topRatio >= MajorityThreshold)
            return topCategory.Key;

        // Check if top 2 categories are related
        if (sortedCategories.Count >= 2)
        {
            var secondCategory = sortedCategories[1];
            string? broaderCategory = FindBroaderCategory(topCategory.Key, secondCategory.Key);
            if (broaderCategory != null)
                return broaderCategory;
        }

        // Mixed bag: Use the most common category or a generic name
        if (IsAppsCategory(topCategory.Key))
            return DefaultAppsName;

        return topCategory.Key;
    }

    /// <inheritdoc/>
    public string GetAppCategory(string appName, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return string.Empty;

        string lowerName = appName.ToLowerInvariant();

        // Check exact and partial matches against known apps
        foreach (var mapping in AppCategoryMappings.AppToCategory)
        {
            if (lowerName.Contains(mapping.Key.ToLowerInvariant()))
                return mapping.Value;
        }

        // Check if it's an executable shortcut and try path-based detection
        if (!string.IsNullOrEmpty(fullPath))
        {
            string lowerPath = fullPath.ToLowerInvariant();
            
            // Check for common install locations
            if (lowerPath.Contains("\\games\\") || lowerPath.Contains("\\steamapps\\"))
                return "Games";
            if (lowerPath.Contains("\\program files\\microsoft office\\"))
                return "Productivity";
            if (lowerPath.Contains("\\jetbrains\\") || lowerPath.Contains("\\visual studio\\"))
                return "Development";
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public string GetFileCategory(string fileName, string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            // Try to extract extension from filename
            if (!string.IsNullOrEmpty(fileName))
                extension = Path.GetExtension(fileName);
        }

        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        // Ensure extension has dot prefix
        if (!extension.StartsWith("."))
            extension = "." + extension;

        if (AppCategoryMappings.ExtensionToCategory.TryGetValue(extension, out var category))
            return category;

        return string.Empty;
    }

    /// <summary>
    /// Determines the category for a single item.
    /// </summary>
    private string GetItemCategory(DesktopItem item)
    {
        if (item == null)
            return string.Empty;

        // For folders, we don't categorize them
        if (item.IsFolder)
            return string.Empty;

        // Try app-based categorization first (for shortcuts)
        string appCategory = GetAppCategory(item.Name, item.FullPath);
        if (!string.IsNullOrEmpty(appCategory))
            return appCategory;

        // Fall back to file extension-based categorization
        if (!string.IsNullOrEmpty(item.FullPath))
        {
            string extension = Path.GetExtension(item.FullPath);
            string fileCategory = GetFileCategory(item.Name, extension);
            if (!string.IsNullOrEmpty(fileCategory))
                return fileCategory;
        }

        // Check if it's a shortcut to an app
        if (item.FullPath?.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) == true ||
            item.FullPath?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DefaultAppsName;
        }

        return string.Empty;
    }

    /// <summary>
    /// Finds a broader category if two categories are related.
    /// </summary>
    private string? FindBroaderCategory(string category1, string category2)
    {
        foreach (var group in AppCategoryMappings.RelatedCategories)
        {
            var relatedSet = new HashSet<string>(group.Value, StringComparer.OrdinalIgnoreCase);
            if (relatedSet.Contains(category1) && relatedSet.Contains(category2))
            {
                return group.Key;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a category represents general apps.
    /// </summary>
    private bool IsAppsCategory(string category)
    {
        return string.Equals(category, "Apps", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(category, DefaultAppsName, StringComparison.OrdinalIgnoreCase);
    }
}
