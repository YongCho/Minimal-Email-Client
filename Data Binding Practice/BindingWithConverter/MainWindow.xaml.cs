using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

// Check here for other built-in WPF converters.
// http://stackoverflow.com/questions/505397/built-in-wpf-ivalueconverters

namespace BindingWithConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class YesNoToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value.ToString().ToLower())
            {
                case "yes":
                case "oui":
                    return true;
                case "no":
                case "non":
                    return false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool) value == true)
                {
                    return "yes";
                }
                else
                {
                    return "no";
                }
            }
            return "no";
        }
    }
}
