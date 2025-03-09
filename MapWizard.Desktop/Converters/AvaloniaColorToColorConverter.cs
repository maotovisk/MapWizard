using System;
using System.Drawing;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MapWizard.Desktop.Converters;

public class AvaloniaColorToColorConverter : IValueConverter
{
    // Convert System.Drawing.Color to Avalonia.Media.Color
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color systemColor)
        {
            return new Avalonia.Media.Color(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
        }
        
        return new Avalonia.Media.Color(255, 0, 0, 0); // Default to black if the value is invalid
    }

    // Convert Avalonia.Media.Color to System.Drawing.Color
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Avalonia.Media.Color avaloniaColor)
        {
            return System.Drawing.Color.FromArgb(avaloniaColor.A, avaloniaColor.R, avaloniaColor.G, avaloniaColor.B);
        }
        return System.Drawing.Color.Black; // Default to black if the value is invalid
    }
    
}