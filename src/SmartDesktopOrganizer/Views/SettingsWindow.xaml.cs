using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Models;
using SmartDesktopOrganizer.Native;

namespace SmartDesktopOrganizer.Views;

/// <summary>
/// Settings dialog window with tabbed navigation.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private AppSettings _workingCopy;
    private bool _isDirty;
    
    public SettingsWindow()
    {
        InitializeComponent();
        
        _settingsService = App.Current.Services.GetRequiredService<ISettingsService>();
        _workingCopy = CloneSettings(_settingsService.Current);
        
        // Wire up tab switching
        TabGeneral.Checked += (s, e) => SwitchTab("General");
        TabAppearance.Checked += (s, e) => SwitchTab("Appearance");
        TabFolders.Checked += (s, e) => SwitchTab("Folders");
        TabOrganization.Checked += (s, e) => SwitchTab("Organization");
        TabIcons.Checked += (s, e) => SwitchTab("Icons");
        TabPerformance.Checked += (s, e) => SwitchTab("Performance");
        TabBackup.Checked += (s, e) => SwitchTab("Backup");
        TabAdvanced.Checked += (s, e) => SwitchTab("Advanced");
        
        LoadSettingsToUI();
        
        // Keyboard navigation
        this.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                CancelBtn_Click(s, e);
        };
    }
    
    private void SwitchTab(string tabName)
    {
        // Hide all content panels
        AppearanceContent.Visibility = Visibility.Collapsed;
        IconsContent.Visibility = Visibility.Collapsed;
        OrganizationContent.Visibility = Visibility.Collapsed;
        ComingSoonContent.Visibility = Visibility.Collapsed;
        
        // Show selected panel
        switch (tabName)
        {
            case "Appearance":
                AppearanceContent.Visibility = Visibility.Visible;
                break;
            case "Icons":
                IconsContent.Visibility = Visibility.Visible;
                break;
            case "Organization":
                OrganizationContent.Visibility = Visibility.Visible;
                break;
            default:
                ComingSoonContent.Visibility = Visibility.Visible;
                ComingSoonTitle.Text = tabName;
                break;
        }
    }
    
    private void LoadSettingsToUI()
    {
        var settings = _workingCopy;
        
        // Appearance
        ThemeCombo.SelectedIndex = (int)settings.Appearance.Theme;
        IconSizeSlider.Value = settings.Appearance.IconSize;
        IconSizeValue.Text = $"{settings.Appearance.IconSize}px";
        GridSpacingSlider.Value = settings.Appearance.GridSpacing;
        GridSpacingValue.Text = $"{settings.Appearance.GridSpacing}px";
        FolderPreviewCombo.SelectedIndex = (int)settings.Appearance.FolderPreviewStyle;
        AnimationSpeedSlider.Value = settings.Appearance.AnimationSpeed;
        WallpaperSyncToggle.IsChecked = settings.Appearance.WallpaperSync;
        LabelVisibilityCombo.SelectedIndex = (int)settings.Appearance.IconLabelVisibility;
        
        // Icons
        RemoveArrowsToggle.IsChecked = settings.Icons.HideShortcutArrows;
        DpiModeCombo.SelectedIndex = (int)settings.Icons.HighDpiMode;
        
        // Organization
        SortModeCombo.SelectedIndex = (int)settings.Organization.DefaultSortMode;
        SortAscending.IsChecked = settings.Organization.SortAscending;
        SortDescending.IsChecked = !settings.Organization.SortAscending;
        AutoSortCombo.SelectedIndex = (int)settings.Organization.AutoSortTrigger;
        
        _isDirty = false;
    }
    
    private void SaveUIToSettings()
    {
        // Appearance
        _workingCopy.Appearance.Theme = (ThemeMode)ThemeCombo.SelectedIndex;
        _workingCopy.Appearance.IconSize = (int)IconSizeSlider.Value;
        _workingCopy.Appearance.GridSpacing = (int)GridSpacingSlider.Value;
        _workingCopy.Appearance.FolderPreviewStyle = (FolderPreviewStyle)FolderPreviewCombo.SelectedIndex;
        _workingCopy.Appearance.AnimationSpeed = AnimationSpeedSlider.Value;
        _workingCopy.Appearance.WallpaperSync = WallpaperSyncToggle.IsChecked ?? true;
        _workingCopy.Appearance.IconLabelVisibility = (LabelVisibility)LabelVisibilityCombo.SelectedIndex;
        
        // Icons
        _workingCopy.Icons.HideShortcutArrows = RemoveArrowsToggle.IsChecked ?? false;
        _workingCopy.Icons.HighDpiMode = (DpiMode)DpiModeCombo.SelectedIndex;
        
        // Organization
        _workingCopy.Organization.DefaultSortMode = (SortMode)SortModeCombo.SelectedIndex;
        _workingCopy.Organization.SortAscending = SortAscending.IsChecked ?? true;
        _workingCopy.Organization.AutoSortTrigger = (AutoSortTrigger)AutoSortCombo.SelectedIndex;
    }
    
    private void IconSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IconSizeValue != null)
        {
            IconSizeValue.Text = $"{(int)e.NewValue}px";
            _isDirty = true;
            
            // Live preview: notify main window
            _settingsService.NotifySettingChanged("Appearance.IconSize");
        }
    }
    
    private void GridSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GridSpacingValue != null)
        {
            GridSpacingValue.Text = $"{(int)e.NewValue}px";
            _isDirty = true;
            
            _settingsService.NotifySettingChanged("Appearance.GridSpacing");
        }
    }
    
    private void RemoveArrowsToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        
        bool newValue = RemoveArrowsToggle.IsChecked ?? false;
        
        if (newValue)
        {
            // Show warning dialog
            var result = MessageBox.Show(
                "This will remove shortcut arrows from ALL shortcuts system-wide.\n\n" +
                "• Requires administrator privileges\n" +
                "• Windows Explorer will restart\n" +
                "• Changes can be reverted by unchecking this option\n\n" +
                "Do you want to continue?",
                "Remove Shortcut Arrows",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
            {
                RemoveArrowsToggle.IsChecked = false;
                return;
            }
        }
        
        _isDirty = true;
    }
    
    private async void RefreshIconsBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshIconsBtn.IsEnabled = false;
            RefreshIconsBtn.Content = "Refreshing...";
            
            // Call desktop refresher to clear icon cache
            var refresher = new DesktopRefresher();
            await refresher.RefreshAsync();
            
            MessageBox.Show("Icon cache refreshed successfully!", "Refresh Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to refresh icons: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshIconsBtn.IsEnabled = true;
            RefreshIconsBtn.Content = "Refresh Now";
        }
    }
    
    private async void ApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        SaveUIToSettings();
        await _settingsService.SaveAsync(_workingCopy);
        _isDirty = false;
        
        // Apply icon changes if needed
        if (_workingCopy.Icons.HideShortcutArrows != _settingsService.Current.Icons.HideShortcutArrows)
        {
            ApplyShortcutArrowChange(_workingCopy.Icons.HideShortcutArrows);
        }
    }
    
    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Discard them?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
        }
        
        DialogResult = false;
        Close();
    }
    
    private async void OKBtn_Click(object sender, RoutedEventArgs e)
    {
        SaveUIToSettings();
        await _settingsService.SaveAsync(_workingCopy);
        
        // Apply icon changes if needed
        if (_workingCopy.Icons.HideShortcutArrows != _settingsService.Current.Icons.HideShortcutArrows)
        {
            ApplyShortcutArrowChange(_workingCopy.Icons.HideShortcutArrows);
        }
        
        DialogResult = true;
        Close();
    }
    
    private void ApplyShortcutArrowChange(bool hideArrows)
    {
        try
        {
            // Registry path for shortcut overlay
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons";
            
            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath);
            if (key != null)
            {
                if (hideArrows)
                {
                    // Set to blank icon
                    key.SetValue("29", "%windir%\\System32\\shell32.dll,-50", Microsoft.Win32.RegistryValueKind.String);
                }
                else
                {
                    // Remove the override (restore default arrow)
                    key.DeleteValue("29", throwOnMissingValue: false);
                }
            }
            
            // Refresh Explorer
            var refresher = new DesktopRefresher();
            _ = refresher.RefreshAsync();
            
            MessageBox.Show(
                hideArrows 
                    ? "Shortcut arrows have been removed. Explorer may need a restart for full effect." 
                    : "Shortcut arrows have been restored.",
                "Shortcut Arrows",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                "Administrator privileges are required to change shortcut arrows.\n" +
                "Please run SmartDesktop as Administrator.",
                "Permission Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to modify shortcut arrows: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private static AppSettings CloneSettings(AppSettings source)
    {
        // Deep clone via JSON serialization
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }
}
