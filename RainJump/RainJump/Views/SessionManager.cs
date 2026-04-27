namespace RainJump.Views
{
    /// <summary>
    /// Tracks the current session state across all views.
    /// </summary>
    public static class SessionManager
    {
        public static UserProfile? CurrentUser { get; private set; } = null;
        public static bool IsGuest => CurrentUser == null;

        public static void Login(UserProfile user)  => CurrentUser = user;
        public static void Logout()                  => CurrentUser = null;

        public static int BestScore
        {
            get => IsGuest ? 0 : UserManager.GetBestScore(CurrentUser!.Nickname);
            set
            {
                if (!IsGuest)
                    UserManager.UpdateBestScore(CurrentUser!.Nickname, value);
            }
        }

        public static void ResetBestScore()
        {
            if (!IsGuest)
                UserManager.ResetBestScore(CurrentUser!.Nickname);
        }
    }
}
