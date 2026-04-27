using System.Windows;
using System.Windows.Media;

namespace RainJump.Resources
{
    /// <summary>
    /// Provides typed access to colors defined in Theme.xaml.
    /// All game code reads colors from here — never hard-coded.
    /// </summary>
    public static class ThemeColors
    {
        public static Color GameBgBlue           => Get("GameBgBlueColor");
        public static Color GameBgPink           => Get("GameBgPinkColor");
        public static Color PinkAccent           => Get("PinkAccentColor");
        public static Color PlayerBody           => Get("PlayerBodyColor");
        public static Color PlayerOutline        => Get("PlayerOutlineColor");
        public static Color PlayerEye            => Get("PlayerEyeColor");
        public static Color PlatformNormal       => Get("PlatformNormalColor");
        public static Color PlatformSpike        => Get("PlatformSpikeColor");
        public static Color PlatformBoostStripe  => Get("PlatformBoostStripeColor");

        private static Color Get(string key)
            => (Color)Application.Current.Resources[key];
    }
}
