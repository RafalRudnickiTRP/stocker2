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
        public class ChartLine
        {
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
                var P1DT = Misc.PixelToSdd(drawingInfo, getP1());
                var P2DT = Misc.PixelToSdd(drawingInfo, getP2());

                // values
                double P1ValY = Math.Round(Misc.RemapRange(getP1().Y,
                    drawingInfo.viewMarginBottom, drawingInfo.maxVal,
                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal), 6);
                double P2ValY = Math.Round(Misc.RemapRange(getP2().Y,
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

                toSerialize.Color = Misc.BrushToString(color);

                return toSerialize;
            }
        }
    }
}