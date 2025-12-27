using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MapWizard.Desktop.Converters;

public class IndexToTransformConverter : IValueConverter
{
    // Set these values according to your layout.
    public double ButtonHeight { get; set; } = 86;
    public double CaretHeight { get; set; } = 30;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            // Calculate the Y offset so that the caret is centered in the button.
            double offset = (index * ButtonHeight) + ((ButtonHeight - CaretHeight) / 2);
            return $"translateY({ Math.Round(offset)}px)";
        }
        return "translateY(0px)";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => BindingOperations.DoNothing;
}