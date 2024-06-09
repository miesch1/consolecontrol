// https://stackoverflow.com/a/356690
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleControlSample.WPF1.Utility
{
    public class IsNullConverter : IValueConverter
    {
        // A more general solution would be to implement an IValueConverter that checks for equality with
        // the ConverterParameter, so you can check against anything, and not just null.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
}
