using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Echo.Models;
using Echo.Views;

namespace Echo.Services
{
    public class UpdateService
    {
        private readonly UpdateChecker _updateChecker;
        private readonly UpdateDownloader _updateDownloader;
        private UpdateInfo? _availableUpdate;

        public UpdateService()
        {
            _updateChecker = new UpdateChecker();
            _updateDownloader = new UpdateDownloader();
        }

        /// <summary>
        /// Check for updates on startup - called automatically
        /// </summary>
        public async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                Logger.Info("Checking for updates on startup...");
                _availableUpdate = await _updateChecker.CheckForUpdatesAsync();

                if (_availableUpdate != null)
                {
                    Logger.Info($"Update available: {_availableUpdate.VersionString}");
                    // Don't show dialog on startup - just set flag
                    // The launcher will display a notification banner
                }
                else
                {
                    Logger.Info("No updates available");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates on startup", ex);
                // Silently fail - don't interrupt app startup
            }
        }

        /// <summary>
        /// Manual update check - called from "Check for Updates" button
        /// </summary>
        public async Task<bool> CheckForUpdatesManualAsync()
        {
            try
            {
                Logger.Info("Manual update check triggered");
                _availableUpdate = await _updateChecker.CheckForUpdatesAsync();

                if (_availableUpdate != null)
                {
                    Logger.Info($"Update available: {_availableUpdate.VersionString}");
                    return true;
                }
                else
                {
                    Logger.Info("No updates available");
                    MessageBox.Show(
                        "You're already running the latest version!",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates manually", ex);
                MessageBox.Show(
                    $"Failed to check for updates:\n{ex.Message}",
                    "Update Check Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Show the update dialog
        /// </summary>
        public bool ShowUpdateDialog(Window owner)
        {
            if (_availableUpdate == null)
            {
                Logger.Warning("ShowUpdateDialog called but no update available");
                return false;
            }

            var dialog = new UpdateDialog(_availableUpdate)
            {
                Owner = owner
            };

            var result = dialog.ShowDialog();

            if (result == true && dialog.ShouldUpdate)
            {
                Logger.Info("User chose to update");
                return true;
            }

            if (dialog.SkipThisVersion)
            {
                Logger.Info($"User chose to skip version {_availableUpdate.VersionString}");
                // TODO: Save skipped version to settings
            }

            return false;
        }

        /// <summary>
        /// Download and install the update
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync(Window owner)
        {
            if (_availableUpdate == null)
            {
                Logger.Error("DownloadAndInstallUpdateAsync called but no update available");
                return false;
            }

            try
            {
                // Create progress window
                var progressWindow = new Window
                {
                    Title = "Downloading Update",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = owner,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.White
                };

                var progressPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                var statusText = new System.Windows.Controls.TextBlock
                {
                    Text = $"Downloading Echo {_availableUpdate.VersionString}...",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var progressBar = new System.Windows.Controls.ProgressBar
                {
                    Height = 25,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                var detailsText = new System.Windows.Controls.TextBlock
                {
                    Text = "Starting download...",
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                progressPanel.Children.Add(statusText);
                progressPanel.Children.Add(progressBar);
                progressPanel.Children.Add(detailsText);
                progressWindow.Content = progressPanel;

                // Show progress window
                progressWindow.Show();

                // Setup progress tracking
                _updateDownloader.ProgressChanged += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = e.ProgressPercentage;
                        detailsText.Text = $"{e.BytesReceived / 1024 / 1024:F2} MB / {e.TotalBytes / 1024 / 1024:F2} MB ({e.ProgressPercentage}%)";
                    });
                };

                // Download the update
                Logger.Info($"Starting download from: {_availableUpdate.DownloadUrl}");
                var downloadPath = await _updateDownloader.DownloadUpdateAsync(
                    _availableUpdate.DownloadUrl,
                    _availableUpdate.FileName);

                Logger.Info($"Download completed: {downloadPath}");

                // Verify download
                statusText.Text = "Verifying download...";
                var isValid = await _updateDownloader.VerifyDownloadAsync(downloadPath, "");
                
                if (!isValid)
                {
                    progressWindow.Close();
                    MessageBox.Show(
                        "Download verification failed. Please try again.",
                        "Update Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // Close progress window
                progressWindow.Close();

                // Launch updater
                Logger.Info("Launching updater...");
                LaunchUpdater(downloadPath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to download and install update", ex);
                MessageBox.Show(
                    $"Failed to download update:\n{ex.Message}",
                    "Update Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Launch the EchoUpdater.exe to perform the update
        /// </summary>
        private void LaunchUpdater(string zipPath)
        {
            try
            {
                // Get paths
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                var updaterPath = Path.Combine(currentDir, "EchoUpdater.exe");
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(currentDir, "Echo.exe");
                var backupPath = Path.Combine(Path.GetTempPath(), $"Echo_Backup_{DateTime.Now:yyyyMMdd_HHmmss}");

                // Check if updater exists
                if (!File.Exists(updaterPath))
                {
                    Logger.Error($"EchoUpdater.exe not found at: {updaterPath}");
                    MessageBox.Show(
                        "Update installer not found. Please reinstall the application.",
                        "Update Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                Logger.Info($"Launching updater: {updaterPath}");
                Logger.Info($"Parameters: --zip \"{zipPath}\" --install \"{currentDir}\" --exe \"{exePath}\" --backup \"{backupPath}\"");

                // Start the updater process
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"--zip \"{zipPath}\" --install \"{currentDir}\" --exe \"{exePath}\" --backup \"{backupPath}\"",
                    UseShellExecute = true,
                    WorkingDirectory = currentDir
                };

                Process.Start(startInfo);

                // Close the current application
                Logger.Info("Shutting down application for update...");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to launch updater", ex);
                MessageBox.Show(
                    $"Failed to launch updater:\n{ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check if an update is available (for UI binding)
        /// </summary>
        public bool IsUpdateAvailable => _availableUpdate != null;

        /// <summary>
        /// Get the available update info (for UI display)
        /// </summary>
        public UpdateInfo? AvailableUpdate => _availableUpdate;
    }
}
