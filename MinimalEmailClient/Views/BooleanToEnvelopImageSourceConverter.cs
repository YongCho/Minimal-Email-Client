using System;
using System.Globalization;
using System.Windows.Data;

namespace MinimalEmailClient.Views
{
    public class BooleanToEnvelopImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "/Resources/Images/envelope_seen.png";
            }
            else
            {
                return "/Resources/Images/envelope_unseen.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
