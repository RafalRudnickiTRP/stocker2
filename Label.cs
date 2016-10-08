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
        private Label crossValue;
        private Label currentValue;
        private Label crossDate;

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

            public void SetDate(DateTime? date)
            {
                if (date.HasValue)
                    valueTextBlock.Text = ((DateTime)date).ToShortDateString();
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
        
        void CreateLabels(Canvas canvas)
        {

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
        }
    }
}
