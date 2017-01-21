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
using System.Globalization;
using HtmlAgilityPack;

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
                Chart.DrawingInfo di = new Chart.DrawingInfo((int)SymbolsTabControl.ActualWidth, (int)SymbolsTabControl.ActualHeight);

                List<Data.SymbolDayData> sdd = dvm.GetSymbolData(symbolInfo.ShortName);
                
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
                sp.Orientation = Orientation.Horizontal;
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
        }

        private void CloseSymbolTab(object sender, EventArgs a)
        {
            string name = ((((Button)sender).Parent as StackPanel).Children[0] as TextBlock).Text;
            MessageBox.Show("close " + name);
        }

        private void SymbolTab_SelectionChanged(object sender, SelectionChangedEventArgs a)
        {
            if (a.Source is ListView) return;

            TabItem activeTab = (TabItem)((TabControl)a.Source).SelectedItem;

            Chart chart = null;
            string headerName = GetHeaderName(activeTab);
            if (DataViewModel.SymbolsDrawings.TryGetValue(headerName, out chart))
                DataViewModel.SetCurrentDrawing(chart);

            foreach (Data.SymbolInfo si in DataViewModel.SymbolsInfoList)
                if (si.FullName == headerName)
                {
                    TextBlock tb = (TextBlock)FindName("TextBlockInfo");
                    if (tb != null)
                        tb.Text =
                            si.ShortName + " current price is: " + chart.drawingInfo.sddList[0].Close
                            + " at " + chart.drawingInfo.currentPriceTime;
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
                workMode = WorkMode.Cross;
                Chart activeChart = DataViewModel.CurrentDrawing;
                activeChart.ShowCross(true);
                activeChart.MoveCross(mousePosition, false);
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

                    foreach (Button b in colors.Children)
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

        void UpdateCurrentColor(Button button)
        {
            foreach (Button b in colors.Children)
            {
                b.BorderThickness = new Thickness(0);
            }
            button.BorderThickness = new Thickness(2);

            string colorName = button.Name.ToString().Split('_')[1];
            currentColor = Misc.StringToBrush(colorName);
        }
        
        private void buttonColor_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentColor((Button)sender);

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
        
        private void TabItem_OnKeyDown(object sender, KeyEventArgs e)
        {
            Chart chart = DataViewModel.CurrentDrawing;

            if (e.Key == Key.Delete)
            {
                // delete lines from chart
                foreach (Chart.ChartLine l in chart.chartLines.ToList())
                {
                    if (l.IsSelected())
                    {
                        chart.DeleteLine(l);
                    }
                }
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
    }
}
