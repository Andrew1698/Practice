using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RainJump.Resources;

namespace RainJump.Views
{
    public partial class CustomizeView : UserControl
    {
        public Action? OnBack { get; set; }

        private static readonly Color SelectedBorder   = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color UnselectedBorder = Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF);

        public CustomizeView()
        {
            InitializeComponent();
            Loaded += (_, _) => RefreshSelection();
        }

        private void RefreshSelection()
        {
            bool isArtificer = ThemeManager.Current == GameTheme.Artificer;

            RivuletCard.BorderBrush  = new SolidColorBrush(isArtificer ? UnselectedBorder : SelectedBorder);
            ArtificerCard.BorderBrush = new SolidColorBrush(isArtificer ? SelectedBorder : UnselectedBorder);

            ActiveThemeLabel.Text = $"Active: {ThemeManager.Current}";
        }

        private void RivuletCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.Apply(GameTheme.Rivulet);
            RefreshSelection();
        }

        private void ArtificerCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.Apply(GameTheme.Artificer);
            RefreshSelection();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => OnBack?.Invoke();
    }
}
