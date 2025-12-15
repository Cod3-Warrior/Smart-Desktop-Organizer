using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SmartDesktop.Core.Utilities;

namespace SmartDesktop.Core.Services;

public static class IconExtractor
{
    /// <summary>
    /// Extracts the icon for a file or shortcut with comprehensive handling for:
    /// - .lnk shortcuts (including Chrome web apps with custom icons)
    /// - UWP/Microsoft Store apps
    /// - Regular executables
    /// </summary>
    public static ImageSource? GetIcon(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        try
        {
            // For .lnk files, resolve the shortcut and get custom icon if available
            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var icon = GetShortcutIcon(path);
                if (icon != null)
                    return icon;
            }

            // Fallback: Use SHGetFileInfo for all file types
            return GetIconViaSHGetFileInfo(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves shortcut (.lnk) files using multiple methods.
    /// Priority: IShellLink (custom icons like Bluestacks) -> IShellItemImageFactory (UWP) -> SHGetFileInfo
    /// </summary>
    private static ImageSource? GetShortcutIcon(string shortcutPath)
    {
        // First try IShellLink for shortcuts with custom icon locations (Bluestacks, Chrome apps, etc.)
        try
        {
            var link = (NativeMethods.IShellLinkW)new NativeMethods.ShellLink();
            var file = (NativeMethods.IPersistFile)link;
            
            // Load the shortcut file
            file.Load(shortcutPath, 0);

            // Get custom icon location if specified
            var iconPath = new StringBuilder(260);
            link.GetIconLocation(iconPath, iconPath.Capacity, out int iconIndex);

            string iconLocation = iconPath.ToString();
            
            // If shortcut has a custom icon location, use it (this handles Bluestacks and Chrome apps)
            if (!string.IsNullOrEmpty(iconLocation) && File.Exists(iconLocation))
            {
                var icon = ExtractIconFromFile(iconLocation, iconIndex);
                if (icon != null)
                {
                    Marshal.ReleaseComObject(file);
                    Marshal.ReleaseComObject(link);
                    return icon;
                }
            }

            // Try to get icon from the target executable
            var targetPath = new StringBuilder(260);
            link.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, NativeMethods.SLGP_RAWPATH);
            
            string target = targetPath.ToString();
            
            Marshal.ReleaseComObject(file);
            Marshal.ReleaseComObject(link);

            // If target exists and is not a UWP app, get icon from it
            if (!string.IsNullOrEmpty(target) && File.Exists(target))
            {
                var targetIcon = GetIconViaSHGetFileInfo(target);
                if (targetIcon != null)
                    return targetIcon;
            }
        }
        catch
        {
            // IShellLink failed
        }

        // Second try IShellItemImageFactory - this works for UWP apps and other special items
        var shellItemIcon = GetIconViaShellItem(shortcutPath);
        if (shellItemIcon != null)
            return shellItemIcon;

        // Final fallback: get icon from the .lnk file itself via SHGetFileInfo
        return GetIconViaSHGetFileInfo(shortcutPath);
    }

    /// <summary>
    /// Extracts icon from UWP/Microsoft Store app shortcuts using IShellItemImageFactory.
    /// This is Windows' official API for getting high-quality icons for any shell item.
    /// </summary>
    private static ImageSource? GetUWPAppIcon(string shortcutPath)
    {
        try
        {
            // First check if this is a UWP app by looking for AppUserModelID
            Guid iid = typeof(NativeMethods.IPropertyStore).GUID;
            int hr = NativeMethods.SHGetPropertyStoreFromParsingName(
                shortcutPath,
                IntPtr.Zero,
                0, // GPS_DEFAULT
                ref iid,
                out NativeMethods.IPropertyStore propertyStore);

            if (hr != 0 || propertyStore == null)
                return null;

            bool isUwpApp = false;
            try
            {
                var key = NativeMethods.PropertyKey.AppUserModelID;
                propertyStore.GetValue(ref key, out NativeMethods.PropVariant pv);

                try
                {
                    string? appUserModelId = pv.GetStringValue();
                    // UWP apps have AppUserModelID in format: PackageFamilyName!AppId
                    isUwpApp = !string.IsNullOrEmpty(appUserModelId) && appUserModelId.Contains("!");
                }
                finally
                {
                    pv.Clear();
                }
            }
            finally
            {
                Marshal.ReleaseComObject(propertyStore);
            }

            // If it's a UWP app, use IShellItemImageFactory for high-quality icon
            if (isUwpApp)
            {
                return GetIconViaShellItem(shortcutPath);
            }
        }
        catch
        {
            // Not a UWP app or error reading properties
        }

        return null;
    }

    /// <summary>
    /// Gets a high-quality icon using IShellItemImageFactory.
    /// This works for all shell items including UWP apps, virtual folders, etc.
    /// </summary>
    private static ImageSource? GetIconViaShellItem(string path)
    {
        IntPtr hBitmap = IntPtr.Zero;
        object? shellItemObj = null;
        
        try
        {
            // Create IShellItem from path
            Guid shellItemGuid = typeof(NativeMethods.IShellItemImageFactory).GUID;
            NativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, shellItemGuid, out shellItemObj);

            if (shellItemObj is NativeMethods.IShellItemImageFactory imageFactory)
            {
                // Request a 256x256 icon for best quality, will be scaled down
                var size = new NativeMethods.SIZE(256, 256);
                
                // BIGGERSIZEOK allows returning a larger icon if available
                // ICONONLY ensures we get the icon, not a thumbnail
                int hr = imageFactory.GetImage(
                    size, 
                    NativeMethods.SIIGBF.SIIGBF_ICONONLY | NativeMethods.SIIGBF.SIIGBF_BIGGERSIZEOK, 
                    out hBitmap);

                if (hr == 0 && hBitmap != IntPtr.Zero)
                {
                    try
                    {
                        // Convert HBITMAP to BitmapSource
                        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        bitmapSource.Freeze();
                        return bitmapSource;
                    }
                    finally
                    {
                        // Delete bitmap AFTER creating BitmapSource
                        NativeMethods.DeleteObject(hBitmap);
                        hBitmap = IntPtr.Zero;
                    }
                }
            }
        }
        catch
        {
            // Failed to get icon via shell item
        }
        finally
        {
            // Clean up
            if (hBitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(hBitmap);
            }
            if (shellItemObj != null)
            {
                Marshal.ReleaseComObject(shellItemObj);
            }
        }

        return null;
    }

