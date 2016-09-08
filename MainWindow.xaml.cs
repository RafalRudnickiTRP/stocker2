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
using System.Diagnostics;


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

            InitializeCommands();
        }
        
        public DataViewModel GetDVM()
        {
            return (DataViewModel)DataContext;
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

            Chart.DrawingInfo di = new Chart.DrawingInfo();
            di.viewHeight = (int)SymbolsTabControl.ActualHeight;
            di.viewWidth = (int)SymbolsTabControl.ActualWidth;
            di.viewMargin = 3;
            di.viewAutoScale = true;

            var dvm = DataContext as DataViewModel;
            Chart chart = new Chart();
            dvm.SymbolsDrawings.Add(si.FullName, chart);
            dvm.SetCurrentDrawing(chart);
            
            newTab.Content = chart.CreateDrawing(di, sdd);
        }

        void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;
            GetDVM().SetCurrentDrawing((Chart)activeTab.Content);
        }

        void SymbolTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            if (tabCtrl.Items.Count == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;

            Chart activeChart = GetDVM().CurrentDrawing;
            Chart.ChartLine line;
            if (activeChart.chartLines.Count == 0)
            {
                line = new Chart.ChartLine();
                activeChart.chartLines.Add(line);
            }
            else
                line = activeChart.chartLines[0];

            if (line.show == false)
            {
                line.p1 = e.MouseDevice.GetPosition(canvas);
                line.p2 = line.p1;
                line.show = true;
                line.editing = true;

                Path linePath = new Path();
                canvas.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);
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
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;

            Chart.ChartLine line;
            if (activeChart.chartLines.Count == 0)
            {
                line = new Chart.ChartLine();
                activeChart.chartLines.Add(line);
            }
            else
                line = activeChart.chartLines[0];
            
            if (line.editing == true)
            {
                line.editing = false;
                line.show = false;
            }
        }

        void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;

            Chart.ChartLine line;
            if (activeChart.chartLines.Count == 0)
            {
                line = new Chart.ChartLine();
                activeChart.chartLines.Add(line);
            }
            else
                line = activeChart.chartLines[0];

            if (line.editing == true)
            {
                line.p2 = e.MouseDevice.GetPosition((Canvas)line.linePath.Parent);
                line.linePath.Data = new LineGeometry(line.p1, line.p2);                
            }
        }        
    }
}
