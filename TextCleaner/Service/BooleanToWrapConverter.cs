using System;
using System.Globalization;
using System.Windows.Data;

namespace TextCleaner.Service
{
    internal class BooleanToWrapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isWrapped = (bool)value;
            return isWrapped ? "Wrap" : "NoWrap";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string wrapMode = (string)value;
            return wrapMode == "Wrap";
        }
    }
}