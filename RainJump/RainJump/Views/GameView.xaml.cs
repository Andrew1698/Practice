using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RainJump.Resources;
using static RainJump.Resources.ThemeManager;

namespace RainJump.Views
{
    // ════════════════════════════════════════════════════════════════════
    //  PLATFORM TYPE
    // ════════════════════════════════════════════════════════════════════
    public enum PlatformType { Normal, Spike, Boost }

    // ════════════════════════════════════════════════════════════════════
    //  PLATFORM DATA CLASS
    // ════════════════════════════════════════════════════════════════════
    public class Platform
    {
        public double       WorldX { get; set; }
        public double       WorldY { get; set; }
        public PlatformType Type   { get; set; }

        // Root canvas element (Grid containing base + decorations)
        public UIElement Shape { get; }

        // The actual base rectangle (for hit-testing bounds)
        public readonly Rectangle BaseRect;

        private const double W = 70;
        private const double H = 14;

        public Platform(double x, double y, PlatformType type)
        {
            WorldX = x;
            WorldY = y;
            Type   = type;

            var grid = new Grid { Width = W };

            // ── Base bar ────────────────────────────────────────────────
            BaseRect = new Rectangle
            {
                Width   = W,
                Height  = H,
                RadiusX = 6,
                RadiusY = 6
            };

            switch (type)
            {
                case PlatformType.Normal:
                    BaseRect.Fill = new SolidColorBrush(ThemeColors.PlatformNormal);
                    break;
                case PlatformType.Spike:
                    BaseRect.Fill = new SolidColorBrush(ThemeColors.PlatformNormal);
                    break;
                case PlatformType.Boost:
                    BaseRect.Fill = new SolidColorBrush(ThemeColors.PlatformNormal);
                    break;
            }

            grid.Children.Add(BaseRect);

            // ── Type-specific decorations ───────────────────────────────
            if (type == PlatformType.Spike)
            {
                // Row of soft triangular spikes along the top
                var spikeCanvas = new Canvas
                {
                    Width  = W,
                    Height = H + 10,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, -10, 0, 0),
                    IsHitTestVisible = false
                };

                int spikeCount = 5;
                double spikeW  = W / spikeCount;       // 14px each
                double spikeH  = 10.0;

                for (int i = 0; i < spikeCount; i++)
                {
                    double cx = i * spikeW + spikeW / 2;
                    // Use invariant culture so decimals always use dots, not commas
                    string data = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "M {0:F2},{1:F2} Q {2:F2},{3:F2} {4:F2},0 Q {5:F2},{3:F2} {6:F2},{1:F2} Z",
                        cx - spikeW * 0.42, spikeH,
                        cx - spikeW * 0.18, spikeH * 0.55,
                        cx,
                        cx + spikeW * 0.18,
                        cx + spikeW * 0.42);

                    var spike = new Path
                    {
                        Fill = new SolidColorBrush(ThemeColors.PlatformSpike),
                        Data = Geometry.Parse(data)
                    };
                    spikeCanvas.Children.Add(spike);
                }
                grid.Children.Add(spikeCanvas);
            }
            else if (type == PlatformType.Boost)
            {
                // Pink horizontal stripe in the centre of the bar
                var stripe = new Rectangle
                {
                    Width   = W - 16,
                    Height  = 4,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill    = new SolidColorBrush(ThemeColors.PlatformBoostStripe),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    IsHitTestVisible    = false
                };
                grid.Children.Add(stripe);
            }

