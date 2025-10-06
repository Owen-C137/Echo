using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Echo.Services;
using Echo.Models;
using System.IO;

namespace Echo.Views
{
    public partial class GdtPackerWindow : Window
    {
        private List<string> _gdtFilePaths = new List<string>();
        private GdtParseResult? _parseResult;
        private ScanResult? _scanResult;
        private SoundAliasParseResult? _soundAliasResult;
        
        // Asset View Models
        private ObservableCollection<AssetViewModel> _allAssets = new ObservableCollection<AssetViewModel>();
        private ObservableCollection<AssetViewModel> _filteredAssets = new ObservableCollection<AssetViewModel>();
        
        // GDT File View Models
        private ObservableCollection<GdtFileItem> _gdtFiles = new ObservableCollection<GdtFileItem>();

        public GdtPackerWindow()
        {
            InitializeComponent();
            Logger.Info("GDT Package Manager opened");
            
            // Bind GDT files grid
            GdtFilesGrid.ItemsSource = _gdtFiles;
            _gdtFiles.CollectionChanged += (s, e) => UpdateDashboard();
            
            // Highlight first nav item
            HighlightNavButton(BtnScanCreate);
            
            // Update dashboard
            UpdateDashboard();
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
            // Just close this window - the launcher will show automatically
            // because it's the Owner and was hidden (not closed)
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
                AddGdtFilesToList(dialog.FileNames);
            }
        }

        private void AddGdtFilesToList(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                if (!_gdtFilePaths.Contains(file))
                {
                    var fileInfo = new FileInfo(file);
                    
                    // Quick estimate: ~50 bytes per asset on average
                    var estimatedAssets = (int)(fileInfo.Length / 50);
                    
                    var gdtItem = new GdtFileItem
                    {
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        FileSize = fileInfo.Length,
                        FileSizeFormatted = AssetScanner.FormatBytes(fileInfo.Length),
                        EstimatedAssets = estimatedAssets,
                        LastModified = fileInfo.LastWriteTime,
                        LastModifiedFormatted = GetFriendlyDate(fileInfo.LastWriteTime),
                        IsSelected = true
                    };

                    _gdtFiles.Add(gdtItem);
                    _gdtFilePaths.Add(file);
                    Logger.Info($"Added GDT file: {file}");
                }
            }
            
