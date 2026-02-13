using System;

namespace MapWizard.Desktop.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    event EventHandler<bool>? DarkThemeChanged;
    void Initialize();
    void SetDarkTheme(bool isDarkTheme);
}
