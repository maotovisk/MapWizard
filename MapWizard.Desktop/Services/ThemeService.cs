using System;
using System.Collections.Generic;
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
    private static readonly string[] SukiBaseResourceKeys =
    [
        "SukiBackground", "SukiStrongBackground", "SukiLightBackground", "SukiCardBackground",
        "SukiPopupBackground", "SukiGlassCardBackground", "SukiGlassCardOpaqueBackground",
        "SukiControlTouchBackground", "SukiDialogBackground", "ControlSukiGlassCardBackground",
        "SukiBorderBrush", "SukiControlBorderBrush", "SukiMediumBorderBrush", "SukiLightBorderBrush",
        "SukiMenuBorderBrush", "GlassBorderBrush", "SukiText", "SukiLowText", "SukiMuteText",
        "SukiDisabledText", "SukiNeedleBrush"
    ];

    private static readonly IReadOnlyDictionary<string, object> NoirDarkResources = CreateNoirDarkResources();
    private static readonly IReadOnlyDictionary<string, object> NoirLightResources = CreateNoirLightResources();
    private static readonly IReadOnlyDictionary<string, object> ClassicDarkResources = CreateClassicDarkResources();
    private static readonly IReadOnlyDictionary<string, object> ClassicLightResources = CreateClassicLightResources();

    private readonly SukiTheme _theme = SukiTheme.GetInstance();
    private readonly Dictionary<(ThemePalette Palette, bool IsDark), SukiColorTheme> _colorThemes = [];
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
        EnsureColorThemes();

        var settings = settingsService.GetMainSettings();
        _colorPalette = NormalizePalette(settings.ColorPalette);
        ApplyPaletteResources(_colorPalette);
        ApplyThemeMode(settings.ThemeMode, persist: false, notify: true);
    }

    public void SetThemeMode(ThemeMode themeMode)
    {
        ApplyThemeMode(themeMode, persist: true, notify: true);
    }

    public void SetColorPalette(ThemePalette colorPalette)
    {
        colorPalette = NormalizePalette(colorPalette);
        if (_colorPalette == colorPalette)
        {
            return;
        }

        EnsureColorThemes();
        _colorPalette = colorPalette;
        ApplyPaletteResources(colorPalette);
        _theme.ChangeColorTheme(GetColorTheme(colorPalette, IsDarkTheme));

        var settings = settingsService.GetMainSettings();
        settings.ColorPalette = colorPalette;
        settingsService.SaveMainSettings(settings);

        ColorPaletteChanged?.Invoke(this, colorPalette);
    }

    private void ApplyThemeMode(ThemeMode themeMode, bool persist, bool notify)
    {
        EnsureColorThemes();

        _themeMode = themeMode;
        var targetVariant = ToThemeVariant(themeMode);
        _theme.ChangeBaseTheme(targetVariant);
        ApplyRequestedThemeVariant(targetVariant);

        var isDarkTheme = ResolveIsDarkTheme(targetVariant);
        _theme.ChangeColorTheme(GetColorTheme(_colorPalette, isDarkTheme));

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
        if (_themeMode != ThemeMode.System)
        {
            return;
        }

        var actualVariant = Application.Current?.ActualThemeVariant;
        if (actualVariant != ThemeVariant.Dark && actualVariant != ThemeVariant.Light)
        {
            return;
        }

        ApplyThemeMode(ThemeMode.System, persist: false, notify: true);
    }

    private void EnsureColorThemes()
    {
        if (_colorThemes.Count > 0)
        {
            return;
        }

        AddColorTheme(ThemePalette.MapWizardNoir, true, "MapWizard Noir Dark", "#B8DB87", "#FAB283");
        AddColorTheme(ThemePalette.MapWizardNoir, false, "MapWizard Noir Light", "#5F7488", "#758B9E");
        AddColorTheme(ThemePalette.MapWizardClassic, true, "MapWizard Classic Dark", "#483D8B", "#FF4500");
        AddColorTheme(ThemePalette.MapWizardClassic, false, "MapWizard Classic Light", "#483D8B", "#FF4500");
    }

    private void AddColorTheme(ThemePalette palette, bool isDark, string name, string primary, string accent)
    {
        var colorTheme = new SukiColorTheme(name, ParseColor(primary), ParseColor(accent));
        _colorThemes.Add((palette, isDark), colorTheme);
        _theme.AddColorTheme(colorTheme);
    }

    private SukiColorTheme GetColorTheme(ThemePalette palette, bool isDark)
    {
        return _colorThemes[(palette, isDark)];
    }

    private static void ApplyPaletteResources(ThemePalette palette)
    {
        var appResources = Application.Current?.Resources;
        if (appResources is null ||
            !appResources.ThemeDictionaries.TryGetValue(ThemeVariant.Dark, out var darkProvider) ||
            !appResources.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out var lightProvider) ||
            darkProvider is not IResourceDictionary darkResources ||
            lightProvider is not IResourceDictionary lightResources)
        {
            return;
        }

        if (palette == ThemePalette.MapWizardClassic)
        {
            RemoveSukiBaseOverrides(darkResources);
            RemoveSukiBaseOverrides(lightResources);
            ApplyResources(darkResources, ClassicDarkResources);
            ApplyResources(lightResources, ClassicLightResources);
            return;
        }

        ApplyResources(darkResources, NoirDarkResources);
        ApplyResources(lightResources, NoirLightResources);
    }

    private static void RemoveSukiBaseOverrides(IResourceDictionary resources)
    {
        foreach (var key in SukiBaseResourceKeys)
        {
            resources.Remove(key);
        }
    }

    private static void ApplyResources(IResourceDictionary resources, IReadOnlyDictionary<string, object> values)
    {
        foreach (var (key, value) in values)
        {
            resources[key] = value;
        }
    }

    public static ThemeVariant ToThemeVariant(ThemeMode themeMode)
    {
        return themeMode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
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

        return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    }

    private static IReadOnlyDictionary<string, object> CreateNoirDarkResources()
    {
        var values = CreateMapWizardResources("#B8DB87", "#8FAE63", "#C9E6A1", "#151A0E",
            "#1A1A1A", "#262626", "#F01A1A1A", "#EEEEEE");

        AddColors(values,
            ("SukiBackground", "Transparent"), ("SukiStrongBackground", "#0A0A0A"),
            ("SukiLightBackground", "Transparent"), ("SukiCardBackground", "#101010"),
            ("SukiPopupBackground", "#141414"), ("SukiGlassCardBackground", "#121212"),
            ("SukiGlassCardOpaqueBackground", "#101010"), ("SukiControlTouchBackground", "#1A1A1A"),
            ("SukiDialogBackground", "#0A0A0A"), ("ControlSukiGlassCardBackground", "#1A1A1A"),
            ("SukiBorderBrush", "#232323"), ("SukiControlBorderBrush", "#303030"),
            ("SukiMediumBorderBrush", "#2A2A2A"), ("SukiLightBorderBrush", "#202020"),
            ("SukiMenuBorderBrush", "#202020"), ("GlassBorderBrush", "#1A1A1A"),
            ("SukiText", "#EEEEEE"), ("SukiLowText", "#A8A8A8"),
            ("SukiMuteText", "#808080"), ("SukiDisabledText", "#5A5A5A"),
            ("SukiNeedleBrush", "#B8DB87"),
            ("MapWizardPickerSurface", "#1F0A0A0A"), ("MapWizardPickerDetail", "#240A0A0A"),
            ("MapWizardPickerChip", "#1F1A1A1A"), ("MapWizardPickerChipHover", "#33262626"),
            ("MapWizardPickerChipBorder", "#33FFFFFF"), ("MapWizardPickerChipHoverBorder", "#59FFFFFF"),
            ("MapWizardPickerSelectedBackground", "#4DB8DB87"), ("MapWizardPickerSelectedBorder", "#B8DB87"),
            ("MapWizardPickerActionHover", "#1FB8DB87"), ("MapWizardPickerActionPressed", "#33B8DB87"),
            ("MapWizardPickerHintBackground", "#337FD88F"), ("MapWizardPickerHintIcon", "#C7FFD9"),
            ("MapWizardPickerHintText", "#EAFBF2"), ("MapWizardPickerCheckIcon", "#BEEFD5"));
        return values;
    }

    private static IReadOnlyDictionary<string, object> CreateNoirLightResources()
    {
        var values = CreateMapWizardResources("#5F7488", "#4B5F70", "#52687C", "#FFFFFF",
            "#F7F9FA", "#E9EFF3", "#FCFDFE", "#20272D");

        AddColors(values,
            ("SukiBackground", "Transparent"), ("SukiStrongBackground", "#F7F9FA"),
            ("SukiLightBackground", "Transparent"), ("SukiCardBackground", "#FFFFFF"),
            ("SukiPopupBackground", "#FFFFFF"), ("SukiGlassCardBackground", "#F9FAFB"),
            ("SukiGlassCardOpaqueBackground", "#FFFFFF"), ("SukiControlTouchBackground", "#F0F3F5"),
            ("SukiDialogBackground", "#FFFFFF"), ("ControlSukiGlassCardBackground", "#F0F3F5"),
            ("SukiBorderBrush", "#D4DCE2"), ("SukiControlBorderBrush", "#C5D0D9"),
            ("SukiMediumBorderBrush", "#D0D9E0"), ("SukiLightBorderBrush", "#E4E9ED"),
            ("SukiMenuBorderBrush", "#D4DCE2"), ("GlassBorderBrush", "#D4DCE2"),
            ("SukiText", "#20272D"), ("SukiLowText", "#56636E"),
            ("SukiMuteText", "#707B84"), ("SukiDisabledText", "#929DA6"),
            ("SukiNeedleBrush", "#5F7488"),
            ("MapWizardPickerSurface", "#F1F4F6"), ("MapWizardPickerDetail", "#E9EEF1"),
            ("MapWizardPickerChip", "#F9FBFC"), ("MapWizardPickerChipHover", "#EBF1F5"),
            ("MapWizardPickerChipBorder", "#C5D0D9"), ("MapWizardPickerChipHoverBorder", "#9DACB8"),
            ("MapWizardPickerSelectedBackground", "#335F7488"), ("MapWizardPickerSelectedBorder", "#5F7488"),
            ("MapWizardPickerActionHover", "#1F5F7488"), ("MapWizardPickerActionPressed", "#335F7488"),
            ("MapWizardPickerHintBackground", "#665F7488"), ("MapWizardPickerHintIcon", "#D7E4ED"),
            ("MapWizardPickerHintText", "#F1F6FA"), ("MapWizardPickerCheckIcon", "#D7E4ED"));
        return values;
    }

    private static IReadOnlyDictionary<string, object> CreateClassicDarkResources()
    {
        var values = CreateMapWizardResources("#483D8B", "#352D68", "#6A5ACD", "#FFFFFF",
            "#242333", "#302E45", "#F0222130", "#F4F1FA");

        AddColors(values,
            ("MapWizardPickerSurface", "#24203A"), ("MapWizardPickerDetail", "#2C2745"),
            ("MapWizardPickerChip", "#302B49"), ("MapWizardPickerChipHover", "#3B3558"),
            ("MapWizardPickerChipBorder", "#5E5680"), ("MapWizardPickerChipHoverBorder", "#8176AA"),
            ("MapWizardPickerSelectedBackground", "#59483D8B"), ("MapWizardPickerSelectedBorder", "#6A5ACD"),
            ("MapWizardPickerActionHover", "#33483D8B"), ("MapWizardPickerActionPressed", "#4D483D8B"),
            ("MapWizardPickerHintBackground", "#4DFF4500"), ("MapWizardPickerHintIcon", "#FFD1C2"),
            ("MapWizardPickerHintText", "#FFF4EF"), ("MapWizardPickerCheckIcon", "#E1DBFF"));
        return values;
    }

    private static IReadOnlyDictionary<string, object> CreateClassicLightResources()
    {
        var values = CreateMapWizardResources("#483D8B", "#352D68", "#594CA5", "#FFFFFF",
            "#F7F6FC", "#ECE9F7", "#FFFCFF", "#272337");

        AddColors(values,
            ("MapWizardPickerSurface", "#F4F2FB"), ("MapWizardPickerDetail", "#ECE9F7"),
            ("MapWizardPickerChip", "#FCFBFF"), ("MapWizardPickerChipHover", "#EEEAF9"),
            ("MapWizardPickerChipBorder", "#CBC5DF"), ("MapWizardPickerChipHoverBorder", "#978DBB"),
            ("MapWizardPickerSelectedBackground", "#33483D8B"), ("MapWizardPickerSelectedBorder", "#483D8B"),
            ("MapWizardPickerActionHover", "#1F483D8B"), ("MapWizardPickerActionPressed", "#33483D8B"),
            ("MapWizardPickerHintBackground", "#66FF4500"), ("MapWizardPickerHintIcon", "#7B2100"),
            ("MapWizardPickerHintText", "#3D1608"), ("MapWizardPickerCheckIcon", "#483D8B"));
        return values;
    }

    private static Dictionary<string, object> CreateMapWizardResources(
        string accent, string accentDark, string accentHover, string accentForeground,
        string diffBackground, string diffHover, string tooltipBackground, string tooltipText)
    {
        var accentColor = ParseColor(accent);
        return new Dictionary<string, object>
        {
            ["MapWizardDiffBackground"] = ParseColor(diffBackground),
            ["MapWizardDiffHoverBackground"] = ParseColor(diffHover),
            ["MapWizardTooltipBackground"] = ParseColor(tooltipBackground),
            ["MapWizardTooltipText"] = ParseColor(tooltipText),
            ["ThemeAccentBadgeColor"] = new Color(0x30, accentColor.R, accentColor.G, accentColor.B),
            ["ThemeAccentBrush"] = new SolidColorBrush(accentColor),
            ["ThemeAccentColor"] = accentColor,
            ["ThemeAccentColor2"] = accentColor,
            ["ThemeAccentColor3"] = accentColor,
            ["ThemeAccentColor4"] = ParseColor(accentDark),
            ["HighlightColor"] = accentColor,
            ["HighlightHoverColor"] = ParseColor(accentHover),
            ["MapWizardAccentForeground"] = ParseColor(accentForeground)
        };
    }

    private static void AddColors(Dictionary<string, object> resources, params (string Key, string Value)[] colors)
    {
        foreach (var (key, value) in colors)
        {
            resources[key] = ParseColor(value);
        }
    }

    private static Color ParseColor(string value) => Color.Parse(value);

    private static ThemePalette NormalizePalette(ThemePalette palette)
    {
        return Enum.IsDefined(palette) ? palette : ThemePalette.MapWizardNoir;
    }
}
