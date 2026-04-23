using System.Windows;

namespace RainJump.Views
{
    public partial class MainWindow : Window
    {
        private const double AspectRatio = 420.0 / 700.0; // width / height
        private bool _adjusting = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Enforce 420:700 aspect ratio on every resize ─────────────────
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_adjusting) return;
            _adjusting = true;

            // Always drive height from width to keep ratio
            double newHeight = Width / AspectRatio;
            Height = newHeight;

            _adjusting = false;
        }

        // ── Navigation helpers ───────────────────────────────────────────
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

            ViewHost.Content        = game;
            ViewHost.Visibility     = Visibility.Visible;
            MainMenuView.Visibility = Visibility.Collapsed;

            game.Focus();
        }

        private void ShowRecords()
        {
            var records = new RecordsView();
            records.OnBack = ShowMenu;

            ViewHost.Content        = records;
            ViewHost.Visibility     = Visibility.Visible;
            MainMenuView.Visibility = Visibility.Collapsed;
        }

        // ── Button Handlers ──────────────────────────────────────────────
        private void PlayButton_Click(object sender, RoutedEventArgs e)    => ShowGame();
        private void RecordsButton_Click(object sender, RoutedEventArgs e) => ShowRecords();
        private void QuitButton_Click(object sender, RoutedEventArgs e)    => Application.Current.Shutdown();
    }
}
