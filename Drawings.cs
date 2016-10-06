using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

using Newtonsoft.Json;

namespace WpfApplication3
{
    public class Chart
    {
        private static int selectionRectWidth2 = 3;
        private static int candleWidth = 5;
        private static int candleMargin = 1;

        public class ChartLine
        {
            public enum Mode
            {
                Invalid,
                Drawing,
                Normal,
                Selected
            }

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
                    p.Y = line.EndPoint.Y + (line.EndPoint.X - p.X) * v.Y;
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

            public struct DataToSerialize
            {
                public string StartPoint { get; set; }
                public string EndPoint { get; set; }
                public string Color { get; set; }
            }

            public DataToSerialize SerializeToJson()
            {
                DataToSerialize toSerialize = new DataToSerialize()
                {
                    StartPoint = line.StartPoint.ToString(),
                    EndPoint = line.EndPoint.ToString()
                };

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

        public class Label
        {
            public enum Mode
            {
                Price,
                Date
            };

            private Mode mode;
            private TextBlock valueTextBlock;


            public Label(Canvas canvas, Mode _mode)
            {
                mode = _mode;
                VerticalCenterAlignment = false;
                HorizontalCenterAlignment = false;

                valueTextBlock = new TextBlock();
                valueTextBlock.Text = "aa";
                valueTextBlock.TextAlignment = TextAlignment.Left;
                valueTextBlock.FontSize = 11;
                valueTextBlock.Width = 100;
                valueTextBlock.Background = Brushes.Black;
                valueTextBlock.Foreground = Brushes.White;
                valueTextBlock.Visibility = Visibility.Hidden;

                Canvas.SetLeft(valueTextBlock, 0);
                Canvas.SetBottom(valueTextBlock, 0);
                canvas.Children.Add(valueTextBlock);
            }

            public void Show(bool show)
            {
                valueTextBlock.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            }

            public void SetValue(double val)
            {
                string valStr = string.Format(" {0:F2}", val);
                valueTextBlock.Text = valStr;
            }

            public void SetPosition(Point pos)
            {
                double xOffset = HorizontalCenterAlignment ? -valueTextBlock.ActualWidth / 2 : 0;
                double yOffset = VerticalCenterAlignment ? -valueTextBlock.ActualHeight / 2 : 0;

                Canvas.SetLeft(valueTextBlock, pos.X + xOffset);
                Canvas.SetTop(valueTextBlock, pos.Y + yOffset);
            }

            public bool VerticalCenterAlignment { get; set; }
            public bool HorizontalCenterAlignment { get; set; }
        }

        public void MoveCross(Point p)
        {
            Debug.Assert(crossGeom.Children.Count == 4);

            // top
            ((LineGeometry)crossGeom.Children[0]).StartPoint = new Point(p.X, drawingInfo.viewMarginTop);
            ((LineGeometry)crossGeom.Children[0]).EndPoint = new Point(p.X, p.Y - drawingInfo.crossMargin);

            // bottom
            ((LineGeometry)crossGeom.Children[1]).StartPoint = new Point(p.X, p.Y + drawingInfo.crossMargin);
            ((LineGeometry)crossGeom.Children[1]).EndPoint = new Point(p.X, drawingInfo.viewHeight - drawingInfo.viewMarginBottom);

            // left
            ((LineGeometry)crossGeom.Children[2]).StartPoint = new Point(drawingInfo.viewMarginLeft, p.Y);
            ((LineGeometry)crossGeom.Children[2]).EndPoint = new Point(p.X - drawingInfo.crossMargin, p.Y);

            // right
            ((LineGeometry)crossGeom.Children[3]).StartPoint = new Point(p.X + drawingInfo.crossMargin, p.Y);
            ((LineGeometry)crossGeom.Children[3]).EndPoint = new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight, p.Y);

            // value
            double val = RemapRange(p.Y, 
                drawingInfo.viewMarginBottom, drawingInfo.maxVal, 
                drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal);
            crossValue.SetValue(val);
            crossValue.SetPosition(new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight + 2, p.Y));

