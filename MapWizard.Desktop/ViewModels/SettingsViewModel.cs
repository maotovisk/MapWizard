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
using Velopack;

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
    private bool _isLoadingMainSettings;
    private int _updateStatusRequestId;
    private UpdateInfo? _availableUpdate;
    private bool _isUpdateActionRunning;

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

    [ObservableProperty]
    private int _hitSoundVisualizerSongVolumePercent = 80;

    [ObservableProperty]
    private int _hitSoundVisualizerHitSoundVolumePercent = 100;

    public string ConfigDirectoryPath { get; } = settingsService.ConfigDirectoryPath;
    public UpdateStream[] UpdateStreams { get; } = [UpdateStream.Release, UpdateStream.PreRelease];

    public void Initialize()
    {
        LoadMainSettingsValues();
        UpdateThemeState(themeService.IsDarkTheme);
        themeService.DarkThemeChanged += OnDarkThemeChanged;
        UpdateStream = updateService.CurrentStream;
        InitializeSongsPath();
        _ = RefreshUpdateStreamBadgeAsync();
    }

    public void RefreshPersistedValues()
    {
        LoadMainSettingsValues();
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
            : "Folder not found. Map Picker will use manual picker fallback.";
    }

    partial void OnHitSoundVisualizerSongVolumePercentChanged(int value)
    {
        HitSoundVisualizerSongVolumePercent = Math.Clamp(value, 0, 100);
        if (_isLoadingMainSettings)
        {
            return;
        }

        SaveHitSoundVisualizerVolumeDefaults();
    }

    partial void OnHitSoundVisualizerHitSoundVolumePercentChanged(int value)
    {
        HitSoundVisualizerHitSoundVolumePercent = Math.Clamp(value, 0, 100);
        if (_isLoadingMainSettings)
        {
            return;
        }

        SaveHitSoundVisualizerVolumeDefaults();
    }

    [RelayCommand]
    private async Task RestartToApplyUpdate(CancellationToken cancellationToken)
    {
        if (_isUpdateActionRunning)
        {
            return;
        }

        if (updateService.IsRestartRequired)
        {
            if (!updateService.RestartToApplyPendingUpdate())
            {
                await RefreshUpdateStreamBadgeAsync();
            }

            return;
        }

        if (_availableUpdate is null)
        {
            await RefreshUpdateStreamBadgeAsync();
            return;
        }

        _isUpdateActionRunning = true;
        CanRestartToApplyUpdate = false;

        try
        {
            UpdateStreamBadgeText = $"Downloading {_availableUpdate.TargetFullRelease.Version}...";
            await updateService.DownloadUpdatesAsync(_availableUpdate, null, cancellationToken);
            _availableUpdate = null;
            await RefreshUpdateStreamBadgeAsync();
        }
        catch (OperationCanceledException)
        {
            await RefreshUpdateStreamBadgeAsync();
        }
        catch
        {
            UpdateStreamBadgeText = "Could not download update right now.";
            CanRestartToApplyUpdate = true;
        }
        finally
        {
            _isUpdateActionRunning = false;
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
        _availableUpdate = null;
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
                : $"Update {update.TargetFullRelease.Version} is available. Click to download.";
            _availableUpdate = update;
            CanRestartToApplyUpdate = update is not null;
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

    private void LoadMainSettingsValues()
    {
        _isLoadingMainSettings = true;
        try
        {
            var settings = settingsService.GetMainSettings();
            HitSoundVisualizerSongVolumePercent = Math.Clamp(settings.HitSoundVisualizerSongVolumePercent, 0, 100);
            HitSoundVisualizerHitSoundVolumePercent = Math.Clamp(settings.HitSoundVisualizerHitSoundVolumePercent, 0, 100);
        }
        finally
        {
            _isLoadingMainSettings = false;
        }
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

    private void SaveHitSoundVisualizerVolumeDefaults()
    {
        var settings = settingsService.GetMainSettings();
        var songVolume = Math.Clamp(HitSoundVisualizerSongVolumePercent, 0, 100);
        var hitsoundVolume = Math.Clamp(HitSoundVisualizerHitSoundVolumePercent, 0, 100);

        if (settings.HitSoundVisualizerSongVolumePercent == songVolume &&
            settings.HitSoundVisualizerHitSoundVolumePercent == hitsoundVolume)
        {
            return;
        }

        settings.HitSoundVisualizerSongVolumePercent = songVolume;
        settings.HitSoundVisualizerHitSoundVolumePercent = hitsoundVolume;
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
