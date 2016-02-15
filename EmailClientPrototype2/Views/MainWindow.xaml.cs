using System.Windows;
using EmailClientPrototype2.ViewModels;

namespace EmailClientPrototype2
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
