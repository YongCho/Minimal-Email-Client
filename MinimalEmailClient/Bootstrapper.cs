using MinimalEmailClient.Views;
using Microsoft.Practices.Unity;
using Prism.Unity;
using System.Windows;

namespace MinimalEmailClient
{
    public class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            MainWindow shell = Container.Resolve<MainWindow>();
            shell.Show();

            return shell;
        }

        
    }
}
