using System;
using System.Linq;
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
            
            // Apply theme based on settings
            ApplyTheme(SettingsManager.CurrentSettings.Theme);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Echo application closing");
            SettingsManager.Save();
            base.OnExit(e);
        }

        public void ApplyTheme(string themeName)
        {
            // Remove existing theme if any
            var existingTheme = Resources.MergedDictionaries.FirstOrDefault(d => 
                d.Source != null && (d.Source.OriginalString.Contains("DarkTheme") || 
                                     d.Source.OriginalString.Contains("LightTheme") ||
                                     d.Source.OriginalString.Contains("DevRawTheme") ||
                                     d.Source.OriginalString.Contains("MidnightPurpleTheme")));
            
            if (existingTheme != null)
            {
                Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Add the selected theme with pack URI
            try
            {
                var themeUri = new Uri($"pack://application:,,,/Styles/{themeName}.xaml", UriKind.Absolute);
                var theme = new ResourceDictionary { Source = themeUri };
                Resources.MergedDictionaries.Add(theme);
                Logger.Info($"Applied theme: {themeName}");
            }
            catch (Exception ex)
            {
                // If theme fails to load, fall back to DarkTheme
                Logger.Warning($"Failed to load theme '{themeName}', falling back to DarkTheme. Error: {ex.Message}");
                var fallbackUri = new Uri("pack://application:,,,/Styles/DarkTheme.xaml", UriKind.Absolute);
                var fallbackTheme = new ResourceDictionary { Source = fallbackUri };
                Resources.MergedDictionaries.Add(fallbackTheme);
                
                // Update settings to use DarkTheme
                SettingsManager.CurrentSettings.Theme = "DarkTheme";
                SettingsManager.Save();
            }
        }
    }
}
