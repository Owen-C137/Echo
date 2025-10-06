using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Echo.Services;

namespace Echo.Views
{
    public partial class GdtPackerWindow : Window
    {
        private List<string> _gdtFilePaths = new List<string>();
        private GdtParseResult? _parseResult;
        private ScanResult? _scanResult;
        private SoundAliasParseResult? _soundAliasResult;

        public GdtPackerWindow()
        {
            InitializeComponent();
            Logger.Info("GDT Package Manager opened");
            
            // Highlight first nav item
            HighlightNavButton(BtnScanCreate);
        }

        private void NavigateToScanCreate(object sender, RoutedEventArgs e)
        {
            ShowPanel(ScanCreatePanel);
            HighlightNavButton(BtnScanCreate);
            Logger.Info("Navigated to Scan & Create");
        }

        private void NavigateToAssetTree(object sender, RoutedEventArgs e)
        {
            ShowPanel(AssetTreePanel);
            HighlightNavButton(BtnAssetTree);
            Logger.Info("Navigated to Asset Tree");
        }

        private void ShowPanel(Grid panel)
        {
            ScanCreatePanel.Visibility = Visibility.Collapsed;
            AssetTreePanel.Visibility = Visibility.Collapsed;
            panel.Visibility = Visibility.Visible;
        }

