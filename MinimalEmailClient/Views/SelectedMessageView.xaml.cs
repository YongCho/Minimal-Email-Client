using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for SelectedMessageView.xaml
    /// </summary>
    public partial class SelectedMessageView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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

                textViewMenuItem.IsChecked = this.selectedTabIndex == 0;
                htmlViewMenuItem.IsChecked = this.selectedTabIndex == 1;
                sourceViewMenuItem.IsChecked = this.selectedTabIndex == 2;
            }
        }

        public SelectedMessageView()
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

            int recommendedWidth = 800;
            int recommendedHeight = 600;
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

        private void textViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = 0;
        }

        private void htmlViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = 1;
        }

        private void sourceViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabIndex = 2;
        }
    }
}
