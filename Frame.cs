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
    }
}