using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChecadorSSSD.Converters;

/// <summary>
/// Convierte un int (valor actual) en un Brush basado en ConverterParameter (número de paso).
/// Si value >= parameter -> verde (completado), sino gris (pendiente).
/// </summary>
public class IntToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int currentStep)
            return new SolidColorBrush(Colors.Gray);

        if (!int.TryParse(parameter?.ToString(), out int targetStep))
            return new SolidColorBrush(Colors.Gray);

        if (currentStep >= targetStep)
            return new SolidColorBrush(Colors.LimeGreen); // Completado
        else
            return new SolidColorBrush(Colors.Gray);      // Pendiente
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}