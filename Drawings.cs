using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.Globalization;

namespace WpfApplication3
{
    public partial class Chart
    {
        private static int selectionRectWidth2 = 3;
        private static int candleWidth = 5;
        private static int candleMargin = 1;
        
        public static double RemapRange(double value, double fromMin, double toMin, double fromMax, double toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        public static Tuple<DateTime, double> PixelToSdd(Point p)
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

        public static double DateToPixel(DateTime dt, double frac)
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

        public struct DrawingInfo
        {
            public int viewHeight;
            public int viewWidth;

            public int viewMarginTop;
            public int viewMarginBottom;
            public int viewMarginLeft;
            public int viewMarginRight;

            public bool viewAutoScale;

            public int candleWidth;
            public int candleMargin;

            public int crossMargin;

            public double maxVal, minVal;

            public int minViewport;
            public int maxViewport;

            public DateTime refDateStart;
            public int refPixelXStart;

            public List<Data.SymbolDayData> sddList;
        }
        public static  DrawingInfo drawingInfo;

        public class ChartLine
        {
            public enum Mode
            {
                Invalid,
                Drawing,
                Normal,
                Selected
            }

            public enum CopyModes
            {
                No,
                NotYet,
                Copied
            };

            public enum DrawingMode
            {
                Invalid, P1, P2, Mid
            }
            public DrawingMode drawingMode;

            public Brush color { get; set; }

            private Mode _mode;
            public Mode mode
            {
                get { return _mode; }
                set
                {
                    _mode = value;
                    //Debug.WriteLine("" + _mode);
                }
            }

            public void Select(bool selected)
            {
                if (selected)
                {
                    mode = Mode.Selected;
                    rectPath.Visibility = Visibility.Visible;
                    chart.selectedLines.Add(this);
                }
                else
                {
                    mode = Mode.Normal;
                    rectPath.Visibility = Visibility.Hidden;
                    chart.selectedLines.Remove(this);
                }
            }

            public bool IsSelected()
            {
                return mode == Mode.Selected;
            }

            public Path linePath { get; }
            public Path rectPath { get; }

            public Point getP1() { return line.StartPoint; }
            public void setP1(Point p) { line.StartPoint = p; }
            public Point getP2() { return line.EndPoint; }
            public void setP2(Point p) { line.EndPoint = p; }
            public Point getMidP() { return line.StartPoint + (line.EndPoint - line.StartPoint) / 2; }

            private LineGeometry line;
            private RectangleGeometry p1Rect;
            private RectangleGeometry p2Rect;
            private RectangleGeometry midRect;

            private Chart chart;
            private static int nextId = 0;
            public int id;

            public ChartLine(Chart parentChart)
            {
                chart = parentChart;
                mode = Mode.Invalid;
                drawingMode = DrawingMode.Invalid;
                id = nextId;
                nextId++;

                linePath = new Path();
                linePath.StrokeThickness = 1;
                linePath.Stroke = Brushes.Black;
                line = new LineGeometry();
                linePath.Data = line;
                linePath.Name = "line_" + id;

                rectPath = new Path();
                rectPath.StrokeThickness = 1;
                rectPath.Stroke = Brushes.Black;
                rectPath.Fill = Brushes.White;

                GeometryGroup geom = new GeometryGroup();
                p1Rect = new RectangleGeometry(new Rect(new Size(selectionRectWidth2 * 2, selectionRectWidth2 * 2)));
                p2Rect = new RectangleGeometry(new Rect(new Size(selectionRectWidth2 * 2, selectionRectWidth2 * 2)));
                midRect = new RectangleGeometry(new Rect(new Size(selectionRectWidth2 * 2, selectionRectWidth2 * 2)));
                geom.Children.Add(p1Rect);
                geom.Children.Add(p2Rect);
                geom.Children.Add(midRect);
                rectPath.Data = geom;
                rectPath.Name = "rect_" + id;

                color = linePath.Stroke;
            }

            public void MoveP1(Point p, bool resize = false)
            {
                if (resize)
                {
                    Vector v = line.StartPoint - line.EndPoint;
                    v.Normalize();
                    p.Y = line.StartPoint.Y + (line.StartPoint.X - p.X) * v.Y;
                }

                Point p2 = line.EndPoint;
                line.StartPoint = p;
                midRect.Transform = new TranslateTransform((p.X + p2.X) / 2 - selectionRectWidth2, (p.Y + p2.Y) / 2 - selectionRectWidth2);
                p1Rect.Transform = new TranslateTransform(p.X - selectionRectWidth2, p.Y - selectionRectWidth2);
            }

            public void MoveP2(Point p, bool resize = false)
            {
                if (resize)
                {
                    Vector v = line.StartPoint - line.EndPoint;
                    v.Normalize();
                    p.Y = line.EndPoint.Y + (p.X - line.EndPoint.X) / v.X * v.Y;
                }

                Point p1 = line.StartPoint;
                line.EndPoint = p;
                midRect.Transform = new TranslateTransform((p1.X + p.X) / 2 - selectionRectWidth2, (p1.Y + p.Y) / 2 - selectionRectWidth2);
                p2Rect.Transform = new TranslateTransform(p.X - selectionRectWidth2, p.Y - selectionRectWidth2);
            }

            public void MoveMid(Point p)
            {
                Point p1 = line.StartPoint;
                Point p2 = line.EndPoint;

                Point midP = p1 + (p2 - p1) / 2;
                Vector delta = p - midP;

                line.StartPoint = new Point(p1.X + delta.X + selectionRectWidth2, p1.Y + delta.Y + selectionRectWidth2);
                line.EndPoint = new Point(p2.X + delta.X + selectionRectWidth2, p2.Y + delta.Y + selectionRectWidth2);

                p1Rect.Transform = new TranslateTransform(p1.X + delta.X, p1.Y + delta.Y);
                p2Rect.Transform = new TranslateTransform(p2.X + delta.X, p2.Y + delta.Y);
                midRect.Transform = new TranslateTransform(midP.X + delta.X, midP.Y + delta.Y);
            }

            public ChartLine CopyLineTo(Chart chart)
            {
                // copy line
                ChartLine newLine = new ChartLine(chart);
                newLine.mode = Mode.Normal;
                newLine.drawingMode = DrawingMode.Invalid;
                newLine.Select(false);

                newLine.color = color;
                newLine.linePath.Stroke = linePath.Stroke;

                chart.chartLines.Add(newLine);
                chart.canvas.Children.Add(newLine.linePath);
                chart.canvas.Children.Add(newLine.rectPath);

                return newLine;
            }

            public struct DataToSerialize
            {
                // public string StartPoint { get; set; }
                public string StartPointDV { get; set; }
                // public string EndPoint { get; set; }
                public string EndPointDV { get; set; }
                public string Color { get; set; }
            }

            public DataToSerialize SerializeToJson()
            {
                // dates 
                var P1DT = PixelToSdd(getP1());
                var P2DT = PixelToSdd(getP2());

                // values
                double P1ValY = Math.Round(RemapRange(getP1().Y,
                    drawingInfo.viewMarginBottom, drawingInfo.maxVal,
                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal), 6);
                double P2ValY = Math.Round(RemapRange(getP2().Y,
                    drawingInfo.viewMarginBottom, drawingInfo.maxVal,
                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal), 6);

                DataToSerialize toSerialize = new DataToSerialize();

                /*
                toSerialize.StartPoint = getP1().X.ToString(Data.numberFormat) + ";" +
                    getP1().Y.ToString(Data.numberFormat);
                toSerialize.EndPoint = getP2().X.ToString(Data.numberFormat) + ";" +
                    getP2().Y.ToString(Data.numberFormat);
                */

                // date + value
                toSerialize.StartPointDV = P1DT.Item1.ToString(Data.dateTimeFormat) + "+" +
                    P1DT.Item2.ToString(Data.numberFormat) + ";" +
                    P1ValY.ToString(Data.numberFormat);
                toSerialize.EndPointDV = P2DT.Item1.ToString(Data.dateTimeFormat) + "+" +
                    P2DT.Item2.ToString(Data.numberFormat) + ";" +
                    P2ValY.ToString(Data.numberFormat);

                if (color == Brushes.Black)
                    toSerialize.Color = "Black";
                else if (color == Brushes.Lime)
                    toSerialize.Color = "Lime";
                else if (color == Brushes.Blue)
                    toSerialize.Color = "Blue";
                else if (color == Brushes.Red)
                    toSerialize.Color = "Red";
                else
                    Debug.Assert(false);

                return toSerialize;
            }
        }

        public Chart(DrawingInfo di)
        {
            chartLines = new List<ChartLine>();
            selectedLines = new List<ChartLine>();

            drawingInfo = di;
        }

        #region Members

        public Canvas canvas;
        public List<ChartLine> chartLines;
        public List<ChartLine> selectedLines;
        static public ChartLine.CopyModes copyMode;
        
        #endregion

        public struct DataToSerialize
        {
            public IList<ChartLine.DataToSerialize> chartLines { get; set; }
        }

        public DataToSerialize SerializeToJson()
        {
            DataToSerialize toSerialize = new DataToSerialize()
            {
                chartLines = new List<ChartLine.DataToSerialize>()
            };

            foreach (ChartLine line in chartLines)
            {
                toSerialize.chartLines.Add(line.SerializeToJson());
            }

            return toSerialize;
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

        public class FrameGeom : Shape
        {
            private GeometryGroup frameGeom = new GeometryGroup();

            public FrameGeom(DrawingInfo drawingInfo)
            {
                frameGeom.Children.Add(new RectangleGeometry(new Rect(
                    new Point(drawingInfo.viewMarginLeft,
                              drawingInfo.viewMarginTop),
                    new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight,
                              drawingInfo.viewHeight - drawingInfo.viewMarginBottom))));
                StrokeThickness = 1;
                Stroke = Brushes.Black;
            }

            // TODO: add clip line func etc. here

            protected override Geometry DefiningGeometry
            {
                get { return frameGeom; }
            }
        }

        public Canvas CreateDrawing(List<Data.SymbolDayData> sddList)
        {
            if (sddList.Count == 0)
                return null;
            
            drawingInfo.candleWidth = candleWidth;
            drawingInfo.candleMargin = candleMargin;
            drawingInfo.sddList = sddList;

            canvas = new Canvas();
            canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            canvas.SnapsToDevicePixels = true;
            canvas.UseLayoutRounding = true;

            FrameGeom frame = new FrameGeom(drawingInfo);
            canvas.Children.Add(frame);
            
            int frameWidth = drawingInfo.viewWidth - drawingInfo.viewMarginLeft - drawingInfo.viewMarginRight - 2 * (int)frame.StrokeThickness;
            int candleWidthWithMargins = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;
            int numCandlesToDraw = frameWidth / candleWidthWithMargins;
            numCandlesToDraw = Math.Min(sddList.Count, numCandlesToDraw);

            int minLow = 1000000, maxHi = 0;
            if (drawingInfo.viewAutoScale)
            {
                foreach(Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
                {
                    int hi = (int)Math.Ceiling(sdd.Hi);
                    int low = (int)Math.Floor(sdd.Low);

                    minLow = low < minLow ? low : minLow;
                    maxHi = hi > maxHi ? hi : maxHi;
                }
            }

            drawingInfo.maxVal = maxHi;
            drawingInfo.minVal = minLow;

            int start = drawingInfo.viewWidth - drawingInfo.viewMarginRight - drawingInfo.candleMargin - 
                (int)frame.StrokeThickness - drawingInfo.candleWidth / 2;
            int minViewport = drawingInfo.viewMarginBottom + (int)frame.StrokeThickness + drawingInfo.candleMargin;
            int maxViewport = drawingInfo.viewHeight - minViewport;

            drawingInfo.minViewport = minViewport;
            drawingInfo.maxViewport = maxViewport;

            // Weeks vertical lines
            int ws = start;
            DateTime prev = sddList[0].Date;
            foreach (Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
            {
                if (sdd.Date.DayOfWeek <= prev.DayOfWeek)
                {
                    prev = sdd.Date;
                }
                else
                {
                    GeometryGroup weekGeom = new GeometryGroup();
                    weekGeom.Children.Add(new LineGeometry(
                        new Point(ws + candleWidthWithMargins - drawingInfo.candleWidth / 2 - 1,
                                    drawingInfo.viewMarginTop),
                        new Point(ws + candleWidthWithMargins - drawingInfo.candleWidth / 2 - 1,
                                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom - 1)));
                    Path weekPath = new Path();
                    weekPath.StrokeThickness = 1;
                    weekPath.StrokeDashArray = new DoubleCollection(new double[] { 2, 2 });
                    weekPath.Stroke = Brushes.LightGray;
                    weekPath.Data = weekGeom;
                    canvas.Children.Add(weekPath);

                    prev = sdd.Date;
                }
                ws -= candleWidthWithMargins;
            }

            // Horizontal snap lines of prices
            // Currently every 1.00 zł for delta < 5 zł else every 10zł
            int step = (drawingInfo.maxVal - drawingInfo.minVal) / 10 > 5 ? 10 : 1;
            for (int i = (int)drawingInfo.minVal % step; i < drawingInfo.maxVal; i += step)
            {
                double x = RemapRange(i, drawingInfo.maxVal, maxViewport,
                    drawingInfo.minVal, minViewport);

                GeometryGroup snapGeom = new GeometryGroup();
                snapGeom.Children.Add(new LineGeometry(
                    new Point(drawingInfo.viewMarginLeft, x),
                    new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight - 1, x)));
                Path snapPath = new Path();
                snapPath.StrokeThickness = 1;
                snapPath.StrokeDashArray = new DoubleCollection(new double[] { 2, 2 });
                snapPath.Stroke = Brushes.LightGray;
                snapPath.Data = snapGeom;
                canvas.Children.Add(snapPath);
            }

            // Candles
            bool refStartCreated = false;
            foreach (Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
            {
                double[] sortedVals = {
                    sdd.Hi,
                    sdd.Open >= sdd.Close ? sdd.Open : sdd.Close,
                    sdd.Open > sdd.Close ? sdd.Close : sdd.Open,
                    sdd.Low
                };

                if (drawingInfo.viewAutoScale)
                {
                    sortedVals[0] = RemapRange(sortedVals[0], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[1] = RemapRange(sortedVals[1], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[2] = RemapRange(sortedVals[2], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[3] = RemapRange(sortedVals[3], minLow, maxViewport, maxHi, minViewport);
                }

                GeometryGroup shadowGeom = new GeometryGroup();
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(start, sortedVals[0]),
                    new Point(start, sortedVals[1])));
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(start, sortedVals[2]),
                    new Point(start, sortedVals[3])));

                Path shadowPath = new Path();
                shadowPath.StrokeThickness = 1;
                shadowPath.Stroke = Brushes.Black;
                shadowPath.Data = shadowGeom;
                canvas.Children.Add(shadowPath);

                GeometryGroup bodyGeom = new GeometryGroup();
                bodyGeom.Children.Add(new RectangleGeometry(new Rect(
                    new Point(start - drawingInfo.candleWidth / 2, sortedVals[1]),
                    new Point(start + drawingInfo.candleWidth / 2, sortedVals[2]))));
                Path bodyPath = new Path();
                bodyPath.StrokeThickness = 1;
                bodyPath.Stroke = Brushes.Black;
                bodyPath.Fill = sdd.Open > sdd.Close ? Brushes.Black : Brushes.White;
                bodyPath.Data = bodyGeom;

                canvas.Children.Add(bodyPath);
                
                if (!refStartCreated)
                {
                    drawingInfo.refDateStart = sdd.Date;
                    drawingInfo.refPixelXStart = start;
                    refStartCreated = true;
                }
                
                start -= candleWidthWithMargins;
            }

            CreateCross(canvas);
            CreateLabels(canvas);

            return canvas;
        }
    }
}
