using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Timers;
namespace hourglass_timer_v1
{
    public partial class MainPage : ContentPage
    {
        private System.Timers.Timer mainTimer;
        private System.Timers.Timer animationTimer;
        const int SIZE = 20;
        TimeSpan time = new();
        TimeSpan totalTime = new();
        int mdInt = 0;
        private Random random = new Random();

        private List<SandParticle> sandParticles = new List<SandParticle>();
        private readonly object sandParticleLock = new object();
        private bool isAnimating = false;
        private float centerX;
        private float neckStartY;
        private float neckEndY;
        private float neckWidth;
        private bool isTimerRunning = false;
        private const int MaxParticles = 150;
        private const float GravityFactor = 0.3f;
        private float elapsedPercentage = 0;

        private bool deviceIsFlipped = false;
        private bool isAccelerometerActive = false;
        private double lastFlipTimestamp = 0;
        private const double FlipCooldownSeconds = 1.0;

        Dictionary<string, int> timerModifiers = new()
        {
            {"1x", 1},
            {"2x", 2},
            {"4x", 4},
            {"20x", 20},
        };

        List<string> timerModifiersText = new()
        {
            "1x", "2x", "4x", "20x"
        };

        public class HourglassCell
        {
            public SKRect Rectangle { get; set; }
            public bool IsFilled { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
            public bool IsWall { get; set; }
        }

        public class SandParticle
        {
            private static Random random = new Random();

            public float X { get; set; }
            public float Y { get; set; }
            public float VelocityX { get; set; }
            public float VelocityY { get; set; }
            public float Size { get; set; }
            public bool InBottomHalf { get; set; }
            public SKColor Color { get; set; }

            public SandParticle(float x, float y, float size)
            {
                X = x;
                Y = y;
                Size = size;
                VelocityX = 0;
                VelocityY = 0;
                InBottomHalf = false;

                Color = new SKColor(
                    (byte)random.Next(220, 245),
                    (byte)random.Next(190, 215),
                    (byte)random.Next(130, 170));
            }
        }

        private List<List<HourglassCell>> hourglassCells;
        private bool isTimerCompleted = false;

        public MainPage()
        {
            InitializeComponent();
            InitializeHourglassCells();
            InitializeAccelerometer();
        }

        private void InitializeAccelerometer()
        {
            if (Accelerometer.Default.IsSupported)
            {
                isAccelerometerActive = true;

                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
            }
            else
            {
                DisplayAlert("Notice", "Accelerometer not available on this device. Flip functionality will not work.", "OK");
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            double currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            if (currentTime - lastFlipTimestamp < FlipCooldownSeconds)
            {
                return;
            }

            bool isCurrentlyFlipped = e.Reading.Acceleration.Z < -0.7;

            if (isCurrentlyFlipped != deviceIsFlipped)
            {
                deviceIsFlipped = isCurrentlyFlipped;
                lastFlipTimestamp = currentTime;

                if (isTimerRunning || isTimerCompleted)
                {
                    FlipHourglass();

                    if (isTimerCompleted)
                    {
                        RestartTimerAfterFlip();
                    }

                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50));
                    }
                    catch
                    {
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    canvasView.InvalidateSurface();
                });
            }
        }

