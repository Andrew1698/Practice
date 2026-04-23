using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

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
                    BaseRect.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0x20, 0x21));
                    break;

                case PlatformType.Spike:
                    BaseRect.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0x20, 0x21));
                    break;

                case PlatformType.Boost:
                    BaseRect.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0x20, 0x21));
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
                        Fill = new SolidColorBrush(Color.FromRgb(0x44, 0x42, 0x43)),
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
                    Fill    = new SolidColorBrush(Color.FromRgb(0xFB, 0xAE, 0xD2)),
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
            _lastSpawnType = PlatformType.Normal;

            ScoreText.Text          = "Score: 0";
            StartPrompt.Visibility  = Visibility.Visible;
            DeathOverlay.Visibility = Visibility.Collapsed;

            GameCanvas.Children.Clear();
            _platforms.Clear();

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
        private void BuildPlayerVisuals()
        {
            var bodyColor = new SolidColorBrush(Color.FromRgb(0xAD, 0xD8, 0xF8));
            var pinkColor = new SolidColorBrush(Color.FromRgb(0xFB, 0xAE, 0xD2));
            var outlineBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xEF, 0xF5));

            _earLeft = new Path
            {
                Fill            = bodyColor,
                Stroke          = outlineBrush,
                StrokeThickness = 2.5,
                Data            = Geometry.Parse(
                    "M 8,20 C 7,20 0,20 1,14 C 2,8 6,2 8,0 C 10,2 15,8 15,14 C 16,20 9,20 8,20 Z"),
                RenderTransformOrigin = new Point(0.5, 1.0),
                RenderTransform       = new RotateTransform(-28)
            };
            _earRight = new Path
            {
                Fill            = bodyColor,
                Stroke          = outlineBrush,
                StrokeThickness = 2.5,
                Data            = Geometry.Parse(
                    "M 8,20 C 7,20 0,20 1,14 C 2,8 6,2 8,0 C 10,2 15,8 15,14 C 16,20 9,20 8,20 Z"),
                RenderTransformOrigin = new Point(0.5, 1.0),
                RenderTransform       = new RotateTransform(28)
            };
            _bodyEllipse = new Ellipse
            {
                Width = PlayerSize, Height = PlayerSize,
                Fill  = bodyColor,
                Stroke = outlineBrush, StrokeThickness = 2.5
            };
            _eyeLeftOuter  = new Ellipse { Width = 13, Height = 13, Fill = pinkColor };
            _eyeRightOuter = new Ellipse { Width = 13, Height = 13, Fill = pinkColor };
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
            _velocityY += Gravity;
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

                    // ── Hit! ─────────────────────────────────────────────
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

            // ── Camera scroll ─────────────────────────────────────────────
            double playerScreenY = _playerY + _cameraY;
            if (playerScreenY < h * 0.45)
            {
                double shift = h * 0.45 - playerScreenY;
                _cameraY += shift;
                _playerY += shift;

                int newScore = (int)(_cameraY / PlatformGapY * 10);
                if (newScore > _score)
                {
                    _score = newScore;
                    ScoreText.Text = $"Score: {_score}";
                }

                foreach (var p in _platforms)
                    Canvas.SetTop(p.Shape, p.WorldY + _cameraY);

                RecyclePlatforms(w, h);
            }
            else
            {
                foreach (var p in _platforms)
                    Canvas.SetTop(p.Shape, p.WorldY + _cameraY);
            }

            // ── Death: fell below screen ──────────────────────────────────
            if (_playerY + _cameraY > h + 20)
            {
                Die();
                return;
            }

            UpdatePlayerPosition();
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

            if (_score > ScoreManager.BestScore)
                ScoreManager.BestScore = _score;

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
