using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    // Helper class to make GridViewColumn not resizable in listview.
    // Source: https://blogs.msdn.microsoft.com/atc_avalon_team/2006/04/10/fixed-width-column-in-listview-a-column-that-cannot-be-resized/
    public class FixedWidthGridViewColumn : GridViewColumn
    {
        static FixedWidthGridViewColumn()
        {
            WidthProperty.OverrideMetadata(typeof(FixedWidthGridViewColumn), new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
        }

        private static object OnCoerceWidth(DependencyObject o, object baseValue)
        {
            FixedWidthGridViewColumn fwc = o as FixedWidthGridViewColumn;
            if (fwc != null)
            {
                return fwc.FixedWidth;
            }
            return baseValue;
        }

        public double FixedWidth
        {
            get { return (double)GetValue(FixedWidthProperty); }
            set { SetValue(FixedWidthProperty, value); }
        }

        public static readonly DependencyProperty FixedWidthProperty =
            DependencyProperty.Register(
                "FixedWidth",
                typeof(double),
                typeof(FixedWidthGridViewColumn),
                new FrameworkPropertyMetadata(double.NaN, new PropertyChangedCallback(OnFixedWidthChanged)));

        private static void OnFixedWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            FixedWidthGridViewColumn fwc = o as FixedWidthGridViewColumn;
            if (fwc != null)
            {
                fwc.CoerceValue(WidthProperty);
            }
        }
    }
}
