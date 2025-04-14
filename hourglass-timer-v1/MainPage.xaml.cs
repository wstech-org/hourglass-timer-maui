using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
namespace hourglass_timer_v1
{
    public partial class MainPage : ContentPage
    {
        const int SIZE = 5;
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
                if (verticalIndex > 4 && changeSides == false) { 
                    changeSides = true; 
                    idx--;
                    verticalIndex--;
                }
                if (changeSides) idx--;
                else idx++;
            }
        }
    }
}
