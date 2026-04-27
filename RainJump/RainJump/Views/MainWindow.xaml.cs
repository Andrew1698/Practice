using System.Windows;
using RainJump.Resources;

namespace RainJump.Views
{
    public partial class MainWindow : Window
    {
        private const double AspectRatio = 420.0 / 700.0;
        private bool _adjusting = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Aspect ratio lock ────────────────────────────────────────────
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_adjusting) return;
            _adjusting = true;
            Height = Width / AspectRatio;
            _adjusting = false;
        }

        // ── Navigation ───────────────────────────────────────────────────
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

        private void ShowSettings()
        {
            var settings = new SettingsView();
            settings.OnBack      = ShowMenu;
            settings.OnCustomize = ShowCustomize;
            ViewHost.Content        = settings;
            ViewHost.Visibility     = Visibility.Visible;
            MainMenuView.Visibility = Visibility.Collapsed;
        }

        private void ShowCustomize()
        {
            var customize = new CustomizeView();
            customize.OnBack = ShowSettings;
            ViewHost.Content    = customize;
            ViewHost.Visibility = Visibility.Visible;
            // MainMenuView already hidden
        }

        // ── Button handlers ──────────────────────────────────────────────
        private void PlayButton_Click(object sender, RoutedEventArgs e)     => ShowGame();
        private void RecordsButton_Click(object sender, RoutedEventArgs e)  => ShowRecords();
        private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();
        private void QuitButton_Click(object sender, RoutedEventArgs e)     => Application.Current.Shutdown();
    }
}
