using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media;

namespace SmartDesktopOrganizer.ViewModels;

public partial class AppItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "App";

    [ObservableProperty]
    private string _iconKind = "Application";

    [ObservableProperty]
    private ImageSource? _iconImage;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isJiggling;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isGhost;

    // For folder rename mode
    [ObservableProperty]
    private bool _isRenaming;

    // For hybrid folder mode (default: enlarged)
    [ObservableProperty]
    private bool _isEnlarged = true;

    // Computed properties for hybrid folder display
    public System.Collections.Generic.IEnumerable<AppItemViewModel> DisplayItems => InnerItems.Take(8);
    public System.Collections.Generic.IEnumerable<AppItemViewModel> ExtraItems => InnerItems.Skip(8);
    public System.Collections.Generic.IEnumerable<AppItemViewModel> ExtraItemsPreview => InnerItems.Skip(8).Take(9); // Max 9 for preview
    public bool HasExtras => InnerItems.Count > 8;

    // For folders
    public ObservableCollection<AppItemViewModel> InnerItems { get; } = new();

    public AppItemViewModel(string name, string iconKind, bool isFolder = false)
    {
        Name = name;
        IconKind = iconKind;
        IsFolder = isFolder;
        
        // Subscribe to InnerItems changes to notify computed properties
        InnerItems.CollectionChanged += InnerItems_CollectionChanged;
    }

    private void InnerItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyItemsChanged();
    }

    /// <summary>
    /// Notifies UI that DisplayItems, ExtraItems, and HasExtras have changed.
    /// </summary>
    public void NotifyItemsChanged()
    {
        OnPropertyChanged(nameof(DisplayItems));
        OnPropertyChanged(nameof(ExtraItems));
        OnPropertyChanged(nameof(ExtraItemsPreview));
        OnPropertyChanged(nameof(HasExtras));
    }
}
