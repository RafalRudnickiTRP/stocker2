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
    public partial class Chart
    {
        private GeometryGroup crossGeom;
        private Path crossPath;


        public void MoveCross(Point p)
        {
            if (crossGeom == null)
                return;
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
            DateTime? dt = PixelToSdd(p);
            crossDate.SetDate(dt);
            crossDate.SetPosition(new Point(p.X, drawingInfo.viewHeight - drawingInfo.viewMarginBottom + 2));
        }

        public void ShowCross(bool show)
        {
            crossPath.Visibility = show ? Visibility.Visible : Visibility.Hidden;

            crossValue.Show(show);
            crossDate.Show(show);
        }

        public void CreateCross(Canvas canvas)
        {
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
        }
    }
}