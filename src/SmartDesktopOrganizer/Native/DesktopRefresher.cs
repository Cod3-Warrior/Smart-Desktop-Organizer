using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDesktopOrganizer.Native
{
    public class DesktopRefresher
    {
        // P/Invoke Definitions

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_FLUSH = 0x1000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint LVM_FIRST = 0x1000;
        private const uint LVM_UPDATE = LVM_FIRST + 42;
        private const uint LVM_REDRAWITEMS = LVM_FIRST + 21;

        /// <summary>
        /// Forces a desktop refresh with both SHELL changes and direct ListView invalidation.
        /// Aims for < 300ms execution.
        /// </summary>
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                // 1. Notify Shell of association changes (Triggers icon cache refresh)
                // Using Task.Run to offload P/Invoke if it blocks, though SHChangeNotify is usually fast.
                await Task.Run(() =>
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                }, cancellationToken);

                // 2. Direct SysListView32 Invalidation (The "Instant" part)
                // Find the Desktop ListView
                IntPtr hDesktopListView = FindDesktopListView();

                if (hDesktopListView != IntPtr.Zero)
                {
                    // Force redraw
                    InvalidateRect(hDesktopListView, IntPtr.Zero, true);
                    UpdateWindow(hDesktopListView);
                    
                    // Optional: LVM_REDRAWITEMS if InvalidateRect isn't enough for specific item states
                    // Sending generic update just in case
                    SendMessage(hDesktopListView, LVM_UPDATE, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    Console.WriteLine("[DesktopRefresher] Warning: Could not find Desktop SysListView32 handle.");
                }

                sw.Stop();
                Console.WriteLine($"[DesktopRefresher] Refresh completed in {sw.ElapsedMilliseconds}ms.");

                if (sw.ElapsedMilliseconds > 300)
                {
                    Console.WriteLine("[DesktopRefresher] Warning: Refresh took longer than 300ms!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DesktopRefresher] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Locates the SysListView32 control that represents the Desktop icons.
        /// Handles the difference between standard wallpaper and active desktop (WorkerW).
        /// </summary>
        private IntPtr FindDesktopListView()
        {
            // Case 1: Standard Desktop (Progman -> SHELLDLL_DefView -> SysListView32)
            IntPtr progman = FindWindow("Progman", null!);
            IntPtr shellDllDefView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null!);
            
            if (shellDllDefView != IntPtr.Zero)
            {
                return FindWindowEx(shellDllDefView, IntPtr.Zero, "SysListView32", null!);
            }

            // Case 2: Wallpaper/Active Desktop (WorkerW -> SHELLDLL_DefView -> SysListView32)
            // We need to iterate over WorkerW windows to find the one containing SHELLDLL_DefView
            IntPtr foundListView = IntPtr.Zero;

            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shell = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null!);
                if (shell != IntPtr.Zero)
                {
                    foundListView = FindWindowEx(shell, IntPtr.Zero, "SysListView32", null!);
                    return false; // Stop enumeration
                }
                return true;
            }, IntPtr.Zero);

            return foundListView;
        }

        /// <summary>
        /// Checks if the current process is running with Administrator privileges.
        /// </summary>
        public bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
