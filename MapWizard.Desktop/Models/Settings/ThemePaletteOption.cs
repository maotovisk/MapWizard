using Avalonia.Media;

namespace MapWizard.Desktop.Models.Settings;

public sealed class ThemePaletteOption(
    ThemePalette value,
    string name,
    Color darkBackground,
    Color darkSurface,
    Color darkAccent,
    Color darkText,
    Color lightBackground,
    Color lightSurface,
    Color lightAccent,
    Color lightText)
{
    public ThemePalette Value { get; } = value;
    public string Name { get; } = name;

    public IBrush DarkBackgroundBrush { get; } = new SolidColorBrush(darkBackground);
    public IBrush DarkSurfaceBrush { get; } = new SolidColorBrush(darkSurface);
    public IBrush DarkAccentBrush { get; } = new SolidColorBrush(darkAccent);
    public IBrush DarkTextBrush { get; } = new SolidColorBrush(darkText);

    public IBrush LightBackgroundBrush { get; } = new SolidColorBrush(lightBackground);
    public IBrush LightSurfaceBrush { get; } = new SolidColorBrush(lightSurface);
    public IBrush LightAccentBrush { get; } = new SolidColorBrush(lightAccent);
    public IBrush LightTextBrush { get; } = new SolidColorBrush(lightText);

    public override string ToString() => Name;
}
