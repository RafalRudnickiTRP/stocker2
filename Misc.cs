using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace WpfApplication3
{
    public class Misc
    {
        public static string BrushToString(Brush br)
        {
            var map = new Dictionary<Brush, string>
            {
                { Brushes.Black, "Black" },
                { Brushes.Lime, "Lime" },
                { Brushes.Blue, "Blue" },
                { Brushes.Red, "Red" },
            };

            return map[br];
        }

        public static Brush StringToBrush(string br)
        {
            var map = new Dictionary<string, Brush>
            {
                { "Black", Brushes.Black },
                { "Lime", Brushes.Lime },
                { "Blue", Brushes.Blue },
                { "Red", Brushes.Red },
            };
            
            return map[br];
        }


        public static double RemapRange(double value, double fromMin, double toMin, double fromMax, double toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static Tuple<DateTime, double> PixelToSdd(Chart.DrawingInfo drawingInfo, Point p)
        {
            int start = drawingInfo.viewWidth - drawingInfo.viewMarginRight - drawingInfo.candleMargin -
                /*(int)framePath.StrokeThickness*/ 1 - drawingInfo.candleWidth / 2;
            int candleWidthWithMargins = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;

            foreach (Data.SymbolDayData sddIt in drawingInfo.sddList)
            {
                int candleStart = start - drawingInfo.candleWidth / 2;
                int nextCandleStart = start + drawingInfo.candleWidth / 2 + drawingInfo.candleMargin * 2;

                if (candleStart <= (int)p.X &&
                    nextCandleStart >= (int)p.X)
                {
                    DateTime dt = new DateTime(sddIt.Date.Ticks);
                    double fract = RemapRange(p.X, candleStart, 0, nextCandleStart, 1);

                    return Tuple.Create(sddIt.Date, fract);
                }

                start -= candleWidthWithMargins;
            }

            return null;
        }

        public static double DateToPixel(Chart.DrawingInfo drawingInfo, DateTime dt, double frac)
        {
            double result = 0;

            int start = drawingInfo.viewWidth - drawingInfo.viewMarginRight - drawingInfo.candleMargin -
                /*(int)framePath.StrokeThickness*/ 1 - drawingInfo.candleWidth / 2;
            int candleWidthWithMargins = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;

            foreach (Data.SymbolDayData sddIt in drawingInfo.sddList)
            {
                int candleStart = start - drawingInfo.candleWidth / 2;
                int nextCandleStart = start + drawingInfo.candleWidth / 2 + drawingInfo.candleMargin * 2;

                if (sddIt.Date == dt)
                {
                    result = candleStart + (nextCandleStart - candleStart) * frac;
                }

                start -= candleWidthWithMargins;
            }

            return result;
        }
    }
}