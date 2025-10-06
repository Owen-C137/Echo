using System.Windows;

namespace Echo.Views
{
    public partial class SoundOptionsDialog : Window
    {
        public int SelectedMode { get; private set; } = 0; // 0=Consolidated, 1=CopyFull, 2=Skip
        public string ConsolidatedFileName { get; private set; } = "echo_consolidated";

        public SoundOptionsDialog()
        {
            InitializeComponent();
        }

        private void ConsolidatedOption_Checked(object sender, RoutedEventArgs e)
        {
            SelectedMode = 0;
        }

        private void CopyFullOption_Checked(object sender, RoutedEventArgs e)
        {
            SelectedMode = 1;
        }

        private void SkipSoundsOption_Checked(object sender, RoutedEventArgs e)
        {
            SelectedMode = 2;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Validate consolidated filename if that option is selected
            if (SelectedMode == 0)
            {
                var fileName = ConsolidatedNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("Please enter a filename for the consolidated CSV file.", 
                        "Invalid Filename", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Remove .csv extension if user added it
                if (fileName.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                }

                ConsolidatedFileName = fileName;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
