using MinimalEmailClient.ViewModels;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    public partial class NewMessageBalloon : UserControl
    {
        public NewMessageBalloon(MessageHeaderViewModel newMessageHeaderViewModel)
        {
            InitializeComponent();
            this.DataContext = newMessageHeaderViewModel;
        }
    }
}
