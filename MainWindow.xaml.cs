using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataViewModel dvm = new DataViewModel();
            DataContext = dvm;
        }
        
        void SymbolsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if ((dataContext is Data.SymbolInfo) == false) return;

            Data.SymbolInfo si = (Data.SymbolInfo)dataContext;
            List<Data.SymbolDayData> sdd = Data.GetSymbolDataFromWeb(si.ShortName);

            TabItem newTab = new TabItem();
            newTab.Header = si.FullName;

            SymbolsTabControl.Items.Add(newTab);
            SymbolsTabControl.SelectedItem = newTab;

            Drawings.DrawingInfo di = new Drawings.DrawingInfo();
            di.viewHeight = (int)SymbolsTabControl.ActualHeight;
            di.viewWidth = (int)SymbolsTabControl.ActualWidth;
            di.viewMargin = 3;
            di.viewAutoScale = true;

            var dvm = DataContext as DataViewModel;
            Drawings drawing = new Drawings();
            dvm.SymbolsDrawings.Add(si.FullName, drawing);
            
            newTab.Content = drawing.CreateDrawing(di, sdd);
        }

        void SymbolTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;

            Drawings.ChartLine line = Drawings.line;
            if (line.show == false)
            {
                line.p1 = e.MouseDevice.GetPosition(canvas);
                line.p2 = line.p1;
                line.show = true;
                line.editing = true;

                Path linePath = new Path();
                linePath.StrokeThickness = 1;
                linePath.Stroke = Brushes.Black;
                linePath.Data = new LineGeometry(line.p1, line.p2);
                line.linePath = linePath;
                canvas.Children.Add(linePath);
            }
            else
            {
                line.p2 = e.MouseDevice.GetPosition(canvas);
                line.linePath.Data = new LineGeometry(line.p1, line.p2);
            }
        }

        void SymbolTab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Drawings.ChartLine line = Drawings.line;
            if (line.editing == true)
            {
                line.editing = false;
                line.show = false;
            }
        }

        void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Drawings.ChartLine line = Drawings.line;
            if (line.editing == true)
            {
                line.p2 = e.MouseDevice.GetPosition((Canvas)line.linePath.Parent);
                line.linePath.Data = new LineGeometry(line.p1, line.p2);
            }
        }
    }
}
