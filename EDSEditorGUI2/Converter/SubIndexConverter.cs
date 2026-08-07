using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EDSEditorGUI2.Converter
{
    public class SubIndexConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values != null && values.Count > 0)
            {
                if (values[0] is string keyString)
                {
                    if (values.Count > 1 && values[1] is EDSEditorGUI2.ViewModels.ObjectDictionary dc)
                    {
                        if (dc.SelectedObjectType == LibCanOpen.OdObject.Types.ObjectType.Var)
                        {
                            return string.Empty;
                        }
                    }
                    
                    if (byte.TryParse(keyString, out byte key))
                    {
                        return $"0x{key:X1}";
                    }
                    else if (keyString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        return keyString;
                    }
                    else if (int.TryParse(keyString, out int intKey))
                    {
                        return $"0x{intKey:X1}";
                    }
                    return keyString;
                }
            }
            return string.Empty;
        }
    }
}
