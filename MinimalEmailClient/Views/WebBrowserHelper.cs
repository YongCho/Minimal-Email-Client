using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    // This class is used to create a binding property to a WebBrowser control.
    // Source: http://stackoverflow.com/questions/4202961/can-i-bind-html-to-a-wpf-web-browser-control
    // XAML example:
    // <WebBrowser local:WebBrowserHelper.Html="{Binding MyHtmlString}" />
    public class WebBrowserHelper
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(WebBrowserHelper),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser webBrowser = dependencyObject as WebBrowser;
            if (webBrowser != null)
                webBrowser.NavigateToString(e.NewValue as string ?? "&nbsp;");
        }
    }
}
