using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChecadorSSSD.Converters;

public sealed class IndexEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int current
            && int.TryParse(parameter?.ToString(), out var expected)
            && current == expected;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
