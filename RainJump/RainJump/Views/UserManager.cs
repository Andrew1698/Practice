using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace RainJump.Views
{
    public class UserProfile
    {
        public string Nickname { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int BestScore { get; set; } = 0;
    }

    public static class UserManager
    {
        private static readonly string _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RainJump", "users.json");

        private static List<UserProfile> _users = new();

        public static void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    _users = JsonSerializer.Deserialize<List<UserProfile>>(json) ?? new();
                }
            }
            catch { _users = new(); }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_savePath)!);
                File.WriteAllText(_savePath,
                    JsonSerializer.Serialize(_users,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        // Returns null if nickname already taken
        public static UserProfile? Register(string nickname, string password)
        {
            Load();
            if (_users.Exists(u => u.Nickname.ToLower() == nickname.ToLower()))
                return null;

            var user = new UserProfile
            {
                Nickname     = nickname,
                PasswordHash = BCryptHash(password),
                BestScore    = 0
            };
            _users.Add(user);
            Save();
            return user;
        }

        // Returns null if not found or wrong password
        public static UserProfile? Login(string nickname, string password)
        {
            Load();
            var user = _users.Find(u => u.Nickname.ToLower() == nickname.ToLower());
            if (user == null) return null;
            return VerifyHash(password, user.PasswordHash) ? user : null;
        }

        public static void UpdateBestScore(string nickname, int score)
        {
            Load();
            var user = _users.Find(u => u.Nickname.ToLower() == nickname.ToLower());
            if (user != null && score > user.BestScore)
            {
                user.BestScore = score;
                Save();
            }
        }

        public static int GetBestScore(string nickname)
        {
            Load();
            return _users.Find(u => u.Nickname.ToLower() == nickname.ToLower())?.BestScore ?? 0;
        }

        public static void ResetBestScore(string nickname)
        {
            Load();
            var user = _users.Find(u => u.Nickname.ToLower() == nickname.ToLower());
            if (user != null) { user.BestScore = 0; Save(); }
        }

        // ── Simple BCrypt-style hash using SHA256 + nickname salt ─────────
        // (No external library needed — good enough for a game)
        private static string BCryptHash(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "_RainJumpSalt_");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        private static bool VerifyHash(string password, string hash)
            => BCryptHash(password) == hash;
    }
}
