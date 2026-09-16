using System.Collections.Generic;
using Avalonia.Media;

namespace MapWizard.Theme.Palettes;

/// <summary>
/// Semantic color tokens for MapWizard's supported visual palettes.
/// Feature code selects a palette; it does not need to know how that palette is built.
/// </summary>
public static class MapWizardPaletteCatalog
{
    public static IReadOnlyDictionary<string, object> GetResources(bool useClassicPalette, bool isDark)
    {
        return (useClassicPalette, isDark) switch
        {
            (true, true) => CreateClassicDark(),
            (true, false) => CreateClassicLight(),
            (false, true) => CreateNoirDark(),
            _ => CreateNoirLight()
        };
    }

    private static Dictionary<string, object> CreateNoirDark()
    {
        var resources = CreateBase(
            window: "#0A0A0A", card: "#101010", surface: "#F2121212", surfaceOpaque: "#101010",
            popup: "#141414", control: "#1A1A1A", hover: "#262626", pressed: "#303030",
            border: "#232323", controlBorder: "#303030", text: "#EEEEEE", secondary: "#A8A8A8",
            muted: "#808080", disabled: "#5A5A5A", accent: "#B8DB87", accentHover: "#C9E6A1",
            accentPressed: "#8FAE63", accentForeground: "#151A0E");

        AddFeatureColors(resources,
            diff: "#1A1A1A", diffHover: "#262626", tooltip: "#FF2B2B2B", tooltipText: "#FFF2F2F2",
            pickerSurface: "#1F0A0A0A", pickerDetail: "#240A0A0A", pickerChip: "#1F1A1A1A",
            pickerChipHover: "#33262626", pickerChipBorder: "#33FFFFFF", pickerChipHoverBorder: "#59FFFFFF",
            selectedBackground: "#4DB8DB87", selectedBorder: "#B8DB87", actionHover: "#1FB8DB87",
            actionPressed: "#33B8DB87", hintBackground: "#337FD88F", hintIcon: "#C7FFD9",
            hintText: "#EAFBF2", checkIcon: "#BEEFD5", noticeBackground: "#1FF5A742",
            noticeBorder: "#73F5A742", noticeAccent: "#FFBD69");
        return resources;
    }

    private static Dictionary<string, object> CreateNoirLight()
    {
        var resources = CreateBase(
            window: "#F7F9FA", card: "#FFFFFF", surface: "#F2F9FAFB", surfaceOpaque: "#FFFFFF",
            popup: "#FFFFFF", control: "#F0F3F5", hover: "#E9EFF3", pressed: "#DDE5EA",
            border: "#D4DCE2", controlBorder: "#C5D0D9", text: "#20272D", secondary: "#56636E",
            muted: "#707B84", disabled: "#929DA6", accent: "#5F7488", accentHover: "#52687C",
            accentPressed: "#4B5F70", accentForeground: "#FFFFFF");

        AddFeatureColors(resources,
            diff: "#F7F9FA", diffHover: "#E9EFF3", tooltip: "#FFFFFF", tooltipText: "#20272D",
            pickerSurface: "#F1F4F6", pickerDetail: "#E9EEF1", pickerChip: "#F9FBFC",
            pickerChipHover: "#EBF1F5", pickerChipBorder: "#C5D0D9", pickerChipHoverBorder: "#9DACB8",
            selectedBackground: "#335F7488", selectedBorder: "#5F7488", actionHover: "#1F5F7488",
            actionPressed: "#335F7488", hintBackground: "#665F7488", hintIcon: "#D7E4ED",
            hintText: "#F1F6FA", checkIcon: "#D7E4ED", noticeBackground: "#26D7831F",
            noticeBorder: "#99B86A12", noticeAccent: "#92510B");
        return resources;
    }

    private static Dictionary<string, object> CreateClassicDark()
    {
        var resources = CreateBase(
            window: "#181623", card: "#201D2C", surface: "#F2262235", surfaceOpaque: "#211E2E",
            popup: "#29253A", control: "#242333", hover: "#302E45", pressed: "#3B3853",
            border: "#37324B", controlBorder: "#4A4462", text: "#F4F1FA", secondary: "#BBB4CC",
            muted: "#9189A5", disabled: "#696276", accent: "#6A5ACD", accentHover: "#8072DB",
            accentPressed: "#483D8B", accentForeground: "#FFFFFF");

        AddFeatureColors(resources,
            diff: "#242333", diffHover: "#302E45", tooltip: "#3A3652", tooltipText: "#F4F1FA",
            pickerSurface: "#24203A", pickerDetail: "#2C2745", pickerChip: "#302B49",
            pickerChipHover: "#3B3558", pickerChipBorder: "#5E5680", pickerChipHoverBorder: "#8176AA",
            selectedBackground: "#59483D8B", selectedBorder: "#6A5ACD", actionHover: "#33483D8B",
            actionPressed: "#4D483D8B", hintBackground: "#4DFF4500", hintIcon: "#FFD1C2",
            hintText: "#FFF4EF", checkIcon: "#E1DBFF", noticeBackground: "#33FF4500",
            noticeBorder: "#99FF6A32", noticeAccent: "#FF8A5B");
        return resources;
    }

