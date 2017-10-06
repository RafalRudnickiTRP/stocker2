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
        public class CandleBodyGeom : Shape
        {
            private GeometryGroup geo = new GeometryGroup();
            private DrawingInfo drawingInfo;

            public CandleBodyGeom(DrawingInfo _drawingInfo, int offset, double[] sortedVals, bool first, bool upDir)
            {
                drawingInfo = _drawingInfo;

                GeometryGroup bodyGeom = new GeometryGroup();
                bodyGeom.Children.Add(new RectangleGeometry(new Rect(
                    new Point(offset - drawingInfo.candleWidth / 2, sortedVals[1]),
                    new Point(offset + drawingInfo.candleWidth / 2, sortedVals[2]))));
                Path bodyPath = new Path();
                bodyPath.StrokeThickness = 1;
                
                geo.Children.Add(bodyGeom);

                StrokeThickness = 1;
                if (first)
                    Stroke = Brushes.Red;
                else
                    Stroke = Brushes.Black;
                Fill = upDir ? Brushes.White : Brushes.Black;
            }

            protected override Geometry DefiningGeometry
            {
                get { return geo; }
            }
        }

        public class CandleShadowGeom : Shape
        {
            private GeometryGroup geo = new GeometryGroup();
            private DrawingInfo drawingInfo;

            public CandleShadowGeom(DrawingInfo _drawingInfo, int offset, double[] sortedVals, bool first, bool upDir)
            {
                drawingInfo = _drawingInfo;

                GeometryGroup shadowGeom = new GeometryGroup();
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(offset, sortedVals[0]),
                    new Point(offset, sortedVals[1])));
                shadowGeom.Children.Add(new LineGeometry(
                    new Point(offset, sortedVals[2]),
                    new Point(offset, sortedVals[3])));
                geo.Children.Add(shadowGeom);

                StrokeThickness = 1;
                if (first)
                    Stroke = Brushes.Red;
                else
                    Stroke = Brushes.Black;
            }

            protected override Geometry DefiningGeometry
            {
                get { return geo; }
            }
        }
        
        public void CreateCandle(Canvas canvas, int offset, Data.SymbolDayData sdd, bool first)
        {
            double[] sortedVals = {
                    sdd.Hi,
                    sdd.Open >= sdd.Close ? sdd.Open : sdd.Close,
                    sdd.Open > sdd.Close ? sdd.Close : sdd.Open,
                    sdd.Low
                };

            if (drawingInfo.viewAutoScale)
            {
                sortedVals[0] = Misc.RemapRangeValToPix(sortedVals[0], drawingInfo);
                sortedVals[1] = Misc.RemapRangeValToPix(sortedVals[1], drawingInfo);
                sortedVals[2] = Misc.RemapRangeValToPix(sortedVals[2], drawingInfo);
                sortedVals[3] = Misc.RemapRangeValToPix(sortedVals[3], drawingInfo);
            }

            bool upDir = sdd.Open > sdd.Close ? false : true ;

            var candle = new CandleBodyGeom(drawingInfo, offset, sortedVals, first, upDir);
            var shadow = new CandleShadowGeom(drawingInfo, offset, sortedVals, first, upDir);

            canvas.Children.Add(candle);
            canvas.Children.Add(shadow);
        }
    }
}