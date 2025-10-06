using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using Echo.Services;

namespace Echo.Views
{
    public partial class AlternativeMatchesDialog : Window
    {
        public FileMatch? SelectedMatch { get; private set; }

        public AlternativeMatchesDialog(MissingFileSearchResult searchResult)
        {
            InitializeComponent();

            MissingFileText.Text = $"Missing: {searchResult.OriginalPath}";

            // Convert matches to view models and bind
            var matchViewModels = searchResult.PossibleMatches
                .Select(m => new FileMatchViewModel(m))
                .ToList();

            MatchesListBox.ItemsSource = matchViewModels;

            // Select the first item by default
            if (matchViewModels.Any())
            {
                MatchesListBox.SelectedIndex = 0;
            }
        }

        private void UseSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedViewModel = MatchesListBox.SelectedItem as FileMatchViewModel;
            if (selectedViewModel != null)
            {
                SelectedMatch = selectedViewModel.FileMatch;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a match to use.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // View model for file matches
    public class FileMatchViewModel
    {
        public FileMatch FileMatch { get; set; }

        public FileMatchViewModel(FileMatch fileMatch)
        {
            FileMatch = fileMatch;
        }

        public string FoundPath => FileMatch.FoundPath;
        public string FullPath => FileMatch.FullPath;
        public int MatchScore => FileMatch.MatchScore;
        public string MatchReason => FileMatch.MatchReason;
        
        public string FileSizeFormatted
        {
            get
            {
                if (FileMatch.FileSize < 1024)
                    return $"{FileMatch.FileSize} B";
                else if (FileMatch.FileSize < 1024 * 1024)
                    return $"{FileMatch.FileSize / 1024.0:F2} KB";
                else if (FileMatch.FileSize < 1024 * 1024 * 1024)
                    return $"{FileMatch.FileSize / (1024.0 * 1024.0):F2} MB";
                else
                    return $"{FileMatch.FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }

        public string ConfidenceColor
        {
            get
            {
                if (MatchScore == 100) return "#16C60C";
                if (MatchScore >= 80) return "#FFB900";
                return "#FF6B6B";
            }
        }
    }
}
