// https://stackoverflow.com/a/21588195/7585517
// https://stackoverflow.com/a/21588195
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Resources;

namespace ConsoleControlSample.WPF1.Utility
{
    public class FileToUIElementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            StreamResourceInfo sri = Application.GetResourceStream(new Uri((string)parameter, UriKind.Relative));
            if (sri != null)
            {
                using (Stream stream = sri.Stream)
                {
                    if (XamlReader.Load(stream) is Viewbox logo)
                    {
                        return logo;
                    }
                }
            }

            throw new Exception("Resource not found");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
