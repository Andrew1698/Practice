using System.IO;

namespace RainJump.Views
{
    /// <summary>
    /// Persists the best score to a local file so it survives app restarts.
    /// </summary>
    public static class ScoreManager
    {
        private static readonly string _savePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "RainJump", "bestscore.txt");

        private static int _bestScore = -1;

        public static int BestScore
        {
            get
            {
                if (_bestScore < 0) Load();
                return _bestScore;
            }
            set
            {
                _bestScore = value;
                Save();
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                    _bestScore = int.Parse(File.ReadAllText(_savePath).Trim());
                else
                    _bestScore = 0;
            }
            catch { _bestScore = 0; }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_savePath)!);
                File.WriteAllText(_savePath, _bestScore.ToString());
            }
            catch { }
        }

        public static void Reset()
        {
            _bestScore = 0;
            Save();
        }
    }
}
