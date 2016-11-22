using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;

using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Windows;

namespace WpfApplication3
{
    public class Data
    {
        private static string currentPath;

        public static string GetPath()
        {
            while (currentPath == "" || currentPath == null)
                ChooseDefaultPath();

            string path = currentPath + @"\stocker\";
            return path;
        }

        public static void ChooseDefaultPath()
        {
            // Configure the message box to be displayed
            string messageBoxText = "Use samba path?";
            string caption = "Choose default path";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // User pressed Yes button
                    currentPath = @"\\samba-users.igk.intel.com\samba\Users\rrudnick\invest";
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
            }
        }

        public class SymbolDayData
        {
            public DateTime Date { get; }
            public float Open { get; }
            public float Hi { get; }
            public float Low { get; }
            public float Close { get; }
            public uint Volume { get; }

            public SymbolDayData(DateTime date, float open, float hi, float low, float close, uint volume)
            {
                Date = date;
                Open = open;
                Hi = hi;
                Low = low;
                Close = close;
                Volume = volume;
            }
        }

        public class SymbolInfo
        {
            public string FullName { get; set; }
            public string ShortName { get; set; }

            public SymbolInfo(string fullName, string shortName)
            {
                FullName = fullName;
                ShortName = shortName;

                SymbolInfoList.Add(this);
            }
        }

        #region Members

        public static List<SymbolInfo> SymbolInfoList = new List<SymbolInfo>();
        public static NumberFormatInfo numberFormat = new NumberFormatInfo();
        public static string dateTimeFormat;

        #endregion