    /// <summary>
    /// Loads a PNG file as an ImageSource.
    /// </summary>
    private static ImageSource? LoadPngIcon(string pngPath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts an icon from a file at a specific index using ExtractIconEx.
    /// Useful for .ico files, .dll resources, and .exe files with multiple icons.
    /// </summary>
    private static ImageSource? ExtractIconFromFile(string filePath, int iconIndex)
    {
        try
        {
            IntPtr[] largeIcons = new IntPtr[1];
            IntPtr[] smallIcons = new IntPtr[1];

            // Try to extract the icon at the specified index
            uint result = NativeMethods.ExtractIconEx(filePath, iconIndex, largeIcons, smallIcons, 1);

            IntPtr hIcon = largeIcons[0] != IntPtr.Zero ? largeIcons[0] : smallIcons[0];

            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    var icon = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    icon.Freeze();
                    return icon;
                }
                finally
                {
                    // Clean up both icons
                    if (largeIcons[0] != IntPtr.Zero)
                        NativeMethods.DestroyIcon(largeIcons[0]);
                    if (smallIcons[0] != IntPtr.Zero)
                        NativeMethods.DestroyIcon(smallIcons[0]);
                }
            }
        }
        catch
        {
            // Ignore extraction errors
        }

        return null;
    }

    /// <summary>
    /// Gets icon using SHGetFileInfo API - works for most file types including UWP apps.
    /// This is the most reliable method for Microsoft Store apps and regular files.
    /// </summary>
    private static ImageSource? GetIconViaSHGetFileInfo(string path)
    {
        try
        {
            var shinfo = new NativeMethods.SHFILEINFO();
            
            // Use LARGEICON for better quality (32x32 instead of 16x16)
            uint flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON;
            
            IntPtr result = NativeMethods.SHGetFileInfo(
                path, 
                0, 
                ref shinfo, 
                (uint)Marshal.SizeOf(shinfo), 
                flags);

            if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var icon = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                icon.Freeze();
                return icon;
            }
            finally
            {
                NativeMethods.DestroyIcon(shinfo.hIcon);
            }
        }
        catch
        {
            return null;
        }
    }
}
