using System.IO;
using System;
using System.Reflection;
using System.Windows;
using Echo.Services;

namespace Echo.Views
{
    public partial class LauncherWindow : Window
    {
        private readonly UpdateService _updateService;

        public LauncherWindow()
        {
            InitializeComponent();
            UpdateBo3PathDisplay();
            UpdateVersionDisplay();
            
            _updateService = new UpdateService();
            CheckForUpdatesAsync();
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                await _updateService.CheckForUpdatesOnStartupAsync();
                if (_updateService.IsUpdateAvailable)
                {
                    UpdateNotificationBanner.Visibility = Visibility.Visible;
                    UpdateVersionLabel.Text = _updateService.AvailableUpdate?.VersionString ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates on startup", ex);
            }
        }

        private void UpdateVersionDisplay()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                VersionText.Text = "v1.0.0";
            }
        }

        private void UpdateBo3PathDisplay()
        {
            // No longer displaying BO3 path in footer (moved to header with version)
        }

        private void OpenGdtPacker(object sender, RoutedEventArgs e)
        {
            Logger.Info("Opening GDT Package Manager");
            try
            {
                var packageManager = new GdtPackerWindow();
                packageManager.Owner = this;
                
                // Hide launcher
                this.Hide();
                
                // Show tool as dialog
                packageManager.ShowDialog();
                
                // Show launcher again when tool closes
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show(); // Make sure launcher is visible if there's an error
                Logger.Error("Failed to open GDT Package Manager", ex);
                MessageBox.Show($"Failed to open Package Manager: {ex.Message}\n\nCheck Logs.txt for details.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening settings window");
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                if (settingsWindow.ShowDialog() == true)
                {
                    UpdateBo3PathDisplay();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open settings window", ex);
                MessageBox.Show($"Failed to open settings: {ex.Message}\n\nCheck Logs.txt for details.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDocs(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening documentation window");
                var docsWindow = new DocsWindow();
                docsWindow.Owner = this;
                docsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open docs window", ex);
                MessageBox.Show($"Failed to open documentation: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenChangelog(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening changelog window");
                var changelogWindow = new ChangelogWindow();
                changelogWindow.Owner = this;
                changelogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open changelog window", ex);
                MessageBox.Show($"Failed to open changelog: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Manual update check triggered");
                var hasUpdate = await _updateService.CheckForUpdatesManualAsync();
                if (hasUpdate)
                {
                    UpdateNotificationBanner.Visibility = Visibility.Visible;
                    UpdateVersionLabel.Text = _updateService.AvailableUpdate?.VersionString ?? "Unknown";
                    ShowUpdateDialogAndInstall();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates", ex);
                MessageBox.Show($"Failed to check for updates: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateNotificationBanner_Click(object sender, RoutedEventArgs e)
        {
            ShowUpdateDialogAndInstall();
        }

        private async void ShowUpdateDialogAndInstall()
        {
            try
            {
                var shouldUpdate = _updateService.ShowUpdateDialog(this);
                if (shouldUpdate)
                {
                    await _updateService.DownloadAndInstallUpdateAsync(this);
                }
                else
                {
                    UpdateNotificationBanner.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show update dialog", ex);
                MessageBox.Show($"Failed to update: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
