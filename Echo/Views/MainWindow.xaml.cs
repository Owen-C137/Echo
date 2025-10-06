using System.Windows;
using Echo.Services;

namespace Echo.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Logger.Info("Main window initialized");
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var settings = SettingsManager.CurrentSettings;
            if (string.IsNullOrWhiteSpace(settings.BlackOps3Path))
            {
                StatusBarText.Text = "Not Configured";
                Bo3PathStatus.Text = "BO3 Path: Not Configured";
            }
            else
            {
                StatusBarText.Text = "Ready";
                Bo3PathStatus.Text = $"BO3 Path: {settings.BlackOps3Path}";
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Opening settings window");
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                UpdateStatus();
                Logger.Info("Settings updated");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Exit requested by user");
            Close();
        }

        private void ParseAGDT_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Parse AGDT clicked");
            MessageBox.Show("AGDT parsing functionality will be implemented here.", 
                          "Parse AGDT", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }

        private void PackAGDT_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Pack AGDT clicked");
            MessageBox.Show("AGDT packing functionality will be implemented here.", 
                          "Pack AGDT", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("About dialog opened");
            MessageBox.Show("Echo\n\nBlack Ops III AGDT Parser/Packer\nVersion 1.0.0\n\nA tool for parsing and packing AGDT files for Call of Duty: Black Ops III.", 
                          "About Echo", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }

        private void Docs_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Opening documentation window");
            var docsWindow = new DocsWindow();
            docsWindow.Owner = this;
            docsWindow.ShowDialog();
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("View logs requested");
            Logger.OpenLogFile();
        }
    }
}
