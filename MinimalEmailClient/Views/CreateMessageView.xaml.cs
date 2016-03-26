using System.Windows.Controls;
using MinimalEmailClient.ViewModels;
using System.Diagnostics;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for ComposeMailView.xaml
    /// </summary>
    public partial class CreateMessageView : UserControl
    {
        public CreateMessageView()
        {
            InitializeComponent();
            this.DataContext = new CreateMessageViewModel();
        }
    }
}
