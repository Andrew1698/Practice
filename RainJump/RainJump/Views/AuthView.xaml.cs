using System.Windows;
using System.Windows.Controls;

namespace RainJump.Views
{
    public partial class AuthView : UserControl
    {
        public Action? OnLogin    { get; set; }
        public Action? OnRegister { get; set; }
        public Action? OnGuest    { get; set; }

        public AuthView() => InitializeComponent();

        private void LoginButton_Click(object sender, RoutedEventArgs e)    => OnLogin?.Invoke();
        private void RegisterButton_Click(object sender, RoutedEventArgs e) => OnRegister?.Invoke();
        private void GuestButton_Click(object sender, RoutedEventArgs e)    => OnGuest?.Invoke();
    }
}
