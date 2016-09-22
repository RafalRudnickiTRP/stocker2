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
        public class ChartLine
        {
            private static int selectionRectWidth2 = 2;

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
                linePath.Name = "line_".ToString() + id.ToString();

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
                rectPath.Name = "rect_".ToString() + id.ToString();
            }

            public void MoveP1(Point p)
            {
                Point p2 = line.EndPoint;
                line.StartPoint = p;
                midRect.Transform = new TranslateTransform(p.X + (p2.X - p.X) / 2 - selectionRectWidth2, p.Y + (p2.Y - p.Y) / 2 - selectionRectWidth2);
                p1Rect.Transform = new TranslateTransform(p.X - selectionRectWidth2, p.Y - selectionRectWidth2);
            }

            public void MoveP2(Point p)
            {
                Point p1 = line.StartPoint;
                line.EndPoint = p;
                midRect.Transform = new TranslateTransform(p1.X + (p.X - p1.X) / 2 - selectionRectWidth2, p1.Y + (p.Y - p1.Y) / 2 - selectionRectWidth2);
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
            }

            public DataToSerialize SerializeToJson()
            {
                DataToSerialize toSerialize = new DataToSerialize()
                {
                    StartPoint = line.StartPoint.ToString(),
                    EndPoint = line.EndPoint.ToString(),
                };
                return toSerialize;
            }
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
        }

        public Chart()
        {
            chartLines = new List<ChartLine>();
            selectedLines = new List<ChartLine>();
        }

        #region Members

        public Canvas canvas;
        public List<ChartLine> chartLines;
        public List<ChartLine> selectedLines;

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

        public static float RemapRange(float value, float from1, float to1, float from2, float to2)
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

        public Canvas CreateDrawing(DrawingInfo di, List<Data.SymbolDayData> sddList)
        {
            if (sddList.Count == 0)
                return null;

            di.candleWidth = 5;
            di.candleMargin = 1;

            canvas = new Canvas();
            canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            canvas.SnapsToDevicePixels = true;
            canvas.UseLayoutRounding = true;

            GeometryGroup frameGeom = new GeometryGroup();
            frameGeom.Children.Add(new RectangleGeometry(new Rect(
                new Point(di.viewMarginLeft, di.viewMarginTop),
                new Point(di.viewWidth - di.viewMarginRight, di.viewHeight - di.viewMarginBottom))));

            Path framePath = new Path();
            framePath.StrokeThickness = 1;
            framePath.Stroke = Brushes.Black;
            framePath.Data = frameGeom;
            canvas.Children.Add(framePath);

            int frameWidth = di.viewWidth - di.viewMarginLeft - di.viewMarginRight - 2 * (int)framePath.StrokeThickness;
            int candleWidth = di.candleWidth + di.candleMargin * 2;
            int numCandlesToDraw = frameWidth / candleWidth;
            numCandlesToDraw = Math.Min(sddList.Count, numCandlesToDraw);

            int minLow = 1000000, maxHi = 0;
            if (di.viewAutoScale)
            {
                foreach(Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
                {
                    int hi = (int)Math.Ceiling(sdd.Hi);
                    int low = (int)Math.Floor(sdd.Low);

                    minLow = low < minLow ? low : minLow;
                    maxHi = hi > maxHi ? hi : maxHi;
                }
            }

            int start = di.viewWidth - di.viewMarginRight - di.candleMargin - 
                (int)framePath.StrokeThickness - di.candleWidth / 2;

            foreach (Data.SymbolDayData sdd in sddList.GetRange(0, numCandlesToDraw))
            {
                float[] vals = {
                    sdd.Hi,
                    sdd.Open >= sdd.Close ? sdd.Open : sdd.Close,
                    sdd.Open > sdd.Close ? sdd.Close : sdd.Open,
                    sdd.Low
                };

                if (di.viewAutoScale)
                {
                    int minV = di.viewMarginBottom + (int)framePath.StrokeThickness + di.candleMargin;
                    int maxV = di.viewHeight - minV;

                    vals[0] = RemapRange(vals[0], minLow, maxV, maxHi, minV);
                    vals[1] = RemapRange(vals[1], minLow, maxV, maxHi, minV);
                    vals[2] = RemapRange(vals[2], minLow, maxV, maxHi, minV);
                    vals[3] = RemapRange(vals[3], minLow, maxV, maxHi, minV);
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
                bodyPath.Fill = sdd.Open > sdd.Close ? Brushes.Black : Brushes.White;
                bodyPath.Data = bodyGeom;

                canvas.Children.Add(bodyPath);

                start -= di.candleWidth + di.candleMargin * 2;
            }

            return canvas;
        }
    }
}
