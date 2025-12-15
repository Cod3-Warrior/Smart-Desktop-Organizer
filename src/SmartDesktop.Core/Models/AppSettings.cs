using System.Text.Json.Serialization;

namespace SmartDesktop.Core.Models;

/// <summary>
/// Root settings model for SmartDesktop Organizer.
/// Persisted as JSON to %APPDATA%\SmartDesktopOrganizer\settings.json
/// </summary>
public class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public FolderSettings Folders { get; set; } = new();
    public OrganizationSettings Organization { get; set; } = new();
    public IconSettings Icons { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public BackupSettings Backup { get; set; } = new();
    public AdvancedSettings Advanced { get; set; } = new();
}

public class GeneralSettings
{
    /// <summary>Launch SmartDesktop on Windows login</summary>
    public bool LaunchOnStartup { get; set; } = false;
    
    /// <summary>Full overlay (launcher) vs Transparent organizer (Fences-style)</summary>
    public OverlayMode OverlayMode { get; set; } = OverlayMode.FullOverlay;
    
    /// <summary>Hide real desktop icons when overlay is active</summary>
    public bool HideDesktopIcons { get; set; } = true;
    
    /// <summary>Number of undo actions to keep (5-50)</summary>
    public int UndoStackDepth { get; set; } = 20;
    
    /// <summary>Auto-save layout interval in minutes (0 = disabled)</summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;
}

public enum OverlayMode
{
    FullOverlay,
    TransparentOrganizer
}

public class AppearanceSettings
{
    /// <summary>UI theme: Light, Dark, System, or Custom</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    
    /// <summary>Icon size in pixels (48 = Small, 64 = Medium, 96 = Large, 128 = Extra Large)</summary>
    public int IconSize { get; set; } = 64;
    
    /// <summary>Grid spacing between icons in pixels (10-40)</summary>
    public int GridSpacing { get; set; } = 20;
    
    /// <summary>Grid padding from edges in pixels (10-50)</summary>
    public int GridPadding { get; set; } = 20;
    
    /// <summary>Folder thumbnail preview style</summary>
    public FolderPreviewStyle FolderPreviewStyle { get; set; } = FolderPreviewStyle.Grid2x2;
    
    /// <summary>Folder background blur/transparency (0-100)</summary>
    public int FolderBackgroundOpacity { get; set; } = 80;
    
    /// <summary>Animation speed multiplier (0.5 = Fast, 1.0 = Normal, 2.0 = Slow)</summary>
    public double AnimationSpeed { get; set; } = 1.0;
    
    /// <summary>Sync overlay background with Windows wallpaper</summary>
    public bool WallpaperSync { get; set; } = true;
    
    /// <summary>Overlay background opacity (0-100)</summary>
    public int OverlayOpacity { get; set; } = 90;
    
    /// <summary>Icon label visibility</summary>
    public LabelVisibility IconLabelVisibility { get; set; } = LabelVisibility.AlwaysShow;
}

public enum ThemeMode
{
    Light,
    Dark,
    System,
    Custom
}

public enum FolderPreviewStyle
{
    Grid2x2,
    Grid3x3,
    StackedIOS,
    SingleLargeIcon
}

public enum LabelVisibility
{
    AlwaysShow,
    HoverOnly,
    Hidden
}

public class FolderSettings
{
    /// <summary>Enable smart category-based naming for new folders</summary>
    public bool SmartNamingEnabled { get; set; } = true;
    
    /// <summary>Auto-open preview hover duration in milliseconds (500-2000)</summary>
    public int AutoOpenPreviewDuration { get; set; } = 800;
    
    /// <summary>Maximum items shown in folder thumbnail preview (4-9)</summary>
    public int MaxPreviewItems { get; set; } = 4;
    
    /// <summary>Allow nested folders (folders inside folders)</summary>
    public bool AllowNestedFolders { get; set; } = false;
    
    /// <summary>Folder rename behavior on creation</summary>
    public FolderRenameBehavior RenameBehavior { get; set; } = FolderRenameBehavior.SmartSuggestion;
}

public enum FolderRenameBehavior
{
    InlineEdit,
    PromptDialog,
    SmartSuggestion
}

public class OrganizationSettings
{
    /// <summary>Default sort mode for items</summary>
    public SortMode DefaultSortMode { get; set; } = SortMode.Manual;
    
    /// <summary>Sort direction (true = A-Z/oldest first, false = Z-A/newest first)</summary>
    public bool SortAscending { get; set; } = true;
    
    /// <summary>Automatic sort trigger</summary>
    public AutoSortTrigger AutoSortTrigger { get; set; } = AutoSortTrigger.None;
    
    /// <summary>Enable rule-based auto-organization (advanced)</summary>
    public bool RuleBasedOrganization { get; set; } = false;
}

public enum SortMode
{
    Manual,
    Name,
    DateModified,
    Type,
    Size,
    CustomCategory
}

public enum AutoSortTrigger
{
    None,
    Daily,
    OnNewItemAdded
}

public class IconSettings
{
    /// <summary>Remove shortcut arrows system-wide (requires UAC + Explorer restart)</summary>
    public bool HideShortcutArrows { get; set; } = false;
    
    /// <summary>High-DPI icon scaling mode</summary>
    public DpiMode HighDpiMode { get; set; } = DpiMode.BestQuality;
    
    /// <summary>Path to custom icon pack folder (future feature)</summary>
    public string? CustomIconPackPath { get; set; }
}

public enum DpiMode
{
    BestQuality,
    Performance
}

public class PerformanceSettings
{
    /// <summary>Number of items before virtualizing the grid (200-2000)</summary>
    public int VirtualizationThreshold { get; set; } = 500;
    
    /// <summary>Background refresh interval for new desktop items in seconds</summary>
    public int BackgroundRefreshInterval { get; set; } = 30;
    
    /// <summary>Disable animations on low-performance systems</summary>
    public bool DisableAnimationsLowPerf { get; set; } = false;
    
    /// <summary>Cache icon thumbnails to disk</summary>
    public bool CacheIcons { get; set; } = true;
}

public class BackupSettings
{
    /// <summary>Auto-backup frequency</summary>
    public BackupFrequency AutoBackupFrequency { get; set; } = BackupFrequency.Weekly;
    
    /// <summary>Custom backup location (null = default %APPDATA% location)</summary>
    public string? BackupLocation { get; set; }
    
    /// <summary>Backup original file locations when moving items</summary>
    public bool BackupOnMove { get; set; } = true;
    
    /// <summary>Maximum number of backup snapshots to keep</summary>
    public int MaxSnapshots { get; set; } = 10;
}

public enum BackupFrequency
{
    None,
    Daily,
    Weekly
}

public class AdvancedSettings
{
    /// <summary>Enable debug logging to file</summary>
    public bool DebugLogging { get; set; } = false;
    
    /// <summary>P/Invoke refresh intensity (1-3)</summary>
    public int RefreshIntensity { get; set; } = 2;
    
    /// <summary>Custom hotkey for jiggle mode (default: Ctrl+Shift+J)</summary>
    public string JiggleModeHotkey { get; set; } = "Ctrl+Shift+J";
    
    /// <summary>Custom hotkey for peek mode</summary>
    public string PeekModeHotkey { get; set; } = "Ctrl+Shift+P";
}
