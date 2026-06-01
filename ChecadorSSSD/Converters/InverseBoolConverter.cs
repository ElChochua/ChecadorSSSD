using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChecadorSSSD.Converters;

/// <summary>
/// Invierte un valor booleano.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return Avalonia.AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return Avalonia.AvaloniaProperty.UnsetValue;
    }
}
