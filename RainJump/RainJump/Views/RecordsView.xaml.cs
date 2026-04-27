using System.Windows;
using System.Windows.Controls;

namespace RainJump.Views
{
    public partial class RecordsView : UserControl
    {
        public Action? OnBack { get; set; }

        public RecordsView()
        {
            InitializeComponent();
            Loaded += (_, _) => RefreshScore();
        }

        private void RefreshScore()
        {
            if (SessionManager.IsGuest)
            {
                BestScoreValue.Text = "—";
                ResetButton.IsEnabled = false;
                ResetButton.Opacity = 0.4;
            }
            else
            {
                BestScoreValue.Text = SessionManager.BestScore.ToString();
                ResetButton.IsEnabled = true;
                ResetButton.Opacity = 1.0;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => OnBack?.Invoke();

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.ResetBestScore();
            RefreshScore();
        }
    }
}
