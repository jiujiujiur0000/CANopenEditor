using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EDSEditorGUI2.Converter;

public sealed class AbsoluteHexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return $"0x{Math.Abs(index):X4}";
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
