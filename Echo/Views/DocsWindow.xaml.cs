using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Echo.Views
{
    public partial class DocsWindow : Window
    {
        public DocsWindow()
        {
            InitializeComponent();
            
            // Show Getting Started by default
            ShowPanel(GettingStartedPanel, BtnGettingStarted);
        }

        private void NavigateToGettingStarted(object sender, RoutedEventArgs e)
        {
            ShowPanel(GettingStartedPanel, BtnGettingStarted);
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            ShowPanel(SettingsPanel, BtnSettings);
        }

        private void NavigateToWorkflow(object sender, RoutedEventArgs e)
        {
            ShowPanel(WorkflowPanel, BtnWorkflow);
        }

        private void NavigateToAssetTypes(object sender, RoutedEventArgs e)
        {
            ShowPanel(AssetTypesPanel, BtnAssetTypes);
        }

        private void NavigateToTroubleshooting(object sender, RoutedEventArgs e)
        {
            ShowPanel(TroubleshootingPanel, BtnTroubleshooting);
        }

        private void NavigateToFAQ(object sender, RoutedEventArgs e)
        {
            ShowPanel(FAQPanel, BtnFAQ);
        }

        private void ShowPanel(ScrollViewer panelToShow, Button activeButton)
        {
            // Hide all panels
            GettingStartedPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            WorkflowPanel.Visibility = Visibility.Collapsed;
            AssetTypesPanel.Visibility = Visibility.Collapsed;
            TroubleshootingPanel.Visibility = Visibility.Collapsed;
            FAQPanel.Visibility = Visibility.Collapsed;

            // Reset all button backgrounds
            BtnGettingStarted.Background = Brushes.White;
            BtnSettings.Background = Brushes.White;
            BtnWorkflow.Background = Brushes.White;
            BtnAssetTypes.Background = Brushes.White;
            BtnTroubleshooting.Background = Brushes.White;
            BtnFAQ.Background = Brushes.White;

            // Show selected panel and highlight button
            panelToShow.Visibility = Visibility.Visible;
            activeButton.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        }
    }
}
