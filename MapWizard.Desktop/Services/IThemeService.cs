using System;
using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

public interface IThemeService
{
    ThemeMode ThemeMode { get; }
    ThemePalette ColorPalette { get; }
    bool IsDarkTheme { get; }
    event EventHandler<bool>? DarkThemeChanged;
    event EventHandler<ThemeMode>? ThemeModeChanged;
    event EventHandler<ThemePalette>? ColorPaletteChanged;
    void Initialize();
    void SetThemeMode(ThemeMode themeMode);
    void SetColorPalette(ThemePalette colorPalette);
}
