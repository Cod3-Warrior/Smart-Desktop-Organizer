using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SmartDesktopOrganizer.ViewModels;

namespace SmartDesktopOrganizer.Controls;

public partial class AppItem : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;
    private const double DragThreshold = 6.0; // 6-pixel threshold

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
        
        // Single-click for folders, double-click for apps
        this.MouseLeftButtonUp += AppItem_MouseLeftButtonUp;
        this.MouseDoubleClick += AppItem_MouseDoubleClick;
    }

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
        
        // Check if moved beyond threshold
        if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold)
        {
            if (!_isDragging && DataContext is AppItemViewModel vm)
            {
                _isDragging = true;
                
                // Find the adorner layer (from the Window)
                var window = Window.GetWindow(this);
                AdornerLayer? adornerLayer = null;
                DragAdorner? dragAdorner = null;
                DragEventHandler? dragOverHandler = null;
                
                if (window != null)
                {
                    adornerLayer = AdornerLayer.GetAdornerLayer(window.Content as UIElement);
                    if (adornerLayer != null)
                    {
                        var startPos = e.GetPosition(window.Content as UIElement);
                        dragAdorner = new DragAdorner(window.Content as UIElement, this, startPos);
                        adornerLayer.Add(dragAdorner);
                        
                        // Store handler reference to remove it later (fix memory leak)
                        dragOverHandler = (s, args) =>
                        {
                            var pos = args.GetPosition(window.Content as UIElement);
                            dragAdorner.UpdatePosition(pos);
                        };
                        window.PreviewDragOver += dragOverHandler;
                    }
                }
                
                // Notify MainViewModel we're dragging
                var mainVm = FindMainViewModel();
                if (mainVm != null) mainVm.DraggedItem = vm;
                
                // Start drag operation
                var data = new DataObject(typeof(AppItemViewModel), vm);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                
                // Clean up: remove event handler to prevent memory leak
                if (window != null && dragOverHandler != null)
                {
                    window.PreviewDragOver -= dragOverHandler;
                }

                // Cleanup adorner
                if (adornerLayer != null && dragAdorner != null)
                {
                    adornerLayer.Remove(dragAdorner);
                }
                
                // Reset after drag
                if (mainVm != null) mainVm.DraggedItem = null;
                _isDragging = false;
            }
        }
    }

    private void AppItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    private void AppItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;
        
        // Check if mouse wasn't dragged (stayed close to start point)
        Point currentPos = e.GetPosition(this);
        Vector diff = _dragStartPoint - currentPos;
        if (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold) return;
        
        if (DataContext is AppItemViewModel vm && vm.IsFolder)
        {
            OpenFolderPopup(vm);
            e.Handled = true;
        }
    }

    private void AppItem_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(AppItemViewModel)))
        {
            var source = e.Data.GetData(typeof(AppItemViewModel)) as AppItemViewModel;
            var target = DataContext as AppItemViewModel;
            
            // Don't highlight self
            if (source != target && target != null)
            {
                // Show blue glow highlight
                IconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                IconBorder.BorderThickness = new Thickness(3);
            }
        }
        e.Handled = true;
    }

    private void AppItem_DragLeave(object sender, DragEventArgs e)
    {
        // Remove highlight
        IconBorder.BorderBrush = null;
        IconBorder.BorderThickness = new Thickness(0);
        e.Handled = true;
    }

    private void AppItem_Drop(object sender, DragEventArgs e)
    {
        // Remove highlight
        IconBorder.BorderBrush = null;
        IconBorder.BorderThickness = new Thickness(0);
        
        if (e.Data.GetDataPresent(typeof(AppItemViewModel)))
        {
            var source = e.Data.GetData(typeof(AppItemViewModel)) as AppItemViewModel;
            var target = DataContext as AppItemViewModel;
            
            if (source != null && target != null && source != target)
            {
                var mainVm = FindMainViewModel();
                if (mainVm != null)
                {
                    // Create folder (or add to existing folder)
                    mainVm.CreateFolder(source, target);
                }
            }
        }
        e.Handled = true;
    }

    private void AppItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return; // Don't open during drag
        
        if (DataContext is AppItemViewModel vm)
        {
            if (vm.IsFolder)
            {
                OpenFolderPopup(vm);
            }
            else if (!string.IsNullOrEmpty(vm.FullPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = vm.FullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to launch app: {ex.Message}");
                }
            }
        }
    }

    private void OpenFolderPopup(AppItemViewModel folder)
    {
        var window = Window.GetWindow(this) as MainWindow;
        window?.OpenFolder(folder);
    }

    private MainViewModel? FindMainViewModel()
    {
        // Walk up visual tree to find MainWindow and get its DataContext
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

    public static readonly DependencyProperty IsJigglingProperty = DependencyProperty.Register(
        nameof(IsJiggling), typeof(bool), typeof(AppItem), new PropertyMetadata(false, OnIsJigglingChanged));

    public bool IsJiggling
    {
        get => (bool)GetValue(IsJigglingProperty);
        set => SetValue(IsJigglingProperty, value);
    }

    private static void OnIsJigglingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppItem item)
        {
            var sb = (System.Windows.Media.Animation.Storyboard)item.IconBorder.Resources["JiggleStoryboard"];
            if ((bool)e.NewValue)
                sb.Begin();
            else
                sb.Stop();
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
}
