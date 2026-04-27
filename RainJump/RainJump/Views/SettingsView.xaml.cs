using System.Windows;
using System.Windows.Controls;

namespace RainJump.Views
{
    public partial class SettingsView : UserControl
    {
        public Action? OnBack       { get; set; }
        public Action? OnCustomize  { get; set; }

        public SettingsView()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)      => OnBack?.Invoke();
        private void CustomizeButton_Click(object sender, RoutedEventArgs e) => OnCustomize?.Invoke();
    }
}
