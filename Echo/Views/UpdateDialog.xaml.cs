using System;
using System.Reflection;
using System.Windows;
using Echo.Models;

namespace Echo.Views
{
    public partial class UpdateDialog : Window
    {
        public UpdateInfo? UpdateInfo { get; private set; }
        public bool ShouldUpdate { get; private set; }
        public bool SkipThisVersion { get; private set; }

        public UpdateDialog(UpdateInfo updateInfo)
        {
            InitializeComponent();
            UpdateInfo = updateInfo;
            LoadUpdateInfo();
        }

        private void LoadUpdateInfo()
        {
            if (UpdateInfo == null) return;

            // Get current version
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersionText.Text = $"v{currentVersion?.Major}.{currentVersion?.Minor}.{currentVersion?.Build ?? 0}";

            // Set new version info
            NewVersionText.Text = UpdateInfo.VersionString;
            FileSizeText.Text = UpdateInfo.FormattedSize;
            ReleaseDateText.Text = UpdateInfo.ReleaseDate.ToString("MMM dd, yyyy");

            // Set changelog
            ChangelogText.Text = ParseChangelog(UpdateInfo.Changelog);
        }

        private string ParseChangelog(string changelog)
        {
            if (string.IsNullOrWhiteSpace(changelog))
                return "No changelog available.";

            // Basic Markdown parsing - remove common Markdown syntax
            var parsed = changelog
                .Replace("## ", "")
                .Replace("### ", "  • ")
                .Replace("**", "")
                .Replace("- ", "  • ")
                .Replace("* ", "  • ");

            return parsed;
        }

        private void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            ShouldUpdate = true;
            SkipThisVersion = SkipVersionCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void RemindLater_Click(object sender, RoutedEventArgs e)
        {
            ShouldUpdate = false;
            SkipThisVersion = SkipVersionCheckBox.IsChecked ?? false;
            DialogResult = false;
            Close();
        }
    }
}
