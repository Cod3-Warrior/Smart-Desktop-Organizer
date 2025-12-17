using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Models;
using System.Linq;

namespace SmartDesktopOrganizer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDesktopService _desktopService;
    private readonly IFolderNamingService _folderNamingService;
    public ObservableCollection<AppItemViewModel> Items { get; } = new();
    
    // Undo/Redo stacks
    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();

    // Currently dragged item (for drop target highlighting)
    [ObservableProperty]
    private AppItemViewModel? _draggedItem;

    public MainViewModel(IDesktopService desktopService, IFolderNamingService folderNamingService)
    {
        _desktopService = desktopService;
        _folderNamingService = folderNamingService;
        LoadDesktopAsync();
    }

    // Design-time constructor
    public MainViewModel() 
    {
        _desktopService = null!;
        _folderNamingService = null!;
    }

    private async void LoadDesktopAsync()
    {
        if (_desktopService == null) return;

        var items = await _desktopService.LoadLayoutAsync();
        if (items == null || !items.Any())
        {
            items = await _desktopService.GetDesktopItemsAsync();
        }

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(ConvertToViewModel(item));
        }

        Items.CollectionChanged += Items_CollectionChanged;
    }

    private AppItemViewModel ConvertToViewModel(DesktopItem item)
    {
        var vm = new AppItemViewModel(item.Name, item.IsFolder ? "Folder" : "File", item.IsFolder);
        vm.FullPath = item.FullPath;
        vm.IconImage = item.Icon;
        vm.IsEnlarged = item.IsEnlarged; // Persist folder mode
        
        foreach (var inner in item.InnerItems)
        {
            vm.InnerItems.Add(ConvertToViewModel(inner));
        }
        
        return vm;
    }

    private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveLayout();
    }

    private async void SaveLayout()
    {
        if (_desktopService == null) return;
        var items = Items.Select(ConvertToDesktopItem);
        await _desktopService.SaveLayoutAsync(items);
    }

    /// <summary>
    /// Forces a layout save. Used when folder properties change (e.g., rename).
    /// </summary>
    public void ForceSaveLayout()
    {
        SaveLayout();
    }

    private DesktopItem ConvertToDesktopItem(AppItemViewModel vm)
    {
        var item = new DesktopItem
        {
            Name = vm.Name,
            FullPath = vm.FullPath,
            Size = 0,
            IsFolder = vm.IsFolder,
            IsEnlarged = vm.IsEnlarged // Persist folder mode
        };
        
        foreach (var inner in vm.InnerItems)
        {
            item.InnerItems.Add(ConvertToDesktopItem(inner));
        }
        
        return item;
    }

    /// <summary>
    /// Creates a folder from two items (smartphone-style drop behavior)
    /// </summary>
    public void CreateFolder(AppItemViewModel source, AppItemViewModel target)
    {
        if (source == null || target == null) return;
        if (source.FullPath == target.FullPath) return; // Same item, do nothing
        
        // Find actual items in the collection
        // Prioritize reference matching (works for folders with virtual paths)
        var actualSource = Items.FirstOrDefault(x => x == source) 
                        ?? Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.FullPath) && x.FullPath == source.FullPath);
        var actualTarget = Items.FirstOrDefault(x => x == target) 
                        ?? Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.FullPath) && x.FullPath == target.FullPath);
        
        if (actualSource == null || actualTarget == null)
        {
            System.Diagnostics.Debug.WriteLine($"CreateFolder FAILED: source={actualSource != null} ({source?.Name}), target={actualTarget != null} ({target?.Name}), source.FullPath={source?.FullPath}, target.FullPath={target?.FullPath}");
            return;
        }
        
        // If target is already a folder, add source to it
        if (actualTarget.IsFolder)
        {
            System.Diagnostics.Debug.WriteLine($"CreateFolder: Target '{actualTarget.Name}' is folder, adding '{actualSource.Name}' to it");
            AddToFolder(actualSource, actualTarget);
            return;
        }
        
        // If source is a folder, add target to it
        if (actualSource.IsFolder)
        {
            AddToFolder(actualTarget, actualSource);
            return;
        }

        // Get positions BEFORE removing
        int sourceIndex = Items.IndexOf(actualSource);
        int targetIndex = Items.IndexOf(actualTarget);
        
        System.Diagnostics.Debug.WriteLine($"CreateFolder: sourceIdx={sourceIndex}, targetIdx={targetIndex}");
        
        // Create new folder containing both items with smart name
        var sourceItem = ConvertToDesktopItem(actualSource);
        var targetItem = ConvertToDesktopItem(actualTarget);
        var suggestedName = _folderNamingService?.SuggestFolderName(new[] { sourceItem, targetItem }) ?? "Folder";
        var folder = new AppItemViewModel(suggestedName, "Folder", isFolder: true);
        folder.FullPath = $"folder://{Guid.NewGuid()}"; // Unique identifier for folders
        
        // Copy items into folder (with full data)
        var sourceCopy = CloneItem(actualSource);
        var targetCopy = CloneItem(actualTarget);
        folder.InnerItems.Add(sourceCopy);
        folder.InnerItems.Add(targetCopy);
        
        // Remove originals FIRST
        bool removedSource = Items.Remove(actualSource);
        bool removedTarget = Items.Remove(actualTarget);
        
        System.Diagnostics.Debug.WriteLine($"CreateFolder: removed source={removedSource}, target={removedTarget}");
        
        // Insert folder at the earlier position
        int insertIndex = Math.Min(sourceIndex, targetIndex);
        if (insertIndex < 0) insertIndex = 0;
        if (insertIndex > Items.Count) insertIndex = Items.Count;
        Items.Insert(insertIndex, folder);
        
        System.Diagnostics.Debug.WriteLine($"CreateFolder: inserted folder at {insertIndex}, Items.Count={Items.Count}");
        
        // Push undo action
        _undoStack.Push(new UndoAction
        {
            Type = UndoActionType.CreateFolder,
            Folder = folder,
            OriginalItems = new List<AppItemViewModel> { actualSource, actualTarget },
            OriginalIndices = new List<int> { sourceIndex, targetIndex }
        });
        _redoStack.Clear();
    }

    /// <summary>
    /// Adds an item to an existing folder
    /// </summary>
    public void AddToFolder(AppItemViewModel item, AppItemViewModel folder)
    {
        if (!folder.IsFolder || item == folder) return;
        
        // Find actual item - prioritize reference matching (same approach as CreateFolder)
        var actualItem = Items.FirstOrDefault(x => x == item) 
                      ?? Items.FirstOrDefault(x => !string.IsNullOrEmpty(x.FullPath) && x.FullPath == item.FullPath);
        if (actualItem == null)
        {
            System.Diagnostics.Debug.WriteLine($"AddToFolder FAILED: item '{item.Name}' not found in Items. Path={item.FullPath}");
            return;
        }
        
        // Check if already in folder (prevent duplicates)
        if (folder.InnerItems.Any(x => x.FullPath == item.FullPath))
        {
            System.Diagnostics.Debug.WriteLine($"AddToFolder: item already in folder. Path={item.FullPath}");
            return;
        }
        
        int itemIndex = Items.IndexOf(actualItem);
        
        var itemCopy = CloneItem(actualItem);
        folder.InnerItems.Add(itemCopy);
        Items.Remove(actualItem);
        
        System.Diagnostics.Debug.WriteLine($"AddToFolder SUCCESS: Added '{actualItem.Name}' to folder '{folder.Name}'. Folder now has {folder.InnerItems.Count} items.");
        
        _undoStack.Push(new UndoAction
        {
            Type = UndoActionType.AddToFolder,
            Folder = folder,
            OriginalItems = new List<AppItemViewModel> { actualItem },
            OriginalIndices = new List<int> { itemIndex }
        });
        _redoStack.Clear();
    }

    /// <summary>
    /// Removes an item from a folder and places it on desktop
    /// </summary>
    public void RemoveFromFolder(AppItemViewModel item, AppItemViewModel folder)
    {
        if (!folder.IsFolder) return;
        
        folder.InnerItems.Remove(item);
        Items.Add(CloneItem(item));
        
        // If folder now has only 1 item, dissolve it
        if (folder.InnerItems.Count == 1)
        {
            var remaining = folder.InnerItems[0];
            int folderIndex = Items.IndexOf(folder);
            Items.Remove(folder);
            Items.Insert(folderIndex, CloneItem(remaining));
        }
    }

    private AppItemViewModel CloneItem(AppItemViewModel source)
    {
        var clone = new AppItemViewModel(source.Name, source.IconKind, source.IsFolder);
        clone.FullPath = source.FullPath;
        clone.IconImage = source.IconImage;
        foreach (var inner in source.InnerItems)
        {
            clone.InnerItems.Add(CloneItem(inner));
        }
        return clone;
    }

    [RelayCommand]
    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        
        var action = _undoStack.Pop();
        
        switch (action.Type)
        {
            case UndoActionType.CreateFolder:
                // Remove folder, restore original items
                Items.Remove(action.Folder!);
                for (int i = 0; i < action.OriginalItems!.Count; i++)
                {
                    int idx = Math.Min(action.OriginalIndices![i], Items.Count);
                    Items.Insert(idx, action.OriginalItems[i]);
                }
                break;
                
            case UndoActionType.AddToFolder:
                // Remove from folder, restore to desktop
                foreach (var item in action.OriginalItems!)
                {
                    var match = action.Folder!.InnerItems.FirstOrDefault(x => x.FullPath == item.FullPath);
                    if (match != null) action.Folder.InnerItems.Remove(match);
                }
                for (int i = 0; i < action.OriginalItems.Count; i++)
                {
                    int idx = Math.Min(action.OriginalIndices![i], Items.Count);
                    Items.Insert(idx, action.OriginalItems[i]);
                }
                break;
        }
        
        _redoStack.Push(action);
    }

    [RelayCommand]
    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        
        var action = _redoStack.Pop();
        
        switch (action.Type)
        {
            case UndoActionType.CreateFolder:
                foreach (var item in action.OriginalItems!)
                {
                    Items.Remove(item);
                }
                Items.Add(action.Folder!);
                break;
                
            case UndoActionType.AddToFolder:
                foreach (var item in action.OriginalItems!)
                {
                    Items.Remove(item);
                    action.Folder!.InnerItems.Add(CloneItem(item));
                }
                break;
        }
        
        _undoStack.Push(action);
    }

    public void MoveItem(AppItemViewModel source, AppItemViewModel target)
    {
        if (source == target) return;

        int oldIndex = Items.IndexOf(source);
        int newIndex = Items.IndexOf(target);

        if (oldIndex != -1 && newIndex != -1)
        {
            Items.Move(oldIndex, newIndex);
        }
    }
}

public enum UndoActionType
{
    CreateFolder,
    AddToFolder,
    RemoveFromFolder
}

public class UndoAction
{
    public UndoActionType Type { get; set; }
    public AppItemViewModel? Folder { get; set; }
    public List<AppItemViewModel>? OriginalItems { get; set; }
    public List<int>? OriginalIndices { get; set; }
}
