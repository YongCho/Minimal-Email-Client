using System.Windows;
using MinimalEmailClient.Models;

namespace MinimalEmailClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!DatabaseManager.IsSchemaCurrent())
            {
                DatabaseManager.CreateDatabase();
            }

            base.OnStartup(e);

            var bs = new Bootstrapper();
            bs.Run();
        }
    }
}
