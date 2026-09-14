using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using MapWizard.Desktop.Models.Settings;
using SukiUI;
using SukiUI.Models;

namespace MapWizard.Desktop.Services;

public class ThemeService(ISettingsService settingsService) : IThemeService
{
    private const string MapWizardDarkThemeName = "MapWizard Dark";
    private const string MapWizardLightThemeName = "MapWizard Light";

    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    private SukiColorTheme? _mapWizardDarkTheme;
    private SukiColorTheme? _mapWizardLightTheme;
    private ThemeMode _themeMode;

    public ThemeMode ThemeMode => _themeMode;
    public bool IsDarkTheme { get; private set; }

    public event EventHandler<bool>? DarkThemeChanged;
    public event EventHandler<ThemeMode>? ThemeModeChanged;

    public void Initialize()
    {
        var settings = settingsService.GetMainSettings();
        ApplyThemeMode(settings.ThemeMode, persist: false, notify: true);
    }

    public void SetThemeMode(ThemeMode themeMode)
    {
        ApplyThemeMode(themeMode, persist: true, notify: true);
    }

    public void SetDarkTheme(bool isDarkTheme)
    {
        ApplyThemeMode(isDarkTheme ? ThemeMode.Dark : ThemeMode.Light, persist: true, notify: true);
    }

    private void ApplyThemeMode(ThemeMode themeMode, bool persist, bool notify)
    {
        EnsureCustomColorTheme();

        var targetVariant = themeMode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };

        _theme.ChangeBaseTheme(targetVariant);
        ApplyRequestedThemeVariant(targetVariant);
        var isDarkTheme = ResolveIsDarkTheme(targetVariant);
        _theme.ChangeColorTheme(isDarkTheme ? _mapWizardDarkTheme! : _mapWizardLightTheme!);

        _themeMode = themeMode;
        IsDarkTheme = isDarkTheme;

        if (persist)
        {
            var settings = settingsService.GetMainSettings();
            settings.ThemeMode = themeMode;
            settingsService.SaveMainSettings(settings);
        }

        if (notify)
        {
            ThemeModeChanged?.Invoke(this, themeMode);
            DarkThemeChanged?.Invoke(this, IsDarkTheme);
        }
    }

    private void EnsureCustomColorTheme()
    {
        if (_mapWizardDarkTheme != null)
        {
            return;
        }

        _mapWizardDarkTheme = new SukiColorTheme(MapWizardDarkThemeName, Color.Parse("#B8DB87"), Color.Parse("#FAB283"));
        _mapWizardLightTheme = new SukiColorTheme(MapWizardLightThemeName, Color.Parse("#5F7488"), Color.Parse("#758B9E"));
        _theme.AddColorTheme(_mapWizardDarkTheme);
        _theme.AddColorTheme(_mapWizardLightTheme);
    }

    private static void ApplyRequestedThemeVariant(ThemeVariant targetVariant)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = targetVariant;

        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                window.RequestedThemeVariant = targetVariant;
            }
        }
    }

    private static bool ResolveIsDarkTheme(ThemeVariant requestedVariant)
    {
        if (requestedVariant == ThemeVariant.Dark)
        {
            return true;
        }

        if (requestedVariant == ThemeVariant.Light)
        {
            return false;
        }

        var actualVariant = Application.Current?.ActualThemeVariant;
        return actualVariant == ThemeVariant.Dark;
    }
}
