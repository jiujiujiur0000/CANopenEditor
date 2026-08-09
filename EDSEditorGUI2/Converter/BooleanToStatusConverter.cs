using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EDSEditorGUI2.Converter;

public sealed class BooleanToStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDisabled)
        {
            return isDisabled ? 1 : 0;
        }
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selectedIndex)
        {
            return selectedIndex == 1; // 1 means Disabled = true
        }
        return false;
    }
}