        public static List<SymbolInfo> GetSymbolsFromWeb()
        {
            List<SymbolInfo> symbols = new List<SymbolInfo>();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();
            int page = 1;
            int added = 0;

            while (true)
            {
                string data = "http://stooq.pl/t/?i=513&v=1&l=" + page.ToString();
                doc = web.Load(data);

                // XPath of symbol name
                // *[@id="f10"]
                HtmlNodeCollection symbolNodes = doc.DocumentNode.SelectNodes("//*/td[@id=\"f10\"]");
                foreach (HtmlNode node in symbolNodes.Skip(2))
                {
                    string fullName = node.InnerText;
                    string shortName = node.ParentNode.FirstChild.FirstChild.InnerText;
                    SymbolInfo si = new SymbolInfo(fullName, shortName);
                    symbols.Add(si);
                }

                if (symbols.Count <= added)
                    throw new Exception("assert");
                added = symbols.Count;

                // check if this is a last page
                string numOfItemsStr = doc.DocumentNode.SelectNodes("//*[@id=\"f13\"]/text()[1]")[0].InnerText;
                Regex reNumOfItems = new Regex(@".*?(\d+) z (\d+).*");
                Match m = reNumOfItems.Match(numOfItemsStr);
                if (m.Groups[1].ToString() == m.Groups[2].ToString())
                {
                    if (symbols.Count.ToString() != m.Groups[2].ToString())
                        throw new Exception("assert");
                    break;
                }

                page += 1;
            }

            return symbols;
        }
    }

    public partial class DataViewModel
    {
        public List<Data.SymbolInfo> SymbolsInfoList { get; set; }

        public Dictionary<string, Chart> SymbolsDrawings { get; set; }
        public Chart CurrentDrawing { get; set; }

        public Dictionary<string, Chart.DataToSerialize> SymbolsDrawingsToSerialize { get; set; }
        public Dictionary<string, List<Data.SymbolDayData>> SDDs { get; set; }
         
        public DataViewModel()
        {
            Data.numberFormat.NumberGroupSeparator = ""; // thousands
            Data.numberFormat.NumberDecimalSeparator = ".";
            Data.dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern;

            SymbolsDrawings = new Dictionary<string, Chart>();
            SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
            SDDs = new Dictionary<string, List<Data.SymbolDayData>>();

            // create default dir
            Directory.CreateDirectory(Data.GetPath());

            LoadSymbolsInfoList();

            // try to load symbols drawings
            try
            {
                // Open the text file using a stream reader.
                using (StreamReader reader = new StreamReader(Data.GetPath() + @"charts.json"))
                {
                    // Read the stream to a string, and write the string to the console.
                    string input = reader.ReadToEnd();
                    SymbolsDrawingsToSerialize =
                        JsonConvert.DeserializeObject<Dictionary<string, Chart.DataToSerialize>>(input);

                    // in case of empty file the result of deserialization will be null,
                    // so create new object
                    if (SymbolsDrawingsToSerialize == null)
                        SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
                }
            }
            catch (FileNotFoundException)
            {
                // no problem
            }

            foreach (var symbolInfo in SymbolsInfoList)
            {
                if (SymbolsDrawingsToSerialize.ContainsKey(symbolInfo.FullName) == false)
                    continue;
                
                var sdd = GetSymbolData(symbolInfo.ShortName);
                foreach(var line in SymbolsDrawingsToSerialize[symbolInfo.FullName].chartLines)
                {
                    if (line.Color != "Lime" && line.Color != "Red")
                        continue;

                    DateTime lineStartDate = DateTime.ParseExact(line.StartPointDV.Split('+')[0], Data.dateTimeFormat, CultureInfo.InvariantCulture);
                    DateTime lineEndDate = DateTime.ParseExact(line.EndPointDV.Split('+')[0], Data.dateTimeFormat, CultureInfo.InvariantCulture);
                    if (lineEndDate < sdd[0].Date)
                        continue;

                    // find sdd at start of line
                    var sddIt = 0;
                    while (sdd[sddIt].Date > lineStartDate)
                        sddIt++;

                    double extraDaysToEnd = double.Parse(line.EndPointDV.Split('+')[1].Split(';')[0]);
                    double numDays = sddIt + extraDaysToEnd;

                    double startVal = double.Parse(line.StartPointDV.Split(';')[1]);
                    double endVal = double.Parse(line.EndPointDV.Split(';')[1]);
                    double step = (endVal - startVal) / numDays;

                    double lineValAtSdd0 = startVal + step * sddIt;

                    if (sdd[0].Low < lineValAtSdd0 && lineValAtSdd0 < sdd[0].Hi)
                    {
                        // cross!!

                    }
                }

            }
        }

        private void LoadSymbolsInfoList()
        {
            // try to load from disk
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_symbols_" + today + ".html";
            Directory.CreateDirectory(Data.GetPath() + @"temp\");
            try
            {
                using (StreamReader reader = new StreamReader(Data.GetPath() + @"temp\" + filename))
                {
                    // Read the stream to a string, and write the string to the console.
                    string loaded = reader.ReadToEnd();
                    SymbolsInfoList = JsonConvert.DeserializeObject<List<Data.SymbolInfo>>(loaded);
                }
            }
            catch (FileNotFoundException)
            {
                if (SymbolsInfoList == null)
                {
                    // load from web
                    SymbolsInfoList = new List<Data.SymbolInfo>(Data.GetSymbolsFromWeb());

                    // save to disk
                    string output = JsonConvert.SerializeObject(SymbolsInfoList, Formatting.Indented);
                    using (StreamWriter outputFile = new StreamWriter(Data.GetPath() + @"temp\" + filename))
                    {
                        outputFile.Write(output);
                    }
                }
            }
        }

        public void SetCurrentDrawing(Chart currentChart)
        {
            CurrentDrawing = currentChart;
        }

        public List<Data.SymbolDayData> GetSymbolData(string symbolName)
        {
            if (SDDs.ContainsKey(symbolName))
                return SDDs[symbolName];

            string csv = "";
            string today = DateTime.Today.ToString("dd -MM-yyyy");
            string filename = "stocker_" + today + "_" + symbolName + ".csv";
            try
            {
                using (StreamReader reader = new StreamReader(Data.GetPath() + @"temp\" + filename))
                {
                    // Read the stream to a string, and write the string to the console.
                    csv = reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
            }

            if (csv == "")
            {
                string url = "http://stooq.pl/q/d/l/?s=" + symbolName + "&i=d";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                csv = sr.ReadToEnd();

                Directory.CreateDirectory(Data.GetPath() + @"temp\");
                using (StreamWriter outputFile = new StreamWriter(Data.GetPath() + @"temp\" + filename))
                {
                    outputFile.Write(csv);
                }
            }

            if (csv == "Przekroczony dzienny limit wywolan")
            {
                MessageBox.Show(csv, "ERROR");
            }

            List<Data.SymbolDayData> result = new List<Data.SymbolDayData>();

            bool header = true;
            foreach (string line in csv.Split('\n'))
            {
                if (header)
                {
                    header = false;
                    continue;
                }

                if (line.Length == 0) continue;

                string l = line.Substring(0, line.Length - 1);
                string[] data = l.Split(',');

                DateTime date = DateTime.ParseExact(data[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                float open = float.Parse(data[1], CultureInfo.InvariantCulture);
                float hi = float.Parse(data[2], CultureInfo.InvariantCulture);
                float low = float.Parse(data[3], CultureInfo.InvariantCulture);
                float close = float.Parse(data[4], CultureInfo.InvariantCulture);
                uint volume = 0;
                if (data.Length == 6)
                    volume = uint.Parse(data[5]);

                Data.SymbolDayData sdd = new Data.SymbolDayData(date, open, hi, low, close, volume);

                result.Add(sdd);
            }

            result.Reverse();
            SDDs.Add(symbolName, result);

            return result;
        }
    }
}
