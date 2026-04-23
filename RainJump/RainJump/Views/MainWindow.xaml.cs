using System.Windows;

namespace RainJump.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Button Handlers ──────────────────────────────────────────────

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Stage 2: will navigate to the game view
            MessageBox.Show("Game starting soon! (Stage 2)", "Rain Jump",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RecordsButton_Click(object sender, RoutedEventArgs e)
        {
            // Stage N: will show high-score / records view
            MessageBox.Show("Records coming soon!", "Rain Jump",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