            Shape = grid;
            Canvas.SetLeft(Shape, WorldX);
            Canvas.SetTop (Shape, WorldY);
        }

        public void UpdatePosition()
        {
            Canvas.SetLeft(Shape, WorldX);
            Canvas.SetTop (Shape, WorldY);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  GAME VIEW
    // ════════════════════════════════════════════════════════════════════
    public partial class GameView : UserControl
    {
        // ── Design resolution (matches the ViewBox content size in MainWindow) ──
        private const double DesignW = 420.0;
        private const double DesignH = 700.0;
        private const double Gravity       = 0.35;
        private const double JumpForce     = -13.5;
        private const double BoostForce    = -13.5 * 2.5;  // 2.5× normal jump (was 5×)
        private const double MoveSpeed     = 5.0;
        private const double PlayerSize    = 44.0;
        private const double PlatformWidth = 70.0;
        private const double PlatformH     = 14.0;
        private const double PlatformGapY  = 90.0;
        private const int    PlatformCount = 29;

        // Spawn weights: Normal=65%, Spike=20%, Boost=15%
        private const int WeightNormal = 65;
        private const int WeightSpike  = 20;
        // Boost fills the rest (15)

        // ── Background colour cycling ─────────────────────────────────────
        private const int    BgCyclePoints = 1500;
        private const double BgPeakFrac    = 750.0 / 1500.0;
        // Read fresh from ThemeColors each cycle so theme changes take effect
        private Color BgBlue => ThemeColors.GameBgBlue;
        private Color BgPink => ThemeColors.GameBgPink;
        private readonly SolidColorBrush _bgBrush = new SolidColorBrush(ThemeColors.GameBgBlue);

        // ── Progressive gravity ───────────────────────────────────────────
        private const double BaseGravity      = 0.35;
        private const double MaxGravityMult   = 2.0;   // cap at 2× base
        private const int    GravityScoreCap  = 3000;  // reached max at 3000 pts
        private const int    GravityStepEvery = 100;   // recalculate every 100 pts
        private double _currentGravity = BaseGravity;

        // ── Game state ───────────────────────────────────────────────────
        private DispatcherTimer _gameLoop = null!;
        private Random          _rng      = new Random();

        private double _playerX, _playerY;
        private double _velocityX, _velocityY;
        private bool   _gameStarted = false;
        private bool   _isDead      = false;
        private int    _score       = 0;
        private double _cameraY     = 0;

        private List<Platform> _platforms     = new();
        private PlatformType   _lastSpawnType = PlatformType.Normal;

        // ── Player visuals ───────────────────────────────────────────────
        private Ellipse _bodyEllipse   = null!;
        private Path    _earLeft       = null!;
        private Path    _earRight      = null!;
        private Ellipse _eyeLeftOuter  = null!;
        private Ellipse _eyeRightOuter = null!;
        private Ellipse _eyeLeftPupil  = null!;
        private Ellipse _eyeRightPupil = null!;
        private Ellipse _eyeLeftHighlight1  = null!;
        private Ellipse _eyeLeftHighlight2  = null!;
        private Ellipse _eyeRightHighlight1 = null!;
        private Ellipse _eyeRightHighlight2 = null!;

        public Action? OnLeave { get; set; }

        public GameView()
        {
            InitializeComponent();
            Loaded += (_, _) => StartGame();
        }

        // ════════════════════════════════════════════════════════════════
        //  INIT
        // ════════════════════════════════════════════════════════════════
        private void StartGame()
        {
            _gameStarted   = false;
            _isDead        = false;
            _score         = 0;
            _cameraY       = 0;
            _velocityX     = 0;
            _velocityY     = 0;
            _lastSpawnType  = PlatformType.Normal;
            _highestPlayerY = double.MaxValue;
            _currentGravity = BaseGravity;

            ScoreText.Text          = "Score: 0";
            StartPrompt.Visibility  = Visibility.Visible;
            DeathOverlay.Visibility = Visibility.Collapsed;

            GameCanvas.Children.Clear();
            _platforms.Clear();

            // Reset background to current theme's blue
            _bgBrush.Color        = BgBlue;
            GameCanvas.Background = _bgBrush;

            double w = DesignW;
            double h = DesignH;

            // First platform: always Normal, centred under player
            double firstPlatY = h * 0.62;
            _platforms.Add(new Platform(w / 2 - PlatformWidth / 2, firstPlatY, PlatformType.Normal));

            for (int i = 1; i <= PlatformCount; i++)
            {
                double px   = _rng.NextDouble() * (w - PlatformWidth);
                double py   = firstPlatY - i * PlatformGapY;
                var    type = PickType();
                _platforms.Add(new Platform(px, py, type));
            }

            _playerX = w / 2 - PlayerSize / 2;
            _playerY = firstPlatY - PlayerSize;

            foreach (var p in _platforms)
                GameCanvas.Children.Add(p.Shape);

            BuildPlayerVisuals();

            Focus();
            KeyDown -= OnKeyDown;
            KeyDown += OnKeyDown;

            _gameLoop?.Stop();
            _gameLoop = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _gameLoop.Tick += GameTick;
            _gameLoop.Start();
        }

        // ── Pick a platform type respecting no-consecutive-spike rule ────
        private PlatformType PickType()
        {
            int roll = _rng.Next(100);
            PlatformType picked;

            if (_lastSpawnType == PlatformType.Spike)
            {
                // Can't spawn spike twice in a row — redistribute spike weight to Normal
                if (roll < WeightNormal + WeightSpike)   // 0-84 → Normal
                    picked = PlatformType.Normal;
                else
                    picked = PlatformType.Boost;
            }
            else
            {
                if      (roll < WeightNormal)               picked = PlatformType.Normal;
                else if (roll < WeightNormal + WeightSpike) picked = PlatformType.Spike;
                else                                        picked = PlatformType.Boost;
            }

            _lastSpawnType = picked;
            return picked;
        }

        // ════════════════════════════════════════════════════════════════
        //  BUILD PLAYER
        // ════════════════════════════════════════════════════════════════

        // Extra Artificer-specific visuals
        private Path?      _artificerRightEyeLine  = null;
        private UIElement? _artificerBruise        = null;
        private Path?      _artificerScar          = null;
        private Path?      _artificerLeftEyeClip   = null;

        private void BuildPlayerVisuals()
        {
            var bodyColor    = new SolidColorBrush(ThemeColors.PlayerBody);
            var eyeColor     = new SolidColorBrush(ThemeColors.PlayerEye);
            var outlineBrush = new SolidColorBrush(ThemeColors.PlayerOutline);

            bool isArtificer = ThemeManager.Current == GameTheme.Artificer;

            _earLeft = new Path
            {
                Fill = bodyColor, Stroke = outlineBrush, StrokeThickness = 2.5,
                Data = Geometry.Parse(
                    "M 8,20 C 7,20 0,20 1,14 C 2,8 6,2 8,0 C 10,2 15,8 15,14 C 16,20 9,20 8,20 Z"),
                RenderTransformOrigin = new Point(0.5, 1.0),
                RenderTransform = new RotateTransform(-28)
            };
            _earRight = new Path
            {
                Fill = bodyColor, Stroke = outlineBrush, StrokeThickness = 2.5,
                Data = Geometry.Parse(
                    "M 8,20 C 7,20 0,20 1,14 C 2,8 6,2 8,0 C 10,2 15,8 15,14 C 16,20 9,20 8,20 Z"),
                RenderTransformOrigin = new Point(0.5, 1.0),
                RenderTransform = new RotateTransform(28)
            };
            _bodyEllipse = new Ellipse
            {
                Width = PlayerSize, Height = PlayerSize,
                Fill = bodyColor, Stroke = outlineBrush, StrokeThickness = 2.5
            };

            if (isArtificer)
            {
                // ── LEFT EYE: D-shape — flat top angled angry (high outside, low inside) ──
                var angryEye = new Path
                {
                    Fill = eyeColor,
                    Data = Geometry.Parse(
                        "M 0,2 L 12,5 L 12,8 A 6,6 0 0 1 0,8 Z")
                };

                // ── RIGHT EYE: bruise circle (smaller, right side) ──────────
                var bruise = new Ellipse
                {
                    Width  = 17,
                    Height = 17,
                    Fill   = new SolidColorBrush(ThemeManager.ArtificerBruiseColor)
                };

                // White horizontal slit
                var eyeSlit = new Path
                {
                    Stroke             = Brushes.White,
                    StrokeThickness    = 2.5,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap   = PenLineCap.Round,
                    Data               = Geometry.Parse("M 0,0 L 10,0")
                };

                // Scar: diagonal crossing the slit top-left to bottom-right
                var scar = new Path
                {
                    Stroke             = new SolidColorBrush(ThemeManager.ArtificerScarColor),
                    StrokeThickness    = 2.0,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap   = PenLineCap.Round,
                    Data               = Geometry.Parse("M 2,0 L 7,13")
                };

                _artificerLeftEyeClip  = angryEye;
                _artificerBruise       = bruise;
                _artificerRightEyeLine = eyeSlit;
                _artificerScar         = scar;

                _eyeLeftOuter       = new Ellipse { Width = 0, Height = 0 };
                _eyeRightOuter      = new Ellipse { Width = 0, Height = 0 };
                _eyeLeftPupil       = new Ellipse { Width = 0, Height = 0 };
                _eyeRightPupil      = new Ellipse { Width = 0, Height = 0 };
                _eyeLeftHighlight1  = new Ellipse { Width = 0, Height = 0 };
                _eyeLeftHighlight2  = new Ellipse { Width = 0, Height = 0 };
                _eyeRightHighlight1 = new Ellipse { Width = 0, Height = 0 };
                _eyeRightHighlight2 = new Ellipse { Width = 0, Height = 0 };

                // Back to front: ears → body → bruise → slit → scar → angry eye
                GameCanvas.Children.Add(_earLeft);
                GameCanvas.Children.Add(_earRight);
                GameCanvas.Children.Add(_bodyEllipse);
                GameCanvas.Children.Add(bruise);
                GameCanvas.Children.Add(scar);
                GameCanvas.Children.Add(eyeSlit);
                GameCanvas.Children.Add(angryEye);
            }
            else
            {
                // ── RIVULET: standard round eyes ────────────────────────────
                _eyeLeftOuter  = new Ellipse { Width = 13, Height = 13, Fill = eyeColor };
                _eyeRightOuter = new Ellipse { Width = 13, Height = 13, Fill = eyeColor };
                _eyeLeftPupil  = new Ellipse { Width = 7,  Height = 7,  Fill = Brushes.Black };
                _eyeRightPupil = new Ellipse { Width = 7,  Height = 7,  Fill = Brushes.Black };
                _eyeLeftHighlight1  = new Ellipse { Width = 2.5, Height = 2.5, Fill = Brushes.White };
                _eyeLeftHighlight2  = new Ellipse { Width = 1.5, Height = 1.5, Fill = Brushes.White };
                _eyeRightHighlight1 = new Ellipse { Width = 2.5, Height = 2.5, Fill = Brushes.White };
                _eyeRightHighlight2 = new Ellipse { Width = 1.5, Height = 1.5, Fill = Brushes.White };

                GameCanvas.Children.Add(_earLeft);
                GameCanvas.Children.Add(_earRight);
                GameCanvas.Children.Add(_bodyEllipse);
                GameCanvas.Children.Add(_eyeLeftOuter);
                GameCanvas.Children.Add(_eyeRightOuter);
                GameCanvas.Children.Add(_eyeLeftPupil);
                GameCanvas.Children.Add(_eyeRightPupil);
                GameCanvas.Children.Add(_eyeLeftHighlight1);
                GameCanvas.Children.Add(_eyeLeftHighlight2);
                GameCanvas.Children.Add(_eyeRightHighlight1);
                GameCanvas.Children.Add(_eyeRightHighlight2);
            }

            UpdatePlayerPosition();
        }

        private void UpdatePlayerPosition()
        {
            double w = DesignW;
            if (_playerX > w)           _playerX = -PlayerSize;
            if (_playerX < -PlayerSize) _playerX = w;

            double sy = _playerY + _cameraY;

            Canvas.SetLeft(_bodyEllipse, _playerX);
            Canvas.SetTop (_bodyEllipse, sy);

            Canvas.SetLeft(_earLeft,  _playerX + 3);
            Canvas.SetTop (_earLeft,  sy - 14);
            Canvas.SetLeft(_earRight, _playerX + PlayerSize - 17);
            Canvas.SetTop (_earRight, sy - 14);

            if (ThemeManager.Current == GameTheme.Artificer)
            {
                // Left eye: D-shape
                if (_artificerLeftEyeClip != null)
                {
                    Canvas.SetLeft(_artificerLeftEyeClip, _playerX + 5);
                    Canvas.SetTop (_artificerLeftEyeClip, sy + 12);
                }

                // Right eye: bruise circle, pushed toward right edge
                if (_artificerBruise != null)
                {
                    Canvas.SetLeft(_artificerBruise, _playerX + PlayerSize - 21);
                    Canvas.SetTop (_artificerBruise, sy + 13);
                }

                // White slit centered on bruise
                if (_artificerRightEyeLine != null)
                {
                    Canvas.SetLeft(_artificerRightEyeLine, _playerX + PlayerSize - 20);
                    Canvas.SetTop (_artificerRightEyeLine, sy + 21);
                }

                // Scar crossing the slit
                if (_artificerScar != null)
                {
                    Canvas.SetLeft(_artificerScar, _playerX + PlayerSize - 18);
                    Canvas.SetTop (_artificerScar, sy + 14);
                }
            }
            else
            {
                // ── Rivulet eye positions ─────────────────────────────────
                Canvas.SetLeft(_eyeLeftOuter,  _playerX + 7);
                Canvas.SetTop (_eyeLeftOuter,  sy + 12);
                Canvas.SetLeft(_eyeRightOuter, _playerX + PlayerSize - 20);
                Canvas.SetTop (_eyeRightOuter, sy + 12);

                Canvas.SetLeft(_eyeLeftPupil,  _playerX + 10);
                Canvas.SetTop (_eyeLeftPupil,  sy + 15);
                Canvas.SetLeft(_eyeRightPupil, _playerX + PlayerSize - 17);
                Canvas.SetTop (_eyeRightPupil, sy + 15);

                Canvas.SetLeft(_eyeLeftHighlight1, _playerX + 10.5);
                Canvas.SetTop (_eyeLeftHighlight1, sy + 15.5);
                Canvas.SetLeft(_eyeLeftHighlight2, _playerX + 13.5);
                Canvas.SetTop (_eyeLeftHighlight2, sy + 19.0);
                Canvas.SetLeft(_eyeRightHighlight1, _playerX + PlayerSize - 16.5);
                Canvas.SetTop (_eyeRightHighlight1, sy + 15.5);
                Canvas.SetLeft(_eyeRightHighlight2, _playerX + PlayerSize - 13.5);
                Canvas.SetTop (_eyeRightHighlight2, sy + 19.0);
            }
        }

        // ── Camera: tracks the highest world Y the player has reached ────
        private double _highestPlayerY = 0; // world Y (lower = higher up)

        // ════════════════════════════════════════════════════════════════
        //  GAME TICK
        // ════════════════════════════════════════════════════════════════
        private void GameTick(object? sender, EventArgs e)
        {
            if (_isDead) return;

            double w = DesignW;
            double h = DesignH;

            // ── Input ────────────────────────────────────────────────────
            if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.A))
            {
                _velocityX = -MoveSpeed;
                if (!_gameStarted) BeginGame();
            }
            else if (Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.D))
            {
                _velocityX = MoveSpeed;
                if (!_gameStarted) BeginGame();
            }
            else { _velocityX = 0; }

            // ── Physics ──────────────────────────────────────────────────
            // Only apply extra gravity when falling — jump arc stays consistent
            double gravThisTick = _velocityY > 0 ? _currentGravity : BaseGravity;
            _velocityY += gravThisTick;
            _playerX   += _velocityX;
            _playerY   += _velocityY;

            // ── Platform collision (falling only) ────────────────────────
            if (_velocityY > 0)
            {
                double feetWorld = _playerY + PlayerSize;

                foreach (var plat in _platforms)
                {
                    bool withinX = _playerX + PlayerSize > plat.WorldX &&
                                   _playerX              < plat.WorldX + PlatformWidth;

                    bool feetOnTop = feetWorld >= plat.WorldY &&
                                     feetWorld <= plat.WorldY + PlatformH + Math.Abs(_velocityY) + 2;

                    if (!withinX || !feetOnTop) continue;

                    switch (plat.Type)
                    {
                        case PlatformType.Normal:
                            _playerY   = plat.WorldY - PlayerSize;
                            _velocityY = JumpForce;
                            break;
                        case PlatformType.Spike:
                            Die();
                            return;
                        case PlatformType.Boost:
                            _playerY   = plat.WorldY - PlayerSize;
                            _velocityY = BoostForce;
                            break;
                    }
                    break;
                }
            }

            // ── Camera: only scroll UP, never down ───────────────────────
            // Track the highest (smallest world Y) the player has reached.
            // _cameraY is an offset added to world Y to get screen Y.
            // We want the player locked to h*0.45 when climbing.
            if (_playerY < _highestPlayerY)
            {
                _highestPlayerY = _playerY;

                // Desired camera so player sits at 45% from top
                double desiredCamera = h * 0.45 - _highestPlayerY;
                // Only ever increase camera (scroll up), never pull back down
                if (desiredCamera > _cameraY)
                    _cameraY = desiredCamera;
            }

            // ── Update score + background colour + gravity ────────────────
            int newScore = (int)(_cameraY / PlatformGapY * 10);
            if (newScore > _score)
            {
                _score = newScore;
                ScoreText.Text = $"Score: {_score}";
                UpdateBackground();
                UpdateGravity();
            }

            // ── Reposition all platforms ──────────────────────────────────
            foreach (var p in _platforms)
                Canvas.SetTop(p.Shape, p.WorldY + _cameraY);

            RecyclePlatforms(w, h);

            // ── Death: fell below screen ──────────────────────────────────
            if (_playerY + _cameraY > h + 20)
            {
                Die();
                return;
            }

            UpdatePlayerPosition();
        }

        // ════════════════════════════════════════════════════════════════
        //  BACKGROUND COLOUR CYCLING
        // ════════════════════════════════════════════════════════════════
        private void UpdateBackground()
        {
            // Position within the current 1500-pt cycle (0.0 → 1.0)
            double cyclePos = (_score % BgCyclePoints) / (double)BgCyclePoints;

            // Blue→Pink: 0 to BgPeakFrac (0 → 750pts)
            // Pink→Blue: BgPeakFrac to 1.0 (750 → 1500pts)
            double t = cyclePos < BgPeakFrac
                ? cyclePos / BgPeakFrac               // 0 → 1
                : (1.0 - cyclePos) / (1.0 - BgPeakFrac); // 1 → 0

            // Smoothstep for a softer ease in/out
            t = t * t * (3 - 2 * t);

            _bgBrush.Color = Lerp(BgBlue, BgPink, t);
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        // ════════════════════════════════════════════════════════════════
        //  PROGRESSIVE GRAVITY
        // ════════════════════════════════════════════════════════════════
        private void UpdateGravity()
        {
            // Only recalculate on multiples of GravityStepEvery
            if (_score % GravityStepEvery != 0) return;

            // Linear ramp: 0 pts → BaseGravity, GravityScoreCap pts → BaseGravity * MaxGravityMult
            double t = Math.Min((double)_score / GravityScoreCap, 1.0);
            _currentGravity = BaseGravity * (1.0 + (MaxGravityMult - 1.0) * t);
        }

        private void BeginGame()
        {
            _gameStarted = true;
            StartPrompt.Visibility = Visibility.Collapsed;
            _velocityY = JumpForce;
        }

        // ════════════════════════════════════════════════════════════════
        //  RECYCLE PLATFORMS
        // ════════════════════════════════════════════════════════════════
        private void RecyclePlatforms(double w, double h)
        {
            double highestWorldY = double.MaxValue;
            foreach (var p in _platforms)
                if (p.WorldY < highestWorldY) highestWorldY = p.WorldY;

            // Collect platforms to recycle first — never modify list while iterating
            var toRecycle = new List<Platform>();
            foreach (var p in _platforms)
                if (p.WorldY + _cameraY > h + PlatformH)
                    toRecycle.Add(p);

            foreach (var p in toRecycle)
            {
                p.WorldY = highestWorldY - PlatformGapY;
                p.WorldX = _rng.NextDouble() * (w - PlatformWidth);
                highestWorldY = p.WorldY;
                RebuildPlatform(p, PickType(), w);
            }
        }

        private void RebuildPlatform(Platform p, PlatformType newType, double w)
        {
            // Remove old shape, build new platform in-place, swap reference
            GameCanvas.Children.Remove(p.Shape);

            // We can't change Shape (readonly), so create fresh and copy into list slot
            var fresh = new Platform(p.WorldX, p.WorldY, newType);

            // Find index and replace
            int idx = _platforms.IndexOf(p);
            if (idx >= 0) _platforms[idx] = fresh;

            // Insert the new shape below the player visuals
            int insertAt = GameCanvas.Children.IndexOf(_earLeft);
            if (insertAt < 0) insertAt = 0;
            GameCanvas.Children.Insert(insertAt, fresh.Shape);
        }

        // ════════════════════════════════════════════════════════════════
        //  DEATH
        // ════════════════════════════════════════════════════════════════
        private void Die()
        {
            _isDead = true;
            _gameLoop.Stop();

            if (_score > SessionManager.BestScore)
                SessionManager.BestScore = _score;

            FinalScoreText.Text     = $"Score: {_score}";
            DeathOverlay.Visibility = Visibility.Visible;
        }

        // ════════════════════════════════════════════════════════════════
        //  INPUT / BUTTONS
        // ════════════════════════════════════════════════════════════════
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!_gameStarted &&
                (e.Key == Key.Left || e.Key == Key.Right ||
                 e.Key == Key.A    || e.Key == Key.D))
                BeginGame();
        }

        private void TryAgainButton_Click(object sender, RoutedEventArgs e) => StartGame();

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            _gameLoop?.Stop();
            OnLeave?.Invoke();
        }
    }
}