        private void HighlightNavButton(Button button)
        {
            // Reset all buttons
            BtnScanCreate.Background = System.Windows.Media.Brushes.White;
            BtnAssetTree.Background = System.Windows.Media.Brushes.White;
            
            // Highlight selected
            button.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xE8, 0xF4, 0xFD));
        }

        private void BackToLauncher(object sender, RoutedEventArgs e)
        {
            Logger.Info("Returning to launcher");
            var launcher = new LauncherWindow();
            launcher.Show();
            this.Close();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open settings window", ex);
                MessageBox.Show($"Failed to open settings: {ex.Message}\n\nCheck Logs.txt for details.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGdtFiles(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "GDT Files (*.gdt)|*.gdt|All Files (*.*)|*.*",
                Multiselect = true,
                Title = "Select GDT Files"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (!_gdtFilePaths.Contains(file))
                    {
                        _gdtFilePaths.Add(file);
                        GdtFilesList.Items.Add(System.IO.Path.GetFileName(file));
                        Logger.Info($"Added GDT file: {file}");
                    }
                }
                
                // Reset scan results when files change
                _parseResult = null;
                _scanResult = null;
                CreatePackageButton.IsEnabled = false;
                ScanStatus.Text = "Ready to scan...";
                PackageStatus.Text = "Scan assets first before creating a package.";
            }
        }

        private void RemoveGdtFile(object sender, RoutedEventArgs e)
        {
            if (GdtFilesList.SelectedIndex >= 0)
            {
                var index = GdtFilesList.SelectedIndex;
                var fileName = GdtFilesList.SelectedItem?.ToString();
                
                _gdtFilePaths.RemoveAt(index);
                GdtFilesList.Items.RemoveAt(index);
                
                Logger.Info($"Removed GDT file: {fileName}");
                
                // Reset scan results when files change
                _parseResult = null;
                _scanResult = null;
                CreatePackageButton.IsEnabled = false;
                ScanStatus.Text = "Ready to scan...";
                PackageStatus.Text = "Scan assets first before creating a package.";
            }
        }

        private void StartScan(object sender, RoutedEventArgs e)
        {
            if (_gdtFilePaths.Count == 0)
            {
                MessageBox.Show("Please add at least one GDT file first.", "No Files", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var bo3Path = SettingsManager.CurrentSettings.BlackOps3Path;
            if (string.IsNullOrWhiteSpace(bo3Path) || !System.IO.Directory.Exists(bo3Path))
            {
                MessageBox.Show("Please configure your Black Ops 3 installation path in Settings first.", 
                    "BO3 Path Not Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ScanButton.IsEnabled = false;
                ScanStatus.Text = "Parsing GDT files...";
                Logger.Info($"Starting scan of {_gdtFilePaths.Count} GDT files");

                // Parse GDT files
                _parseResult = GdtParser.ParseMultipleGdtFiles(_gdtFilePaths);

                if (_parseResult.Errors.Count > 0)
                {
                    Logger.Warning($"GDT parsing completed with {_parseResult.Errors.Count} errors");
                }

                ScanStatus.Text = $"Found {_parseResult.TotalAssets} assets with {_parseResult.TotalFiles} file references. ";

                // Parse sound aliases if we found any sound alias references
                var totalSoundAliases = _parseResult.Assets.Sum(a => a.SoundAliases.Count);
                if (totalSoundAliases > 0 && SettingsManager.CurrentSettings.SoundAliasHandling != 2) // 2 = Skip sounds
                {
                    ScanStatus.Text += $"Parsing sound aliases ({totalSoundAliases} references)...";
                    _soundAliasResult = SoundAliasParser.ParseAllAliasCsvFiles(bo3Path);
                    Logger.Info($"Parsed {_soundAliasResult.TotalAliases} sound aliases from CSV files");
                }
                else
                {
                    _soundAliasResult = null;
                }

                ScanStatus.Text = "Scanning file system...";

                // Scan file system for assets (including sounds if available)
                _scanResult = AssetScanner.ScanAssets(_parseResult, bo3Path, _soundAliasResult, _gdtFilePaths);

                // Update UI with results
                var foundPercent = _scanResult.TotalAssets > 0 
                    ? (_scanResult.FoundAssets * 100.0 / _scanResult.TotalAssets).ToString("F1") 
                    : "0";

                var statusText = $"✓ Scan complete: {_scanResult.FoundAssets}/{_scanResult.TotalAssets} assets found ({foundPercent}%), " +
                                 $"Total size: {AssetScanner.FormatBytes(_scanResult.TotalSize)}";

                if (_scanResult.TotalSoundFiles > 0)
                {
                    statusText += $"\n🔊 {_scanResult.TotalSoundFiles} sound files found from {_scanResult.ReferencedSoundAliases.Count} aliases";
                }

                if (_scanResult.MissingAssets > 0)
                {
                    statusText += $"\n⚠️ Warning: {_scanResult.MissingAssets} assets are missing!";
                }

                ScanStatus.Text = statusText;

                // Populate asset tree
                PopulateAssetTree();

                // Enable package creation
                CreatePackageButton.IsEnabled = true;
                PackageStatus.Text = _scanResult.MissingAssets > 0
                    ? $"Ready to create package (with {_scanResult.MissingAssets} missing files)"
                    : "Ready to create package";

                Logger.Info($"Scan complete: {_scanResult.FoundAssets} found, {_scanResult.MissingAssets} missing, {_scanResult.TotalSoundFiles} sounds");
            }
            catch (Exception ex)
            {
                ScanStatus.Text = $"❌ Error: {ex.Message}";
                Logger.Error("Scan failed", ex);
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ScanButton.IsEnabled = true;
            }
        }

        private void PopulateAssetTree()
        {
            if (_scanResult == null) return;

            AssetTreeView.Items.Clear();

            // Group regular assets by type
            var groupedAssets = _scanResult.Assets
                .GroupBy(a => a.AssetType)
                .OrderBy(g => g.Key);

            foreach (var group in groupedAssets)
            {
                var typeNode = new System.Windows.Controls.TreeViewItem
                {
                    Header = $"{group.Key} ({group.Count()} assets)",
                    IsExpanded = true
                };

                foreach (var asset in group.OrderBy(a => a.AssetName))
                {
                    var assetNode = new System.Windows.Controls.TreeViewItem
                    {
                        Header = $"{asset.AssetName} - {asset.RelativePath} " +
                                (asset.Exists ? $"({AssetScanner.FormatBytes(asset.FileSize)})" : "[MISSING]")
                    };
                    
                    typeNode.Items.Add(assetNode);
                }

                AssetTreeView.Items.Add(typeNode);
            }

            // Add sound files group if any
            if (_scanResult.SoundFiles.Count > 0)
            {
                var soundNode = new System.Windows.Controls.TreeViewItem
                {
                    Header = $"🔊 Sound Files ({_scanResult.SoundFiles.Count} files from {_scanResult.ReferencedSoundAliases.Count} aliases)",
                    IsExpanded = false
                };

                // Group by alias name
                var soundsByAlias = _scanResult.SoundFiles
                    .GroupBy(s => s.AssetName)
                    .OrderBy(g => g.Key);

                foreach (var aliasGroup in soundsByAlias)
                {
                    var aliasNode = new System.Windows.Controls.TreeViewItem
                    {
                        Header = $"{aliasGroup.Key} ({aliasGroup.Count()} files)",
                        IsExpanded = false
                    };

                    foreach (var sound in aliasGroup)
                    {
                        var soundFileNode = new System.Windows.Controls.TreeViewItem
                        {
                            Header = $"{System.IO.Path.GetFileName(sound.RelativePath)} " +
                                    (sound.Exists ? $"({AssetScanner.FormatBytes(sound.FileSize)})" : "[MISSING]")
                        };
                        aliasNode.Items.Add(soundFileNode);
                    }

                    soundNode.Items.Add(aliasNode);
                }

                AssetTreeView.Items.Add(soundNode);
            }

            Logger.Info($"Asset tree populated with {groupedAssets.Count()} asset types + sound files");
        }

        private void CreatePackage(object sender, RoutedEventArgs e)
        {
            if (_scanResult == null || _gdtFilePaths.Count == 0)
            {
                MessageBox.Show("Please scan assets first before creating a package.", "Not Ready", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check for missing assets and ask user
            if (_scanResult.MissingAssets > 0)
            {
                var result = MessageBox.Show(
                    $"Warning: {_scanResult.MissingAssets} assets are missing!\n\n" +
                    $"A missing_files.txt report will be included in the package.\n\n" +
                    $"Do you want to continue creating the package?",
                    "Missing Assets",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    Logger.Info("Package creation cancelled due to missing assets");
                    return;
                }
            }

            // Get sound handling settings (no dialog needed - use settings)
            var settings = SettingsManager.CurrentSettings;
            int soundMode = settings.SoundAliasHandling; // 0=Consolidated, 1=CopyFull, 2=Skip
            string consolidatedName = "echo_consolidated";
            
            Logger.Info($"Using sound handling mode from settings: {soundMode} (0=Consolidated, 1=CopyFull, 2=Skip)");

            // Show progress window and create package asynchronously
            var progressWindow = new PackageProgressWindow();
            progressWindow.Owner = this;

            CreatePackageButton.IsEnabled = false;
            PackageStatus.Text = "Creating package...";
            Logger.Info("Starting package creation");

            var bo3Path = SettingsManager.CurrentSettings.BlackOps3Path;
            var packageName = PackageNameTextBox.Text.Trim();

            // Create progress reporter
            var progressReporter = new PackageProgressReporter(progressWindow);

            // Run package creation on background thread
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    return PackageCreator.CreatePackage(
                        _gdtFilePaths,
                        _scanResult,
                        packageName,
                        bo3Path,
                        _soundAliasResult,
                        soundMode,
                        consolidatedName,
                        progressReporter);
                }
                catch (Exception ex)
                {
                    Logger.Error("Package creation failed", ex);
                    progressWindow.SetComplete(false, $"Error: {ex.Message}");
                    return new Services.PackageResult { Success = false };
                }
            });

            // Handle completion
            task.ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    CreatePackageButton.IsEnabled = true;

                    var packageResult = t.Result;

                    if (packageResult.Success)
                    {
                        progressWindow.SetComplete(true,
                            $"Package created: {AssetScanner.FormatBytes(packageResult.PackageSize)} in {packageResult.Duration.TotalSeconds:F1}s",
                            packageResult.PackagePath);

                        PackageStatus.Text = $"✓ Package created: {System.IO.Path.GetFileName(packageResult.PackagePath)} " +
                                            $"({AssetScanner.FormatBytes(packageResult.PackageSize)}) in {packageResult.Duration.TotalSeconds:F2}s";

                        Logger.Info($"Package created successfully: {packageResult.PackagePath}");
                    }
                    else
                    {
                        progressWindow.SetComplete(false, "Package creation failed");
                        PackageStatus.Text = "❌ Package creation failed!";
                        Logger.Error("Package creation failed");
                    }
                });
            });

            // Show progress window (modal)
            progressWindow.ShowDialog();
        }
    }

    // Progress reporter implementation
    internal class PackageProgressReporter : Services.IPackageProgress
    {
        private readonly PackageProgressWindow _window;

        public PackageProgressReporter(PackageProgressWindow window)
        {
            _window = window;
        }

        public void ReportProgress(int current, int total, string message)
        {
            _window.UpdateProgress(current, total, message);
        }

        public void ReportLog(string message)
        {
            _window.AddLog(message);
        }
    }
}
