using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MapWizard.Desktop.Models.Settings;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.ViewModels;

public partial class SettingsViewModel(
    IThemeService themeService,
    ISettingsService settingsService,
    IUpdateService updateService) : ViewModelBase
{
    private bool _isUpdatingFromThemeService;
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

    public string ConfigDirectoryPath { get; } = settingsService.ConfigDirectoryPath;
    public UpdateStream[] UpdateStreams { get; } = [UpdateStream.Release, UpdateStream.PreRelease];

    public void Initialize()
    {
        UpdateThemeState(themeService.IsDarkTheme);
        themeService.DarkThemeChanged += OnDarkThemeChanged;
        UpdateStream = updateService.CurrentStream;
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

    [RelayCommand]
    private void RestartToApplyUpdate()
    {
        if (!updateService.RestartToApplyPendingUpdate())
        {
            _ = RefreshUpdateStreamBadgeAsync();
        }
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
}
