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
            BestScoreValue.Text = ScoreManager.BestScore.ToString();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            OnBack?.Invoke();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ScoreManager.Reset();
            RefreshScore();
        }
    }
}
