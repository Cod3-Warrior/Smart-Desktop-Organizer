using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SmartDesktopOrganizer.ViewModels;

namespace SmartDesktopOrganizer.Controls;

public partial class AppItem : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private const double DragThreshold = 6.0;

    public AppItem()
    {
        InitializeComponent();
        
        // Drag events
        this.PreviewMouseLeftButtonDown += AppItem_PreviewMouseLeftButtonDown;
        this.PreviewMouseMove += AppItem_PreviewMouseMove;
        this.PreviewMouseLeftButtonUp += AppItem_PreviewMouseLeftButtonUp;
        
        // Drop target events
        this.AllowDrop = true;
        this.DragEnter += AppItem_DragEnter;
        this.DragLeave += AppItem_DragLeave;
        this.Drop += AppItem_Drop;
        
        // Single-click for launching apps (not double-click)
        this.MouseLeftButtonUp += AppItem_MouseLeftButtonUp;
        
        // Data context changed -> update visuals
        this.DataContextChanged += AppItem_DataContextChanged;
        this.Loaded += AppItem_Loaded;
        
        // Context menu opening
        this.ContextMenuOpening += AppItem_ContextMenuOpening;
    }

    private void AppItem_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateFolderVisibility();
    }

    private void AppItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AppItemViewModel oldVm)
        {
            oldVm.PropertyChanged -= Vm_PropertyChanged;
        }
        if (e.NewValue is AppItemViewModel newVm)
        {
            newVm.PropertyChanged += Vm_PropertyChanged;
            UpdateFolderVisibility();
        }
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppItemViewModel.IsEnlarged) || 
            e.PropertyName == nameof(AppItemViewModel.IsFolder))
        {
            UpdateFolderVisibility();
        }
    }

    /// <summary>
    /// Updates visibility of Enlarged/Shrunk folder grids based on IsFolder and IsEnlarged.
    /// </summary>
    private void UpdateFolderVisibility()
    {
        if (DataContext is not AppItemViewModel vm) return;
        
        if (vm.IsFolder)
        {
            EnlargedFolderGrid.Visibility = vm.IsEnlarged ? Visibility.Visible : Visibility.Collapsed;
            ShrunkFolderGrid.Visibility = vm.IsEnlarged ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            EnlargedFolderGrid.Visibility = Visibility.Collapsed;
            ShrunkFolderGrid.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Context menu opening: hide irrelevant items for non-folders.
    /// </summary>
    private void AppItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (DataContext is AppItemViewModel vm && vm.IsFolder)
        {
            EnlargeMenuItem.Visibility = vm.IsEnlarged ? Visibility.Collapsed : Visibility.Visible;
            ShrinkMenuItem.Visibility = vm.IsEnlarged ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            // Hide folder-specific menu items for non-folders
            EnlargeMenuItem.Visibility = Visibility.Collapsed;
            ShrinkMenuItem.Visibility = Visibility.Collapsed;
        }
    }

    #region Quick Launch (Enlarged Mode Slots 0-7)
    
    /// <summary>
    /// Handles click on quick-launch slots in enlarged folder mode.
    /// Launches the app at the specified index directly.
    /// </summary>
    private void QuickLaunch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not AppItemViewModel folder || !folder.IsFolder) return;
        
        // Get index from Tag
        if (!int.TryParse(btn.Tag?.ToString(), out int index)) return;
        if (index < 0 || index >= folder.InnerItems.Count) return;
        
        var targetApp = folder.InnerItems[index];
        if (!string.IsNullOrEmpty(targetApp.FullPath) && !targetApp.IsFolder)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetApp.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QuickLaunch failed: {ex.Message}");
            }
        }
        else if (targetApp.IsFolder)
        {
            // If it's a nested folder, open popup for it
            OpenFolderPopup(targetApp);
        }
        
        e.Handled = true;
    }
    
    #endregion

    #region Subfolder / Shrunk Click -> Open Overlay
    
    /// <summary>
    /// Clicking the 9th subfolder slot opens the full folder overlay.
    /// </summary>
    private void SubfolderSlot_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AppItemViewModel folder && folder.IsFolder)
        {
            OpenFolderPopup(folder);
        }
        e.Handled = true;
    }
    
    /// <summary>
    /// Clicking shrunk folder opens the full folder overlay.
    /// </summary>
    private void ShrunkFolder_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AppItemViewModel folder && folder.IsFolder)
        {
            OpenFolderPopup(folder);
        }
        e.Handled = true;
    }
    
    #endregion

    #region Context Menu: Enlarge / Shrink Toggle
    
    private void EnlargeFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppItemViewModel vm && vm.IsFolder)
        {
            vm.IsEnlarged = true;
            PlayEnlargeAnimation();
            SaveLayout();
        }
    }
    
    private void ShrinkFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppItemViewModel vm && vm.IsFolder)
        {
            vm.IsEnlarged = false;
            PlayShrinkAnimation();
            SaveLayout();
        }
    }
    
    private void PlayEnlargeAnimation()
    {
        if (Resources["EnlargeStoryboard"] is Storyboard sb)
        {
            sb.Begin(MainContainer);
        }
    }
    
    private void PlayShrinkAnimation()
    {
        if (Resources["ShrinkStoryboard"] is Storyboard sb)
        {
            sb.Begin(MainContainer);
        }
    }
    
    private void SaveLayout()
    {
        var mainVm = FindMainViewModel();
        mainVm?.ForceSaveLayout();
    }
    
    #endregion

    #region Drag & Drop
    
    private void AppItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
    }

    private void AppItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        
        Point currentPos = e.GetPosition(this);
        Vector diff = _dragStartPoint - currentPos;
        
        if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
        {
            if (!_isDragging && DataContext is AppItemViewModel vm)
            {
                _isDragging = true;
                
                var window = Window.GetWindow(this);
                AdornerLayer? adornerLayer = null;
                DragAdorner? dragAdorner = null;
                
                if (window != null)
                {
                    adornerLayer = AdornerLayer.GetAdornerLayer(window.Content as UIElement);
                    if (adornerLayer != null)
                    {
                        var startPos = e.GetPosition(window.Content as UIElement);
                        dragAdorner = new DragAdorner(window.Content as UIElement, this, startPos);
                        adornerLayer.Add(dragAdorner);
                        
                        window.PreviewDragOver += (s, args) =>
                        {
                            var pos = args.GetPosition(window.Content as UIElement);
                            dragAdorner.UpdatePosition(pos);
                        };
                    }
                }
                
                var mainVm = FindMainViewModel();
                if (mainVm != null) mainVm.DraggedItem = vm;
                
                var data = new DataObject(typeof(AppItemViewModel), vm);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                
                if (adornerLayer != null && dragAdorner != null)
                {
                    adornerLayer.Remove(dragAdorner);
                }
                
                if (mainVm != null) mainVm.DraggedItem = null;
                _isDragging = false;
            }
        }
    }

    private void AppItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    private void AppItem_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(AppItemViewModel)))
        {
            var source = e.Data.GetData(typeof(AppItemViewModel)) as AppItemViewModel;
            var target = DataContext as AppItemViewModel;
            
            if (source != target && target != null)
            {
                // Highlight the appropriate border based on mode
                var border = GetHighlightBorder();
                if (border != null)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                    border.BorderThickness = new Thickness(3);
                }
            }
        }
        e.Handled = true;
    }

    private void AppItem_DragLeave(object sender, DragEventArgs e)
    {
        var border = GetHighlightBorder();
        if (border != null)
        {
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
        }
        e.Handled = true;
    }

    private void AppItem_Drop(object sender, DragEventArgs e)
    {
        var border = GetHighlightBorder();
        if (border != null)
        {
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
        }
        
        if (e.Data.GetDataPresent(typeof(AppItemViewModel)))
        {
            var source = e.Data.GetData(typeof(AppItemViewModel)) as AppItemViewModel;
            var target = DataContext as AppItemViewModel;
            
            if (source != null && target != null && source != target)
            {
                var mainVm = FindMainViewModel();
                mainVm?.CreateFolder(source, target);
            }
        }
        e.Handled = true;
    }

    /// <summary>
    /// Gets the border to highlight based on current folder mode.
    /// </summary>
    private Border? GetHighlightBorder()
    {
        if (DataContext is AppItemViewModel vm)
        {
            if (!vm.IsFolder) return IconBorder;
            if (vm.IsEnlarged) return null; // Enlarged has internal structure
            return ShrunkBorder;
        }
        return IconBorder;
    }
    
    #endregion

    #region Single-Click Launch (Desktop apps and folder overlay items)
    
    /// <summary>
    /// Single-click to launch non-folder apps. Checks drag threshold to avoid false triggers.
    /// </summary>
    private void AppItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;
        
        // Check if mouse wasn't dragged (stayed close to start point)
        Point currentPos = e.GetPosition(this);
        Vector diff = _dragStartPoint - currentPos;
        if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold) return;
        
        if (DataContext is AppItemViewModel vm)
        {
            if (vm.IsFolder)
            {
                // For folders, only open popup if not in enlarged mode (shrunk mode)
                // Enlarged mode has its own buttons
                if (!vm.IsEnlarged)
                {
                    OpenFolderPopup(vm);
                }
            }
            else if (!string.IsNullOrEmpty(vm.FullPath))
            {
                // Launch app with single click
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = vm.FullPath,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
        e.Handled = true;
    }
    
    #endregion

    #region Helpers
    
    private void OpenFolderPopup(AppItemViewModel folder)
    {
        var window = Window.GetWindow(this) as MainWindow;
        window?.OpenFolder(folder);
    }

    private MainViewModel? FindMainViewModel()
    {
        DependencyObject? current = this;
        while (current != null)
        {
            if (current is Window window && window.DataContext is MainViewModel vm)
            {
                return vm;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    
    #endregion

    #region Dependency Properties
    
    public static readonly DependencyProperty IsJigglingProperty = DependencyProperty.Register(
        nameof(IsJiggling), typeof(bool), typeof(AppItem), new PropertyMetadata(false, OnIsJigglingChanged));

    public bool IsJiggling
    {
        get => (bool)GetValue(IsJigglingProperty);
        set => SetValue(IsJigglingProperty, value);
    }

    private static void OnIsJigglingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppItem item && item.Resources["JiggleStoryboard"] is Storyboard sb)
        {
            if ((bool)e.NewValue)
                sb.Begin(item.MainContainer);
            else
                sb.Stop(item.MainContainer);
        }
    }

    public static readonly DependencyProperty IsGhostProperty = DependencyProperty.Register(
        nameof(IsGhost), typeof(bool), typeof(AppItem), new PropertyMetadata(false, OnIsGhostChanged));

    public bool IsGhost
    {
        get => (bool)GetValue(IsGhostProperty);
        set => SetValue(IsGhostProperty, value);
    }

    private static void OnIsGhostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppItem item)
        {
            item.Opacity = (bool)e.NewValue ? 0.5 : 1.0;
        }
    }
    
    #endregion
}