            // Reset scan results when files change
            ResetScanResults();
        }

        private string GetFriendlyDate(DateTime date)
        {
            var now = DateTime.Now;
            var diff = now - date;

            if (diff.TotalHours < 1) return "Just now";
            if (diff.TotalHours < 24) return "Today";
            if (diff.TotalDays < 2) return "Yesterday";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
            return date.ToString("MMM dd, yyyy");
        }

        private void RemoveSelectedGdtFiles(object sender, RoutedEventArgs e)
        {
            var selectedItems = _gdtFiles.Where(f => f.IsSelected).ToList();
            
            foreach (var item in selectedItems)
            {
                _gdtFiles.Remove(item);
                _gdtFilePaths.Remove(item.FullPath);
                Logger.Info($"Removed GDT file: {item.FileName}");
            }
            
            ResetScanResults();
        }

        private void ClearAllGdtFiles(object sender, RoutedEventArgs e)
        {
            if (_gdtFiles.Count == 0) return;

            var result = MessageBox.Show(
                $"Remove all {_gdtFiles.Count} GDT files?",
                "Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _gdtFiles.Clear();
                _gdtFilePaths.Clear();
                ResetScanResults();
                Logger.Info("Cleared all GDT files");
            }
        }

        private void GdtDropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void GdtDropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var gdtFiles = files.Where(f => f.EndsWith(".gdt", StringComparison.OrdinalIgnoreCase)).ToArray();
                
                if (gdtFiles.Length > 0)
                {
                    AddGdtFilesToList(gdtFiles);
                    Logger.Info($"Dropped {gdtFiles.Length} GDT files");
                }
                else
                {
                    MessageBox.Show("No GDT files found in dropped items.", "Invalid Files",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private string GenerateAutoPackageName()
        {
            if (_gdtFiles.Count == 0) return "";
            if (_gdtFiles.Count == 1) return Path.GetFileNameWithoutExtension(_gdtFiles[0].FileName);
            
            // Use first GDT file name + count
            return $"{Path.GetFileNameWithoutExtension(_gdtFiles[0].FileName)}_and_{_gdtFiles.Count - 1}_more";
        }

        private void UpdateDashboard()
        {
            // Enable/disable scan button
            ScanButton.IsEnabled = _gdtFiles.Count > 0;
        }

        private void ResetScanResults()
        {
            _parseResult = null;
            _scanResult = null;
            _allAssets.Clear();
            ScanStatusText.Text = "Ready to scan...";
        }

        private void RemoveGdtFile(object sender, RoutedEventArgs e)
        {
            // Keep for backward compatibility - redirects to new method
            RemoveSelectedGdtFiles(sender, e);
        }

        // Scan GDT Files (no package creation - just redirect to Asset Browser)
        private void ScanGdtFiles(object sender, RoutedEventArgs e)
        {
            if (_gdtFilePaths.Count == 0)
            {
                MessageBox.Show("Please add at least one GDT file first.", "No Files", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var bo3Path = SettingsManager.CurrentSettings.BlackOps3Path;
            if (string.IsNullOrWhiteSpace(bo3Path) || !Directory.Exists(bo3Path))
            {
                MessageBox.Show("Please configure your Black Ops 3 installation path in Settings first.", 
                    "BO3 Path Not Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ScanButton.IsEnabled = false;
                ScanStatusText.Text = "Parsing GDT files...";
                Logger.Info($"Starting scan of {_gdtFilePaths.Count} GDT files");

                // Parse GDT files
                _parseResult = GdtParser.ParseMultipleGdtFiles(_gdtFilePaths);

                if (_parseResult.Errors.Count > 0)
                {
                    Logger.Warning($"GDT parsing completed with {_parseResult.Errors.Count} errors");
                }

                ScanStatusText.Text = $"Found {_parseResult.TotalAssets} assets. ";

                // Parse sound aliases (use setting from Settings window)
                var totalSoundAliases = _parseResult.Assets.Sum(a => a.SoundAliases.Count);
                var soundMode = SettingsManager.CurrentSettings.SoundAliasHandling; // 0=Consolidated, 1=CopyFull, 2=Skip

                if (totalSoundAliases > 0 && soundMode != 2)
                {
                    ScanStatusText.Text += $"Parsing sound aliases ({totalSoundAliases} references)...";
                    _soundAliasResult = SoundAliasParser.ParseAllAliasCsvFiles(bo3Path);
                    Logger.Info($"Parsed {_soundAliasResult.TotalAliases} sound aliases");
                }
                else
                {
                    _soundAliasResult = null;
                }

                ScanStatusText.Text = "Scanning file system...";

                // Scan file system
                _scanResult = AssetScanner.ScanAssets(_parseResult, bo3Path, _soundAliasResult, _gdtFilePaths);

                // Update status
                var foundPercent = _scanResult.TotalAssets > 0 
                    ? (_scanResult.FoundAssets * 100.0 / _scanResult.TotalAssets).ToString("F1") 
                    : "0";

                ScanStatusText.Text = $"✓ Scan complete: {_scanResult.FoundAssets}/{_scanResult.TotalAssets} found ({foundPercent}%)";

                if (_scanResult.MissingAssets > 0)
                {
                    ScanStatusText.Text += $"\n⚠️ {_scanResult.MissingAssets} assets missing!";
                }

                // Populate asset browser
                PopulateAssetTree();

                Logger.Info($"Scan complete: {_scanResult.FoundAssets} found, {_scanResult.MissingAssets} missing");

                // Show/hide Find Missing Files button
                if (_scanResult.MissingAssets > 0)
                {
                    FindMissingFilesButton.Visibility = Visibility.Visible;
                    FindMissingFilesButton.Content = $"🔍 Find {_scanResult.MissingAssets} Missing Files";
                }
                else
                {
                    FindMissingFilesButton.Visibility = Visibility.Collapsed;
                }

                // Switch to Asset Browser tab (change panel visibility)
                ScanCreatePanel.Visibility = Visibility.Collapsed;
                AssetTreePanel.Visibility = Visibility.Visible;

                // Show success message
                MessageBox.Show(
                    $"Scan complete!\n\n" +
                    $"✓ {_scanResult.FoundAssets}/{_scanResult.TotalAssets} assets found ({foundPercent}%)\n" +
                    (_scanResult.MissingAssets > 0 ? $"⚠️ {_scanResult.MissingAssets} assets missing\n\n" : "\n") +
                    $"Select assets in the Asset Browser to create a package.",
                    "Scan Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ScanStatusText.Text = $"❌ Error: {ex.Message}";
                Logger.Error("Scan failed", ex);
                MessageBox.Show($"Scan failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ScanButton.IsEnabled = true;
            }
        }

        // Asset Browser - Create Package from Selected Assets
        private void CreatePackage(object sender, RoutedEventArgs e)
        {
            if (_scanResult == null || _gdtFilePaths.Count == 0)
            {
                MessageBox.Show("Please scan assets first before creating a package.", "Not Ready",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get selected assets count
            var selectedCount = _allAssets.Count(a => a.IsSelected);
            if (selectedCount == 0)
            {
                MessageBox.Show("Please select at least one asset to include in the package.", "No Assets Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create filtered scan result with only selected assets
            var selectedAssets = _allAssets.Where(a => a.IsSelected && a.ScannedAssetRef != null).Select(a => a.ScannedAssetRef!).ToList();

            var filteredScanResult = new ScanResult
            {
                Assets = selectedAssets.Where(a => a.AssetType != "Sound").ToList(),
                SoundFiles = selectedAssets.Where(a => a.AssetType == "Sound").ToList(),
                ReferencedSoundAliases = _scanResult.ReferencedSoundAliases,
                TotalAssets = selectedCount,
                MissingAssets = _allAssets.Count(a => a.IsSelected && !a.Exists),
                TotalSize = _allAssets.Where(a => a.IsSelected).Sum(a => a.FileSize)
            };

            // Check for missing assets in selection
            if (filteredScanResult.MissingAssets > 0)
            {
                var result = MessageBox.Show(
                    $"Warning: {filteredScanResult.MissingAssets} selected assets are missing!\n\n" +
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

            // Get package name (use textbox or auto-generate)
            var packageName = string.IsNullOrWhiteSpace(PackageNameTextBox.Text)
                ? $"package_{DateTime.Now:yyyyMMdd_HHmmss}"
                : PackageNameTextBox.Text.Trim();

            // Get sound handling settings
            var settings = SettingsManager.CurrentSettings;
            int soundMode = settings.SoundAliasHandling;
            string consolidatedName = "echo_consolidated";

            Logger.Info($"Creating package from Asset Browser with {selectedCount} selected assets");

            // Show progress window and create package asynchronously
            var progressWindow = new PackageProgressWindow();
            progressWindow.Owner = this;

            var bo3Path = SettingsManager.CurrentSettings.BlackOps3Path;

            // Create progress reporter
            var progressReporter = new PackageProgressReporter(progressWindow);

            // Run package creation on background thread
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    return PackageCreator.CreatePackage(
                        _gdtFilePaths,
                        filteredScanResult,
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
                    var packageResult = t.Result;

                    if (packageResult.Success)
                    {
                        progressWindow.SetComplete(true,
                            $"Package created: {AssetScanner.FormatBytes(packageResult.PackageSize)} in {packageResult.Duration.TotalSeconds:F1}s",
                            packageResult.PackagePath);

                        Logger.Info($"Package created successfully: {packageResult.PackagePath}");

                        // Optionally open folder
                        var openResult = MessageBox.Show(
                            $"Package created successfully!\n\n" +
                            $"Location: {packageResult.PackagePath}\n" +
                            $"Size: {AssetScanner.FormatBytes(packageResult.PackageSize)}\n\n" +
                            $"Open package folder?",
                            "Success",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (openResult == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{packageResult.PackagePath}\"");
                        }
                    }
                    else
                    {
                        progressWindow.SetComplete(false, "Package creation failed");
                        Logger.Error("Package creation failed");
                    }
                });
            });

            // Show progress window (modal)
            progressWindow.ShowDialog();
        }

        private void PopulateAssetTree()
        {
            if (_scanResult == null) return;

            _allAssets.Clear();

            // Convert all assets to view models
            foreach (var asset in _scanResult.Assets)
            {
                var viewModel = new AssetViewModel
                {
                    AssetName = asset.AssetName,
                    AssetType = asset.AssetType,
                    RelativePath = asset.RelativePath,
                    FileSize = asset.FileSize,
                    FileSizeFormatted = asset.Exists ? AssetScanner.FormatBytes(asset.FileSize) : "N/A",
                    Exists = asset.Exists,
                    IsSelected = true, // Select all by default
                    ScannedAssetRef = asset
                };
                _allAssets.Add(viewModel);
            }

            // Add sound files
            foreach (var sound in _scanResult.SoundFiles)
            {
                var viewModel = new AssetViewModel
                {
                    AssetName = sound.AssetName,
                    AssetType = "Sound",
                    RelativePath = sound.RelativePath,
                    FileSize = sound.FileSize,
                    FileSizeFormatted = sound.Exists ? AssetScanner.FormatBytes(sound.FileSize) : "N/A",
                    Exists = sound.Exists,
                    IsSelected = true,
                    ScannedAssetRef = sound
                };
                _allAssets.Add(viewModel);
            }

            // Populate asset type tree
            PopulateAssetTypeTree();

            // Show all assets in grid by default
            RefreshAssetGrid();

            // Update statistics
            UpdateStatistics();

            Logger.Info($"Asset tree populated with {_allAssets.Count} total assets");
        }

        private void PopulateAssetTypeTree()
        {
            AssetTypeList.Items.Clear();

            // Add "All Assets" option first
            AssetTypeList.Items.Add(new AssetTypeItem
            {
                Icon = "📦",
                TypeName = "All Assets",
                TypeKey = "ALL",
                Count = _allAssets.Count
            });

            // Group by asset type
            var assetTypes = _allAssets
                .GroupBy(a => a.AssetType)
                .OrderBy(g => g.Key);

            foreach (var typeGroup in assetTypes)
            {
                AssetTypeList.Items.Add(new AssetTypeItem
                {
                    Icon = GetAssetTypeIcon(typeGroup.Key),
                    TypeName = typeGroup.Key,
                    TypeKey = typeGroup.Key,
                    Count = typeGroup.Count()
                });
            }

            // Select "All Assets" by default
            AssetTypeList.SelectedIndex = 0;
        }

        private string GetAssetTypeIcon(string assetType)
        {
            return assetType.ToLower() switch
            {
                "xmodel" => "📦",
                "material" => "🎨",
                "image" => "🖼️",
                "sound" => "🎵",
                "xanim" => "🎬",
                "rawfile" => "📄",
                "weapon" => "🔫",
                "vehicle" => "🚗",
                "fx" => "✨",
                "localize" => "🌐",
                _ => "📁"
            };
        }

        private void RefreshAssetGrid(string? filterType = null)
        {
            _filteredAssets.Clear();

            var searchText = SearchBox.Text.ToLower();
            var showMissingOnly = FilterShowMissingOnly.IsChecked == true;
            var showSelectedOnly = FilterShowSelectedOnly.IsChecked == true;

            var filtered = _allAssets.AsEnumerable();

            // Filter by type
            if (!string.IsNullOrEmpty(filterType) && filterType != "ALL")
            {
                filtered = filtered.Where(a => a.AssetType == filterType);
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(a =>
                    a.AssetName.ToLower().Contains(searchText) ||
                    a.AssetType.ToLower().Contains(searchText) ||
                    a.RelativePath.ToLower().Contains(searchText));
            }

            // Filter by missing
            if (showMissingOnly)
            {
                filtered = filtered.Where(a => !a.Exists);
            }

            // Filter by selected
            if (showSelectedOnly)
            {
                filtered = filtered.Where(a => a.IsSelected);
            }

            foreach (var asset in filtered)
            {
                _filteredAssets.Add(asset);
            }

            AssetDataGrid.ItemsSource = _filteredAssets;

            // Update grid title
            if (!string.IsNullOrEmpty(filterType) && filterType != "ALL")
            {
                AssetGridTitle.Text = $"{filterType.ToUpper()} ({_filteredAssets.Count})";
            }
            else
            {
                AssetGridTitle.Text = $"ALL ASSETS ({_filteredAssets.Count})";
            }
        }

        private void UpdateStatistics()
        {
            var totalAssets = _allAssets.Count;
            var totalSize = _allAssets.Sum(a => a.FileSize);
            var selectedCount = _allAssets.Count(a => a.IsSelected);
            var missingCount = _allAssets.Count(a => !a.Exists);

            StatsTotal.Text = totalAssets.ToString();
            StatsSize.Text = AssetScanner.FormatBytes(totalSize);
            StatsSelected.Text = selectedCount.ToString();
            StatsMissing.Text = missingCount.ToString();
        }

        // Event Handlers
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshAssetGrid(GetCurrentFilterType());
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            RefreshAssetGrid(GetCurrentFilterType());
        }

        private void AssetTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssetTypeList.SelectedItem is AssetTypeItem selectedItem)
            {
                RefreshAssetGrid(selectedItem.TypeKey);
            }
        }

        private void AssetDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssetDataGrid.SelectedItem is AssetViewModel selected)
            {
                DetailsAssetName.Text = $"📝 {selected.AssetName}";
                DetailsFullPath.Text = $"Full Path: {selected.RelativePath}";
            }
            else
            {
                DetailsAssetName.Text = "📝 Select an asset to view details";
                DetailsFullPath.Text = "";
            }
        }

        private void SelectAllAssets(object sender, RoutedEventArgs e)
        {
            foreach (var asset in _allAssets)
            {
                asset.IsSelected = true;
            }
            UpdateStatistics();
            Logger.Info("All assets selected");
        }

        private void DeselectAllAssets(object sender, RoutedEventArgs e)
        {
            foreach (var asset in _allAssets)
            {
                asset.IsSelected = false;
            }
            UpdateStatistics();
            Logger.Info("All assets deselected");
        }

        private void RefreshAssetTree(object sender, RoutedEventArgs e)
        {
            PopulateAssetTree();
            Logger.Info("Asset tree refreshed");
        }

        private void ExportAssetList(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "AssetList.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var lines = new List<string>
                    {
                        "Asset Name,Type,Size,Status,Path"
                    };

                    foreach (var asset in _allAssets.Where(a => a.IsSelected))
                    {
                        lines.Add($"\"{asset.AssetName}\",\"{asset.AssetType}\",\"{asset.FileSizeFormatted}\",\"{asset.StatusText}\",\"{asset.RelativePath}\"");
                    }

                    File.WriteAllLines(saveDialog.FileName, lines);
                    MessageBox.Show($"Asset list exported successfully!\n\n{lines.Count - 1} assets exported.", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logger.Info($"Asset list exported to {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export asset list: {ex.Message}", "Export Failed", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Error($"Failed to export asset list: {ex.Message}");
                }
            }
        }

        private string? GetCurrentFilterType()
        {
            if (AssetTypeList.SelectedItem is AssetTypeItem selectedItem)
            {
                return selectedItem.TypeKey;
            }
            return null;
        }

        private void FindMissingFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_scanResult == null || !_scanResult.MissingFiles.Any())
            {
                MessageBox.Show("No missing files to search for.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Logger.Info($"Opening Missing File Finder for {_scanResult.MissingFiles.Count} missing files");

            try
            {
                var bo3RootPath = SettingsManager.CurrentSettings.BlackOps3Path;
                if (string.IsNullOrWhiteSpace(bo3RootPath))
                {
                    MessageBox.Show(
                        "BO3 root path is not configured.\n\nPlease configure it in Settings first.",
                        "Configuration Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var finderWindow = new MissingFileFinderWindow(_scanResult.MissingFiles, bo3RootPath);
                finderWindow.Owner = this;
                finderWindow.ShowDialog();

                // TODO: After finding files, user may want to re-scan to update the results
                // For now, just show a message suggesting to re-scan
                if (_scanResult.MissingFiles.Any())
                {
                    var rescanResult = MessageBox.Show(
                        "Would you like to re-scan the GDT files to check if any fixes resolved missing files?",
                        "Re-scan Assets?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (rescanResult == MessageBoxResult.Yes)
                    {
                        // Switch back to scan panel and trigger re-scan
                        ScanCreatePanel.Visibility = Visibility.Visible;
                        AssetTreePanel.Visibility = Visibility.Collapsed;
                        ScanGdtFiles(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open Missing File Finder: {ex.Message}");
                MessageBox.Show($"Failed to open Missing File Finder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
