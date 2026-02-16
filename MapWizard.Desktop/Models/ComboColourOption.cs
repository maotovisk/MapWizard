using System.Drawing;
using Avalonia.Media;

namespace MapWizard.Desktop.Models;

public class ComboColourOption
{
    public int Number { get; init; }
    public System.Drawing.Color Colour { get; init; }

    public Avalonia.Media.Color PreviewColor => new(255, Colour.R, Colour.G, Colour.B);
    public IBrush PreviewBrush => new SolidColorBrush(PreviewColor);
    public string Label => $"Combo{Number}";
}