    private static Dictionary<string, object> CreateClassicLight()
    {
        var resources = CreateBase(
            window: "#F7F6FC", card: "#FFFFFF", surface: "#F2FCFBFF", surfaceOpaque: "#FFFFFF",
            popup: "#FFFFFF", control: "#F0EDF8", hover: "#E8E3F4", pressed: "#DDD6EE",
            border: "#D8D2E5", controlBorder: "#C8C0DC", text: "#272337", secondary: "#625B72",
            muted: "#7C748A", disabled: "#9B94A6", accent: "#483D8B", accentHover: "#594CA5",
            accentPressed: "#352D68", accentForeground: "#FFFFFF");

        AddFeatureColors(resources,
            diff: "#F7F6FC", diffHover: "#ECE9F7", tooltip: "#FFFFFF", tooltipText: "#272337",
            pickerSurface: "#F4F2FB", pickerDetail: "#ECE9F7", pickerChip: "#FCFBFF",
            pickerChipHover: "#EEEAF9", pickerChipBorder: "#CBC5DF", pickerChipHoverBorder: "#978DBB",
            selectedBackground: "#33483D8B", selectedBorder: "#483D8B", actionHover: "#1F483D8B",
            actionPressed: "#33483D8B", hintBackground: "#66FF4500", hintIcon: "#7B2100",
            hintText: "#3D1608", checkIcon: "#483D8B", noticeBackground: "#26FF4500",
            noticeBorder: "#99C83A00", noticeAccent: "#9B2B00");
        return resources;
    }

    private static Dictionary<string, object> CreateBase(
        string window, string card, string surface, string surfaceOpaque, string popup,
        string control, string hover, string pressed, string border, string controlBorder,
        string text, string secondary, string muted, string disabled, string accent,
        string accentHover, string accentPressed, string accentForeground)
    {
        var accentColor = Color.Parse(accent);
        return new Dictionary<string, object>
        {
            ["MapWizardWindowBackground"] = Color.Parse(window),
            ["MapWizardCard"] = Color.Parse(card),
            ["MapWizardSurface"] = Color.Parse(surface),
            ["MapWizardSurfaceOpaque"] = Color.Parse(surfaceOpaque),
            ["MapWizardPopup"] = Color.Parse(popup),
            ["MapWizardControlBackground"] = Color.Parse(control),
            ["MapWizardControlHover"] = Color.Parse(hover),
            ["MapWizardControlPressed"] = Color.Parse(pressed),
            ["MapWizardBorder"] = Color.Parse(border),
            ["MapWizardControlBorder"] = Color.Parse(controlBorder),
            ["MapWizardText"] = Color.Parse(text),
            ["MapWizardTextSecondary"] = Color.Parse(secondary),
            ["MapWizardTextMuted"] = Color.Parse(muted),
            ["MapWizardTextDisabled"] = Color.Parse(disabled),
            ["MapWizardAccent"] = accentColor,
            ["MapWizardAccentHover"] = Color.Parse(accentHover),
            ["MapWizardAccentPressed"] = Color.Parse(accentPressed),
            ["MapWizardAccentForeground"] = Color.Parse(accentForeground),
            ["MapWizardAccent25"] = WithAlpha(accentColor, 0x40),
            ["MapWizardAccent50"] = WithAlpha(accentColor, 0x80),
            ["MapWizardAccentBackdrop"] = WithAlpha(accentColor, 0x0E),
            ["SystemAccentColor"] = accentColor,
            ["SystemAccentColorDark1"] = Color.Parse(accentPressed),
            ["SystemAccentColorLight1"] = Color.Parse(accentHover),
            ["SystemAccentColorLight2"] = Color.Parse(accentHover),
            ["SystemAccentColorLight3"] = Color.Parse(accentHover)
        };
    }

    private static void AddFeatureColors(
        Dictionary<string, object> resources,
        string diff, string diffHover, string tooltip, string tooltipText,
        string pickerSurface, string pickerDetail, string pickerChip, string pickerChipHover,
        string pickerChipBorder, string pickerChipHoverBorder, string selectedBackground,
        string selectedBorder, string actionHover, string actionPressed, string hintBackground,
        string hintIcon, string hintText, string checkIcon, string noticeBackground,
        string noticeBorder, string noticeAccent)
    {
        Add(resources,
            ("MapWizardDiffBackground", diff), ("MapWizardDiffHoverBackground", diffHover),
            ("MapWizardTooltipBackground", tooltip), ("MapWizardTooltipText", tooltipText),
            ("MapWizardPickerSurface", pickerSurface), ("MapWizardPickerDetail", pickerDetail),
            ("MapWizardPickerChip", pickerChip), ("MapWizardPickerChipHover", pickerChipHover),
            ("MapWizardPickerChipBorder", pickerChipBorder), ("MapWizardPickerChipHoverBorder", pickerChipHoverBorder),
            ("MapWizardPickerSelectedBackground", selectedBackground), ("MapWizardPickerSelectedBorder", selectedBorder),
            ("MapWizardPickerActionHover", actionHover), ("MapWizardPickerActionPressed", actionPressed),
            ("MapWizardPickerHintBackground", hintBackground), ("MapWizardPickerHintIcon", hintIcon),
            ("MapWizardPickerHintText", hintText), ("MapWizardPickerCheckIcon", checkIcon),
            ("MapWizardImportantNoticeBackground", noticeBackground),
            ("MapWizardImportantNoticeBorder", noticeBorder), ("MapWizardImportantNoticeAccent", noticeAccent));
    }

    private static void Add(Dictionary<string, object> resources, params (string Key, string Value)[] colors)
    {
        foreach (var (key, value) in colors)
        {
            resources[key] = Color.Parse(value);
        }
    }

    private static Color WithAlpha(Color color, byte alpha) => new(alpha, color.R, color.G, color.B);
}
