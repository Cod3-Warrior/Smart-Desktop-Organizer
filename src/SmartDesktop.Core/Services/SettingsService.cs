using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SmartDesktop.Core.Interfaces;
using SmartDesktop.Core.Models;

namespace SmartDesktop.Core.Services;

/// <summary>
/// JSON-based settings persistence service.
/// Thread-safe with debounced saves for live preview performance.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _current;
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 300;
    
    public AppSettings Current => _current;
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    
    public string SettingsFilePath { get; }
    
    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDir = Path.Combine(appDataPath, "SmartDesktopOrganizer");
        SettingsFilePath = Path.Combine(settingsDir, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        _current = new AppSettings();
    }
    
    public async Task<AppSettings> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                _current = new AppSettings();
                return _current;
            }
            
            var json = await File.ReadAllTextAsync(SettingsFilePath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            return _current;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Load error: {ex.Message}");
            _current = new AppSettings();
            return _current;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public Task SaveAsync() => SaveAsync(_current);
    
    public async Task SaveAsync(AppSettings settings)
    {
        // Cancel any pending debounced save
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        
        try
        {
            // Debounce for live preview performance
            await Task.Delay(DebounceDelayMs, token);
            
            await _lock.WaitAsync(token);
            try
            {
                _current = settings;
                
                // Ensure directory exists
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(SettingsFilePath, json, token);
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled, new save incoming
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Save error: {ex.Message}");
        }
    }
    
    public async Task ResetToDefaultsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _current = new AppSettings();
            
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
            
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("*"));
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public void NotifySettingChanged(string propertyPath)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(propertyPath));
    }
}
