using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartDesktopOrganizer.ViewModels;

namespace SmartDesktopOrganizer.Controls;

public partial class FolderPopup : UserControl
{
    private AppItemViewModel? _currentFolder;
    private string _originalName = string.Empty;
    private MainViewModel? _mainViewModel;

    public FolderPopup()
    {
        InitializeComponent();
    }

    public void ShowFolder(AppItemViewModel folder, MainViewModel? mainViewModel = null)
    {
        _currentFolder = folder;
        _mainViewModel = mainViewModel;
        _originalName = folder.Name;
        
        // Ensure we're in display mode
        SetEditMode(false);
        
        // Update folder name
        FolderNameText.Text = folder.Name;
        
        // Clear existing items
        ItemsPanel.Children.Clear();
        
        // Add items from folder
        foreach (var item in folder.InnerItems)
        {
            var appItem = new AppItem();
            appItem.DataContext = item;
            appItem.Margin = new Thickness(10);
            ItemsPanel.Children.Add(appItem);
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFolder != null)
        {
            _originalName = _currentFolder.Name;
            FolderNameTextBox.Text = _currentFolder.Name;
            SetEditMode(true);
            
            // Focus and select all text
            FolderNameTextBox.Focus();
            FolderNameTextBox.SelectAll();
        }
    }

    private void SaveRename_Click(object sender, RoutedEventArgs e)
    {
        SaveRename();
    }

    private void CancelRename_Click(object sender, RoutedEventArgs e)
    {
        CancelRename();
    }

    private void FolderNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveRename();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelRename();
            e.Handled = true;
        }
    }

    private void SaveRename()
    {
        if (_currentFolder != null)
        {
            string newName = FolderNameTextBox.Text?.Trim() ?? string.Empty;
            
            // Validate name
            if (string.IsNullOrWhiteSpace(newName))
            {
                newName = "Folder";
            }
            
            // Update the folder name
            _currentFolder.Name = newName;
            FolderNameText.Text = newName;
            
            // Exit edit mode
            SetEditMode(false);
            
            // Trigger layout save (through MainViewModel if available)
            TriggerLayoutSave();
        }
    }

    private void CancelRename()
    {
        if (_currentFolder != null)
        {
            // Revert to original name
            FolderNameTextBox.Text = _originalName;
            SetEditMode(false);
        }
    }

    private void SetEditMode(bool isEditing)
    {
        if (_currentFolder != null)
        {
            _currentFolder.IsRenaming = isEditing;
        }
        
        DisplayModePanel.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        EditModePanel.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TriggerLayoutSave()
    {
        // Use the stored MainViewModel reference or find it
        if (_mainViewModel != null)
        {
            _mainViewModel.ForceSaveLayout();
            return;
        }
        
        // Fallback: Find MainWindow and trigger save
        var window = Window.GetWindow(this) as MainWindow;
        if (window?.DataContext is MainViewModel mainVm)
        {
            mainVm.ForceSaveLayout();
        }
    }

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // If in edit mode, save first
        if (_currentFolder?.IsRenaming == true)
        {
            SaveRename();
        }
        
        // Close popup
        this.Visibility = Visibility.Collapsed;
    }
}
