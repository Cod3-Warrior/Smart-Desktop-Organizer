using System;
using System.Threading.Tasks;
using SmartDesktop.Core.Models;

namespace SmartDesktop.Core.Interfaces;

/// <summary>
/// Service for loading, saving, and managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings (in-memory cached copy).
    /// </summary>
    AppSettings Current { get; }
    
    /// <summary>
    /// Raised when any setting changes. Provides the property path that changed.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    
    /// <summary>
    /// Loads settings from disk. If no settings file exists, returns defaults.
    /// </summary>
    Task<AppSettings> LoadAsync();
    
    /// <summary>
    /// Saves the current settings to disk.
    /// </summary>
    Task SaveAsync();
    
    /// <summary>
    /// Saves specific settings to disk (for partial updates/live preview).
    /// </summary>
    Task SaveAsync(AppSettings settings);
    
    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    Task ResetToDefaultsAsync();
    
    /// <summary>
    /// Applies settings changes and raises SettingsChanged event.
    /// </summary>
    void NotifySettingChanged(string propertyPath);
    
    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    string SettingsFilePath { get; }
}

/// <summary>
/// Event args for settings change notifications.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public string PropertyPath { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    
    public SettingsChangedEventArgs(string propertyPath, object? oldValue = null, object? newValue = null)
    {
        PropertyPath = propertyPath;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
