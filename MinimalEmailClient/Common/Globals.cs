using System;

namespace MinimalEmailClient.Common
{
    public static class Globals
    {
        public static readonly string AppName = "Minimal Email Client";
        public static readonly string UserSettingsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + AppName;
    }
}
