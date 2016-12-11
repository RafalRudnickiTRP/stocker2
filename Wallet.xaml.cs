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
using System.Windows.Shapes;

namespace WpfApplication3
{
    /// <summary>
    /// Interaction logic for WalletItem.xaml
    /// </summary>
    public partial class Wallet : Window
    {
        public List<Data.SymbolInfo> SymbolsList { get; set; }
        public List<string> TypesList { get; set; }

        private bool fromList;
        public bool add = false;
        public bool edit = false;
        public bool remove = false;

        public Data.SymbolInfo selectedSymbol;
        public string type;
        public DateTime selectedDateTime;
        public double price;

        public Wallet(List<Data.SymbolInfo> symbolsList, bool fromList)
        {
            this.fromList = fromList;

            SymbolsList = symbolsList;
            TypesList = new List<string> {"Buy", "Sell"};

            DataContext = this;
            InitializeComponent();

            if (fromList == false)
                RemoveBtn.IsEnabled = false;
        }

        private void ButtonAddEdit_Click(object sender, RoutedEventArgs e)
        {
            if (fromList)
                edit = true;
            else
                add = true;

            selectedSymbol = SymbolsCb.SelectedItem as Data.SymbolInfo;
            selectedDateTime = DatePicker.SelectedDate ?? DateTime.Today;
            type = TypeCb.SelectedItem as string;
            price = double.Parse(Price.Text.ToString(), Data.numberFormat);

            Close();
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            remove = true;

            selectedSymbol = SymbolsCb.SelectedItem as Data.SymbolInfo;
            selectedDateTime = DatePicker.SelectedDate ?? DateTime.Today;
            type = TypeCb.SelectedItem as string;
            price = double.Parse(Price.Text.ToString(), Data.numberFormat);

            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
