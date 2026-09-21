using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using MapWizard.Desktop.Models.Settings;
using MapWizard.Theme;
using MapWizard.Theme.Palettes;

namespace MapWizard.Desktop.Services;

/// <summary>
/// Applies application settings to the visual tokens exposed by MapWizard.Theme.
/// Persistence and system-theme behavior remain in the application domain.
/// </summary>
public sealed class ThemeService(ISettingsService settingsService) : IThemeService
{
    private ThemeMode _themeMode;
    private ThemePalette _colorPalette;
    private bool _isInitialized;
    private bool _hookedActualThemeVariant;

    public ThemeMode ThemeMode => _themeMode;
    public ThemePalette ColorPalette => _colorPalette;
    public bool IsDarkTheme { get; private set; }

    public event EventHandler<bool>? DarkThemeChanged;
    public event EventHandler<ThemeMode>? ThemeModeChanged;
    public event EventHandler<ThemePalette>? ColorPaletteChanged;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        HookActualThemeVariant();

        var settings = settingsService.GetMainSettings();
        _colorPalette = NormalizePalette(settings.ColorPalette);
        ApplyThemeMode(settings.ThemeMode, persist: false, notify: true);
    }

    public void SetThemeMode(ThemeMode themeMode) =>
        ApplyThemeMode(themeMode, persist: true, notify: true);

    public void SetColorPalette(ThemePalette colorPalette)
    {
        colorPalette = NormalizePalette(colorPalette);
        if (_colorPalette == colorPalette)
        {
            return;
        }

        _colorPalette = colorPalette;
        ApplyPaletteResources(colorPalette, IsDarkTheme);

        var settings = settingsService.GetMainSettings();
        settings.ColorPalette = colorPalette;
        settingsService.SaveMainSettings(settings);
        ColorPaletteChanged?.Invoke(this, colorPalette);
    }

    private void ApplyThemeMode(ThemeMode themeMode, bool persist, bool notify)
    {
        _themeMode = themeMode;
        var targetVariant = ToThemeVariant(themeMode);
        ApplyRequestedThemeVariant(targetVariant);

        IsDarkTheme = ResolveIsDarkTheme(targetVariant);
        ApplyPaletteResources(_colorPalette, IsDarkTheme);

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

    private void HookActualThemeVariant()
    {
        if (_hookedActualThemeVariant || Application.Current is null)
        {
            return;
        }

        _hookedActualThemeVariant = true;
        Application.Current.ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (_themeMode == ThemeMode.System)
        {
            ApplyThemeMode(ThemeMode.System, persist: false, notify: true);
        }
    }

    private static void ApplyPaletteResources(ThemePalette palette, bool isDark)
    {
        var appResources = Application.Current?.Resources;
        if (appResources is null)
        {
            return;
        }

        var paletteResources = MapWizardPaletteCatalog.GetResources(ToCatalogPalette(palette), isDark);
        var variant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        ApplyVariantResources(appResources, variant, paletteResources);

        // Brushes declared inside the style package resolve their colors from
        // that package first, so update its dictionary as well as app resources.
        foreach (var style in Application.Current!.Styles)
        {
            if (style is MapWizardTheme mapWizardTheme)
            {
                ApplyVariantResources(mapWizardTheme.Resources, variant, paletteResources);
            }
        }
    }

    private static void ApplyVariantResources(
        IResourceDictionary root,
        ThemeVariant variant,
        IReadOnlyDictionary<string, object> values)
    {
        if (!root.ThemeDictionaries.TryGetValue(variant, out var provider) ||
            provider is not IResourceDictionary resources)
        {
            var newResources = new ResourceDictionary();
            root.ThemeDictionaries[variant] = newResources;
            resources = newResources;
        }

        ApplyResources(resources, values);
    }

    private static void ApplyResources(
        IResourceDictionary resources,
        IReadOnlyDictionary<string, object> values)
    {
        foreach (var (key, value) in values)
        {
            resources[key] = value;
        }
    }

    public static ThemeVariant ToThemeVariant(ThemeMode themeMode) => themeMode switch
    {
        ThemeMode.Dark => ThemeVariant.Dark,
        ThemeMode.Light => ThemeVariant.Light,
        _ => ThemeVariant.Default
    };

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

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    }

    private static ThemePalette NormalizePalette(ThemePalette palette) =>
        Enum.IsDefined(palette) ? palette : ThemePalette.MapWizardNoir;

    private static MapWizardPalette ToCatalogPalette(ThemePalette palette) => palette switch
    {
        ThemePalette.MapWizardClassic => MapWizardPalette.Classic,
        ThemePalette.Gruvbox => MapWizardPalette.Gruvbox,
        _ => MapWizardPalette.Noir
    };
}
