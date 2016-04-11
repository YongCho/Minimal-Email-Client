using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MinimalEmailClient.Views.Converters
{
    public class FilePathToIconImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapSource bmpSrc;
            string filePath = (string)value;

            if (File.Exists(filePath))
            {
                var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                sysicon.Dispose();
            }
            else
            {
                bmpSrc = new BitmapImage(new Uri("pack://application:,,,/MinimalEmailClient;component/Resources/Images/document.png"));
            }

            return bmpSrc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
