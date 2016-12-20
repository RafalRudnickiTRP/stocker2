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
            public int maxNoVertLines;

            public int minViewport;
            public int maxViewport;

            public DateTime refDateStart;
            public int refPixelXStart;

            public List<Data.SymbolDayData> sddList;

            public DrawingInfo(int width, int height)
            {
                viewHeight = height;
                viewWidth = width;
                viewMarginTop = 3;
                viewMarginBottom = 30 /* TODO: status bar h */ + 20;
                viewMarginLeft = 3;
                viewMarginRight = 100;
                viewAutoScale = true;
                crossMargin = 15;
                candleWidth = 5;
                candleMargin = 1;
                maxNoVertLines = 15; // should be max PIXELS height

                maxVal = minVal = 0;
                minViewport = maxViewport = 0;
                refDateStart = DateTime.Now;
                refPixelXStart = 0;
                sddList = null;

            }
        }
        public DrawingInfo drawingInfo;
        
        #region Members

        public Canvas canvas;
        public FrameGeom frame;
        public List<ChartLine> chartLines;
        public List<ChartLine> selectedLines;
        static public ChartLine.CopyModes copyMode;

        #endregion

        public Chart(DrawingInfo di)
        {
            chartLines = new List<ChartLine>();
            selectedLines = new List<ChartLine>();

            drawingInfo = di;
        }

        public void DeleteLine(ChartLine line)
        {
            List<Path> toDel = new List<Path>();
            foreach (var p in canvas.Children)
            {
                if (p.GetType() == typeof(Path))
                {
                    Path path = p as Path;
                    if (path.Name == "rect_" + line.id)
                    {
                        toDel.Add(path);
                    }
                    if (path.Name == "line_" + line.id)
                    {
                        toDel.Add(path);
                    }
                }
            }

            for (int i = 0; i < toDel.Count; i++)
            {
                canvas.Children.Remove(toDel[i]);
            }

            chartLines.Remove(line);
            selectedLines.Remove(line);
        }

        public Canvas CreateDrawing(List<Data.SymbolDayData> sddList)
        {
            if (sddList.Count == 0)
                return null;
            
            drawingInfo.sddList = sddList;

            canvas = new Canvas();
            canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            canvas.SnapsToDevicePixels = true;
            canvas.UseLayoutRounding = true;

            frame = new FrameGeom(drawingInfo);
            canvas.Children.Add(frame);
            
            int frameWidth = drawingInfo.viewWidth - drawingInfo.viewMarginLeft - drawingInfo.viewMarginRight - 2 * (int)frame.StrokeThickness;
            int candleWidthWithMargins = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;
            int numCandlesToDraw = frameWidth / candleWidthWithMargins;
            numCandlesToDraw = Math.Min(sddList.Count, numCandlesToDraw);

            double minLow = 1000000, maxHi = 0;
            if (drawingInfo.viewAutoScale)
            {
                foreach(Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
                {
                    double hi = sdd.Hi;
                    double low = sdd.Low;

                    minLow = low < minLow ? low : minLow;
                    maxHi = hi > maxHi ? hi : maxHi;
                }
            }

            double minMaxStep = 0.1;
            if ((maxHi - minLow) / 0.1 > drawingInfo.maxNoVertLines)
                minMaxStep = 1;
            if ((maxHi - minLow) / 1 > drawingInfo.maxNoVertLines)
                minMaxStep = 5;
            if ((maxHi - minLow) / 5 > drawingInfo.maxNoVertLines)
                minMaxStep = 10;
            if ((maxHi - minLow) / 10 > drawingInfo.maxNoVertLines)
                minMaxStep = 100;

            if (minMaxStep > 1)
            {
                drawingInfo.maxVal = Math.Ceiling(maxHi);
                drawingInfo.minVal = Math.Floor(minLow);
            }
            else
            {
                drawingInfo.maxVal = Math.Round(maxHi + minMaxStep, 1);
                drawingInfo.minVal = Math.Round(minLow, 1);
            }
            
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
            for (double i = drawingInfo.minVal % minMaxStep; i < drawingInfo.maxVal; i += minMaxStep)
            {
                double x = Misc.RemapRange(i, drawingInfo.maxVal, maxViewport,
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
                    sortedVals[0] = Misc.RemapRange(sortedVals[0], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[1] = Misc.RemapRange(sortedVals[1], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[2] = Misc.RemapRange(sortedVals[2], minLow, maxViewport, maxHi, minViewport);
                    sortedVals[3] = Misc.RemapRange(sortedVals[3], minLow, maxViewport, maxHi, minViewport);
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

        public void AddLoadedChartLines(Dictionary<string, Chart.DataToSerialize> symbolsDrawingsToSerialize, string name)
        {
            // add loaded chart lines
            foreach (var data in symbolsDrawingsToSerialize)
            {
                if (data.Key == name)
                {
                    // found drawing for symbol
                    foreach (var line in data.Value.chartLines)
                    {
                        ChartLine lineToAdd = new ChartLine(this);

                        // Create and add new points
                        lineToAdd.setP1(Misc.LineStringToPoint(drawingInfo, line.StartPointDV));
                        lineToAdd.setP2(Misc.LineStringToPoint(drawingInfo, line.EndPointDV));

                        lineToAdd.color = Misc.StringToBrush(line.Color);
                        lineToAdd.linePath.Stroke = lineToAdd.color;

                        lineToAdd.mode = ChartLine.Mode.Normal;
                        lineToAdd.drawingMode = ChartLine.DrawingMode.Invalid;
                        lineToAdd.Select(false);

                        chartLines.Add(lineToAdd);
                        canvas.Children.Add(lineToAdd.linePath);
                        canvas.Children.Add(lineToAdd.rectPath);

                        lineToAdd.MoveP1(lineToAdd.getP1());
                        lineToAdd.MoveP2(lineToAdd.getP2());                        
                    }
                    break;
                }
            }
        }
    }
}
