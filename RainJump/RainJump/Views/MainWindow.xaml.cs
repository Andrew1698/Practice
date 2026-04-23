using System.Windows;

namespace RainJump.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Show / hide helpers ──────────────────────────────────────────

        private void ShowMenu()
        {
            MainMenuView.Visibility = Visibility.Visible;
            ViewHost.Visibility     = Visibility.Collapsed;
            ViewHost.Content        = null;
        }

        private void ShowGame()
        {
            var game = new GameView();
            game.OnLeave = ShowMenu;

            ViewHost.Content    = game;
            ViewHost.Visibility = Visibility.Visible;
            MainMenuView.Visibility = Visibility.Collapsed;

            // Focus so keyboard input works immediately
            game.Focus();
        }

        private void ShowRecords()
        {
            var records = new RecordsView();
            records.OnBack = ShowMenu;

            ViewHost.Content    = records;
            ViewHost.Visibility = Visibility.Visible;
            MainMenuView.Visibility = Visibility.Collapsed;
        }

        // ── Button Handlers ──────────────────────────────────────────────

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGame();
        }

        private void RecordsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRecords();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
