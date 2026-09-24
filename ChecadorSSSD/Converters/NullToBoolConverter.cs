using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChecadorSSSD.Converters;

/// <summary>
/// Convierte un valor null en false y no-null en true (o viceversa segun el parametro).
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s.ToLowerInvariant() == "true";
        bool result = value != null;
        if (invert) result = !result;
        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}