            // date
            crossDate.SetValue(352.4);
            crossDate.SetPosition(new Point(p.X, drawingInfo.viewHeight - drawingInfo.viewMarginBottom + 2));
        }

        public void ShowCross(bool show)
        {
            crossPath.Visibility = show ? Visibility.Visible : Visibility.Hidden;

            crossValue.Show(show);
            crossDate.Show(show);
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
        }
        private DrawingInfo drawingInfo;

        public Chart(DrawingInfo di)
        {
            chartLines = new List<ChartLine>();
            selectedLines = new List<ChartLine>();

            drawingInfo = di;
        }

        public enum CopyModes
        {
            No,
            NotYet,
            Copied
        };
        
        #region Members

        public Canvas canvas;
        public List<ChartLine> chartLines;
        public List<ChartLine> selectedLines;
        static public CopyModes copyMode;

        private GeometryGroup crossGeom;
        private Path crossPath;
        private Label crossValue;

        private Label currentValue;

        private Label crossDate;

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

        public static double RemapRange(double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
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

        public Canvas CreateDrawing(List<Data.SymbolDayData> sddList)
        {
            if (sddList.Count == 0)
                return null;
            
            drawingInfo.candleWidth = candleWidth;
            drawingInfo.candleMargin = candleMargin;

            canvas = new Canvas();
            canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            canvas.SnapsToDevicePixels = true;
            canvas.UseLayoutRounding = true;
            
            GeometryGroup frameGeom = new GeometryGroup();
            frameGeom.Children.Add(new RectangleGeometry(new Rect(
                new Point(drawingInfo.viewMarginLeft, 
                          drawingInfo.viewMarginTop),
                new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight, 
                          drawingInfo.viewHeight - drawingInfo.viewMarginBottom))));

            Path framePath = new Path();
            framePath.StrokeThickness = 1;
            framePath.Stroke = Brushes.Black;
            framePath.Data = frameGeom;
            canvas.Children.Add(framePath);

            int frameWidth = drawingInfo.viewWidth - drawingInfo.viewMarginLeft - drawingInfo.viewMarginRight - 2 * (int)framePath.StrokeThickness;
            int candleWidthTotal = drawingInfo.candleWidth + drawingInfo.candleMargin * 2;
            int numCandlesToDraw = frameWidth / candleWidthTotal;
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
                (int)framePath.StrokeThickness - drawingInfo.candleWidth / 2;
            int minViewport = drawingInfo.viewMarginBottom + (int)framePath.StrokeThickness + drawingInfo.candleMargin;
            int maxViewport = drawingInfo.viewHeight - minViewport;

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

                start -= candleWidthTotal;
            }

            crossGeom = new GeometryGroup();
            crossGeom.Children.Add(new LineGeometry());
            crossGeom.Children.Add(new LineGeometry());
            crossGeom.Children.Add(new LineGeometry());
            crossGeom.Children.Add(new LineGeometry());
            crossPath = new Path();
            crossPath.StrokeThickness = 1;
            crossPath.Stroke = Brushes.Black;
            crossPath.Data = crossGeom;
            crossPath.Visibility = Visibility.Hidden;
            canvas.Children.Add(crossPath);

            crossValue = new Label(canvas, Label.Mode.Price);
            crossValue.Show(false);
            crossValue.VerticalCenterAlignment = true;
            
            currentValue = new Label(canvas, Label.Mode.Price);
            currentValue.SetValue(sddList[0].Close);
            currentValue.SetPosition(new Point(
                drawingInfo.viewWidth - drawingInfo.viewMarginRight + 2,
                RemapRange(sddList[0].Close, minLow, maxViewport, maxHi, minViewport)));
            currentValue.Show(true);

            crossDate = new Label(canvas, Label.Mode.Date);
            crossDate.Show(false);
            crossDate.HorizontalCenterAlignment = true;

            return canvas;
        }
    }
}
