using System.Collections.Generic;
using System.Windows.Media;

namespace SmartDesktop.Core.Models;

public class DesktopItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource? Icon { get; set; }
    public long Size { get; set; }
    
    // Folder support
    public bool IsFolder { get; set; }
    public bool IsEnlarged { get; set; } = true; // Default to enlarged mode
    public List<DesktopItem> InnerItems { get; set; } = new();
}
