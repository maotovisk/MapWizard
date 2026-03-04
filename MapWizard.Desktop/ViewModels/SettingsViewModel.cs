using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models.Settings;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.Playback;
using Velopack;

namespace MapWizard.Desktop.ViewModels;

public partial class SettingsViewModel(
    IThemeService themeService,
    ISettingsService settingsService,
    IFilesService filesService,
    ISongLibraryService songLibraryService,
    IUpdateService updateService,
    IAudioPlaybackService audioPlaybackService) : ViewModelBase
{
    private bool _isUpdatingFromThemeService;
    private bool _isUpdatingSongsPath;
    private bool _isLoadingMainSettings;
    private int _updateStatusRequestId;
    private UpdateInfo? _availableUpdate;
    private bool _isUpdateActionRunning;
    
    [ObservableProperty]
    private ThemeMode _selectedThemeMode;
    
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
    private bool _isHitSoundVisualizerEnabled;

    [ObservableProperty]
    private int _audioPreviewSongVolumePercent = 80;

    [ObservableProperty]
    private int _audioPreviewHitSoundVolumePercent = 100;

    [ObservableProperty]
    private IReadOnlyList<AudioOutputDeviceOption> _audioOutputDevices = Array.Empty<AudioOutputDeviceOption>();

    [ObservableProperty]
    private AudioOutputDeviceOption? _selectedAudioOutputDevice;

    [ObservableProperty]
    private string _audioOutputDeviceStatusText = "Using system default output device.";

    public string ConfigDirectoryPath { get; } = settingsService.ConfigDirectoryPath;
    public UpdateStream[] UpdateStreams { get; } = [UpdateStream.Release, UpdateStream.PreRelease];
    public ThemeMode[] ThemeModes { get; } = [ThemeMode.System, ThemeMode.Light, ThemeMode.Dark];

    public void Initialize()
    {
        LoadMainSettingsValues();
        UpdateThemeState(themeService.ThemeMode);
        themeService.ThemeModeChanged += OnThemeModeChanged;
        UpdateStream = updateService.CurrentStream;
        InitializeSongsPath();
        LoadAudioOutputDevices();
        _ = RefreshUpdateStreamBadgeAsync();
    }

    public void RefreshPersistedValues()
    {
        LoadMainSettingsValues();
    }

    private void OnThemeModeChanged(object? sender, ThemeMode themeMode)
    {
        UpdateThemeState(themeMode);
    }

    partial void OnSelectedThemeModeChanged(ThemeMode value)
    {
        if (_isUpdatingFromThemeService)
        {
            return;
        }

        themeService.SetThemeMode(value);
    }

    private void UpdateThemeState(ThemeMode themeMode)
    {
        _isUpdatingFromThemeService = true;
        SelectedThemeMode = themeMode;
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

    partial void OnIsHitSoundVisualizerEnabledChanged(bool value)
    {
        if (_isLoadingMainSettings)
        {
            return;
        }

        SaveHitSoundVisualizerEnabled(value);
    }

    partial void OnAudioPreviewSongVolumePercentChanged(int value)
    {
        AudioPreviewSongVolumePercent = Math.Clamp(value, 0, 100);
        if (_isLoadingMainSettings)
        {
            return;
        }

        SaveAudioPreviewVolumeDefaults();
    }

    partial void OnAudioPreviewHitSoundVolumePercentChanged(int value)
    {
        AudioPreviewHitSoundVolumePercent = Math.Clamp(value, 0, 100);
        if (_isLoadingMainSettings)
        {
            return;
        }

        SaveAudioPreviewVolumeDefaults();
    }

    partial void OnSelectedAudioOutputDeviceChanged(AudioOutputDeviceOption? value)
    {
        if (_isLoadingMainSettings || value is null)
        {
            return;
        }

        var applied = false;
        try
        {
            applied = audioPlaybackService.SetSelectedAudioOutputDevice(value.Id);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            applied = false;
        }

        if (!applied)
        {
            AudioOutputDeviceStatusText = "Could not switch audio output device. Reverting selection.";
            LoadAudioOutputDevices();
            return;
        }

        SaveAudioOutputDevice(value.Id);
        AudioOutputDeviceStatusText = $"Using {value.DisplayName}.";
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
        catch (OperationCanceledException ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            await RefreshUpdateStreamBadgeAsync();
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
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
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
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
            IsHitSoundVisualizerEnabled = settings.EnableHitSoundVisualizer;
            AudioPreviewSongVolumePercent = Math.Clamp(settings.AudioPreviewSongVolumePercent, 0, 100);
            AudioPreviewHitSoundVolumePercent = Math.Clamp(settings.AudioPreviewHitSoundVolumePercent, 0, 100);
        }
        finally
        {
            _isLoadingMainSettings = false;
        }
    }

    private void LoadAudioOutputDevices()
    {
        _isLoadingMainSettings = true;
        try
        {
            var settings = settingsService.GetMainSettings();
            var devices = audioPlaybackService.GetAudioOutputDevices();
            AudioOutputDevices = devices;

            var preferredId = string.IsNullOrWhiteSpace(settings.AudioOutputDeviceId)
                ? audioPlaybackService.GetSelectedAudioOutputDeviceId()
                : settings.AudioOutputDeviceId;

            var selected = devices.FirstOrDefault(x => string.Equals(x.Id, preferredId, StringComparison.Ordinal))
                ?? devices.FirstOrDefault(x => string.Equals(x.Id, audioPlaybackService.GetSelectedAudioOutputDeviceId(), StringComparison.Ordinal))
                ?? devices.FirstOrDefault();

            if (selected is not null)
            {
                SelectedAudioOutputDevice = selected;
                AudioOutputDeviceStatusText = $"Using {selected.DisplayName}.";
            }
            else
            {
                AudioOutputDeviceStatusText = "No audio output devices available.";
            }
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

    private void SaveHitSoundVisualizerEnabled(bool enabled)
    {
        var settings = settingsService.GetMainSettings();
        if (settings.EnableHitSoundVisualizer == enabled)
        {
            return;
        }

        settings.EnableHitSoundVisualizer = enabled;
        settingsService.SaveMainSettings(settings);
    }

    private void SaveAudioPreviewVolumeDefaults()
    {
        var settings = settingsService.GetMainSettings();
        var songVolume = Math.Clamp(AudioPreviewSongVolumePercent, 0, 100);
        var hitsoundVolume = Math.Clamp(AudioPreviewHitSoundVolumePercent, 0, 100);

        if (settings.AudioPreviewSongVolumePercent == songVolume &&
            settings.AudioPreviewHitSoundVolumePercent == hitsoundVolume)
        {
            return;
        }

        settings.AudioPreviewSongVolumePercent = songVolume;
        settings.AudioPreviewHitSoundVolumePercent = hitsoundVolume;
        settingsService.SaveMainSettings(settings);
    }

    private void SaveAudioOutputDevice(string deviceId)
    {
        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? "default" : deviceId.Trim();
        var settings = settingsService.GetMainSettings();
        if (string.Equals(settings.AudioOutputDeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            return;
        }

        settings.AudioOutputDeviceId = normalizedDeviceId;
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
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return trimmed;
        }
    }

}