        private void FlipHourglass()
        {
            lock (sandParticleLock)
            {
                sandParticles.Clear();
            }

            if (isTimerRunning && totalTime.TotalSeconds > 0)
            {
                float remainingPercentage = (float)(time.TotalSeconds / totalTime.TotalSeconds);

                float flippedPercentage = 1.0f - remainingPercentage;

                time = TimeSpan.FromSeconds(totalTime.TotalSeconds * flippedPercentage);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimerLabel.Text = time.ToString(@"mm\:ss");
                });
            }
            else if (isTimerCompleted)
            {
                time = totalTime;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                canvasView.InvalidateSurface();
            });
        }

        private void InitializeHourglassCells()
        {
            hourglassCells = new List<List<HourglassCell>>();
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            SKPaint paintBrown = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(67, 37, 14),
                StrokeWidth = 5,
            };

            SKPaint paintSand = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.SandyBrown,
                StrokeWidth = 5,
            };

            SKPaint paintPale = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.PaleGoldenrod,
                StrokeWidth = 5,
            };

            float canvasWidth = info.Width;
            float canvasHeight = info.Height;
            float rectWidth = 25;
            float rectHeight = 25;
            int idx = 0;
            bool changeSides = false;

            centerX = canvasWidth / 2;

            if (deviceIsFlipped)
            {
                neckStartY = (SIZE + (SIZE / 4)) * rectHeight / 2 + 100;
                neckEndY = (SIZE - (SIZE / 4)) * rectHeight / 2 + 100;
            }
            else
            {
                neckStartY = (SIZE - (SIZE / 4)) * rectHeight / 2 + 100;
                neckEndY = (SIZE + (SIZE / 4)) * rectHeight / 2 + 100;
            }

            neckWidth = rectWidth * 2;

            hourglassCells.Clear();

            canvas.Save();

            if (deviceIsFlipped)
            {
                canvas.RotateRadians((float)Math.PI, canvasWidth / 2, canvasHeight / 2);
            }

            for (int verticalIndex = 0; verticalIndex < 2 * SIZE - (SIZE / 4); verticalIndex++)
            {
                var rowCells = new List<HourglassCell>();
                hourglassCells.Add(rowCells);

                for (int horizontalIndex = 0 + idx; horizontalIndex < SIZE; horizontalIndex++)
                {
                    float x = (canvasWidth / 2 - (SIZE * rectWidth) / 2) + horizontalIndex * rectWidth;
                    float y = verticalIndex * rectHeight + 100;

                    SKRect rect = new(x - idx * (rectWidth / 2) + 5, y + 5, x + rectWidth - idx * (rectHeight / 2), y + rectHeight);
                    SKPaint paint = paintBrown;

                    if (verticalIndex < 17 && verticalIndex > 1 && horizontalIndex != 0 + idx && horizontalIndex != SIZE - 1)
                    {
                        if ((isTimerRunning || isTimerCompleted) && totalTime.TotalSeconds > 0)
                        {
                            float topSandPercentage;

                            if (isTimerCompleted)
                                topSandPercentage = 0;
                            else
                                topSandPercentage = (float)(time.TotalSeconds / totalTime.TotalSeconds);

                            if (topSandPercentage > 0 &&
                                verticalIndex > (17 * (1 - topSandPercentage)))
                            {
                                paint = paintSand;
                            }
                            else
                            {
                                paint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.PaleGoldenrod };
                            }
                        }
                        else
                        {
                            paint = paintSand;
                        }
                    }
                    else if (verticalIndex > 16 && verticalIndex < 33 && horizontalIndex != 0 + idx && horizontalIndex != SIZE - 1)
                    {
                        if ((isTimerRunning || isTimerCompleted) && totalTime.TotalSeconds > 0)
                        {
                            float bottomSandPercentage;

                            if (isTimerCompleted)
                                bottomSandPercentage = 1.0f;
                            else
                                bottomSandPercentage = 1 - (float)(time.TotalSeconds / totalTime.TotalSeconds);

                            if (bottomSandPercentage > 0 &&
                                verticalIndex > 16 + (16 * (1 - bottomSandPercentage)))
                            {
                                paint = paintSand;
                            }
                            else
                            {
                                paint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.PaleGoldenrod };
                            }
                        }
                        else
                        {
                            paint = paintPale;
                        }
                    }

                    var cell = new HourglassCell
                    {
                        Rectangle = rect,
                        Row = verticalIndex,
                        Column = horizontalIndex,
                        IsFilled = paint == paintSand || paint == paintPale,
                        IsWall = paint == paintBrown
                    };

                    rowCells.Add(cell);
                    canvas.DrawRect(cell.Rectangle, paint);
                }

                if (verticalIndex > 16 && changeSides == false)
                {
                    changeSides = true;
                    idx--;
                    continue;
                }
                if (changeSides) idx--;
                else idx++;
            }

            if ((isAnimating || isTimerCompleted) && sandParticles.Count > 0)
            {
                SKPaint particlePaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                };

                List<SandParticle> particlesCopy = new List<SandParticle>(sandParticles);

                foreach (var particle in particlesCopy)
                {
                    particlePaint.Color = particle.Color;
                    canvas.DrawCircle(particle.X, particle.Y, particle.Size, particlePaint);
                }
            }

            canvas.Restore();
        }

        private void UpdateSandParticles()
        {
            if (!isAnimating) return;

            List<SandParticle> particlesToAdd = new List<SandParticle>();
            List<SandParticle> particlesToRemove = new List<SandParticle>();

            List<SandParticle> currentParticles;
            lock (sandParticleLock)
            {
                currentParticles = new List<SandParticle>(sandParticles);
            }

            if (currentParticles.Count < MaxParticles && random.NextDouble() < 0.3 && time.TotalSeconds > 1)
            {
                float offsetX = (float)(random.NextDouble() * neckWidth - neckWidth / 2);
                float size = (float)(random.NextDouble() * 2 + 2);
                particlesToAdd.Add(new SandParticle(centerX + offsetX, neckStartY, size));
            }

            foreach (var particle in currentParticles)
            {
                if (deviceIsFlipped)
                {
                    particle.VelocityY -= GravityFactor;
                }
                else
                {
                    particle.VelocityY += GravityFactor;
                }

                if (random.NextDouble() < 0.1)
                {
                    particle.VelocityX += (float)((random.NextDouble() - 0.5) * 0.5);
                }

                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;

                bool reachedEndOfNeck;

                if (deviceIsFlipped)
                {
                    reachedEndOfNeck = particle.Y < neckEndY && !particle.InBottomHalf;
                }
                else
                {
                    reachedEndOfNeck = particle.Y > neckEndY && !particle.InBottomHalf;
                }

                if (reachedEndOfNeck)
                {
                    particle.VelocityY *= 0.5f;
                    particle.InBottomHalf = true;

                    float dispersionFactor = (float)Math.Abs(neckEndY - neckStartY) / 8;
                    particle.VelocityX = (float)((random.NextDouble() - 0.5) * dispersionFactor);
                }

                float particleBoundary;
                if (deviceIsFlipped)
                {
                    particleBoundary = neckEndY - 200;
                    if (particle.Y < particleBoundary)
                    {
                        particlesToRemove.Add(particle);
                    }
                }
                else
                {
                    particleBoundary = neckEndY + 200;
                    if (particle.Y > particleBoundary)
                    {
                        particlesToRemove.Add(particle);
                    }
                }
            }

            lock (sandParticleLock)
            {
                foreach (var particle in particlesToRemove)
                {
                    sandParticles.Remove(particle);
                }

                sandParticles.AddRange(particlesToAdd);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                canvasView.InvalidateSurface();
            });
        }

        private void TimerButton_Clicked(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            if (button.Text.Equals("-") && time.TotalSeconds > 0)
            {
                if (TimeSpan.FromSeconds(time.TotalSeconds) < TimeSpan.FromSeconds(15 * timerModifiers[decreaseTimerModifierButton.Text]))
                    time = TimeSpan.FromSeconds(0);
                else
                    time -= TimeSpan.FromSeconds(15 * timerModifiers[decreaseTimerModifierButton.Text]);

                TimerLabel.Text = time.ToString(@"mm\:ss");
            }
            else if (button.Text.Equals("+") && time.TotalSeconds < 3600)
            {
                time += TimeSpan.FromSeconds(15 * timerModifiers[increaseTimerModifierButton.Text]);
                TimerLabel.Text = time.ToString(@"mm\:ss");
            }
        }

        private void ModifierButton_Clicked(object sender, EventArgs e)
        {
            if (mdInt < 3) mdInt++;
            else mdInt = 0;

            Button button = (Button)sender;

            button.Text = timerModifiersText[mdInt];
        }

        private void startTimerButton_Clicked(object sender, EventArgs e)
        {
            if (time.TotalSeconds <= 0) return;

            if (isTimerCompleted)
            {
                if (!deviceIsFlipped)
                {
                    lock (sandParticleLock)
                    {
                        sandParticles.Clear();
                    }
                    elapsedPercentage = 0;
                }
                else
                {
                    time = totalTime;
                }
            }
            else
            {
                if (!deviceIsFlipped)
                {
                    lock (sandParticleLock)
                    {
                        sandParticles.Clear();
                    }
                }
                else
                {
                    time = TimeSpan.Zero;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TimerLabel.Text = "00:00";
                        isTimerRunning = false;
                        isTimerCompleted = true;
                        startTimerButton.IsEnabled = true;

                        canvasView.InvalidateSurface();
                    });

                    return;
                }
            }

            isTimerRunning = true;
            isTimerCompleted = false;
            totalTime = time;

            mainTimer = new System.Timers.Timer();
            mainTimer.Interval = 1000;
            mainTimer.Enabled = true;
            mainTimer.Elapsed += OnTimerElapsed;

            isAnimating = true;

            animationTimer = new System.Timers.Timer();
            animationTimer.Interval = 16;
            animationTimer.Enabled = true;
            animationTimer.Elapsed += AnimationTimer_Elapsed;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                startTimerButton.IsEnabled = false;
                resetTimerButton.IsEnabled = true;
            });

            canvasView.InvalidateSurface();
        }

        private void AnimationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isAnimating)
            {
                UpdateSandParticles();
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (time.TotalSeconds > 0)
                {
                    time -= TimeSpan.FromSeconds(1);
                    TimerLabel.Text = time.ToString(@"mm\:ss");
                    elapsedPercentage = 1 - (float)(time.TotalSeconds / totalTime.TotalSeconds);

                    canvasView.InvalidateSurface();
                }
                else
                {
                    if (mainTimer != null)
                    {
                        mainTimer.Stop();
                        mainTimer.Dispose();
                        mainTimer = null;
                    }
                    if (animationTimer != null)
                    {
                        animationTimer.Stop();
                        animationTimer.Dispose();
                        animationTimer = null;
                    }

                    TimerLabel.Text = "00:00";
                    isTimerRunning = false;
                    isAnimating = false;
                    isTimerCompleted = true;

                    startTimerButton.IsEnabled = true;

                    canvasView.InvalidateSurface();
                }
            });
        }

        private void StopTimers()
        {
            if (mainTimer != null)
            {
                mainTimer.Stop();
                mainTimer.Dispose();
                mainTimer = null;
            }

            if (animationTimer != null)
            {
                animationTimer.Stop();
                animationTimer.Dispose();
                animationTimer = null;
            }

            isAnimating = false;
            isTimerRunning = false;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                startTimerButton.IsEnabled = true;
            });
        }

        private void resetTimerButton_Clicked(object sender, EventArgs e)
        {
            StopTimers();
            time = TimeSpan.Zero;
            TimerLabel.Text = time.ToString(@"mm\:ss");
            isTimerCompleted = false;
            lock (sandParticleLock)
            {
                sandParticles.Clear();
            }
            elapsedPercentage = 0;
            canvasView.InvalidateSurface();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (isAccelerometerActive)
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
            }
        }
        private void RestartTimerAfterFlip()
        {
            if (!isTimerCompleted) return;

            time = totalTime;

            mainTimer = new System.Timers.Timer();
            mainTimer.Interval = 1000;
            mainTimer.Enabled = true;
            mainTimer.Elapsed += OnTimerElapsed;

            isAnimating = true;

            animationTimer = new System.Timers.Timer();
            animationTimer.Interval = 16;
            animationTimer.Enabled = true;
            animationTimer.Elapsed += AnimationTimer_Elapsed;

            isTimerCompleted = false;
            isTimerRunning = true;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerLabel.Text = time.ToString(@"mm\:ss");
                startTimerButton.IsEnabled = false;
                resetTimerButton.IsEnabled = true;
            });
        }
    }
}