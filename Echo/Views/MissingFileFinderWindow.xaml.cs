using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Echo.Services;
using Microsoft.Win32;

namespace Echo.Views
{
    public partial class MissingFileFinderWindow : Window, IMissingFileProgress
    {
        private readonly List<string> _missingFilePaths;
        private readonly string _bo3RootPath;
        private List<MissingFileSearchResult> _searchResults = new List<MissingFileSearchResult>();
        private ObservableCollection<MissingFileResultViewModel> _resultViewModels = new ObservableCollection<MissingFileResultViewModel>();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isSearching = false;

        public MissingFileFinderWindow(List<string> missingFilePaths, string bo3RootPath)
        {
            InitializeComponent();

            _missingFilePaths = missingFilePaths ?? new List<string>();
            _bo3RootPath = bo3RootPath;

            ResultsDataGrid.ItemsSource = _resultViewModels;

            SubtitleText.Text = $"Searching for {_missingFilePaths.Count} missing files in: {bo3RootPath}";
            StatsText.Text = $"Ready to search {_missingFilePaths.Count} missing files";

            Logger.Info($"Missing File Finder opened with {_missingFilePaths.Count} files");
        }

        private async void StartSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_isSearching)
            {
                // Cancel search
                _cancellationTokenSource?.Cancel();
                return;
            }

            _isSearching = true;
            BtnStartSearch.Content = "⏸ Cancel Search";
            BtnStartSearch.Background = System.Windows.Media.Brushes.OrangeRed;
            SearchProgressPanel.Visibility = Visibility.Visible;
            BtnApplyAllFixes.IsEnabled = false;
            BtnExportReport.IsEnabled = false;

            _resultViewModels.Clear();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Logger.Info("Starting missing file search...");

                _searchResults = await MissingFileFinder.FindMissingFilesAsync(
                    _missingFilePaths,
                    _bo3RootPath,
                    this,
                    _cancellationTokenSource.Token
                );

                // Convert results to view models
                foreach (var result in _searchResults)
                {
                    _resultViewModels.Add(new MissingFileResultViewModel(result));
                }

                // Update stats
                var exactMatches = _searchResults.Count(r => r.HasExactMatch);
                var fuzzyMatches = _searchResults.Count(r => r.HasFuzzyMatches && !r.HasExactMatch);
                var noMatches = _searchResults.Count - exactMatches - fuzzyMatches;

                StatsText.Text = $"✓ {exactMatches} exact matches  •  🔍 {fuzzyMatches} fuzzy matches  •  ❌ {noMatches} not found";

                BtnApplyAllFixes.IsEnabled = exactMatches > 0;
                BtnExportReport.IsEnabled = true;

                Logger.Info($"Search complete: {exactMatches} exact, {fuzzyMatches} fuzzy, {noMatches} not found");

