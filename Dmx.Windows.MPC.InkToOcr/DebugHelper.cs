using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dmx.Win.MPC.InkToOcr
{
    static class DebugHelper
    {
        public static void ShowNewDebuggingCanvas(double x, double y, double cropHeight, double cropWidth, Grid grid, ref Canvas canvas)
        {
            grid.Children.Remove(canvas);
            canvas = new Canvas();
            Color col = new Color();
            col.A = 80;
            col.B = 10;
            col.G = 10;
            col.R = 100;

            grid.Children.Add(canvas);

            // compute rect
            Rectangle rect = new Rectangle();
            rect.Width = cropWidth;
            rect.Height = cropHeight;
            rect.SetValue(Canvas.LeftProperty, x);
            rect.SetValue(Canvas.TopProperty, y);

            rect.Fill = new SolidColorBrush(col);
            canvas.Children.Add(rect);
        }
    }
}
