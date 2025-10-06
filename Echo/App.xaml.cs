using System.Windows;
using Echo.Services;

namespace Echo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize logger
            Logger.Initialize();
            Logger.Info("Echo application started");
            
            // Load settings
            SettingsManager.Load();
            Logger.Info("Settings loaded successfully");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Echo application closing");
            SettingsManager.Save();
            base.OnExit(e);
        }
    }
}
