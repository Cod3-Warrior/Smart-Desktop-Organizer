using System.Collections.Generic;
using System.Threading.Tasks;
using SmartDesktop.Core.Models;

namespace SmartDesktop.Core.Interfaces;

public interface IDesktopService
{
    Task<IEnumerable<DesktopItem>> GetDesktopItemsAsync();
    Task RefreshDesktopAsync();
    Task SaveLayoutAsync(IEnumerable<DesktopItem> items);
    Task<IEnumerable<DesktopItem>> LoadLayoutAsync();
    string GetWallpaperPath();
}
