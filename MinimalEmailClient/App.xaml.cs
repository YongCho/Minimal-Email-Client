using System.Windows;
using MinimalEmailClient.Models;
using MinimalEmailClient.Services;

namespace MinimalEmailClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseManager.Initialize();

            base.OnStartup(e);

            var bs = new Bootstrapper();
            bs.Run();
        }
    }
}
