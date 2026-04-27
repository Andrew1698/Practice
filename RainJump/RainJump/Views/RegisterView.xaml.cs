using System.Windows;
using System.Windows.Controls;

namespace RainJump.Views
{
    public partial class RegisterView : UserControl
    {
        public Action? OnSuccess { get; set; }
        public Action? OnCancel  { get; set; }

        public RegisterView() => InitializeComponent();

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var nickname = NicknameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (nickname.Length < 4)
            {
                ShowError("Nickname must be at least 4 characters.");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters.");
                return;
            }

            var user = UserManager.Register(nickname, password);
            if (user == null)
            {
                ShowError($"Nickname \"{nickname}\" is already taken.");
                return;
            }

            SessionManager.Login(user);
            OnSuccess?.Invoke();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => OnCancel?.Invoke();

        private void ShowError(string msg)
        {
            ErrorLabel.Text       = msg;
            ErrorLabel.Visibility = Visibility.Visible;
        }
    }
}
