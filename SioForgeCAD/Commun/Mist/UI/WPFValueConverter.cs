using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SioForgeCAD.Commun.Mist.UI
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double total = (double)value;
            double percent = System.Convert.ToDouble(parameter);
            return new GridLength(total * percent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}