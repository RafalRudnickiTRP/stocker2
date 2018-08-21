using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Threading.Tasks;

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
        static bool lpmPressed = false;

        static bool showedFromReport = false;

        static public bool testMode = false;

        public string currentLayer = "L1";

        private enum CtrlZMode
        {
            none,
            move,
            delete
        }
        private CtrlZMode ctrlZMode = CtrlZMode.none;

        public MainWindow()
        {
            InitializeComponent();
            
            DataViewModel dvm = new DataViewModel();
            DataContext = dvm;

            currentColor = Brushes.Black;

            UpdateListView();

            InitializeCommands();

            var handle = Task.Factory.StartNew(() => BackgroundWork(this, dvm));
        }

        public static void BackgroundWork(MainWindow window, DataViewModel dvm)
        {
            dvm.GenerateReport();

            window.Dispatcher.Invoke(() =>
            {
                TabItem reportTab = (TabItem)window.SymbolsTabControl.Items[0];
                Debug.Assert(reportTab.Header.ToString() == "Report");
                reportTab.Foreground = Brushes.Red;

                window.ReportView.ItemsSource = DataViewModel.ReportItems;
            });
        }

        private DataViewModel GetDVM()
        {
            return (DataViewModel)DataContext;
        }

        private void SymbolsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if ((dataContext is Data.SymbolInfo) == false) return;

            ShowSymbolTab(((Data.SymbolInfo)dataContext).FullName);
        }

        private void Report_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if ((dataContext is DataViewModel.ReportItem) == false) return;

            ShowSymbolTab(((DataViewModel.ReportItem)dataContext).Symbol);
            showedFromReport = true;
        }

        private string GetHeaderName(TabItem tab)
        {
            string fullName = "";
            var sp = tab.Header as StackPanel;
            if (sp != null)
                fullName = (sp.Children[0] as TextBlock).Text;
            else
                fullName = tab.Header.ToString();

            return fullName;
        }

        private void ShowSymbolTab(string symbolFullName)
        {
            var dvm = DataContext as DataViewModel;
            Chart chart = null;
            Data.SymbolInfo symbolInfo = Data.SymbolInfoList.Single(s => s.FullName == symbolFullName);

            if (DataViewModel.SymbolsDrawings.TryGetValue(symbolInfo.FullName, out chart) == false)
            {
                Chart.DrawingInfo di = new Chart.DrawingInfo(symbolInfo, (int)SymbolsTabControl.ActualWidth, (int)SymbolsTabControl.ActualHeight);

                List<Data.SymbolDayData> sdd = DataViewModel.GetSymbolData(dvm.SDDs, symbolInfo);

                // current price - new sdd at [0] and price time in drawingInfo
                string time = "";
                Data.SymbolDayData current = Data.GetCurrentSdd(symbolInfo.ShortName, out time);
                if (current != null)
                {
                    sdd.Insert(0, current);
                    di.currentPriceTime = time;
                }

                TabItem newTab = new TabItem();
                newTab.KeyDown += TabItem_OnKeyDown;
                newTab.KeyUp += TabItem_OnKeyUp;

                var sp = new StackPanel();
                sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
                sp.Children.Add(new TextBlock() { Text = symbolInfo.FullName });
                var btn = new Button()
                {
                    Focusable = false,
                    Content = "X",
                    FontFamily = new FontFamily("Courier"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(5, 1, 0, 0),
                    Padding = new Thickness(0, 0, 0, 0),
                    VerticalContentAlignment = VerticalAlignment.Bottom,
                    Width = 16,
                    Height = 16
                };
                btn.Click += CloseSymbolTab;
                sp.Children.Add(btn);

                newTab.Header = sp;
                SymbolsTabControl.Items.Add(newTab);

                chart = new Chart(di);
                DataViewModel.SymbolsDrawings.Add(symbolInfo.FullName, chart);
                newTab.Content = chart.CreateDrawing(sdd);

                if (DataViewModel.SymbolsDrawingsToSerialize != null)
                {
                    chart.AddLoadedChartLines(DataViewModel.SymbolsDrawingsToSerialize, symbolInfo.FullName);
                    // remove added symbol
                    DataViewModel.SymbolsDrawingsToSerialize.Remove(symbolInfo.FullName);
                }

                // select current tab
                // this should be done last
                SymbolsTabControl.SelectedItem = newTab;
            }
            else
            {
                foreach (TabItem item in SymbolsTabControl.Items)
                {
                    if (GetHeaderName(item) == symbolInfo.FullName)
                    {
                        SymbolsTabControl.SelectedItem = item;
                        break;
                    }
                }
            }

            deselectAllLines();
            DataViewModel.SetCurrentDrawing(chart);
            currentSymbolInfo = symbolInfo;

            // set default layer to L1 only
            currentLayer = "";
            buttonLayerHelper("L1");
        }

        private void CloseSymbolTab(object sender, EventArgs a)
        {
            string name = ((((Button)sender).Parent as StackPanel).Children[0] as TextBlock).Text;
            MessageBox.Show("close " + name);
        }

        private void UpdateTextInfo(string shortName, float closePrice, string time)
        {
            TextBlock tb = (TextBlock)FindName("TextBlockInfo");
            if (tb != null)
                tb.Text = shortName + " current price is: " + closePrice + " at " + time;
        }

        private void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            if (a.Source is ListView) return;

            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;

            Chart chart = null;
            string headerName = GetHeaderName(activeTab);
            if (DataViewModel.SymbolsDrawings.TryGetValue(headerName, out chart))
                DataViewModel.SetCurrentDrawing(chart);

            // restore black color on Report tab after the generation of the report.
            if (headerName == "Report" ||
                (a.RemovedItems.Count > 0 &&
                 ((TabItem)a.RemovedItems[0]).Header.ToString() == "Report"))
            {
                if (a.RemovedItems.Count > 0)
                    ((TabItem)a.RemovedItems[0]).Foreground = Brushes.Black;
                else
                    activeTab.Foreground = Brushes.Black;
            }

            if (chart == null)
                return;

            foreach (Data.SymbolInfo si in DataViewModel.SymbolsInfoList)
                if (si.FullName == headerName)
                {
                    UpdateTextInfo(si.ShortName, chart.drawingInfo.sddList[0].Close, chart.drawingInfo.currentPriceTime);
                    break;
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
            if (GetHeaderName(tabItem) == "Report") return;

            tabItem.Focus();
            
            lpmPressed = (e.LeftButton == MouseButtonState.Pressed);

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
                Chart activeChart = DataViewModel.CurrentDrawing;

                if (workMode == WorkMode.Cross)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    ellipse.Stroke = Brushes.Blue;
                    ellipse.Width = 10;
                    ellipse.Height = 10;
                    ellipse.Margin = new Thickness(mousePosition.X - 5, mousePosition.Y - 5, 0, 0);

                    activeChart.canvas.Children.Add(ellipse);
                }
                else
                {
                    workMode = WorkMode.Cross;
                    activeChart.ShowCross(true);
                    activeChart.MoveCross(mousePosition, false);
                }
            }

            Chart chart = DataViewModel.CurrentDrawing;
            if (chart != null)
            {
                foreach (var line in chart.selectedLines)
                    line.StorePrevPos();

                ctrlZMode = CtrlZMode.move;
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
        
        private void AddLines(List<Chart.ChartLine> lines)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;

            foreach (var line in lines)
            {
                line.mode = Chart.ChartLine.Mode.Normal;
                line.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                line.Select(false);

                activeChart.chartLines.Add(line);
                activeChart.canvas.Children.Add(line.linePath);
                activeChart.canvas.Children.Add(line.rectPath);

                line.MoveP1(line.getP1());
                line.MoveP2(line.getP2());
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

            if (currentLayer.Contains("L1"))
                line.layerData = "L1";
            if (currentLayer.Contains("L2"))
                line.layerData = "L2";
            if (currentLayer.Contains("L3"))
                line.layerData = "L3";

            activeChart.chartLines.Add(line);
            activeChart.canvas.Children.Add(line.linePath);
            activeChart.canvas.Children.Add(line.rectPath);

            line.MoveP1(mousePosition);
            line.MoveP2(new Point(mousePosition.X + 1, mousePosition.Y + 1));
            
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
                if (tabItem.Header.ToString() == "Report") return;

                Canvas canvas = (Canvas)tabItem.Content;
                Point mousePosition = e.MouseDevice.GetPosition(canvas);

                activeChart.MoveCross(mousePosition, lpmPressed);
            }
        }

        private void SymbolTab_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart == null) return;

            lpmPressed = (e.LeftButton == MouseButtonState.Pressed);

            Chart.ChartLine line = activeChart.chartLines.FirstOrDefault(l => l.mode == Chart.ChartLine.Mode.Drawing);
            if (line != null)
            {
                line.drawingMode = Chart.ChartLine.DrawingMode.Invalid;
                line.mode = Chart.ChartLine.Mode.Selected;
                workMode = WorkMode.Selecting;

                if (Misc.LineLength(line) < 5)
                    activeChart.DeleteLine(line);

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
            string name = "";
            if (currentSymbolInfo != null)
                name = currentSymbolInfo.FullName;
            DataViewModel.UpdateInfoNames(name);

            SortSymbolsList();

            System.Windows.Controls.ListView sl = (System.Windows.Controls.ListView)FindName("SymbolsList");
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
            System.Windows.Controls.TabControl tabCtrl = (System.Windows.Controls.TabControl)sender;
            // first item is a Report
            if (tabCtrl.Items.Count == 1) return;
            if (tabCtrl.SelectedIndex == 0) return;
            if (showedFromReport)
            {
                showedFromReport = false;
                return;
            }

            TabItem tabItem = (TabItem)tabCtrl.Items[tabCtrl.SelectedIndex];
            Canvas canvas = (Canvas)tabItem.Content;
            Point mousePosition = e.MouseDevice.GetPosition(canvas);

            Chart activeChart = DataViewModel.CurrentDrawing;
            if (activeChart != null)
            {
                // calc distance to nearest object
                // lines

                // TODO: limit min distance to some value

                double minDist = double.MaxValue;
                Chart.ChartLine closestLine = null;
                foreach (Chart.ChartLine line in activeChart.chartLines)
                {
                    if (line.linePath.Visibility == Visibility.Hidden)
                        continue;

                    double dist = Misc.SegmentPointDistance(line.getP1(), line.getP2(), mousePosition);
                    if (dist < minDist)
                    {
                        minDist = dist; closestLine = line;
                    }
                }

                if (closestLine != null)
                {
                    closestLine.Select(!closestLine.IsSelected());
                    workMode = WorkMode.Selecting;

                    foreach (System.Windows.Controls.Button b in colors.Children)
                    {
                        if (b.Background == closestLine.color)
                        {
                            UpdateCurrentColor(b);
                            break;
                        }
                    }
                }
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            // backup first previous data
            string now = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");

            string folderId = Drive.CreateDirectory("temp");
            Drive.RenameFile(folderId, "charts.json", "charts_" + now + ".json");
            
            // try to write data
            string output = GetDVM().SerializeToJson();
            string fileId = Drive.UploadFile(folderId, "charts.json", output);
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {            
            string folderId = Drive.CreateDirectory("temp");
            string fileId = Drive.GetFileId("charts.json");
            string input = Drive.DownloadFile(fileId, "charts.json");
            
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

        void UpdateCurrentColor(System.Windows.Controls.Button button)
        {
            foreach (System.Windows.Controls.Button b in colors.Children)
            {
                b.BorderThickness = new Thickness(0);
            }
            button.BorderThickness = new Thickness(2);

            string colorName = button.Name.ToString().Split('_')[1];
            currentColor = Misc.StringToBrush(colorName);
        }
        
        private void buttonColor_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentColor((System.Windows.Controls.Button)sender);

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
        
        private void selectDeselectLines()
        {
            Chart chart = DataViewModel.CurrentDrawing;

            // if everything is selected - deselect all
            // else - select everything
            bool everythingSelected = true;
            foreach (Chart.ChartLine l in chart.chartLines)
            {
                if (l.layerData.Contains(currentLayer))
                {
                    if (l.IsSelected() == false)
                    {
                        everythingSelected = false;
                        break;
                    }
                }
            }

            chart.selectedLines.Clear();
            foreach (Chart.ChartLine l in chart.chartLines)
            {
                if (l.layerData.Contains(currentLayer))
                {
                    l.Select(!everythingSelected);
                }
            }
        }

        private void deselectAllLines()
        {
            Chart chart = DataViewModel.CurrentDrawing;

            foreach (Chart.ChartLine l in chart.chartLines)
            {
                l.Select(false);
            }
        }

        private void buttonSelectDeselect_Click(object sender, RoutedEventArgs e)
        {
            selectDeselectLines();
        }

        public string peaksOptions = "o c l h";
        public string peaksSpace = "2";
        List<Point> peaksPoints = new List<Point>();

        private void TabItem_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Chart chart = DataViewModel.CurrentDrawing;
            Chart.DrawingInfo di = chart.drawingInfo;

            switch (e.Key)
            {
                // generate peaks
                case Key.P:
                    {
                        // cleanup first;
                        List<UIElement> toDel = new List<UIElement>();
                        foreach (var obj in chart.canvas.Children)
                        {
                            Ellipse ellipse = obj as Ellipse;
                            if (ellipse != null)
                            {
                                if ((string)ellipse.Tag == "peak")
                                    toDel.Add(ellipse);
                            }
                        }
                        foreach (UIElement ui in toDel)
                        {
                            chart.canvas.Children.Remove(ui);
                        }

                        peaksPoints.Clear();

                        // find peaks
                        int space = 2;
                        int.TryParse(peaksSpace, out space);

                        for (int i = space; i < di.sddList.Count - space; i++)
                        {
                            Data.SymbolDayData sdd = di.sddList[i];
                            double x = Misc.DateToPixel(di, sdd.Date, 0);
                            if (x < 0)
                                continue;

                            if (peaksOptions.Contains("h"))
                            {
                                double max = -1;
                                for (int j = -space; j < space; j++)
                                    max = max > di.sddList[i + j].Hi ? max : di.sddList[i + j].Hi;
                                if (sdd.Hi >= max)
                                {
                                    double y = Math.Round(Misc.RemapRangeValToPix(sdd.Hi, di), 0);
                                    chart.AddCircle(x, y, 10, Brushes.Red, "peak");
                                    peaksPoints.Add(new Point(x, y));
                                }
                            }
                            if (peaksOptions.Contains("c"))
                            {
                                double max = -1;
                                for (int j = -space; j < space; j++)
                                    max = max > di.sddList[i + j].Close ? max : di.sddList[i + j].Close;
                                if (sdd.Close >= max)
                                {
                                    double y = Math.Round(Misc.RemapRangeValToPix(sdd.Close, di), 0);
                                    chart.AddCircle(x, y, 10, Brushes.Blue, "peak");
                                    peaksPoints.Add(new Point(x, y));
                                }
                            }
                            if (peaksOptions.Contains("l"))
                            {
                                double min = 10000;
                                for (int j = -space; j < space; j++)
                                    min = min < di.sddList[i + j].Low ? min : di.sddList[i + j].Low;
                                if (sdd.Low <= min)
                                {
                                    double y = Math.Round(Misc.RemapRangeValToPix(sdd.Low, di), 0);
                                    chart.AddCircle(x, y, 10, Brushes.Green, "peak");
                                    peaksPoints.Add(new Point(x, y));
                                }
                            }
                            if (peaksOptions.Contains("o"))
                            {
                                double min = 10000;
                                for (int j = -space; j < space; j++)
                                    min = min < di.sddList[i + j].Open ? min : di.sddList[i + j].Open;
                                if (sdd.Open <= min)
                                {
                                    double y = Math.Round(Misc.RemapRangeValToPix(sdd.Open, di), 0);
                                    chart.AddCircle(x, y, 10, Brushes.Brown, "peak");
                                    peaksPoints.Add(new Point(x, y));
                                }
                            }
                        }
                    }
                    break;

                // generate trends
                case Key.T:
                    {
                        // cleanup first;
                        List<UIElement> toDel = new List<UIElement>();
                        foreach (var obj in chart.canvas.Children)
                        {
                            Line line = obj as Line;
                            if (line != null)
                            {
                                if ((string)line.Tag == "trend")
                                    toDel.Add(line);
                            }
                        }
                        foreach (UIElement ui in toDel)
                        {
                            chart.canvas.Children.Remove(ui);
                        }

                        double dist = 0.5;
                        int minHits = 5;

                        List<Line> lines = new List<Line>();

                        // create lines
                        for (int i = 0; i < peaksPoints.Count; i++)
                        {
                            Point x = peaksPoints[i];

                            for (int j = i; j < peaksPoints.Count; j++)
                            {
                                if (i == j)
                                    continue;

                                Point y = peaksPoints[j];

                                // find if some point is aligned to this line
                                int hits = 0;
                                // line should be as long as possible
                                Point lowp = new Point();
                                Point hip = new Point();
                                for (int p = j; p < peaksPoints.Count; p++)
                                {
                                    if (p == j || p == i)
                                        continue;

                                    Point z = peaksPoints[p];

                                    // eliminate the same points
                                    if (x.X == y.X && x.Y == y.Y)
                                        continue;
                                    if (x.X == z.X && x.Y == z.Y)
                                        continue;
                                    if (z.X == y.X && z.Y == y.Y)
                                        continue;
                                    // eliminate vertical lines
                                    if (x.X == y.X || x.X == z.X)
                                        continue;
                                    // eliminate horizontal lines
                                    if (x.Y == y.Y || x.Y == z.Y)
                                        continue;

                                    if (Misc.LinePointDistance(x, y, z) < dist)
                                    {
                                        hits++;

                                        lowp.X = x.X;
                                        lowp.Y = x.Y;
                                        hip.X = z.X;
                                        hip.Y = z.Y;
                                    }
                                }

                                if (hits >= minHits)
                                {
                                    Line l = new Line();
                                    l.X1 = lowp.X;
                                    l.X2 = hip.X;
                                    l.Y1 = lowp.Y;
                                    l.Y2 = hip.Y;
                                    lines.Add(l);
                                }
                            }
                        }

                        foreach (var l in lines)
                            chart.AddLine(l, Brushes.Black, "trend");
                    }
                    break;

                // hide/show all lines added to chart
                case Key.H:
                    {
                        foreach (Chart.ChartLine l in chart.chartLines.ToList())
                        {
                            if (l.layerData.Contains(currentLayer))
                            {
                                l.linePath.Visibility = l.linePath.Visibility ==
                                    Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;

                                // deselect hidden lines
                                if (l.linePath.Visibility == Visibility.Hidden)
                                {
                                    l.rectPath.Visibility = Visibility.Hidden;
                                    l.Select(false);
                                }
                            }
                        }
                    }
                    break;

                case Key.Z:
                    {
                        if (ctrlPressed)
                        {
                            switch (ctrlZMode)
                            {
                                case CtrlZMode.move:
                                    {
                                        foreach (var line in chart.selectedLines)
                                            line.LoadPrevPos();

                                        ctrlZMode = CtrlZMode.none;
                                    }
                                    break;

                                case CtrlZMode.delete:
                                    {
                                        AddLines(chart.deletedLines);
                                        chart.deletedLines.Clear();

                                        ctrlZMode = CtrlZMode.none;
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    break;

                case Key.Up:
                    {
                        if (ctrlPressed && shiftPressed)
                            createNewLine(true);
                    }
                    break;

                case Key.Down:
                    {
                        if (ctrlPressed && shiftPressed)
                            createNewLine(false);
                    }
                    break;

                case Key.M:
                case Key.D1:
                    {
                        if (ctrlPressed && shiftPressed)
                            createMiddleLines(1);
                    }
                    break;

                case Key.D2:
                    {
                        if (ctrlPressed && shiftPressed)
                            createMiddleLines(2);
                    }
                    break;

                case Key.D3:
                    {
                        if (ctrlPressed && shiftPressed)
                            createMiddleLines(3);
                    }
                    break;
                    
                case Key.D4:
                    {
                        if (ctrlPressed && shiftPressed)
                            createMiddleLines(4);
                    }
                    break;

                // delete selected lines from chart
                case Key.Delete:
                    {
                        ctrlZMode = CtrlZMode.delete;

                        // remember deleted lines
                        if (chart.deletedLines == null)
                            chart.deletedLines = new List<Chart.ChartLine>();

                        chart.deletedLines.Clear();

                        foreach (var line in chart.selectedLines)
                            chart.deletedLines.Add(line);

                        // delete
                        foreach (Chart.ChartLine l in chart.chartLines.ToList())
                        {
                            if (l.IsSelected())
                                chart.DeleteLine(l);
                        }
                        chart.selectedLines.Clear();
                    }
                    break;

                case Key.A:
                    {
                        if (ctrlPressed == true)
                            selectDeselectLines();
                    }
                    break;

                default:
                    break;
            }            
            
            if (e.Key == Key.LeftCtrl || 
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

        private void createMiddleLines(int n)
        {
            Chart chart = DataViewModel.CurrentDrawing;

            // should be two and only lines selected
            if (chart.selectedLines.Count != 2)
                return;

            Chart.ChartLine l1 = chart.selectedLines[0];
            Chart.ChartLine l2 = chart.selectedLines[1];

            if (n == 1)
            {
                Chart.ChartLine newLine = l1.CopyLineTo(chart);
                newLine.Select(true);

                newLine.MoveP1(new Point((l1.getP1().X + l2.getP1().X) / 2, (l1.getP1().Y + l2.getP1().Y) / 2));
                newLine.MoveP2(new Point((l1.getP2().X + l2.getP2().X) / 2, (l1.getP2().Y + l2.getP2().Y) / 2));
            }
            else
            {                
                bool L1P1IsLeft = l1.getP1().X < l1.getP2().X;
                double L1XLeft = L1P1IsLeft ? l1.getP1().X : l1.getP2().X;
                double L1YLeft = L1P1IsLeft ? l1.getP1().Y : l1.getP2().Y;
                double L1XRight = L1P1IsLeft ? l1.getP2().X : l1.getP1().X;
                double L1YRight = L1P1IsLeft ? l1.getP2().Y : l1.getP1().Y;

                bool L2P1IsLeft = l2.getP1().X < l2.getP2().X;
                double L2XLeft = L2P1IsLeft ? l2.getP1().X : l1.getP2().X;
                double L2YLeft = L2P1IsLeft ? l2.getP1().Y : l1.getP2().Y;
                double L2XRight = L2P1IsLeft ? l2.getP2().X : l1.getP1().X;
                double L2YRight = L2P1IsLeft ? l2.getP2().Y : l1.getP1().Y;

                double DivXLeft = (L1XLeft - L2XLeft) / n;
                double DivYLeft = (L1YLeft - L2YLeft) / n;
                double DivXRight = (L1XRight - L2XRight) / n;
                double DivYRight = (L1YRight - L2YRight) / n;

                for (int i = 1; i < n; i++)
                {
                    Chart.ChartLine newLine = l1.CopyLineTo(chart);
                    newLine.Select(true);

                    newLine.MoveP1(new Point(L1XLeft - DivXLeft * i, L1YLeft - DivYLeft * i));
                    newLine.MoveP2(new Point(L1XRight - DivXRight * i, L1YRight - DivYRight * i));
                }
            }
        }

        private void createNewLine(bool modeUp)
        {
            Chart chart = DataViewModel.CurrentDrawing;

            // should be two and only lines selected
            if (chart.selectedLines.Count != 2)
                return;

            Chart.ChartLine l1 = chart.selectedLines[0];
            Chart.ChartLine l2 = chart.selectedLines[1];

            l1.Select(false);
            l2.Select(false);

            double L1P1X = l1.getP1().X;
            double L1P1Y = l1.getP1().Y;
            double L1P2X = l1.getP2().X;
            double L1P2Y = l1.getP2().Y;
            double L1PMX = l1.getMidP().X;
            double L1PMY = l1.getMidP().Y;
            double L2P1X = l2.getP1().X;
            double L2P1Y = l2.getP1().Y;
            double L2P2X = l2.getP2().X;
            double L2P2Y = l2.getP2().Y;
            double L2PMX = l2.getMidP().X;
            double L2PMY = l2.getMidP().Y;

            double M = (L1P2Y - L1P1Y) / (L1P2X - L1P1X);
            double B = L1P1Y - M * L1P1X;

            // use second line mid point
            double YL = M * L2PMX + B;
            double YP = L2PMY;

            double D = YL - YP;

            bool below = D > 0;

            Chart.ChartLine upper = null;
            Chart.ChartLine lower = null;
            if (below)
            {
                lower = l1;
                upper = l2;
            }
            else
            {
                upper = l1;
                lower = l2;
            }
            
            upper.Select(modeUp);
            lower.Select(!modeUp);

            Chart.ChartLine newLine = l1.CopyLineTo(chart);
            newLine.Select(true);

            // calculate from mid points
            double lenx = Math.Abs(L1PMX - L2PMX);
            double leny = Math.Abs(L1PMY - L2PMY);

            if (modeUp && upper.getMidP().X < lower.getMidP().X)
                lenx *= -1;
            if (!modeUp && upper.getMidP().X > lower.getMidP().X)
                lenx *= -1;

            if (modeUp)
            {
                newLine.MoveP1(new Point(upper.getP1().X + lenx, upper.getP1().Y - leny));
                newLine.MoveP2(new Point(upper.getP2().X + lenx, upper.getP2().Y - leny));
            }
            else
            {
                newLine.MoveP1(new Point(lower.getP1().X + lenx, lower.getP1().Y + leny));
                newLine.MoveP2(new Point(lower.getP2().X + lenx, lower.getP2().Y + leny));
            }
        }

        private void TabItem_OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void buttonbuttonWalletAdd_Click(object sender, RoutedEventArgs e)
        {
            var wiDialog = new Wallet(Data.SymbolInfoList, fromList:false);
            wiDialog.ShowDialog();
            if (wiDialog.add)
            {
                var wi = new DataViewModel.WalletItem()
                {
                    Symbol = wiDialog.selectedSymbol.FullName,
                    Type = wiDialog.type,
                    OpenDate = wiDialog.selectedDateTime,
                    OpenPrice = wiDialog.price
                };

                DataViewModel.WalletItems.Add(wi);

                WalletView.ItemsSource = null;
                WalletView.ItemsSource = DataViewModel.WalletItems;
            }
        }

        private void Wallet_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataContext = ((FrameworkElement)e.OriginalSource).DataContext;
            if ((dataContext is DataViewModel.WalletItem) == false) return;
            DataViewModel.WalletItem wi = (DataViewModel.WalletItem)dataContext;

            var wiDialog = new Wallet(Data.SymbolInfoList, fromList: true);
            wiDialog.SymbolsCb.SelectedItem = Data.SymbolInfoList.First(s => s.FullName == wi.Symbol);
            wiDialog.TypeCb.SelectedValue = wi.Type;
            wiDialog.DatePicker.SelectedDate = wi.OpenDate;
            wiDialog.Price.Text = wi.OpenPrice.ToString();
            wiDialog.ShowDialog();

            if (wiDialog.edit)
            {
                int wid = DataViewModel.WalletItems.FindIndex(it => it.Equals((DataViewModel.WalletItem)WalletView.SelectedItem));
                DataViewModel.WalletItems[wid].Symbol = wiDialog.selectedSymbol.FullName;
                DataViewModel.WalletItems[wid].Type = wiDialog.type;
                DataViewModel.WalletItems[wid].OpenDate = wiDialog.selectedDateTime;
                DataViewModel.WalletItems[wid].OpenPrice = wiDialog.price;
            }
            else if (wiDialog.remove)
            {
                int wid = DataViewModel.WalletItems.FindIndex(it => it.Equals((DataViewModel.WalletItem)WalletView.SelectedItem));
                DataViewModel.WalletItems.RemoveAt(wid);
            }

            WalletView.ItemsSource = null;
            WalletView.ItemsSource = DataViewModel.WalletItems;
        }
        
        private void buttonPeaks_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Form form1 = new System.Windows.Forms.Form();
            form1.Text = "peaks options";

            System.Windows.Forms.TextBox tb = new System.Windows.Forms.TextBox();
            tb.Location = new System.Drawing.Point(10, 10);
            tb.Text = peaksOptions;
            form1.Controls.Add(tb);

            int h = tb.Height + 5;

            System.Windows.Forms.TextBox tb2 = new System.Windows.Forms.TextBox();
            tb2.Location = new System.Drawing.Point(10, 10 + h * 1);
            tb2.Text = peaksSpace;
            form1.Controls.Add(tb2);

            System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
            button1.Text = "OK";
            button1.Location = new System.Drawing.Point(10, 10 + h * 2);
            button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            form1.Controls.Add(button1);

            System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();            
            button2.Text = "Cancel";
            button2.Location = new System.Drawing.Point(10, 10 + h * 3);
            button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            form1.Controls.Add(button2);

            form1.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            form1.AcceptButton = button1;
            form1.CancelButton = button2;
            form1.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;           
            
            form1.ShowDialog();
            
            if (form1.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                peaksOptions = tb.Text;
                peaksSpace = tb2.Text;

                MessageBox.Show("Peaks set to: " + peaksOptions + " with space: " + peaksSpace);
                form1.Dispose();
            }
            else
            {
                MessageBox.Show("The Cancel button on the form was clicked.");
                form1.Dispose();
            }
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {            
            Chart chart = DataViewModel.CurrentDrawing;
            Chart.DrawingInfo di = chart.drawingInfo;
            
            // current price - new sdd at [0] and price time in drawingInfo
            string time = "";
            Data.SymbolDayData current = Data.GetCurrentSdd(di.si.ShortName, out time);
            if (current != null)
            {
                var sdd = DataViewModel.GetSymbolData(GetDVM().SDDs, di.si);
                di.currentPriceTime = time;
                sdd[0] = current;

                chart.UpdateLastSDD();

                chart.priceLabel.SetValue(di.sddList[0].Close);
                double y = Misc.RemapRangeValToPix(di.sddList[0].Close, di);
                chart.priceLabel.SetPosition(new Point(di.viewWidth - di.viewMarginRight + 2, y));

                UpdateTextInfo(di.si.ShortName, di.sddList[0].Close, di.currentPriceTime);
            }
        }

        void buttonLayerHelper(string selectedLayer)
        {
            // deselect all for now
            buttonLayer1.BorderThickness = new Thickness(1, 1, 1, 1);
            buttonLayer2.BorderThickness = new Thickness(1, 1, 1, 1);
            buttonLayer3.BorderThickness = new Thickness(1, 1, 1, 1);

            // deselect all layers button
            buttonLayerAll.BorderThickness = new Thickness(1, 1, 1, 1);

            if (currentLayer.Contains(selectedLayer) == false)
                currentLayer += " " + selectedLayer;
            else
                currentLayer = currentLayer.Replace(selectedLayer, " ");

            currentLayer = currentLayer.Replace("  ", " ");

            // select all that should be selected
            if (currentLayer.Contains("L1"))
                buttonLayer1.BorderThickness = new Thickness(2, 2, 2, 2);
            if (currentLayer.Contains("L2"))
                buttonLayer2.BorderThickness = new Thickness(2, 2, 2, 2);
            if (currentLayer.Contains("L3"))
                buttonLayer3.BorderThickness = new Thickness(2, 2, 2, 2);            

            // deselect all lines
            foreach (var line in DataViewModel.CurrentDrawing.chartLines)
                line.Select(false);

            // make all lines from selected layers visible, hide others
            foreach (var line in DataViewModel.CurrentDrawing.chartLines)
            {
                if (currentLayer.Contains(line.layerData) == false)
                    line.linePath.Visibility = Visibility.Hidden;
                else
                    line.linePath.Visibility = Visibility.Visible;
            }

        }

        private void buttonLayer1_Click(object sender, RoutedEventArgs e)
        {
            buttonLayerHelper("L1");
        }

        private void buttonLayer2_Click(object sender, RoutedEventArgs e)
        {
            buttonLayerHelper("L2");
        }
        
        private void buttonLayer3_Click(object sender, RoutedEventArgs e)
        {
            buttonLayerHelper("L3");
        }

        private void buttonLayerAll_Click(object sender, RoutedEventArgs e)
        {
            currentLayer = "L1 L2 L3";

            buttonLayerAll.BorderThickness = new Thickness(2, 2, 2, 2);
            buttonLayer1.BorderThickness = new Thickness(1, 1, 1, 1);
            buttonLayer2.BorderThickness = new Thickness(1, 1, 1, 1);
            buttonLayer3.BorderThickness = new Thickness(1, 1, 1, 1);

            foreach (var line in DataViewModel.CurrentDrawing.chartLines)
                line.linePath.Visibility = Visibility.Visible;
        }
    }
}
