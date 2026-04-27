using System.Windows;
using System.Windows.Media;

namespace RainJump.Resources
{
    public enum GameTheme { Rivulet, Artificer }

    /// <summary>
    /// Switches themes at runtime by overwriting the color entries in App.Resources.
    /// All XAML StaticResource bindings pick up the new values automatically on next render.
    /// </summary>
    public static class ThemeManager
    {
        public static GameTheme Current { get; private set; } = GameTheme.Rivulet;

        // ── Rivulet (default) palette ────────────────────────────────────
        private static readonly Dictionary<string, Color> Rivulet = new()
        {
            ["GameBgBlueColor"]          = C(0xC6, 0xDB, 0xEF),
            ["GameBgPinkColor"]          = C(0xF8, 0xDE, 0xEB),
            ["PinkAccentColor"]          = C(0xFB, 0xAE, 0xD2),
            ["PlayerBodyColor"]          = C(0xAD, 0xD8, 0xF8),
            ["PlayerOutlineColor"]       = C(0xE8, 0xEF, 0xF5),
            ["PlayerEyeColor"]           = C(0xFB, 0xAE, 0xD2),
            ["PlatformNormalColor"]      = C(0x22, 0x20, 0x21),
            ["PlatformSpikeColor"]       = C(0x44, 0x42, 0x43),
            ["PlatformBoostStripeColor"] = C(0xFB, 0xAE, 0xD2),
            ["ButtonBgColor"]            = C(0xFF, 0xFF, 0xFF),
            ["ButtonFgColor"]            = C(0x6A, 0xAE, 0xD6),
            ["ButtonHoverBgColor"]       = C(0xEE, 0xF6, 0xFF),
            ["ButtonPressBgColor"]       = C(0xDD, 0xEE, 0xF8),
            ["ButtonBorderColor"]        = C(0xD8, 0xEE, 0xF8),
            ["ButtonHoverBorderColor"]   = C(0xFB, 0xAE, 0xD2),
            ["ButtonShadowColor"]        = Color.FromArgb(0x22, 0xAA, 0xCC, 0xDD),
            ["BubbleWhiteColor"]         = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
            ["BubbleWhiteFaintColor"]    = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
            ["PinkAccentFaintColor"]     = Color.FromArgb(0x22, 0xFB, 0xAE, 0xD2),
            ["PinkAccentSubtleColor"]    = Color.FromArgb(0x44, 0xFB, 0xAE, 0xD2),
            ["PinkAccentMutedColor"]     = Color.FromArgb(0xAA, 0xFB, 0xAE, 0xD2),
            ["PinkAccentSoftColor"]      = Color.FromArgb(0xBB, 0xFB, 0xAE, 0xD2),
            ["ScoreTextColor"]           = C(0x00, 0x00, 0x00),
            ["VersionTextColor"]         = Color.FromArgb(0x66, 0x8A, 0xBA, 0xCC),
            ["TitleShadowColor"]         = Color.FromArgb(0xCC, 0x88, 0x90, 0xAA),
        };

        // ── Artificer palette ────────────────────────────────────────────
        private static readonly Dictionary<string, Color> Artificer = new()
        {
            ["GameBgBlueColor"]          = C(0xFF, 0x87, 0x80),   // light red bg
            ["GameBgPinkColor"]          = C(0xF7, 0xF5, 0xBC),   // light yellowish second bg
            ["PinkAccentColor"]          = C(0x42, 0x0C, 0x09),   // dark red text/accent
            ["PlayerBodyColor"]          = C(0x80, 0x18, 0x18),   // maroon player
            ["PlayerOutlineColor"]       = C(0x55, 0x10, 0x10),   // darker maroon outline
            ["PlayerEyeColor"]           = C(0xFF, 0xFF, 0xFF),   // white eyes
            ["PlatformNormalColor"]      = C(0x22, 0x20, 0x21),
            ["PlatformSpikeColor"]       = C(0x44, 0x42, 0x43),
            ["PlatformBoostStripeColor"] = C(0xC3, 0x21, 0x48),   // deeper pink stripe
            ["ButtonBgColor"]            = C(0xFF, 0xFF, 0xFF),
            ["ButtonFgColor"]            = C(0x42, 0x0C, 0x09),   // dark red button text
            ["ButtonHoverBgColor"]       = C(0xFF, 0xF0, 0xEE),
            ["ButtonPressBgColor"]       = C(0xFF, 0xDD, 0xDA),
            ["ButtonBorderColor"]        = C(0xFF, 0xCC, 0xC8),
            ["ButtonHoverBorderColor"]   = C(0x42, 0x0C, 0x09),
            ["ButtonShadowColor"]        = Color.FromArgb(0x22, 0xAA, 0x44, 0x44),
            ["BubbleWhiteColor"]         = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
            ["BubbleWhiteFaintColor"]    = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
            ["PinkAccentFaintColor"]     = Color.FromArgb(0x22, 0x42, 0x0C, 0x09),
            ["PinkAccentSubtleColor"]    = Color.FromArgb(0x44, 0x42, 0x0C, 0x09),
            ["PinkAccentMutedColor"]     = Color.FromArgb(0xAA, 0x42, 0x0C, 0x09),
            ["PinkAccentSoftColor"]      = Color.FromArgb(0xBB, 0x42, 0x0C, 0x09),
            ["ScoreTextColor"]           = C(0x42, 0x0C, 0x09),
            ["VersionTextColor"]         = Color.FromArgb(0x88, 0x42, 0x0C, 0x09),
            ["TitleShadowColor"]         = Color.FromArgb(0xCC, 0x20, 0x06, 0x04),
        };

        // ── Extra Artificer-only colors (not in Theme.xaml, used in code) ─
        public static Color ArtificerBruiseColor  => C(0x42, 0x03, 0x03);
        public static Color ArtificerScarColor    => C(0x94, 0x45, 0x47);

        // ── API ──────────────────────────────────────────────────────────
        public static void Apply(GameTheme theme)
        {
            Current = theme;
            var palette = theme == GameTheme.Artificer ? Artificer : Rivulet;
            var res = Application.Current.Resources;

            foreach (var (key, color) in palette)
            {
                // Update the Color entry
                if (res.Contains(key))
                    res[key] = color;

                // Replace the brush with a new unfrozen one
                string brushKey = key.Replace("Color", "Brush");
                if (res.Contains(brushKey))
                    res[brushKey] = new SolidColorBrush(color);
            }
        }

        private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
    }
}
