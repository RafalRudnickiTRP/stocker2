using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApplication3
{
    public class Drawings
    {
        public struct DrawingInfo
        {
            public int viewHeight;
            public int viewWidth;
            public int viewMargin;

            public bool viewAutoScale;

            public int candleWidth;
            public int candleMargin;
        }

        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
        }

        public static Canvas CreateDrawing(DrawingInfo di, List<Data.SymbolDayData> sddList)
        {
            di.candleWidth = 5;
            di.candleMargin = 1;

            Canvas canvas = new Canvas();
            canvas.SnapsToDevicePixels = true;
            canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);

            GeometryGroup frameGeom = new GeometryGroup();
            frameGeom.Children.Add(new RectangleGeometry(new Rect(
                new Point(di.viewMargin, di.viewMargin),
                new Point(di.viewWidth - di.viewMargin, di.viewHeight - di.viewMargin))));

            Path framePath = new Path();
            framePath.StrokeThickness = 1;
            framePath.Stroke = Brushes.Black;
            framePath.Data = frameGeom;
            canvas.Children.Add(framePath);

            int frameWidth = di.viewWidth - 2 * di.viewMargin - 2 * (int)framePath.StrokeThickness;
            int candleWidth = di.candleWidth + di.candleMargin * 2;
            int numCandlesToDraw = frameWidth / candleWidth;

            int minLow = 1000000, maxHi = 0;
            if (di.viewAutoScale)
            {
                foreach(Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
                {
                    int hi = (int)Math.Ceiling(sdd.hi);
                    int low = (int)Math.Floor(sdd.low);

                    minLow = low < minLow ? low : minLow;
                    maxHi = hi > maxHi ? hi : maxHi;
                }
            }

            int start = di.viewWidth - di.viewMargin - di.candleMargin - (int)framePath.StrokeThickness - di.candleWidth / 2;
            foreach (Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
            {
                float[] vals = {
                    sdd.hi,
                    sdd.open >= sdd.close ? sdd.open : sdd.close,
                    sdd.open > sdd.close ? sdd.close : sdd.open,
                    sdd.low
                };

                if (di.viewAutoScale)
                {
                    int minV = di.viewMargin + (int)framePath.StrokeThickness + di.candleMargin;
                    int maxV = di.viewHeight - minV;

                    vals[0] = Remap(vals[0], minLow, maxV, maxHi, minV);
                    vals[1] = Remap(vals[1], minLow, maxV, maxHi, minV);
                    vals[2] = Remap(vals[2], minLow, maxV, maxHi, minV);
                    vals[3] = Remap(vals[3], minLow, maxV, maxHi, minV);
                }

                GeometryGroup shadowGeom = new GeometryGroup();
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(start, vals[0]),
                    new Point(start, vals[1])));
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(start, vals[2]),
                    new Point(start, vals[3])));

                Path shadowPath = new Path();
                shadowPath.StrokeThickness = 1;
                shadowPath.Stroke = Brushes.Black;
                shadowPath.Data = shadowGeom;
                canvas.Children.Add(shadowPath);

                GeometryGroup bodyGeom = new GeometryGroup();
                bodyGeom.Children.Add(new RectangleGeometry(new Rect(
                    new Point(start - di.candleWidth / 2, vals[1]),
                    new Point(start + di.candleWidth / 2, vals[2]))));
                Path bodyPath = new Path();
                bodyPath.StrokeThickness = 1;
                bodyPath.Stroke = Brushes.Black;
                bodyPath.Fill = sdd.open > sdd.close ? Brushes.Black : Brushes.White;
                bodyPath.Data = bodyGeom;

                canvas.Children.Add(bodyPath);

                start -= di.candleWidth + di.candleMargin * 2;
            }

            return canvas;
        }        
    }
}
