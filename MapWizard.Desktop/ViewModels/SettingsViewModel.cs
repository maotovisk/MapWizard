using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.ViewModels;

public partial class SettingsViewModel(IThemeService themeService, ISettingsService settingsService) : ViewModelBase
{
    private bool _isUpdatingFromThemeService;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private MaterialIconKind _themeToggleIcon;

    public string ConfigDirectoryPath { get; } = settingsService.ConfigDirectoryPath;

    public void Initialize()
    {
        UpdateThemeState(themeService.IsDarkTheme);
        themeService.DarkThemeChanged += OnDarkThemeChanged;
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
}
