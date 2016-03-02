using System;

namespace MinimalEmailClient.Common
{
    public static class Globals
    {
        public static readonly string UserSettingsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + Properties.Settings.Default.AppName;
    }
}
