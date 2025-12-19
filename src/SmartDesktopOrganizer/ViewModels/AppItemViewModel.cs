using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
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

    // For folders
    public ObservableCollection<AppItemViewModel> InnerItems { get; } = new();

    public AppItemViewModel(string name, string iconKind, bool isFolder = false)
    {
        Name = name;
        IconKind = iconKind;
        IsFolder = isFolder;
    }
}
