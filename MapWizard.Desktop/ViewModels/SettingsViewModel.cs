using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MapWizard.Desktop.Models.Settings;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.ViewModels;

public partial class SettingsViewModel(
    IThemeService themeService,
    ISettingsService settingsService,
    IFilesService filesService,
    ISongLibraryService songLibraryService,
    IUpdateService updateService) : ViewModelBase
{
    private bool _isUpdatingFromThemeService;
    private bool _isUpdatingSongsPath;
    private int _updateStatusRequestId;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private MaterialIconKind _themeToggleIcon;

    [ObservableProperty]
    private UpdateStream _updateStream;

    [ObservableProperty]
    private string _updateStreamBadgeText = "Checking updates...";

    [ObservableProperty]
    private bool _canRestartToApplyUpdate;

    [ObservableProperty]
    private string _songsPath = string.Empty;

    [ObservableProperty]
    private string _songsPathStatusText = "Songs folder not configured.";

    public string ConfigDirectoryPath { get; } = settingsService.ConfigDirectoryPath;
    public UpdateStream[] UpdateStreams { get; } = [UpdateStream.Release, UpdateStream.PreRelease];

    public void Initialize()
    {
        UpdateThemeState(themeService.IsDarkTheme);
        themeService.DarkThemeChanged += OnDarkThemeChanged;
        UpdateStream = updateService.CurrentStream;
        InitializeSongsPath();
        _ = RefreshUpdateStreamBadgeAsync();
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeToggleIcon = value ? MaterialIconKind.WeatherNight : MaterialIconKind.WhiteBalanceSunny;
        if (_isUpdatingFromThemeService)
        {
            return;
        }

        themeService.SetDarkTheme(value);
    }

    private void OnDarkThemeChanged(object? sender, bool isDarkTheme)
    {
        UpdateThemeState(isDarkTheme);
    }

    private void UpdateThemeState(bool isDarkTheme)
    {
        _isUpdatingFromThemeService = true;
        ThemeToggleIcon = isDarkTheme ? MaterialIconKind.WeatherNight : MaterialIconKind.WhiteBalanceSunny;
        IsDarkTheme = isDarkTheme;
        _isUpdatingFromThemeService = false;
    }

    partial void OnUpdateStreamChanged(UpdateStream value)
    {
        updateService.SetUpdateStream(value);
        _ = RefreshUpdateStreamBadgeAsync();
    }

    partial void OnSongsPathChanged(string value)
    {
        if (_isUpdatingSongsPath)
        {
            return;
        }

        var normalized = NormalizePath(value);
        SaveSongsPath(normalized);
        SongsPathStatusText = songLibraryService.IsValidSongsPath(normalized)
            ? "Using configured Songs folder."
            : "Folder not found. Song Select will use manual picker fallback.";
    }

    [RelayCommand]
    private void RestartToApplyUpdate()
    {
        if (!updateService.RestartToApplyPendingUpdate())
        {
            _ = RefreshUpdateStreamBadgeAsync();
        }
    }

    [RelayCommand]
    private async Task PickSongsFolder(CancellationToken cancellationToken)
    {
        var startPath = songLibraryService.IsValidSongsPath(SongsPath)
            ? SongsPath
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var suggestedFolder = await filesService.TryGetFolderFromPathAsync(startPath);
        var folders = await filesService.OpenFolderAsync(new FolderPickerOpenOptions
        {
            Title = "Select osu! Songs folder",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder
        });

        if (cancellationToken.IsCancellationRequested || folders.Count == 0)
        {
            return;
        }

        var selectedPath = NormalizePath(folders[0].Path.LocalPath);
        SetSongsPath(selectedPath, persist: true);
        SongsPathStatusText = songLibraryService.IsValidSongsPath(selectedPath)
            ? "Using selected Songs folder."
            : "Selected folder was saved but does not exist.";
    }

    [RelayCommand]
    private void AutoDetectSongsFolder()
    {
        var detectedPath = songLibraryService.TryDetectSongsPath();
        if (songLibraryService.IsValidSongsPath(detectedPath))
        {
            SetSongsPath(NormalizePath(detectedPath), persist: true);
            SongsPathStatusText = "Auto-detected Songs folder.";
            return;
        }

        SongsPathStatusText = "Could not auto-detect Songs folder.";
    }

    private async Task RefreshUpdateStreamBadgeAsync()
    {
        var requestId = Interlocked.Increment(ref _updateStatusRequestId);
        CanRestartToApplyUpdate = false;
        UpdateStreamBadgeText = $"Checking {GetCurrentStreamLabel()} stream...";

        if (!updateService.IsInstalled)
        {
            UpdateStreamBadgeText = "Updates unavailable in local builds.";
            return;
        }

        if (updateService.IsRestartRequired)
        {
            UpdateStreamBadgeText = "Update downloaded. Click to restart and apply.";
            CanRestartToApplyUpdate = true;
            return;
        }

        try
        {
            var update = await updateService.CheckForUpdatesAsync();
            if (requestId != _updateStatusRequestId)
            {
                return;
            }

            UpdateStreamBadgeText = update == null
                ? $"You're on the latest {GetCurrentStreamLabel()} update."
                : $"Update {update.TargetFullRelease.Version} is available.";
        }
        catch
        {
            if (requestId != _updateStatusRequestId)
            {
                return;
            }

            UpdateStreamBadgeText = "Could not check updates right now.";
        }
    }

    private string GetCurrentStreamLabel()
    {
        return UpdateStream == UpdateStream.PreRelease ? "pre-release" : "release";
    }

    private void InitializeSongsPath()
    {
        var settings = settingsService.GetMainSettings();
        var configuredPath = NormalizePath(settings.SongsPath);

        if (songLibraryService.IsValidSongsPath(configuredPath))
        {
            SetSongsPath(configuredPath, persist: false);
            SongsPathStatusText = "Using configured Songs folder.";
            return;
        }

        var detectedPath = NormalizePath(songLibraryService.TryDetectSongsPath());
        if (songLibraryService.IsValidSongsPath(detectedPath))
        {
            SetSongsPath(detectedPath, persist: true);
            SongsPathStatusText = "Auto-detected Songs folder.";
            return;
        }

        SetSongsPath(configuredPath, persist: false);
        SongsPathStatusText = "Songs folder not found. Use Browse or Auto Detect.";
    }

    private void SetSongsPath(string path, bool persist)
    {
        _isUpdatingSongsPath = true;
        SongsPath = path;
        _isUpdatingSongsPath = false;

        if (persist)
        {
            SaveSongsPath(path);
        }
    }

    private void SaveSongsPath(string path)
    {
        var settings = settingsService.GetMainSettings();
        if (string.Equals(settings.SongsPath, path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        settings.SongsPath = path;
        settingsService.SaveMainSettings(settings);
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch
        {
            return trimmed;
        }
    }
}
