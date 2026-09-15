using System.Drawing;
using Avalonia.Media;

namespace MapWizard.Desktop.Models;

public class ComboColourOption
{
    public int Number { get; init; }
    public System.Drawing.Color Colour { get; init; }

    public Avalonia.Media.Color PreviewColor => new(255, Colour.R, Colour.G, Colour.B);
    public IBrush PreviewBrush => new SolidColorBrush(PreviewColor);
    public IBrush LabelBrush => RelativeLuminance(Colour) > 0.179 ? Brushes.Black : Brushes.White;
    public string Label => $"Combo{Number}";

    private static double RelativeLuminance(System.Drawing.Color colour) =>
        0.2126 * LinearChannel(colour.R) +
        0.7152 * LinearChannel(colour.G) +
        0.0722 * LinearChannel(colour.B);

    private static double LinearChannel(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : System.Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
