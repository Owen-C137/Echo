using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Echo.Views
{
    public partial class PackageProgressWindow : Window
    {
        private string? _packagePath;

        public PackageProgressWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int current, int total, string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (total > 0)
                {
                    ProgressBar.Value = (current * 100.0) / total;
                    ProgressText.Text = $"{message} ({current}/{total})";
                }
                else
                {
                    ProgressText.Text = message;
                }
            });
        }

        public void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(LogText.Text))
                {
                    LogText.Text += Environment.NewLine;
                }
                LogText.Text += $"[{DateTime.Now:HH:mm:ss}] {message}";
                
                // Auto-scroll to bottom
                LogScrollViewer.ScrollToEnd();
            });
        }

        public void SetComplete(bool success, string message, string? packagePath = null)
        {
            Dispatcher.Invoke(() =>
            {
                _packagePath = packagePath;

                if (success)
                {
                    TitleText.Text = "✓ Package created successfully!";
                    ProgressBar.Value = 100;
                    ProgressText.Text = message;
                }
                else
                {
                    TitleText.Text = "✗ Package creation failed";
                    ProgressText.Text = message;
                    OpenFolderButton.Visibility = Visibility.Collapsed;
                }
                
                ButtonPanel.Visibility = Visibility.Visible;
            });
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_packagePath) && File.Exists(_packagePath))
            {
                try
                {
                    var folderPath = Path.GetDirectoryName(_packagePath);
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{_packagePath}\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
