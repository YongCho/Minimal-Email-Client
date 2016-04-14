using System;
using System.IO;

namespace MinimalEmailClient.Common
{
    public static class Globals
    {
        public static readonly string UserSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Properties.Settings.Default.AppName);

        public static bool BootStrapperLoaded = false;
    }
}
