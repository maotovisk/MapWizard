using System;
using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

public interface IThemeService
{
    ThemeMode ThemeMode { get; }
    bool IsDarkTheme { get; }
    event EventHandler<bool>? DarkThemeChanged;
    event EventHandler<ThemeMode>? ThemeModeChanged;
    void Initialize();
    void SetThemeMode(ThemeMode themeMode);
    void SetDarkTheme(bool isDarkTheme);
}
