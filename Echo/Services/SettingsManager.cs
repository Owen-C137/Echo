using System;
using System.IO;
using Newtonsoft.Json;

namespace Echo.Services
{
    public class AppSettings
    {
        // General
        public string BlackOps3Path { get; set; } = string.Empty;
        
        // Packaging
        public string? PackageOutputDirectory { get; set; }
        public int CompressionLevel { get; set; } = 2; // 0=None, 1=Fastest, 2=Optimal
        public int ArchiveStructure { get; set; } = 0; // 0=Direct, 1=Wrapped
        public bool IncludeSourceGdt { get; set; } = true;
        public bool ValidateAssets { get; set; } = true;
        
        // Sound Handling
        public int SoundAliasHandling { get; set; } = 0; // 0=Consolidate (create custom CSV), 1=Copy Full CSV Files, 2=Skip Sounds
        
        // Attachment Handling
        public bool IncludeAttachmentGdt { get; set; } = true; // Generate GDT file for resolved attachments
        
        // Advanced
        public bool EnableLogging { get; set; } = true;
        public bool AutoOpenLogsOnError { get; set; } = false;
        public string PackageNamePattern { get; set; } = "echo_packaged_{date}_{time}";
    }

    public static class SettingsManager
    {
        private static string? _settingsFilePath;
        private static AppSettings _currentSettings = new AppSettings();

        public static AppSettings CurrentSettings => _currentSettings;

        static SettingsManager()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(appDir, "settings.json");
        }

        public static void Load()
        {
            try
            {
                if (string.IsNullOrEmpty(_settingsFilePath))
                {
                    Logger.Warning("Settings file path is not initialized");
                    return;
                }

                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        _currentSettings = settings;
                        Logger.Info("Settings loaded from file");
                    }
                }
                else
                {
                    Logger.Info("No settings file found, using defaults");
                    _currentSettings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings", ex);
                _currentSettings = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                if (string.IsNullOrEmpty(_settingsFilePath))
                {
                    Logger.Warning("Settings file path is not initialized");
                    return;
                }

                var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
                Logger.Info("Settings saved to file");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
            }
        }
    }
}
