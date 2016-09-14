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
        }
        
        private void DrawLine(Point mousePosition)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null)
                return;

            // if there is no selected lines, create a new line and select it
            if (activeChart.selectedLines.Count == 0)
            {
                Chart.ChartLine line = new Chart.ChartLine(activeChart);
                activeChart.chartLines.Add(line);
                activeChart.canvas.Children.Add(line.linePath);
                activeChart.canvas.Children.Add(line.rectPath);

                line.mode = Chart.ChartLine.Mode.Drawing;
                activeChart.selectedLines.Add(line);

                line.MoveP1(mousePosition);
                line.MoveP2(mousePosition);
            }
            else
            {
                Chart.ChartLine line = activeChart.selectedLines[0];
                line.MoveP2(mousePosition);
            }
        }

        void SymbolTab_MouseMove(object sender, MouseEventArgs e)
        {
            Chart activeChart = GetDVM().CurrentDrawing;
            if (activeChart == null) return;

            if (workMode != WorkMode.Drawing)
                return;
            
            if (activeChart.selectedLines.Count > 0)
            {
                Chart.ChartLine line = activeChart.selectedLines[0];
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

            if (activeChart.selectedLines.Count > 0)
            {
                Chart.ChartLine line = activeChart.selectedLines[0];
                if (line.mode == Chart.ChartLine.Mode.Drawing)
                {
                    line.Select(false);
                    workMode = WorkMode.Selecting;
                }
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
