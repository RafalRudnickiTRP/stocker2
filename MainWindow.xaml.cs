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
using System.IO;

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

            AddToSymbolList(dataContext as Data.SymbolInfo);      
        }

        private void AddToSymbolList(Data.SymbolInfo symbolInfo)
        {
            List<Data.SymbolDayData> sdd = Data.GetSymbolDataFromWeb(symbolInfo.ShortName);

            var dvm = DataContext as DataViewModel;
            Chart chart = null;
            if (dvm.SymbolsDrawings.TryGetValue(symbolInfo.FullName, out chart) == false)
            {
                TabItem newTab = new TabItem();
                newTab.Header = symbolInfo.FullName;

                SymbolsTabControl.Items.Add(newTab);
                SymbolsTabControl.SelectedItem = newTab;

                Chart.DrawingInfo di = new Chart.DrawingInfo();
                di.viewHeight = (int)SymbolsTabControl.ActualHeight;
                di.viewWidth = (int)SymbolsTabControl.ActualWidth;
                di.viewMargin = 3;
                di.viewAutoScale = true;

                chart = new Chart();
                dvm.SymbolsDrawings.Add(symbolInfo.FullName, chart);
                newTab.Content = chart.CreateDrawing(di, sdd);
            }
            else
            {
                foreach (TabItem item in SymbolsTabControl.Items)
                {
                    if (item.Header.ToString() == symbolInfo.FullName)
                    {
                        SymbolsTabControl.SelectedItem = item;
                        break;
                    }
                }
            }
            dvm.SetCurrentDrawing(chart);
        }

        void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;
            GetDVM().SetCurrentDrawing(activeTab.Content as Chart);
        }

        void SymbolTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            if (tabCtrl.Items.Count == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            if (workMode == WorkMode.Drawing)
            {
                DrawLine(mousePosition);
            }
            else if (workMode == WorkMode.Selecting)
            {
                // check if we clicked on a controll point of selected line

                Chart activeChart = GetDVM().CurrentDrawing;

                float minDist = 4;
                Chart.ChartLine choosenLine = null;
                Point choosenPoint;
                Chart.ChartLine.DrawingMode drawingMode = Chart.ChartLine.DrawingMode.Invalid;

                foreach (Chart.ChartLine line in activeChart.selectedLines)
                {
                    float distP1 = Chart.PointPointDistance(line.getP1(), mousePosition);
                    if (distP1 < minDist)
                    {
                        choosenLine = line;
                        choosenPoint = line.getP1();
                        drawingMode = Chart.ChartLine.DrawingMode.P1;
                    }

                    float distP2 = Chart.PointPointDistance(line.getP2(), mousePosition);
                    if (distP2 < minDist)
                    {
                        choosenLine = line;
                        choosenPoint = line.getP2();
                        drawingMode = Chart.ChartLine.DrawingMode.P2;
                    }
                }

                if (choosenLine != null)
                {
                    choosenLine.mode = Chart.ChartLine.Mode.Drawing;
                    choosenLine.drawingMode = drawingMode;
                }           
            }
        }
        
        private void DrawLine(Point mousePosition)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null)
                return;

            // if there is no lines in drawing state, create a new line
            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);

            if (line == null)
            {
                line = new Chart.ChartLine(activeChart);
                line.mode = Chart.ChartLine.Mode.Drawing;

                activeChart.chartLines.Add(line);
                activeChart.canvas.Children.Add(line.linePath);
                activeChart.canvas.Children.Add(line.rectPath);

                line.MoveP1(mousePosition);
                line.MoveP2(mousePosition);
            }
            else
            {
                line.MoveP2(mousePosition);
            }
        }

        void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;
            
            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                if (line.mode == Chart.ChartLine.Mode.Drawing)
                {
                    Point mousePosition = e.MouseDevice.GetPosition((Canvas)line.linePath.Parent);
                    line.MoveP2(mousePosition);
                }
            }
        }

        void SymbolTab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;

            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                line.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                line.mode = Chart.ChartLine.Mode.Normal;
                line.Select(false);
                workMode = WorkMode.Selecting;
            }
        }

        void SymbolTab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            if (tabCtrl.Items.Count == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            if (workMode == WorkMode.Selecting)
            {
                Chart activeChart = GetDVM().CurrentDrawing;

                // calc distance to neares object
                // lines

                // TODO: limit min distance to some value

                float minDist = float.MaxValue;
                Chart.ChartLine closestLine = null;
                foreach (Chart.ChartLine line in activeChart.chartLines)
                {
                    float dist = Chart.LinePointDistance(line.getP1(), line.getP2(), mousePosition);
                    if (dist < minDist)
                    {
                        minDist = dist; closestLine = line;
                    }
                }
                
                closestLine.Select(!closestLine.IsSelected());
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            string output = GetDVM().SerializeToJson();

            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\charts.json"))
            {
                outputFile.WriteLine(output);
            }
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            string input;
            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Open the text file using a stream reader.
            using (StreamReader sr = new StreamReader(mydocpath + @"\charts.json"))
            {
                // Read the stream to a string, and write the string to the console.
                input = sr.ReadToEnd();
            }

            GetDVM().DeserializeFromJson(input);            
        }
    }    
}
