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
        int mdInt = 0;

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

        private List<List<HourglassCell>> hourglassCells;

        public MainPage()
        {
            InitializeComponent();
            InitializeHourglassCells();
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
                Color = new SKColor(67, 37, 14), // Fixed conversion issue
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

            // Clear previous cells
            hourglassCells.Clear();

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

                    paint = paintBrown;

                    if (verticalIndex < 17 && verticalIndex > 1 && horizontalIndex != 0 + idx && horizontalIndex != SIZE - 1) paint = paintSand;
                    if (verticalIndex > 16 && verticalIndex < 33 && horizontalIndex != 0 + idx && horizontalIndex != SIZE - 1) paint = paintPale;

                    var cell = new HourglassCell
                    {
                        Rectangle = rect,
                        Row = verticalIndex,
                        Column = horizontalIndex,
                        IsFilled = paint == paintSand,
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
            mainTimer = new System.Timers.Timer(time);
            mainTimer.Interval = 1000;
            mainTimer.Enabled = true;
            mainTimer.Elapsed += OnTimerElapsed;

            animationTimer = new System.Timers.Timer(time);
            animationTimer.Interval = 16; // 60fps
            animationTimer.Enabled = true;
            animationTimer.Elapsed += AnimationTimer_Elapsed;


        }

        private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                //if(!animationTimer.Enabled || )

            });
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                time -= TimeSpan.FromSeconds(1);
                TimerLabel.Text = time.ToString(@"mm\:ss");
                if (time.TotalSeconds <= 0)
                {
                    mainTimer.Stop();
                    TimerLabel.Text = "00:00";
                    mainTimer.Dispose();
                    time = TimeSpan.Zero;
                }
            });
        }

        private void resetTimerButton_Clicked(object sender, EventArgs e)
        {
            time = TimeSpan.Zero;
            TimerLabel.Text = time.ToString(@"mm\:ss");
        }
    }
}
