using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
namespace hourglass_timer_v1
{
    public partial class MainPage : ContentPage
    {
        const int SIZE = 5;
        //TimePicker timerText = new();
        //DateTime time = DateTime.Parse("00:00");
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
        public MainPage()
        {
            InitializeComponent();
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            SKPaint paintGray = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Gray,
                StrokeWidth = 5,
            };

            SKPaint paintRed = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.LightGray,
                StrokeWidth = 5,
            };

            SKPaint paintCyan = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Cyan,
                StrokeWidth = 5,
            };

            float canvasWidth = info.Width;
            float canvasHeight = info.Height;
            float rectWidth = 100;
            float rectHeight = 100;
            int idx = 0;
            bool changeSides = false;
            for (int verticalIndex = 0; verticalIndex < 2*SIZE-1; verticalIndex++) 
            {
                for (int horizontalIndex = 0 + idx; horizontalIndex < SIZE; horizontalIndex ++) 
                {
                    float x = (canvasWidth/2 - (SIZE*rectWidth)/2) + horizontalIndex * rectWidth;
                    float y = verticalIndex * rectHeight;

                    SKRect rect = new(x - idx*(rectWidth/2)+5, y+5, x + rectWidth - idx * (rectHeight / 2), y + rectHeight);
                    SKPaint paint = paintRed;

                    paint = (verticalIndex % 2 == 0) 
                               ? (horizontalIndex % 2 == 0) 
                                   ? paintGray 
                                   : paintRed 
                               : (horizontalIndex % 2 == 1) 
                                   ? paintGray
                                   : paintRed;

                    if (verticalIndex == 4) paint = paintCyan;

                    canvas.DrawRect(rect, paint);
                }
                if (verticalIndex > 3 && changeSides == false) { 
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

            if(button.Text.Equals("-") && time.TotalSeconds > 0)
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
    }
}
