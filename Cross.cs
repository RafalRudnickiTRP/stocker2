﻿using System;
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
        public class CrossGeom : Shape
        {
            private GeometryGroup geo = new GeometryGroup();
            private DrawingInfo drawingInfo;

            public CrossGeom(DrawingInfo _drawingInfo)
            {
                drawingInfo = _drawingInfo;

                geo.Children.Add(new LineGeometry());
                geo.Children.Add(new LineGeometry());
                geo.Children.Add(new LineGeometry());
                geo.Children.Add(new LineGeometry());
                
                Visibility = Visibility.Hidden;
                StrokeThickness = 1;
                Stroke = Brushes.Black;
            }

            public void Show(bool show)
            {
                Visibility = show ? Visibility.Visible : Visibility.Hidden;
            }

            public void Move(Point p)
            {
                // top
                ((LineGeometry)geo.Children[0]).StartPoint = new Point(p.X, drawingInfo.viewMarginTop);
                ((LineGeometry)geo.Children[0]).EndPoint = new Point(p.X, p.Y - drawingInfo.crossMargin);

                // bottom
                ((LineGeometry)geo.Children[1]).StartPoint = new Point(p.X, p.Y + drawingInfo.crossMargin);
                ((LineGeometry)geo.Children[1]).EndPoint = new Point(p.X, drawingInfo.viewHeight - drawingInfo.viewMarginBottom);

                // left
                ((LineGeometry)geo.Children[2]).StartPoint = new Point(drawingInfo.viewMarginLeft, p.Y);
                ((LineGeometry)geo.Children[2]).EndPoint = new Point(p.X - drawingInfo.crossMargin, p.Y);

                // right
                ((LineGeometry)geo.Children[3]).StartPoint = new Point(p.X + drawingInfo.crossMargin, p.Y);
                ((LineGeometry)geo.Children[3]).EndPoint = new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight, p.Y);
            }

            protected override Geometry DefiningGeometry
            {
                get { return geo; }
            }
        }

        public class HelperLineGeom : Shape
        {
            private GeometryGroup geo = new GeometryGroup();
            private DrawingInfo drawingInfo;

            public HelperLineGeom(DrawingInfo _drawingInfo)
            {
                drawingInfo = _drawingInfo;

                geo.Children.Add(new LineGeometry());

                Visibility = Visibility.Hidden;
                StrokeThickness = 1;
                Stroke = Brushes.Black;
            }

            public Point GetStart()
            {
                var line = geo.Children[0] as LineGeometry;
                return line.StartPoint;
            }

            public void Start(Point p)
            {
                var line = geo.Children[0] as LineGeometry;
                line.StartPoint = p;
            }

            public void End(Point p)
            {
                var line = geo.Children[0] as LineGeometry;
                line.EndPoint = p;
            }

            public void Show(bool show)
            {
                Visibility = show ? Visibility.Visible : Visibility.Hidden;
            }

            protected override Geometry DefiningGeometry
            {
                get { return geo; }
            }
        }

        private CrossGeom cross;
        private HelperLineGeom helper;
        private bool helperLineStarted;

        public void MoveCross(Point p, bool lpm)
        {
            if (cross == null)
                return;
            
            // helper line
            helper.Visibility = lpm ? Visibility.Visible : Visibility.Hidden;
            if (lpm)
            {
                if (!helperLineStarted)
                {
                    helper.Start(p);
                    helperLineStarted = true;
                }

                helper.End(p);        
            }

            cross.Move(p);
            ShowCross(frame.InsideFrame(p), true);

            // value
            if (helperLineStarted)
            {
                crossValueStart.SetValue(Misc.RemapRangePixToVal(helper.GetStart().Y, drawingInfo));
                crossValueStart.SetPosition(new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight + 2, helper.GetStart().Y));

                double distS = Math.Round(Misc.RemapRangePixToVal(p.Y, drawingInfo), 2);
                double distE = Math.Round(Misc.RemapRangePixToVal(helper.GetStart().Y, drawingInfo), 2);              
                double prc = 100 * (distE - distS) / -distE;
                crossValueInfo.SetValue(Math.Abs(distE - distS), " " + Math.Round(prc, 2) + "%");
                crossValueInfo.SetPosition(new Point(p.X + 2, p.Y - 10));
            }
            crossValueEnd.SetValue(Misc.RemapRangePixToVal(p.Y, drawingInfo));
            crossValueEnd.SetPosition(new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight + 2, p.Y));

            // date
            var dt = Misc.PixelToSdd(drawingInfo, p);
            crossDate.SetDate(dt?.Item1);
            crossDate.SetPosition(new Point(p.X, drawingInfo.viewHeight - drawingInfo.viewMarginBottom + 2));
        }

        public void ShowCross(bool show, bool fromMove = false)
        {
            cross.Visibility = show ? Visibility.Visible : Visibility.Hidden;

            if (!show && !fromMove)
            {
                helperLineStarted = false;
                helper.Visibility = Visibility.Hidden;
            }

            crossValueStart.Show(helperLineStarted);
            crossValueInfo.Show(helperLineStarted);

            crossValueEnd.Show(show);
            crossDate.Show(show);
        }

        public void CreateCross(Canvas canvas)
        {
            cross = new CrossGeom(drawingInfo);
            helper = new HelperLineGeom(drawingInfo);

            helperLineStarted = false;

            canvas.Children.Add(cross);
            canvas.Children.Add(helper);
        }
    }
}