using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Echo.Services;

namespace Echo.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            HighlightNavButton(BtnGeneral);
            Logger.Info("Settings window opened");
        }

        private void LoadSettings()
        {
            var settings = SettingsManager.CurrentSettings;
            
            // General
            BlackOps3PathTextBox.Text = settings.BlackOps3Path;
            
            // Packaging
            OutputDirectoryTextBox.Text = settings.PackageOutputDirectory ?? 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packaged");
            
            CompressionLevelComboBox.SelectedIndex = settings.CompressionLevel;
            ArchiveStructureComboBox.SelectedIndex = settings.ArchiveStructure;
            IncludeSourceGdtCheckBox.IsChecked = settings.IncludeSourceGdt;
            ValidateAssetsCheckBox.IsChecked = settings.ValidateAssets;
            SoundAliasHandlingComboBox.SelectedIndex = settings.SoundAliasHandling;
            IncludeAttachmentGdtCheckBox.IsChecked = settings.IncludeAttachmentGdt;
            
            // Advanced
            EnableLoggingCheckBox.IsChecked = settings.EnableLogging;
            AutoOpenLogsCheckBox.IsChecked = settings.AutoOpenLogsOnError;
            PackageNamePatternTextBox.Text = settings.PackageNamePattern ?? "echo_packaged_{date}_{time}";
        }

        private void NavigateToGeneral(object sender, RoutedEventArgs e)
        {
            ShowPanel(GeneralPanel);
            HighlightNavButton(BtnGeneral);
        }

        private void NavigateToPackaging(object sender, RoutedEventArgs e)
        {
            ShowPanel(PackagingPanel);
            HighlightNavButton(BtnPackaging);
        }

        private void NavigateToAdvanced(object sender, RoutedEventArgs e)
        {
            ShowPanel(AdvancedPanel);
            HighlightNavButton(BtnAdvanced);
        }

        private void ShowPanel(ScrollViewer panel)
        {
            GeneralPanel.Visibility = Visibility.Collapsed;
            PackagingPanel.Visibility = Visibility.Collapsed;
            AdvancedPanel.Visibility = Visibility.Collapsed;
            panel.Visibility = Visibility.Visible;
        }

        private void HighlightNavButton(Button button)
        {
            BtnGeneral.Background = System.Windows.Media.Brushes.White;
            BtnPackaging.Background = System.Windows.Media.Brushes.White;
            BtnAdvanced.Background = System.Windows.Media.Brushes.White;
            
            button.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xE8, 0xF4, 0xFD));
        }

        private void BrowseBlackOps3Path_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Black Ops III Installation Folder",
                InitialDirectory = string.IsNullOrWhiteSpace(BlackOps3PathTextBox.Text) 
                    ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) 
                    : BlackOps3PathTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                BlackOps3PathTextBox.Text = dialog.FolderName;
                Logger.Info($"Black Ops III path selected: {dialog.FolderName}");
            }
        }

        private void BrowseOutputDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Package Output Directory",
                InitialDirectory = string.IsNullOrWhiteSpace(OutputDirectoryTextBox.Text) 
                    ? AppDomain.CurrentDomain.BaseDirectory 
                    : OutputDirectoryTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                OutputDirectoryTextBox.Text = dialog.FolderName;
                Logger.Info($"Package output directory selected: {dialog.FolderName}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate BO3 path if provided
            if (!string.IsNullOrWhiteSpace(BlackOps3PathTextBox.Text))
            {
                if (!Directory.Exists(BlackOps3PathTextBox.Text))
                {
                    MessageBox.Show("The specified Black Ops III path does not exist. Please select a valid directory.", 
                                  "Invalid Path", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    Logger.Warning($"Invalid Black Ops III path attempted: {BlackOps3PathTextBox.Text}");
                    return;
                }
            }

            // Validate output directory
            if (!string.IsNullOrWhiteSpace(OutputDirectoryTextBox.Text))
            {
                try
                {
                    if (!Directory.Exists(OutputDirectoryTextBox.Text))
                    {
                        Directory.CreateDirectory(OutputDirectoryTextBox.Text);
                        Logger.Info($"Created output directory: {OutputDirectoryTextBox.Text}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create output directory: {ex.Message}", 
                                  "Error", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                    Logger.Error("Failed to create output directory", ex);
                    return;
                }
            }

            // Save all settings
            var settings = SettingsManager.CurrentSettings;
            
            // General
            settings.BlackOps3Path = BlackOps3PathTextBox.Text;
            
            // Packaging
            settings.PackageOutputDirectory = OutputDirectoryTextBox.Text;
            settings.CompressionLevel = CompressionLevelComboBox.SelectedIndex;
            settings.ArchiveStructure = ArchiveStructureComboBox.SelectedIndex;
            settings.IncludeSourceGdt = IncludeSourceGdtCheckBox.IsChecked ?? true;
            settings.ValidateAssets = ValidateAssetsCheckBox.IsChecked ?? true;
            settings.SoundAliasHandling = SoundAliasHandlingComboBox.SelectedIndex;
            settings.IncludeAttachmentGdt = IncludeAttachmentGdtCheckBox.IsChecked ?? true;
            
            // Advanced
            settings.EnableLogging = EnableLoggingCheckBox.IsChecked ?? true;
            settings.AutoOpenLogsOnError = AutoOpenLogsCheckBox.IsChecked ?? false;
            settings.PackageNamePattern = PackageNamePatternTextBox.Text;
            
            SettingsManager.Save();
            
            Logger.Info("Settings saved successfully");
            MessageBox.Show("Settings saved successfully!", 
                          "Success", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Settings window cancelled");
            DialogResult = false;
            Close();
        }
    }
}
