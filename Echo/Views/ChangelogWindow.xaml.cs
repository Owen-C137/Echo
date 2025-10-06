using System;
using System.Reflection;
using System.Windows;

namespace Echo.Views
{
    public partial class ChangelogWindow : Window
    {
        public ChangelogWindow()
        {
            InitializeComponent();
            LoadCurrentVersion();
        }

        private void LoadCurrentVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    CurrentVersionText.Text = $"Current Version: {version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                CurrentVersionText.Text = "Current Version: 1.0.0";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
