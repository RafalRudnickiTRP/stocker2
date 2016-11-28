using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using System;
using System.ComponentModel;
using System.Windows.Data;

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
        static bool ctrlPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            
            DataViewModel dvm = new DataViewModel();
            DataContext = dvm;

            currentColor = Brushes.Black;

            UpdateListView();

            InitializeCommands();

            ReportView.ItemsSource = DataViewModel.ReportItems;
        }

        private DataViewModel GetDVM()
        {
            return (DataViewModel)DataContext;
        }

        private void SymbolsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if ((dataContext is Data.SymbolInfo) == false) return;

            ShowSymbolTab(dataContext as Data.SymbolInfo);
        }
        
        private void ShowSymbolTab(Data.SymbolInfo symbolInfo)
        {
            var dvm = DataContext as DataViewModel;
            Chart chart = null;
            if (DataViewModel.SymbolsDrawings.TryGetValue(symbolInfo.FullName, out chart) == false)
            {
                List<Data.SymbolDayData> sdd = dvm.GetSymbolData(symbolInfo.ShortName);

                TabItem newTab = new TabItem();
                newTab.KeyDown += TabItem_OnKeyDown;
                newTab.KeyUp += TabItem_OnKeyUp;

                newTab.Header = symbolInfo.FullName;
                SymbolsTabControl.Items.Add(newTab);

                Chart.DrawingInfo di = new Chart.DrawingInfo((int)SymbolsTabControl.ActualWidth, (int)SymbolsTabControl.ActualHeight);

                chart = new Chart(di);
                DataViewModel.SymbolsDrawings.Add(symbolInfo.FullName, chart);
                newTab.Content = chart.CreateDrawing(sdd);

                if (DataViewModel.SymbolsDrawingsToSerialize != null)
                {
                    chart.AddLoadedChartLines(DataViewModel.SymbolsDrawingsToSerialize, symbolInfo.FullName);
                    // remove added symbol
                    DataViewModel.SymbolsDrawingsToSerialize.Remove(symbolInfo.FullName);
                }

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
            DataViewModel.SetCurrentDrawing(chart);
            currentSymbolInfo = symbolInfo;
        }

        private void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            if (a.Source is ListView) return;

            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;

            Chart chart = null;
            if (DataViewModel.SymbolsDrawings.TryGetValue(activeTab.Header.ToString(), out chart))
            {
                DataViewModel.SetCurrentDrawing(chart);
            }

            activeTab.Focus();
            UpdateLayout();
        }

        private void SymbolTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            // report is a first item
            if (tabCtrl.Items.Count == 1) return;

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
                Chart activeChart = DataViewModel.CurrentDrawing;
                activeChart.ShowCross(true);
                activeChart.MoveCross(mousePosition);
            }
        }

        private void SelectControlPoints(Point mousePosition)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart != null)
            {
                float minDist = minControlPointDistance;
                Chart.ChartLine choosenLine = null;
                Point choosenPoint;
                Chart.ChartLine.DrawingMode drawingMode = Chart.ChartLine.DrawingMode.Invalid;

                foreach (Chart.ChartLine line in activeChart.selectedLines)
                {
                    float distP1 = Misc.PointPointDistance(line.getP1(), mousePosition);
                    if (distP1 < minDist)
                    {
                        choosenLine = line;
                        choosenPoint = line.getP1();
                        drawingMode = Chart.ChartLine.DrawingMode.P1;
                    }

                    float distP2 = Misc.PointPointDistance(line.getP2(), mousePosition);
                    if (distP2 < minDist)
                    {
                        choosenLine = line;
                        choosenPoint = line.getP2();
                        drawingMode = Chart.ChartLine.DrawingMode.P2;
                    }

                    float distMidP = Misc.PointPointDistance(line.getMidP(), mousePosition);
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
            Chart activeChart = DataViewModel.CurrentDrawing;
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

        private void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;
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
                    line.MoveControlPoint(line, mousePosition, shiftPressed);
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

        private void SymbolTab_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart == null) return;

            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                line.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                line.mode = Chart.ChartLine.Mode.Selected;
                workMode = WorkMode.Selecting;

                UpdateListView();
            }

            if (e.ChangedButton == MouseButton.Left &&
                workMode == WorkMode.Cross)
            {
                workMode = WorkMode.Selecting;
                activeChart.ShowCross(false);
            }
        }

        private void UpdateListView()
        {
            DataViewModel.UpdateInfoNames();

            SortSymbolsList();            

            ListView sl = (ListView)FindName("SymbolsList");
            if (sl != null)
            {
                sl.Items.Refresh();
                sl.UpdateLayout();

                if (sl.SelectedItems.Count > 0)
                {
                    sl.ScrollIntoView(sl.SelectedItems[0]);
                }
            }
        }

        private void SymbolTab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabControl tabCtrl = (TabControl)sender;
            // first item is a Report
            if (tabCtrl.Items.Count == 1) return;
            if (tabCtrl.SelectedIndex == 0) return;

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart != null)
            {
                // calc distance to neares object
                // lines

                // TODO: limit min distance to some value

                float minDist = float.MaxValue;
                Chart.ChartLine closestLine = null;
                foreach (Chart.ChartLine line in activeChart.chartLines)
                {
                    float dist = Misc.LinePointDistance(line.getP1(), line.getP2(), mousePosition);
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
            // backup first previous data
            string now = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
            try
            {
                File.Move(Data.GetPath() + @"charts.json", Data.GetPath() + @"charts_" + now + ".json");
            }
            catch (Exception)
            {
                // file missing or sth worse..
            }

            // try to write data
            using (StreamWriter outputFile = new StreamWriter(Data.GetPath() + @"charts.json"))
            {
                string output = GetDVM().SerializeToJson();
                outputFile.WriteLine(output);
            }
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            string input;
            // Open the text file using a stream reader.
            using (StreamReader sr = new StreamReader(Data.GetPath() + @"charts.json"))
            {
                // Read the stream to a string, and write the string to the console.
                input = sr.ReadToEnd();
            }

            // clear all SymbolsDrawingsToSerialize and create new based on loaded data
            GetDVM().DeserializeFromJson(input);

            // add loaded chart lines for this symbol
            Chart activeChart = DataViewModel.CurrentDrawing;
            activeChart.AddLoadedChartLines(DataViewModel.SymbolsDrawingsToSerialize, currentSymbolInfo.FullName);
            // remove added symbol
            DataViewModel.SymbolsDrawingsToSerialize.Remove(currentSymbolInfo.FullName);
            
            UpdateListView();
        }

        private void buttonInverse_Click(object sender, RoutedEventArgs e)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;
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

            string colorName = ((Button)sender).Name.ToString().Split('_')[1];
            currentColor = Misc.StringToBrush(colorName);

            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart != null)
            {
                foreach (Chart.ChartLine line in activeChart.chartLines)
                {
                    if (line.IsSelected())
                    {
                        foreach (var p in activeChart.canvas.Children)
                        {
                            if (p.GetType() == typeof(System.Windows.Shapes.Path))
                            {
                                System.Windows.Shapes.Path path = p as System.Windows.Shapes.Path;
                                if (path.Name == "line_" + line.id)
                                {
                                    path.Stroke = currentColor;
                                }
                            }
                        }
                        line.color = currentColor;
                        Chart.ChartLine.ColorUpdate(line);
                    }
                }
            }

            UpdateListView();
            UpdateLayout();
        }

        //private void buttonRaport_Click(object sender, RoutedEventArgs e)
        //{
        //    TextBlock tb = (TextBlock)FindName("TextBlockControl");
        //    if (tb == null)
        //        return;

        //    GetDVM().GenerateRaport();

        //    //foreach (KeyValuePair<string, Chart.DataToSerialize> dic in GetDVM().SymbolsDrawingsToSerialize)
        //    //{
        //    //    string name = dic.Key;

        //    //    // find short name
        //    //    string shname = "";
        //    //    foreach (Data.SymbolInfo si in GetDVM().SymbolsInfoList)
        //    //    {
        //    //        if (si.FullName == name)
        //    //        {
        //    //            shname = si.ShortName;
        //    //            break;
        //    //        }
        //    //    }

        //    //    List<Data.SymbolDayData> sdds = GetDVM().SDDs[shname];

        //    //    Chart.DataToSerialize data = dic.Value;
        //    //    IList<Chart.ChartLine.DataToSerialize> list = data.chartLines;

        //    //    foreach (Chart.ChartLine.DataToSerialize line in list)
        //    //    {
        //    //        bool CheckTrendUp = (line.Color.ToString() == Misc.BrushToString(Brushes.Lime));
        //    //        bool CheckTrendDown = (line.Color.ToString() == Misc.BrushToString(Brushes.Red));

        //    //        if (!CheckTrendUp && !CheckTrendDown)
        //    //            continue;

        //    //        foreach (Data.SymbolDayData sdd in sdds)
        //    //        {
        //    //            // Trigger.Type type = Trigger.Check(line, sdd);
        //    //            // 
        //    //            // if (type == Trigger.Type.CrossUpLineWithTrend && CheckTrendUp)
        //    //            //     tb.Text = "UP at date " + sdd.Date.ToShortDateString();
        //    //            // if (type == Trigger.Type.CrossDownLineWithTrend && CheckTrendDown)
        //    //            //     tb.Text = "DOWN at date " + sdd.Date.ToShortDateString();
        //    //        }
        //    //    }
        //    //}
        //}

        private void selectDeselectLines()
        {
            Chart chart = DataViewModel.CurrentDrawing;

            // if everything is selected - deselect all
            // else - select everything
            bool everythingSelected = true;
            foreach (Chart.ChartLine l in chart.chartLines)
            {
                if (l.IsSelected() == false)
                {
                    everythingSelected = false;
                    break;
                }
            }

            foreach (Chart.ChartLine l in chart.chartLines)
            {
                l.Select(!everythingSelected);
            }
        }

        private void buttonSelectDeselect_Click(object sender, RoutedEventArgs e)
        {
            selectDeselectLines();
        }

        private void TabItem_OnKeyDown(object sender, KeyEventArgs e)
        {
            Chart chart = DataViewModel.CurrentDrawing;

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
            else if (e.Key == Key.A)
            {
                if (ctrlPressed == true)
                {
                    selectDeselectLines();
                }
            }
            else if (e.Key == Key.LeftCtrl || 
                     e.Key == Key.RightCtrl)
            {
                ctrlPressed = true;
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

            UpdateListView();
        }

        private void TabItem_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl ||
                e.Key == Key.RightCtrl)
            {
                ctrlPressed = false;
                Chart.copyMode = Chart.ChartLine.CopyModes.No;
            }
            else if (e.Key == Key.LeftShift ||
                     e.Key == Key.RightShift)
            {
                shiftPressed = false;
            }
        }       

        private void SortSymbolsList()
        {
            // sort SymbolsInfoList
            DataViewModel.SymbolsInfoList.Sort(
                delegate (Data.SymbolInfo x, Data.SymbolInfo y) { return x.CompareTo(y); });
        }
        
        private void SymbolsList_ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            SortSymbolsList();
        }
    }
}
