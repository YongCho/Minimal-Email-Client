using System.Windows;
using MinimalEmailClient.ViewModels;

namespace MinimalEmailClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
    }
}
