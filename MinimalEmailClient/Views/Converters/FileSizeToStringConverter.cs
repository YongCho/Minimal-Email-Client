using System;
using System.Globalization;
using System.Windows.Data;

namespace MinimalEmailClient.Views.Converters
{
    public class FileSizeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long)
            {
                long fileSizeBytes = (long)value;

                if (fileSizeBytes < 1000)
                {
                    return System.Convert.ToString(fileSizeBytes) + " B";
                }
                else if (fileSizeBytes < 10000000)
                {
                    decimal fileSizeKBytes = fileSizeBytes / 1000;
                    return fileSizeKBytes.ToString("#,0") + " kB";
                }
                else if (fileSizeBytes >= 10000000)
                {
                    decimal fileSizeMBytes = (decimal)(fileSizeBytes / 1000000.0);
                    return fileSizeMBytes.ToString("#,0.0") + " MB";
                }
                else
                {
                    return "Size Unknown";
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
