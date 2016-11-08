using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;

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

            if (p.X > start)
            {
                // point is drawn in "future"
                // set date to current (last day) + fract > 1
                double fract = RemapRange(p.X - start, 0, 0, candleWidthWithMargins, 1);
                return Tuple.Create(drawingInfo.sddList[0].Date, fract);
            }
            else foreach (Data.SymbolDayData sddIt in drawingInfo.sddList)
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

            // shouldn't ever happen
            Debug.Assert(false);
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

        public static double LineValueOnSdd(Chart.ChartLine line, Data.SymbolDayData sdd)
        {
            throw new NotImplementedException();
            return 0;
        }
        
        public static float LinePointDistance(Point p1, Point p2, Point p)
        {
            return (float)(Math.Abs((p2.Y - p1.Y) * p.X - (p2.X - p1.X) * p.Y + p2.X * p1.Y - p2.Y * p1.X) /
                Math.Sqrt(Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.X - p1.X, 2)));
        }

        public static float PointPointDistance(Point p1, Point p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static Point LineStringToPoint(Chart.DrawingInfo drawingInfo, string dataDV)
        {
            char[] separators = new char[] { '+', ';' };
            string[] PDV = dataDV.Split(separators);

            // value
            double PV = double.Parse(PDV[2], Data.numberFormat);
            double PVR = Math.Round(Misc.RemapRange(PV,
                drawingInfo.maxVal, drawingInfo.viewMarginBottom,
                drawingInfo.minVal, drawingInfo.viewHeight - drawingInfo.viewMarginBottom), 6);

            // date
            DateTime PD = DateTime.ParseExact(PDV[0], Data.dateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            double PDf = double.Parse(PDV[1], Data.numberFormat); // factor
            double PDR = Misc.DateToPixel(drawingInfo, PD, PDf);

            return new Point(PDR, PVR);
        }

    }
}