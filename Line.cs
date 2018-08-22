
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;

namespace WpfApplication3
{
    public partial class Chart
    {
        public partial class ChartLine
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

            private LineGeometry line;
            private RectangleGeometry p1Rect;
            private RectangleGeometry p2Rect;
            private RectangleGeometry midRect;

            private Chart chart;
            private static int nextId = 0;
            public int id;

            public Point prevStartPoint;
            public Point prevEndPoint;
            
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

            public Brush color { get; set; }
            public string layerData { get; set; }

            public Path linePath { get; }
            public Path rectPath { get; }
            
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

            public Point getP1()
            {
                return line.StartPoint;
            }

            public void setP1(Point p)
            {
                line.StartPoint = p;
            }

            public Point getP2()
            {
                return line.EndPoint;
            }

            public void setP2(Point p)
            {
                line.EndPoint = p;
            }

            public Point getMidP()
            {
                return line.StartPoint + (line.EndPoint - line.StartPoint) / 2;
            }

            public DrawingInfo GetDrawingInfo()
            {
                return chart.drawingInfo;
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

            public void StorePrevPos()
            {
                prevStartPoint = new Point(getP1().X, getP1().Y);
                prevEndPoint = new Point(getP2().X, getP2().Y);
            }

            public void LoadPrevPos()
            {
                if (prevStartPoint != null)
                    MoveP1(prevStartPoint);
                if (prevEndPoint != null)
                    MoveP2(prevEndPoint);
            }

            public void MoveP1(Point p1, bool resize = false)
            {
                if (resize)
                {
                    Vector v = line.StartPoint - line.EndPoint;
                    v.Normalize();
                    p1.Y = line.StartPoint.Y + (p1.X - line.StartPoint.X) / v.X * v.Y;

                    if ((p1 - line.EndPoint).Length < 1)
                        return; // prevent from 0-len line
                }
                
                if (MainWindow.testMode)
                {
                    Debug.WriteLine("move P1: " + p1.ToString());
                }

                Point p2 = line.EndPoint;
                line.StartPoint = p1;
                midRect.Transform = new TranslateTransform((p1.X + p2.X) / 2 - selectionRectWidth2, (p1.Y + p2.Y) / 2 - selectionRectWidth2);
                p1Rect.Transform = new TranslateTransform(p1.X - selectionRectWidth2, p1.Y - selectionRectWidth2);
                
                // make sure that p1x < p2x
                if (p1.X > p2.X)
                {
                    double t = p1.X;
                    p1.X = p2.X;
                    p2.X = t;
                }

                ColorUpdate(this);
            }

            public void MoveP2(Point p2, bool resize = false)
            {
                if (resize)
                {
                    Vector v = line.StartPoint - line.EndPoint;
                    v.Normalize();
                    p2.Y = line.EndPoint.Y + (p2.X - line.EndPoint.X) / v.X * v.Y;

                    if ((line.StartPoint - p2).Length < 1)
                        return; // prevent from 0-len line
                }

                if (MainWindow.testMode)
                {
                    Debug.WriteLine("move P2: " + p2.ToString());
                }
                
                Point p1 = line.StartPoint;
                line.EndPoint = p2;
                midRect.Transform = new TranslateTransform((p1.X + p2.X) / 2 - selectionRectWidth2, (p1.Y + p2.Y) / 2 - selectionRectWidth2);
                p2Rect.Transform = new TranslateTransform(p2.X - selectionRectWidth2, p2.Y - selectionRectWidth2);

                // make sure that p1x < p2x
                if (p1.X > p2.X)
                {
                    double t = p1.X;
                    p1.X = p2.X;
                    p2.X = t;
                }
                ColorUpdate(this);
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

            public static void ColorUpdate(ChartLine line)
            {
                if (line.color == Brushes.Lime || line.color == Brushes.Red)
                {
                    bool upLine = line.getP1().X > line.getP2().X ?
                        line.getP1().Y < line.getP2().Y : line.getP1().Y > line.getP2().Y;
                    if (upLine)
                        line.color = line.linePath.Stroke = Brushes.Lime;
                    else
                        line.color = line.linePath.Stroke = Brushes.Red;
                }
            }

            public void MoveControlPoint(ChartLine line, Point mousePosition, bool resize)
            {
                if (line.drawingMode == DrawingMode.P1)
                    line.MoveP1(mousePosition, resize);
                else if (line.drawingMode == DrawingMode.P2)
                    line.MoveP2(mousePosition, resize);
                else if (line.drawingMode == DrawingMode.Mid)
                    line.MoveMid(mousePosition);
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

                newLine.layerData = layerData;

                chart.chartLines.Add(newLine);
                chart.canvas.Children.Add(newLine.linePath);
                chart.canvas.Children.Add(newLine.rectPath);

                return newLine;
            }
        }
    }
}