using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using Echo.Services;

namespace Echo
{
    public partial class App : Application
    {
        public App()
        {
            // Fix WPF pack:// URI assembly loading issue in published .NET apps
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name);
                if (assemblyName.Name == "Echo")
                {
                    // Return the currently executing assembly
                    return Assembly.GetExecutingAssembly();
                }
                return null;
            };

            // Global exception handler
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                var errorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                System.IO.File.WriteAllText(errorPath, $"CRASH: {DateTime.Now}\n{ex?.ToString() ?? "Unknown error"}");
                MessageBox.Show($"Fatal error: {ex?.Message}\n\nSee crash_log.txt for details", "Echo Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            DispatcherUnhandledException += (sender, args) =>
            {
                var errorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                System.IO.File.WriteAllText(errorPath, $"UI CRASH: {DateTime.Now}\n{args.Exception}");
                MessageBox.Show($"UI error: {args.Exception.Message}\n\nSee crash_log.txt for details", "Echo Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                var errorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt");
                System.IO.File.WriteAllText(errorPath, $"STARTUP ERROR: {DateTime.Now}\n{ex}");
                MessageBox.Show($"Startup error: {ex.Message}\n\nSee startup_error.txt for details", "Echo Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
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
