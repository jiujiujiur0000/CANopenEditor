using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EDSEditorGUI2.Converter;

public sealed class BrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool bValue)
        {
            string resourceKey = bValue ? "SystemControlErrorTextForegroundBrush" : "SystemControlForegroundBaseHighBrush";
            if (Application.Current != null && Application.Current.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource) && resource is IBrush brush)
            {
                return brush;
            }
            
            // Fallback just in case resources are missing
            if (bValue) return new SolidColorBrush(Color.Parse("#C50F1F")); // Standard red
            return new SolidColorBrush(Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark ? Colors.White : Colors.Black);
        }
        return new SolidColorBrush(Colors.Orange);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
