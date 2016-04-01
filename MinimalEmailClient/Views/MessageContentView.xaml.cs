using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    public partial class MessageContentView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private enum TabIndexEnum { BrowserTab = 0, TextTab, HtmlTab, BrowserContentTab, SourceTab }

        private int selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                if (this.selectedTabIndex != value)
                {
                    this.selectedTabIndex = value;
                    NotifyPropertyChanged("SelectedTabIndex");
                }

                defaultViewMenuItem.IsChecked = this.selectedTabIndex == (int)TabIndexEnum.BrowserTab;
                textViewMenuItem.IsChecked = this.selectedTabIndex == (int)TabIndexEnum.TextTab;
                htmlViewMenuItem.IsChecked = this.selectedTabIndex == (int)TabIndexEnum.HtmlTab;
                browserContentViewMenuItem.IsChecked = this.selectedTabIndex == (int)TabIndexEnum.BrowserContentTab;
                sourceViewMenuItem.IsChecked = this.selectedTabIndex == (int)TabIndexEnum.SourceTab;
            }
        }

        public MessageContentView()
        {
            InitializeComponent();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Hack to enforce initial window size without making it grow with content.
            // Seriously, there's gotta be a better way to achieve this.

            int recommendedWidth = 1200;
            int recommendedHeight = 800;
            Window parentWindow = Window.GetWindow(this);

            // Force the initial size here.
            parentWindow.MinWidth = recommendedWidth;
            parentWindow.MaxWidth = recommendedWidth;
            parentWindow.MinHeight = recommendedHeight;
            parentWindow.MaxHeight = recommendedHeight;

            // Then remove the restriction so the user can resize the window.
            parentWindow.ClearValue(Window.MinWidthProperty);
            parentWindow.ClearValue(Window.MaxWidthProperty);
            parentWindow.ClearValue(Window.MinHeightProperty);
            parentWindow.ClearValue(Window.MaxHeightProperty);

            // Do not let the controls grow the window any more.
            parentWindow.SizeToContent = SizeToContent.Manual;
        }

        private void defaultViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = (int)TabIndexEnum.BrowserTab;
        }

        private void textViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = (int)TabIndexEnum.TextTab;
        }

        private void htmlViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = (int)TabIndexEnum.HtmlTab;
        }

        private void browserContentViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = (int)TabIndexEnum.BrowserContentTab;
        }

        private void sourceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = (int)TabIndexEnum.SourceTab;
        }

        private void textBodyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            HandleContentChange();
        }

        private void htmlBodyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            HandleContentChange();
        }

        private void HandleContentChange()
        {
            if (string.IsNullOrWhiteSpace(htmlBodyTextBox.Text))
            {
                textViewMenuItem_Click(this, new RoutedEventArgs());
            }
            else
            {
                defaultViewMenuItem_Click(this, new RoutedEventArgs());
            }
        }
    }
}
