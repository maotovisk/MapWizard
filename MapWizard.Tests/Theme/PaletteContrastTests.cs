using System;
using System.Collections.Generic;
using Avalonia.Media;
using MapWizard.Theme.Palettes;
using Xunit;

namespace MapWizard.Tests.Theme;

public class PaletteContrastTests
{
    public static IEnumerable<object[]> AllPalettes()
    {
        yield return [false, true];
        yield return [false, false];
        yield return [true, true];
        yield return [true, false];
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Tooltip_text_contrasts_with_tooltip_background(bool useClassicPalette, bool isDark)
    {
        var resources = MapWizardPaletteCatalog.GetResources(useClassicPalette, isDark);
        var background = GetColor(resources, "MapWizardTooltipBackground");
        var text = GetColor(resources, "MapWizardTooltipText");

        Assert.True(
            ContrastRatio(background, text) >= 4.5,
            $"Tooltip contrast {ContrastRatio(background, text):F2} is too low for " +
            $"(classic: {useClassicPalette}, dark: {isDark}).");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Accent_foreground_contrasts_with_accent_button_states(bool useClassicPalette, bool isDark)
    {
        var resources = MapWizardPaletteCatalog.GetResources(useClassicPalette, isDark);
        var foreground = GetColor(resources, "MapWizardAccentForeground");

        foreach (var key in new[] { "MapWizardAccent", "MapWizardAccentHover", "MapWizardAccentPressed" })
        {
            var background = GetColor(resources, key);
            Assert.True(
                ContrastRatio(background, foreground) >= 3.0,
                $"{key} contrast {ContrastRatio(background, foreground):F2} is too low for " +
                $"(classic: {useClassicPalette}, dark: {isDark}).");
        }
    }

    private static Color GetColor(IReadOnlyDictionary<string, object> resources, string key)
    {
        Assert.True(resources.TryGetValue(key, out var value), $"Missing palette resource '{key}'.");
        return Assert.IsType<Color>(value);
    }

    private static double ContrastRatio(Color first, Color second)
    {
        var lighter = Math.Max(RelativeLuminance(first), RelativeLuminance(second));
        var darker = Math.Min(RelativeLuminance(first), RelativeLuminance(second));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));

        static double Channel(byte value)
        {
            var channel = value / 255d;
            return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
    }
}
