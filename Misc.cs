using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using static WpfApplication3.Chart;

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
                { Brushes.Orange, "Orange" },
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
                { "Orange", Brushes.Orange },
                { "Red", Brushes.Red },
            };
            
            return map[br];
        }

        public static double RemapRangePixToVal(double pix, DrawingInfo di)
        {
            return RemapRange(pix, di.minViewport, di.maxVal, di.maxViewport, di.minVal);
        }

        public static double RemapRangeValToPix(double value, DrawingInfo di)
        {
            return RemapRange(value, di.minVal, di.maxViewport, di.maxVal, di.minViewport);
        }

        private static double RemapRange(double value, double fromMin, double toMin, double fromMax, double toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        public static Tuple<DateTime, double> PixelToDate(DrawingInfo di, Point p)
        {
            int start = di.viewWidth - di.viewMarginRight - di.candleMargin -
                /*(int)framePath.StrokeThickness*/ 1 - di.candleWidth / 2;
            int candleWidthWithMargins = di.candleWidth + di.candleMargin * 2;
            int candleStart = start - di.candleWidth / 2;
            int nextCandleStart = start + di.candleWidth / 2 + di.candleMargin * 2;
            double maxFract = (double)(nextCandleStart - candleStart - 1) / (nextCandleStart - candleStart);

            int sddIt = 0;
            int candles = 0;

            while (true)
            {
                candleStart = start - di.candleWidth / 2; 
                nextCandleStart = start + di.candleWidth / 2 + di.candleMargin * 2;

                if (candleStart <= p.X &&
                p.X <= nextCandleStart)
                    break;

                if (p.X > nextCandleStart)
                {
                    candles += 1;
                    start += candleWidthWithMargins;
                }
                else
                {
                    start -= candleWidthWithMargins;
                    sddIt++;
                }
            }

            double fract = candles + RemapRange(p.X, candleStart, 0, nextCandleStart, maxFract);
            return Tuple.Create(di.sddList[sddIt].Date, fract);
        }

        // Given three co-linear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        static bool OnSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        static int Orientation(Point p, Point q, Point r)
        {
            // See http://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            double val = (q.Y - p.Y) * (r.X - q.X) -
                      (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) return 0;  // colinear

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        static bool DoIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

            // p1, q1 and p2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }

        public static bool LineValueOnSdd(ChartLine line, Data.SymbolDayData sdd)
        {
            DrawingInfo di = line.GetDrawingInfo();

            double PDR = DateToPixel(di, sdd.Date, 0);

            double PVRLO = Math.Round(RemapRangeValToPix(sdd.Low, di), 6);
            double PVRHI = Math.Round(RemapRangeValToPix(sdd.Hi, di), 6);

            Point pLO = new Point(PDR, PVRLO);
            Point pHI = new Point(PDR, PVRHI);

            return DoIntersect(line.getP1(), line.getP2(), pLO, pHI);
        }

        public static double DateToPixel(DrawingInfo drawingInfo, DateTime dt, double frac)
        {
            double result = 0;

            int start = drawingInfo.viewWidth - drawingInfo.viewMarginRight - drawingInfo.candleMargin -
                /*(int)framePath.StrokeThickness*/ 1 - drawingInfo.candleWidth / 2;
            int candleWidthWithMargins = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;

            foreach (Data.SymbolDayData sddIt in drawingInfo.sddList)
            {
                int candleStart = start - drawingInfo.candleWidth / 2;
                int nextCandleStart = start + drawingInfo.candleWidth / 2 + drawingInfo.candleMargin * 2;

                if (sddIt.Date <= dt)
                {
                    result = candleStart + Math.Round((nextCandleStart - candleStart + 1) * frac);
                    break;
                }

                start -= candleWidthWithMargins;
            }

            return result;
        }

        public static float LinePointDistance(Point p1, Point p2, Point p)
        {
            return (float)(Math.Abs((p2.Y - p1.Y) * p.X - (p2.X - p1.X) * p.Y + p2.X * p1.Y - p2.Y * p1.X) /
                Math.Sqrt(Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.X - p1.X, 2)));
        }

        public static float LineLength(Chart.ChartLine line)
        {
            return PointPointDistance(line.getP1(), line.getP2());
        }

        public static float PointPointDistance(Point p1, Point p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static Point LineStringToPoint(DrawingInfo drawingInfo, string dataDV)
        {
            char[] separators = new char[] { '+', ';' };
            string[] PDV = dataDV.Split(separators);

            // value
            double PV = double.Parse(PDV[2], Data.numberFormat);
            double PVR = Math.Round(RemapRangeValToPix(PV, drawingInfo), 6);

            // date
            DateTime PD = DateTime.ParseExact(PDV[0], Data.dateTimeFormat,
                System.Globalization.CultureInfo.InvariantCulture);
            double PDf = double.Parse(PDV[1], Data.numberFormat); // factor
            double PDR = DateToPixel(drawingInfo, PD, PDf);

            return new Point(PDR, PVR);
        }

    }
}