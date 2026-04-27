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
            UserManager.Load();
            // Start at auth screen
            Loaded += (_, _) => ShowAuth();
        }

        // ── Aspect ratio lock ────────────────────────────────────────────
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_adjusting) return;
            _adjusting = true;
            Height = Width / AspectRatio;
            _adjusting = false;
        }

        // ── Auth flow ────────────────────────────────────────────────────
        private void ShowAuth()
        {
            var auth = new AuthView();
            auth.OnLogin    = ShowLogin;
            auth.OnRegister = ShowRegister;
            auth.OnGuest    = () => { SessionManager.Logout(); ShowMenu(); };
            SwapView(auth);
        }

        private void ShowLogin()
        {
            var login = new LoginView();
            login.OnSuccess = ShowMenu;
            login.OnCancel  = ShowAuth;
            SwapView(login);
        }

        private void ShowRegister()
        {
            var register = new RegisterView();
            register.OnSuccess = ShowMenu;
            register.OnCancel  = ShowAuth;
            SwapView(register);
        }

        // ── Main navigation ──────────────────────────────────────────────
        private void ShowMenu()
        {
            MainMenuView.Visibility = Visibility.Visible;
            ViewHost.Visibility     = Visibility.Collapsed;
            ViewHost.Content        = null;

            // Show logged-in user name if applicable
            if (!SessionManager.IsGuest)
                WelcomeLabel.Text = $"Hi, {SessionManager.CurrentUser!.Nickname}!";
            else
                WelcomeLabel.Text = "Playing as Guest";
        }

        private void ShowGame()
        {
            var game = new GameView();
            game.OnLeave = ShowMenu;
            SwapView(game);
            game.Focus();
        }

        private void ShowRecords()
        {
            var records = new RecordsView();
            records.OnBack = ShowMenu;
            SwapView(records);
        }

        private void ShowSettings()
        {
            var settings = new SettingsView();
            settings.OnBack      = ShowMenu;
            settings.OnCustomize = ShowCustomize;
            SwapView(settings);
        }

        private void ShowCustomize()
        {
            var customize = new CustomizeView();
            customize.OnBack = ShowSettings;
            SwapView(customize);
        }

        private void SwapView(System.Windows.Controls.UserControl view)
        {
            MainMenuView.Visibility = Visibility.Collapsed;
            ViewHost.Content        = view;
            ViewHost.Visibility     = Visibility.Visible;
        }

        // ── Button Handlers ──────────────────────────────────────────────
        private void PlayButton_Click(object sender, RoutedEventArgs e)     => ShowGame();
        private void RecordsButton_Click(object sender, RoutedEventArgs e)  => ShowRecords();
        private void SettingsButton_Click(object sender, RoutedEventArgs e) => ShowSettings();
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Logout();
            ShowAuth();
        }
        private void QuitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
