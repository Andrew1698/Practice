using System.Windows;
using System.Windows.Controls;

namespace RainJump.Views
{
    public partial class LoginView : UserControl
    {
        public Action?         OnSuccess { get; set; }
        public Action?         OnCancel  { get; set; }

        public LoginView() => InitializeComponent();

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var nickname = NicknameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(password))
            {
                ShowError("Please fill in all fields.");
                return;
            }

            var user = UserManager.Login(nickname, password);
            if (user == null)
            {
                ShowError("Incorrect nickname or password.");
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
