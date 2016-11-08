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

        public static List<SymbolDayData> GetSymbolData(string symbolName)
        {
            string csv = "";
            string today = DateTime.Today.ToString("dd -MM-yyyy");
            string filename = "stocker_" + today + "_" + symbolName + ".csv";
            try
            {
                using (StreamReader reader = new StreamReader(GetPath() + @"temp\" + filename))
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

                Directory.CreateDirectory(GetPath() + @"temp\");
                using (StreamWriter outputFile = new StreamWriter(GetPath() + @"temp\" + filename))
                {
                    outputFile.Write(csv);
                }
            }

            if (csv == "Przekroczony dzienny limit wywolan")
            {
                MessageBox.Show(csv, "ERROR");
            }

            List<SymbolDayData> result = new List<SymbolDayData>();

            bool header = true;
            foreach(string line in csv.Split('\n'))
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

                SymbolDayData sdd = new SymbolDayData(date, open, hi, low, close, volume);

                result.Add(sdd);
            }

            result.Reverse();
            return result;
        }

    }

    public class DataViewModel
    {
        public List<Data.SymbolInfo> SymbolsInfoList { get; set; }

        public Dictionary<string, Chart> SymbolsDrawings { get; set; }
        public Chart CurrentDrawing { get; set; }

        public Dictionary<string, Chart.DataToSerialize> SymbolsDrawingsToSerialize { get; set; }
 
        public string SerializeToJson()
        {
            foreach (KeyValuePair<string, Chart> pairSymbolsDrawings in SymbolsDrawings)
            {
                Chart.DataToSerialize data = pairSymbolsDrawings.Value.SerializeToJson();
                string key = pairSymbolsDrawings.Key;
                if (SymbolsDrawingsToSerialize.ContainsKey(key) == false)
                    SymbolsDrawingsToSerialize.Add(key, data);
            }

            string output = JsonConvert.SerializeObject(SymbolsDrawingsToSerialize, Formatting.Indented);
            return output;
        }

        public void DeserializeFromJson(string input)
        {
            SymbolsDrawingsToSerialize = JsonConvert.DeserializeObject<Dictionary<string, Chart.DataToSerialize>>(input);

            if (SymbolsDrawingsToSerialize == null)
                SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
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
        
        public DataViewModel()
        {
            SymbolsDrawings = new Dictionary<string, Chart>();
            SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
            
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

            Data.numberFormat.NumberGroupSeparator = ""; // thousands
            Data.numberFormat.NumberDecimalSeparator = ".";

            Data.dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern;

            // debug
            //SymbolsInfoList = new List<Data.SymbolInfo>();
            //SymbolsInfoList.Add(new Data.SymbolInfo("DOMDEV", "DOM"));
            //SymbolsInfoList.Add(new Data.SymbolInfo("11BIT", "11B"));
            //SymbolsInfoList.Add(new Data.SymbolInfo("KGHM", "KGH"));
        }

        public void SetCurrentDrawing(Chart currentChart)
        {
            CurrentDrawing = currentChart;
        }
    }

    public partial class Chart
    {
        public struct DataToSerialize
        {
            public IList<ChartLine.DataToSerialize> chartLines { get; set; }
        }

        public DataToSerialize SerializeToJson()
        {
            DataToSerialize toSerialize = new DataToSerialize()
            {
                chartLines = new List<ChartLine.DataToSerialize>()
            };

            foreach (ChartLine line in chartLines)
            {
                toSerialize.chartLines.Add(line.SerializeToJson(drawingInfo));
            }

            return toSerialize;
        }

        public partial class ChartLine
        {
            public struct DataToSerialize
            {
                // public string StartPoint { get; set; }
                public string StartPointDV { get; set; }
                // public string EndPoint { get; set; }
                public string EndPointDV { get; set; }
                public string Color { get; set; }
            }

            public DataToSerialize SerializeToJson(Chart.DrawingInfo drawingInfo)
            {
                // dates 
                var P1DT = Misc.PixelToSdd(drawingInfo, getP1());
                var P2DT = Misc.PixelToSdd(drawingInfo, getP2());

                // values
                double P1ValY = Math.Round(Misc.RemapRange(getP1().Y,
                    drawingInfo.viewMarginBottom, drawingInfo.maxVal,
                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal), 6);
                double P2ValY = Math.Round(Misc.RemapRange(getP2().Y,
                    drawingInfo.viewMarginBottom, drawingInfo.maxVal,
                    drawingInfo.viewHeight - drawingInfo.viewMarginBottom, drawingInfo.minVal), 6);

                DataToSerialize toSerialize = new DataToSerialize();

                /*
                toSerialize.StartPoint = getP1().X.ToString(Data.numberFormat) + ";" +
                    getP1().Y.ToString(Data.numberFormat);
                toSerialize.EndPoint = getP2().X.ToString(Data.numberFormat) + ";" +
                    getP2().Y.ToString(Data.numberFormat);
                */

                // date + value
                toSerialize.StartPointDV = P1DT.Item1.ToString(Data.dateTimeFormat) + "+" +
                    P1DT.Item2.ToString(Data.numberFormat) + ";" +
                    P1ValY.ToString(Data.numberFormat);
                toSerialize.EndPointDV = P2DT.Item1.ToString(Data.dateTimeFormat) + "+" +
                    P2DT.Item2.ToString(Data.numberFormat) + ";" +
                    P2ValY.ToString(Data.numberFormat);

                toSerialize.Color = Misc.BrushToString(color);

                return toSerialize;
            }
        }
    }
}
