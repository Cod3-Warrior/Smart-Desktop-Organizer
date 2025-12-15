using Microsoft.Extensions.DependencyInjection;
using SmartDesktop.Core;
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Services;
using SmartDesktopOrganizer.ViewModels;
using System;
using System.Windows;

namespace SmartDesktopOrganizer;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        try
        {
            Services = ConfigureServices();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("startup_error.log", $"{ex.Message}\n\n{ex.StackTrace}\n\nInner: {ex.InnerException?.Message}");
            throw;
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<IFileSystem, FileSystemWrapper>();
        services.AddSingleton<IDesktopService, DesktopService>();
        services.AddSingleton<IFolderNamingService, FolderNamingService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Add global exception handler for debugging
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Unhandled exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
