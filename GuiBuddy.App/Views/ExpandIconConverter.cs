using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiBuddy.App.Views
{
    public class ExpandIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "\uE70E" /* ChevronUp */ : "\uE70D"; /* ChevronDown */

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