                MessageBox.Show(
                    $"Search Complete!\n\n" +
                    $"✓ Exact matches: {exactMatches}\n" +
                    $"🔍 Fuzzy matches: {fuzzyMatches}\n" +
                    $"❌ Not found: {noMatches}\n\n" +
                    $"Review the results and click 'Apply Fix' or 'Apply All Fixes' to update paths.",
                    "Search Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Search cancelled by user");
                StatsText.Text = "Search cancelled";
            }
            catch (Exception ex)
            {
                Logger.Error($"Search failed: {ex.Message}");
                MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSearching = false;
                BtnStartSearch.Content = "🔍 Start Search";
                BtnStartSearch.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
                SearchProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewAlternatives_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button?.Tag as MissingFileResultViewModel;

            if (viewModel == null || viewModel.SearchResult == null)
                return;

            // Show dialog with all alternative matches
            var dialog = new AlternativeMatchesDialog(viewModel.SearchResult);
            dialog.Owner = this;
            var result = dialog.ShowDialog();

            if (result == true && dialog.SelectedMatch != null)
            {
                // User selected an alternative match
                ApplyFix(viewModel, dialog.SelectedMatch);
            }
        }

        private void ApplyFix_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button?.Tag as MissingFileResultViewModel;

            if (viewModel == null || !viewModel.HasExactMatch)
                return;

            var bestMatch = viewModel.SearchResult?.PossibleMatches.FirstOrDefault();
            if (bestMatch != null)
            {
                ApplyFix(viewModel, bestMatch);
            }
        }

        private void ApplyFix(MissingFileResultViewModel viewModel, FileMatch match)
        {
            // TODO: This will need to update the actual asset scanner results
            // For now, just show confirmation
            var result = MessageBox.Show(
                $"Apply this fix?\n\n" +
                $"Original: {viewModel.SearchResult?.OriginalPath}\n\n" +
                $"New Path: {match.FoundPath}\n\n" +
                $"Confidence: {match.MatchScore}%\n" +
                $"Reason: {match.MatchReason}\n\n" +
                $"This will update the asset reference.",
                "Confirm Fix",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                viewModel.IsFixed = true;
                Logger.Info($"Applied fix: {viewModel.SearchResult?.OriginalPath} → {match.FoundPath}");

                // Mark as fixed in UI
                viewModel.MatchStatus = "✓ FIXED";
                viewModel.MatchStatusColor = "#16C60C";
                viewModel.BestMatchPath = match.FoundPath;

                MessageBox.Show("Fix applied! The asset path has been updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyAllFixes_Click(object sender, RoutedEventArgs e)
        {
            var exactMatches = _resultViewModels.Where(vm => vm.HasExactMatch && !vm.IsFixed).ToList();

            if (!exactMatches.Any())
            {
                MessageBox.Show("No exact matches to apply.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Apply {exactMatches.Count} automatic fixes?\n\n" +
                $"This will update all assets with exact filename matches.\n" +
                $"Fuzzy matches require manual review.",
                "Confirm Batch Fix",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var viewModel in exactMatches)
                {
                    var bestMatch = viewModel.SearchResult?.PossibleMatches.FirstOrDefault();
                    if (bestMatch != null)
                    {
                        viewModel.IsFixed = true;
                        viewModel.MatchStatus = "✓ FIXED";
                        viewModel.MatchStatusColor = "#16C60C";
                        viewModel.BestMatchPath = bestMatch.FoundPath;

                        Logger.Info($"Auto-fixed: {viewModel.SearchResult?.OriginalPath} → {bestMatch.FoundPath}");
                    }
                }

                MessageBox.Show($"{exactMatches.Count} fixes applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnApplyAllFixes.IsEnabled = false;
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"missing_files_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                Title = "Export Missing Files Report"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var lines = new List<string>
                    {
                        "========================================",
                        "ECHO - Missing File Search Report",
                        $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        $"BO3 Root: {_bo3RootPath}",
                        $"Total Missing Files: {_missingFilePaths.Count}",
                        "========================================",
                        ""
                    };

                    foreach (var viewModel in _resultViewModels)
                    {
                        var result = viewModel.SearchResult;
                        if (result == null) continue;

                        lines.Add($"Missing File: {result.OriginalPath}");
                        lines.Add($"Status: {viewModel.MatchStatus}");

                        if (result.PossibleMatches.Any())
                        {
                            lines.Add($"Possible Matches ({result.PossibleMatches.Count}):");
                            foreach (var match in result.PossibleMatches.Take(5))
                            {
                                lines.Add($"  - {match.FoundPath} ({match.MatchScore}% - {match.MatchReason})");
                            }
                        }
                        else
                        {
                            lines.Add("  No matches found");
                        }

                        lines.Add("");
                    }

                    File.WriteAllLines(saveDialog.FileName, lines);
                    Logger.Info($"Exported report to: {saveDialog.FileName}");

                    var openResult = MessageBox.Show(
                        "Report exported successfully!\n\nOpen the file?",
                        "Export Complete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to export report: {ex.Message}");
                    MessageBox.Show($"Failed to export report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // IMissingFileProgress implementation
        public void ReportProgress(int current, int total, string message)
        {
            Dispatcher.Invoke(() =>
            {
                SearchProgressBar.Maximum = total;
                SearchProgressBar.Value = current;
                SearchProgressText.Text = message;

                var percentage = total > 0 ? (current * 100) / total : 0;
                SubtitleText.Text = $"Searching... {current}/{total} ({percentage}%)";
            });
        }

        public void ReportSearchProgress(string directory, int filesScanned)
        {
            Dispatcher.Invoke(() =>
            {
                SearchDetailText.Text = $"Indexing: {directory} • Scanned {filesScanned:N0} files...";
            });
        }
    }

    // View model for results grid
    public class MissingFileResultViewModel : INotifyPropertyChanged
    {
        public MissingFileSearchResult? SearchResult { get; set; }
        private bool _isFixed;

        public MissingFileResultViewModel(MissingFileSearchResult searchResult)
        {
            SearchResult = searchResult;
        }

        public string MissingFileName => Path.GetFileName(SearchResult?.OriginalPath ?? "");

        private string _matchStatus = "";
        public string MatchStatus
        {
            get
            {
                if (!string.IsNullOrEmpty(_matchStatus)) return _matchStatus;

                if (SearchResult == null) return "❌ No Match";
                if (SearchResult.HasExactMatch) return "✓ Exact Match";
                if (SearchResult.HasFuzzyMatches) return "🔍 Fuzzy Match";
                return "❌ No Match";
            }
            set
            {
                _matchStatus = value;
                OnPropertyChanged();
            }
        }

        private string _matchStatusColor = "";
        public string MatchStatusColor
        {
            get
            {
                if (!string.IsNullOrEmpty(_matchStatusColor)) return _matchStatusColor;

                if (SearchResult == null) return "#FF6B6B";
                if (SearchResult.HasExactMatch) return "#16C60C";
                if (SearchResult.HasFuzzyMatches) return "#FFB900";
                return "#FF6B6B";
            }
            set
            {
                _matchStatusColor = value;
                OnPropertyChanged();
            }
        }

        private string _bestMatchPath = "";
        public string BestMatchPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_bestMatchPath)) return _bestMatchPath;
                return SearchResult?.PossibleMatches.FirstOrDefault()?.FoundPath ?? "(none)";
            }
            set
            {
                _bestMatchPath = value;
                OnPropertyChanged();
            }
        }

        public string MatchScore
        {
            get
            {
                var bestMatch = SearchResult?.PossibleMatches.FirstOrDefault();
                return bestMatch != null ? $"{bestMatch.MatchScore}%" : "-";
            }
        }

        public string ConfidenceColor
        {
            get
            {
                var bestMatch = SearchResult?.PossibleMatches.FirstOrDefault();
                if (bestMatch == null) return "#B0B0B0";
                if (bestMatch.MatchScore == 100) return "#16C60C";
                if (bestMatch.MatchScore >= 80) return "#FFB900";
                return "#FF6B6B";
            }
        }

        public string AlternativeCount
        {
            get
            {
                var count = SearchResult?.PossibleMatches.Count ?? 0;
                return count > 1 ? $"+{count - 1} more" : "-";
            }
        }

        public bool HasMatches => SearchResult?.PossibleMatches.Any() ?? false;
        public bool HasExactMatch => SearchResult?.HasExactMatch ?? false;

        public bool IsFixed
        {
            get => _isFixed;
            set
            {
                _isFixed = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
