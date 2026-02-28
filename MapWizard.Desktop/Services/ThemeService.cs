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
    private const string MapWizardThemeName = "MapWizard";

    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    private SukiColorTheme? _mapWizardTheme;
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
        _theme.ChangeColorTheme(_mapWizardTheme!);
        ApplyRequestedThemeVariant(targetVariant);

        _themeMode = themeMode;
        IsDarkTheme = ResolveIsDarkTheme(targetVariant);

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
        if (_mapWizardTheme != null)
        {
            return;
        }

        _mapWizardTheme = new SukiColorTheme(MapWizardThemeName, Colors.DarkSlateBlue, Colors.OrangeRed);
        _theme.AddColorTheme(_mapWizardTheme);
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
