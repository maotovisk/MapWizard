using System;
using System.Drawing;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MapWizard.Desktop.Converters;

public class AvaloniaColorToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color systemColor)
        {
            // The UI does not expose alpha editing; keep previews opaque.
            return new Avalonia.Media.Color(255, systemColor.R, systemColor.G, systemColor.B);
        }
        
        return new Avalonia.Media.Color(255, 0, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Avalonia.Media.Color avaloniaColor)
        {
            return System.Drawing.Color.FromArgb(255, avaloniaColor.R, avaloniaColor.G, avaloniaColor.B);
        }

        return System.Drawing.Color.Black;
    }
}
