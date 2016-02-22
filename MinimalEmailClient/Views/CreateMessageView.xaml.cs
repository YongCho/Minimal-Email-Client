using System.Windows.Controls;
using MinimalEmailClient.ViewModels;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for ComposeMailView.xaml
    /// </summary>
    public partial class CreateMessageView : UserControl
    {
        public CreateMessageView()
        {
            this.DataContext = new CreateMessageViewModel();
            InitializeComponent();
        }
    }
}
