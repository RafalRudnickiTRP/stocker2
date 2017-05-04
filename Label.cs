using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApplication3
{
    public partial class Chart
    {
        private Label crossValueEnd;
        private Label crossValueInfo;
        private Label crossValueStart;
        private Label currentValue;
        private Label crossDate;

        public class Label
        {
            public enum Mode
            {
                Price,
                GhostPrice,
                CrossPrice,
                Date
            };

            private Mode mode;
            private TextBlock valueTextBlock;

            public Label(Canvas canvas, Mode _mode, int z)
            {
                mode = _mode;
                VerticalCenterAlignment = false;
                HorizontalCenterAlignment = false;

                valueTextBlock = new TextBlock();
                valueTextBlock.Text = "aa";
                valueTextBlock.TextAlignment = TextAlignment.Left;
                valueTextBlock.FontSize = 11;
                valueTextBlock.Width = 100;
                if (mode == Mode.GhostPrice)
                {
                    valueTextBlock.Background = Brushes.White;
                    valueTextBlock.Foreground = Brushes.Gray;
                }
                else if (mode == Mode.CrossPrice)
                {
                    valueTextBlock.Background = Brushes.Green;
                    valueTextBlock.Foreground = Brushes.White;
                }
                else
                {
                    valueTextBlock.Background = Brushes.Black;
                    valueTextBlock.Foreground = Brushes.White;
                }
                valueTextBlock.Visibility = Visibility.Hidden;

                Canvas.SetLeft(valueTextBlock, 0);
                Canvas.SetBottom(valueTextBlock, 0);
                Canvas.SetZIndex(valueTextBlock, z);
                canvas.Children.Add(valueTextBlock);
            }

            public void Show(bool show)
            {
                valueTextBlock.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            }

            public void SetValue(double val, string comment = "")
            {
                string valStr = string.Format(" {0:F2}", val);
                valueTextBlock.Text = valStr;
                if (!comment.Equals("") && !comment.Equals("0"))
                    valueTextBlock.Text += comment;
            }

            public void SetDate(DateTime? date)
            {
                if (date.HasValue)
                {
                    valueTextBlock.Text = ((DateTime)date).ToShortDateString() + " " + 
                        ((DateTime)date).DayOfWeek.ToString().Substring(0, 3);
                }
                else
                    valueTextBlock.Text = "";
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

        Label CreatePriceLabel(Canvas canvas, double value, bool show, Label.Mode mode)
        {
            currentValue = new Label(canvas, mode, 0);
            currentValue.SetValue(value);
            double y = Misc.RemapRangeValToPix(value, drawingInfo);
            currentValue.SetPosition(new Point(drawingInfo.viewWidth - drawingInfo.viewMarginRight + 2, y));
            currentValue.Show(show);

            return currentValue;
        }

        void CreateCrossLabels(Canvas canvas)
        {
            crossValueStart = new Label(canvas, Label.Mode.CrossPrice, 1);
            crossValueStart.Show(false);
            crossValueStart.VerticalCenterAlignment = true;
            crossValueInfo = new Label(canvas, Label.Mode.CrossPrice, 1);
            crossValueInfo.Show(false);
            crossValueInfo.VerticalCenterAlignment = true;
            crossValueEnd = new Label(canvas, Label.Mode.CrossPrice, 1);
            crossValueEnd.Show(false);
            crossValueEnd.VerticalCenterAlignment = true;

            crossDate = new Label(canvas, Label.Mode.Date, 0);
            crossDate.Show(false);
            crossDate.HorizontalCenterAlignment = true;            
        }
    }
}
