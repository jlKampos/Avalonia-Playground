using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace OmniWatch.Helpers
{
    public class BoolToPasswordCharConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? '\0' : '*';
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }

}
