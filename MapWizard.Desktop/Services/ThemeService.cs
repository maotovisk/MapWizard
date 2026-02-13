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
    private readonly MainSettings _mainSettings = settingsService.GetMainSettings();

    private SukiColorTheme? _mapWizardTheme;

    public bool IsDarkTheme { get; private set; }

    public event EventHandler<bool>? DarkThemeChanged;

    public void Initialize()
    {
        ApplyTheme(_mainSettings.DarkMode, persist: false, notify: true);
    }

    public void SetDarkTheme(bool isDarkTheme)
    {
        ApplyTheme(isDarkTheme, persist: true, notify: true);
    }

    private void ApplyTheme(bool isDarkTheme, bool persist, bool notify)
    {
        EnsureCustomColorTheme();

        var targetVariant = isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

        _theme.ChangeBaseTheme(targetVariant);
        _theme.ChangeColorTheme(_mapWizardTheme!);
        ApplyRequestedThemeVariant(targetVariant);

        IsDarkTheme = isDarkTheme;

        if (persist)
        {
            _mainSettings.DarkMode = isDarkTheme;
            settingsService.SaveMainSettings(_mainSettings);
        }

        if (notify)
        {
            DarkThemeChanged?.Invoke(this, isDarkTheme);
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
}
