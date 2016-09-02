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

            newTab.Content = Drawings.CreateDrawing(di, sdd);
        }        
    }
}
