﻿using System;
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
        static int minControlPointDistance = 6;
        static public Brush currentColor = Brushes.Black;
        static public Data.SymbolInfo currentSymbolInfo;
        static bool shiftPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            DataViewModel dvm = new DataViewModel();
            DataContext = dvm;

            currentColor = Brushes.Black;

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

            ShowSymbolTab(dataContext as Data.SymbolInfo);
        }
 
        private void AddLoadedChartLines(string name, Chart chart)
        {
            var dvm = DataContext as DataViewModel;

            // add loaded chart lines
            foreach (var data in dvm.SymbolsDrawingsToSerialize)
            {
                if (data.Key == name)
                {
                    // found drawing for symbol
                    foreach (var line in data.Value.chartLines)
                    {
                        Chart.ChartLine lineToAdd = new Chart.ChartLine(chart);

                        // Price to value
                        string[] SPDV = line.StartPointDV.Split(';');
                        double SPV = double.Parse(SPDV[1], Data.numberFormat);
                        double SPVR = Math.Round(Chart.RemapRange(SPV,
                            Chart.drawingInfo.maxVal, Chart.drawingInfo.viewMarginBottom,
                            Chart.drawingInfo.minVal, Chart.drawingInfo.viewHeight - Chart.drawingInfo.viewMarginBottom), 2);

                        string[] EPDV = line.EndPointDV.Split(';');
                        double EPV = double.Parse(EPDV[1], Data.numberFormat);
                        double EPVR = Math.Round(Chart.RemapRange(EPV,
                            Chart.drawingInfo.maxVal, Chart.drawingInfo.viewMarginBottom,
                            Chart.drawingInfo.minVal, Chart.drawingInfo.viewHeight - Chart.drawingInfo.viewMarginBottom), 2);

                        // Create and add new points
                        string[] P1Coords = line.StartPoint.Split(';');
                        lineToAdd.setP1(
                            new Point(double.Parse(P1Coords[0], Data.numberFormat),
                            SPVR));
                        string[] P2Coords = line.EndPoint.Split(';');
                        lineToAdd.setP2(
                            new Point(double.Parse(P2Coords[0], Data.numberFormat),
                            EPVR));

                        if (line.Color == "Black")
                            lineToAdd.color = System.Windows.Media.Brushes.Black;
                        if (line.Color == "Blue")
                            lineToAdd.color = System.Windows.Media.Brushes.Blue;
                        if (line.Color == "Lime")
                            lineToAdd.color = System.Windows.Media.Brushes.Lime;
                        if (line.Color == "Red")
                            lineToAdd.color = System.Windows.Media.Brushes.Red;
                        lineToAdd.linePath.Stroke = lineToAdd.color;

                        lineToAdd.mode = Chart.ChartLine.Mode.Normal;
                        lineToAdd.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                        lineToAdd.Select(false);

                        chart.chartLines.Add(lineToAdd);
                        chart.canvas.Children.Add(lineToAdd.linePath);
                        chart.canvas.Children.Add(lineToAdd.rectPath);

                        lineToAdd.MoveP1(lineToAdd.getP1());
                        lineToAdd.MoveP2(lineToAdd.getP2());

                        chart.selectedLines.Add(lineToAdd);
                    }
                    break;
                }
            }
        }

        private void ShowSymbolTab(Data.SymbolInfo symbolInfo)
        {
            var dvm = DataContext as DataViewModel;
            Chart chart = null;
            if (dvm.SymbolsDrawings.TryGetValue(symbolInfo.FullName, out chart) == false)
            {
                List<Data.SymbolDayData> sdd = Data.GetSymbolData(symbolInfo.ShortName);

                TabItem newTab = new TabItem();
                newTab.KeyDown += TabItem_OnKeyDown;
                newTab.KeyUp += TabItem_OnKeyUp;

                newTab.Header = symbolInfo.FullName;
                SymbolsTabControl.Items.Add(newTab);

                Chart.DrawingInfo di = new Chart.DrawingInfo();
                di.viewHeight = (int)SymbolsTabControl.ActualHeight;
                di.viewWidth = (int)SymbolsTabControl.ActualWidth;
                di.viewMarginTop = 3;
                di.viewMarginBottom = 30 /* TODO: status bar h */ + 20;
                di.viewMarginLeft = 3;
                di.viewMarginRight = 100;
                di.viewAutoScale = true;
                di.crossMargin = 15;

                chart = new Chart(di);
                dvm.SymbolsDrawings.Add(symbolInfo.FullName, chart);
                newTab.Content = chart.CreateDrawing(sdd);

                AddLoadedChartLines(symbolInfo.FullName, chart);
                // remove added symbol
                dvm.SymbolsDrawingsToSerialize.Remove(symbolInfo.FullName);

                SymbolsTabControl.SelectedItem = newTab;
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
            currentSymbolInfo = symbolInfo;
        }

        void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;

            Chart chart = null;
            if (GetDVM().SymbolsDrawings.TryGetValue(activeTab.Header.ToString(), out chart))
            {
                GetDVM().SetCurrentDrawing(chart);
            }

            activeTab.Focus();
            UpdateLayout();
        }

        void SymbolTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            if (tabCtrl.Items.Count == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                if (workMode == WorkMode.Drawing)
                {
                    DrawNewLine(mousePosition);
                }
                else if (workMode == WorkMode.Selecting)
                {
                    // check if we clicked on a control point of selected line
                    SelectControlPoints(mousePosition);
                }
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                workMode = WorkMode.Cross;
                Chart activeChart = GetDVM().CurrentDrawing;
                activeChart.ShowCross(true);
                activeChart.MoveCross(mousePosition);
            }
        }

        private void SelectControlPoints(Point mousePosition)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart != null)
            {
                float minDist = minControlPointDistance;
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

                    float distMidP = Chart.PointPointDistance(line.getMidP(), mousePosition);
                    if (distMidP < minDist)
                    {
                        choosenLine = line;
                        choosenPoint = line.getMidP();
                        drawingMode = Chart.ChartLine.DrawingMode.Mid;
                    }
                }

                if (choosenLine != null)
                {
                    choosenLine.mode = Chart.ChartLine.Mode.Drawing;
                    choosenLine.drawingMode = drawingMode;
                }
            }
        }

        private void DrawNewLine(Point mousePosition)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null)
                return;

            // if there is no lines in drawing state, create a new line
            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            // only one line is drawn at a time
            Debug.Assert(line == null);

            line = new Chart.ChartLine(activeChart);
            line.mode = Chart.ChartLine.Mode.Drawing;
            line.drawingMode = Chart.ChartLine.DrawingMode.P2;

            line.color = currentColor;
            line.linePath.Stroke = currentColor;

            activeChart.chartLines.Add(line);
            activeChart.canvas.Children.Add(line.linePath);
            activeChart.canvas.Children.Add(line.rectPath);

            line.MoveP1(mousePosition);
            line.MoveP2(mousePosition);

            activeChart.selectedLines.Add(line);
        }

        void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;
            
            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                Point mousePosition = e.MouseDevice.GetPosition((Canvas)line.linePath.Parent);
                if (Chart.copyMode == Chart.ChartLine.CopyModes.NotYet)
                {
                    Chart.copyMode = Chart.ChartLine.CopyModes.Copied;
                    Chart.ChartLine newLine = line.CopyLineTo(activeChart);

                    newLine.MoveP1(line.getP1());
                    newLine.MoveP2(line.getP2());
                }

                if (line.mode == Chart.ChartLine.Mode.Drawing)
                {
                    MoveControlPoint(line, mousePosition);
                }
            }

            if (workMode == WorkMode.Cross)
            {
                TabControl tabCtrl = (TabControl)sender;
                TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
                Canvas canvas = (Canvas)tabItem.Content;
                Point mousePosition = e.MouseDevice.GetPosition(canvas);

                activeChart.MoveCross(mousePosition);
            }
        }

        void MoveControlPoint(Chart.ChartLine line, Point mousePosition)
        {
            if (line.drawingMode == Chart.ChartLine.DrawingMode.P1)
                line.MoveP1(mousePosition, shiftPressed);
            else if (line.drawingMode == Chart.ChartLine.DrawingMode.P2)
                line.MoveP2(mousePosition, shiftPressed);
            else if (line.drawingMode == Chart.ChartLine.DrawingMode.Mid)
                line.MoveMid(mousePosition);
        }

        void SymbolTab_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;

            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                line.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                line.mode = Chart.ChartLine.Mode.Selected;
                workMode = WorkMode.Selecting;
            }

            if (e.ChangedButton == MouseButton.Left &&
                workMode == WorkMode.Cross)
            {
                workMode = WorkMode.Selecting;
                activeChart.ShowCross(false);
            }
        }

        void SymbolTab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            if (tabCtrl.Items.Count == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart != null)
            {
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

                if (closestLine != null)
                {
                    closestLine.Select(!closestLine.IsSelected());
                    workMode = WorkMode.Selecting;
                }
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            string output = GetDVM().SerializeToJson();

            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(mydocpath + @"\stocker\");
            using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\stocker\charts.json"))
            {
                outputFile.WriteLine(output);
            }
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            string input;
            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Open the text file using a stream reader.
            using (StreamReader sr = new StreamReader(mydocpath + @"\stocker\charts.json"))
            {
                // Read the stream to a string, and write the string to the console.
                input = sr.ReadToEnd();
            }

            // clear all SymbolsDrawingsToSerialize and create new based on loaded data
            GetDVM().DeserializeFromJson(input);

            // add loaded chart lines for this symbol
            Chart activeChart = GetDVM().CurrentDrawing;
            AddLoadedChartLines(currentSymbolInfo.FullName, activeChart);
            // remove added symbol
            GetDVM().SymbolsDrawingsToSerialize.Remove(currentSymbolInfo.FullName);
        }

        private void buttonInverse_Click(object sender, RoutedEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null)
                return;

            Chart.ChartLine[] linesToInverse = new Chart.ChartLine[activeChart.selectedLines.Count];
            activeChart.selectedLines.CopyTo(linesToInverse);

            foreach (Chart.ChartLine line in linesToInverse)
            {
                Chart.ChartLine newLine = line.CopyLineTo(activeChart);
                line.Select(false);

                Point p1, p2, midP;
                p1 = line.getP1();
                p2 = line.getP2();
                midP = line.getMidP();

                newLine.MoveP1(new Point(p1.X, midP.Y - p1.Y));
                newLine.MoveP2(new Point(p2.X, midP.Y - p2.Y));
                newLine.MoveMid(midP);

                newLine.Select(true);
            }
        }
        
        private void buttonColor_Click(object sender, RoutedEventArgs e)
        {
            foreach (Button b in ((Grid)((Button)sender).Parent).Children)
            {
                b.BorderThickness = new Thickness(0);
            }

            ((Button)sender).BorderThickness = new Thickness(2);
            
            switch (((Button)sender).Name.ToString())
            {
                case "button_black":
                    currentColor = Brushes.Black;
                    break;
                case "button_red":
                    currentColor = Brushes.Red;
                    break;
                case "button_lime":
                    currentColor = Brushes.Lime;
                    break;
                case "button_blue":
                    currentColor = Brushes.Blue;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart != null)
            {
                foreach (Chart.ChartLine l in activeChart.chartLines)
                {
                    if (l.IsSelected())
                    {
                        foreach (var p in activeChart.canvas.Children)
                        {
                            if (p.GetType() == typeof(System.Windows.Shapes.Path))
                            {
                                System.Windows.Shapes.Path path = p as System.Windows.Shapes.Path;
                                if (path.Name == "line_" + l.id)
                                {
                                    path.Stroke = currentColor;
                                }
                            }
                        }
                        l.color = currentColor;
                    }
                }
            }
            UpdateLayout();
        }
                
        public void TabItem_OnKeyDown(object sender, KeyEventArgs e)
        {
            Chart chart = GetDVM().CurrentDrawing;

            if (e.Key == Key.Delete)
            {
                // delete lines from chart
                List<System.Windows.Shapes.Path> toDel = new List<System.Windows.Shapes.Path>();
                foreach (Chart.ChartLine l in chart.chartLines)
                {
                    if (l.IsSelected())
                    {
                        foreach (var p in chart.canvas.Children)
                        {
                            if (p.GetType() == typeof(System.Windows.Shapes.Path))
                            {
                                System.Windows.Shapes.Path path = p as System.Windows.Shapes.Path;
                                if (path.Name == "rect_" + l.id)
                                {
                                    toDel.Add(path);
                                }
                                if (path.Name == "line_" + l.id)
                                {
                                    toDel.Add(path);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < toDel.Count; i++)
                {
                    chart.canvas.Children.Remove(toDel.ElementAt(i));
                }
                chart.chartLines.RemoveAll(l => l.IsSelected());
                chart.selectedLines.Clear();
            }
            else if (e.Key == Key.LeftCtrl || 
                     e.Key == Key.RightCtrl)
            {
                if (Chart.copyMode == Chart.ChartLine.CopyModes.No)
                {
                    Chart.copyMode = Chart.ChartLine.CopyModes.NotYet;
                }
            }
            else if (e.Key == Key.LeftShift ||
                     e.Key == Key.RightShift)
            {
                shiftPressed = true;
            }
        }

        public void TabItem_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.RightCtrl)
            {
                Chart.copyMode = Chart.ChartLine.CopyModes.No;
            }
            else if (e.Key == Key.LeftShift ||
                     e.Key == Key.RightShift)
            {
                shiftPressed = false;
            }
        }
    }
}
