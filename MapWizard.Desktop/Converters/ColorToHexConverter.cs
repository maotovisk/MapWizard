using System;
using System.Drawing;
using System.Globalization;
using Avalonia.Data.Converters;
using Color = System.Drawing.Color;

namespace MapWizard.Desktop.Converters;

public class ColorToHexConverter : IValueConverter
{
    // Convert Color to Hex string
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}"; // RGB Hex format (#RRGGBB)
        }
        return "#000000"; // Default to black if value is not a color
    }

    // Convert Hex string to Color
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            // Handle #RRGGBB format
            if (hex.StartsWith("#") && hex.Length == 7)
            {
                try
                {
                    return ColorTranslator.FromHtml(hex); // Convert HTML color string to Color
                }
                catch
                {
                    return Color.Black; // Default if parsing fails
                }
            }
        }
        return  Color.Black; // Default color if invalid hex string
    }
}