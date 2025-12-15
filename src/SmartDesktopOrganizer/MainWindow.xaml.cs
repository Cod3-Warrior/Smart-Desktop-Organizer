using System.Windows;
using SmartDesktopOrganizer.ViewModels;
using System.Windows.Media; // Added for ImageBrush, SolidColorBrush, Color
using System; // Added for Uri, EventArgs

namespace SmartDesktopOrganizer;

public partial class MainWindow : Window
{
    private readonly SmartDesktop.Core.Interfaces.IDesktopService _desktopService;

    public MainWindow(MainViewModel viewModel, SmartDesktop.Core.Interfaces.IDesktopService desktopService)
    {
        InitializeComponent();
        // Original code: DataContext = viewModel;
        // The provided edit had a comment about potentially overwriting, but then correctly set it.
        this.DataContext = viewModel;
        
        _desktopService = desktopService;

        // Existing WindowState setting
        WindowState = WindowState.Maximized;

        // New event subscriptions from the edit
        this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        this.PreviewMouseMove += OnPreviewMouseMove;
        this.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;

        // Wallpaper Sync
        UpdateWallpaper();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        this.Closed += OnMainWindowClosed;
    }

    private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        if (e.Category == Microsoft.Win32.UserPreferenceCategory.Desktop)
        {
            Dispatcher.Invoke(() => UpdateWallpaper());
        }
    }

    private void UpdateWallpaper()
    {
        string path = _desktopService.GetWallpaperPath();
        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // Fix file locking
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();

                this.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
            catch
            {
                // Fallback
                this.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }
        }
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
         Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    public void OpenFolder(ViewModels.AppItemViewModel folder)
    {
        if (folder == null || !folder.IsFolder) return;
        
        var mainVm = DataContext as ViewModels.MainViewModel;
        MainFolderPopup.ShowFolder(folder, mainVm);
        MainFolderPopup.Visibility = Visibility.Visible;
    }

    private Point _startPoint;
    private Controls.AppItem? _draggedContainer;
    private ViewModels.AppItemViewModel? _draggedItemViewModel;

    private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        if (e.OriginalSource is DependencyObject source)
        {
            _draggedContainer = FindAncestor<Controls.AppItem>(source);
            if (_draggedContainer != null)
            {
                _draggedItemViewModel = _draggedContainer.DataContext as ViewModels.AppItemViewModel;
            }
        }
    }

    private void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && !_isDragging)
        {
            Point position = e.GetPosition(null);
            if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // Start Drag
                if (_draggedContainer != null && _draggedItemViewModel != null)
                {
                    _isDragging = true;
                    _draggedContainer.IsGhost = true; 
                    
                    DragDrop.DoDragDrop(_draggedContainer, _draggedItemViewModel, DragDropEffects.Move);
                    
                    _draggedContainer.IsGhost = false; 
                    _isDragging = false;
                    _draggedContainer = null;
                    _draggedItemViewModel = null;
                }
            }
        }
    }

    private void OnPreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = false;
        _draggedContainer = null;
    }

    private void AppItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is Controls.AppItem targetContainer && 
            targetContainer.DataContext is ViewModels.AppItemViewModel targetViewModel &&
            e.Data.GetData(typeof(ViewModels.AppItemViewModel)) is ViewModels.AppItemViewModel sourceViewModel &&
            this.DataContext is ViewModels.MainViewModel mainViewModel)
        {
            mainViewModel.MoveItem(sourceViewModel, targetViewModel);
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T)
            {
                return (T)current;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);
        return null;
    }

    private bool _isDragging;

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Views.SettingsWindow
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
}
