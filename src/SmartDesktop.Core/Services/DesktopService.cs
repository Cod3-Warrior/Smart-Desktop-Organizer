using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartDesktop.Core; // For IFileSystem
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Models;
using SmartDesktop.Core.Utilities;

namespace SmartDesktop.Core.Services;

public class DesktopService : IDesktopService
{
    private readonly string _desktopPath;
    private readonly IFileSystem _fileSystem;

    public DesktopService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public async Task<IEnumerable<DesktopItem>> GetDesktopItemsAsync()
    {
        return await Task.Run(() => 
        {
            var items = new List<DesktopItem>();
            try
            {
               var files = _fileSystem.GetFiles(_desktopPath);
               foreach (var file in files)
               {
                   long size = 0;
                   try { size = new FileInfo(file).Length; } catch {}

                   var item = new DesktopItem
                   {
                       Name = file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) 
                           ? Path.GetFileNameWithoutExtension(file) 
                           : Path.GetFileName(file),
                       FullPath = file,
                       Size = size,
                       Icon = IconExtractor.GetIcon(file)
                   };
                   items.Add(item);
               }
            }
            catch (Exception) 
            {
                // ignore
            }
            return (IEnumerable<DesktopItem>)items;
        });
    }

    public Task RefreshDesktopAsync()
    {
        return Task.Run(() => 
        {
            NativeMethods.SHChangeNotify(NativeMethods.SHCNE_ASSOCCHANGED, NativeMethods.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        });
    }
    private readonly string _layoutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartDesktop", "layout.json");

    public async Task SaveLayoutAsync(IEnumerable<DesktopItem> items)
    {
        try
        {
            var directory = Path.GetDirectoryName(_layoutPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(items);
            await File.WriteAllTextAsync(_layoutPath, json);
        }
        catch { }
    }

    public async Task<IEnumerable<DesktopItem>> LoadLayoutAsync()
    {
        if (!File.Exists(_layoutPath)) return Enumerable.Empty<DesktopItem>();
        try
        {
            var json = await File.ReadAllTextAsync(_layoutPath);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<DesktopItem>>(json);
            if (items != null)
            {
                foreach (var item in items)
                {
                    RegenerateIcons(item);
                }
                return items;
            }
        }
        catch { }
        return Enumerable.Empty<DesktopItem>();
    }

    /// <summary>
    /// Recursively regenerates icons for an item and all its nested InnerItems.
    /// This ensures folder thumbnails display correctly after app restart.
    /// </summary>
    private void RegenerateIcons(DesktopItem item)
    {
        if (!string.IsNullOrEmpty(item.FullPath) && File.Exists(item.FullPath))
        {
            item.Icon = IconExtractor.GetIcon(item.FullPath);
        }
        
        // Recursively regenerate icons for all nested items (folder contents)
        foreach (var inner in item.InnerItems)
        {
            RegenerateIcons(inner);
        }
    }

    public string GetWallpaperPath()
    {
        try
        {
            // 1. Try standard registry key
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
            {
                if (key != null)
                {
                    string wallpaper = key.GetValue("Wallpaper") as string;
                    if (!string.IsNullOrEmpty(wallpaper) && File.Exists(wallpaper))
                    {
                        return wallpaper;
                    }

                    // 2. Try TranscodedImageCache if standard key fails
                    var transcoded = key.GetValue("TranscodedImageCache") as byte[];
                    if (transcoded != null && transcoded.Length > 24)
                    {
                        // Skip first 24 bytes (header) and read UTF-16LE string
                        string path = System.Text.Encoding.Unicode.GetString(transcoded, 24, transcoded.Length - 24);
                        
                        // Trim null terminators and garbage at the end
                        int nullIndex = path.IndexOf('\0');
                        if (nullIndex >= 0)
                        {
                            path = path.Substring(0, nullIndex);
                        }

                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }
        }
        catch 
        {
            // Fail silently or log if logger available
        }
        return string.Empty;
    }
}
