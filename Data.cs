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
            public string InfoName { get; set; }
            public string ShortName { get; set; }
            public bool Visibility;

            public string Group;

            private bool _IsRed()
            {
                string[] arr = InfoName.Split('/');
                if (arr.Length > 1)
                {
                    int act = 0;
                    if (int.TryParse(arr[1], out act))
                    {
                        if (act > 0)
                            return false;
                    }
                    return true;
                }
                return false;
            }
            public bool IsRed
            {
                get { return _IsRed(); }
            }

            private bool _IsBold()
            {
                return (InfoName != FullName);
            }
            public bool IsBold
            {
                get { return _IsBold(); }
            }

            public SymbolInfo(string fullName, string shortName)
            {
                InfoName = FullName = fullName;
                ShortName = shortName;

                SymbolInfoList.Add(this);
            }

            public int CompareTo(SymbolInfo si)
            {
                int result = 0;
                if (si._IsBold() && _IsBold())
                    result = string.Compare(si.InfoName, InfoName);
                else if (si._IsBold() && !_IsBold())
                    result = -1;
                else if (!si._IsBold() && _IsBold())
                    result = 1;
                else
                    result = string.Compare(si.InfoName, InfoName);

                // first list of bolds
                return result * -1;
            }
        }

        #region Members

        public static List<SymbolInfo> SymbolInfoList = new List<SymbolInfo>();
        public static NumberFormatInfo numberFormat = new NumberFormatInfo();
        public static string dateTimeFormat;

        #endregion

        public static SymbolDayData GetCurrentSdd(string shortName, out string time)
        {
            time = "";
            SymbolDayData current = null;

            SymbolInfo currentSi = null;
            foreach (SymbolInfo si in DataViewModel.SymbolsInfoList)
                if (si.ShortName == shortName)
                {
                    currentSi = si;
                    break;
                }

            current = Sources.GetSdd(currentSi, out time);
            return current;
        }

    }

    public partial class DataViewModel
    {
        public static List<Data.SymbolInfo> SymbolsInfoList { get; set; }
        public static List<Data.SymbolInfo> VisibleSymbolsInfoList { get; set; }

        public static Dictionary<string, Chart> SymbolsDrawings { get; set; }
        public static Chart CurrentDrawing { get; set; }

        public static Dictionary<string, Chart.DataToSerialize> SymbolsDrawingsToSerialize { get; set; }
        public Dictionary<string, List<Data.SymbolDayData>> SDDs { get; set; }

        public struct ReportItem
        {
            public DateTime Date { get; set; }
            public string Symbol { get; set; }
            public string Event { get; set; }
        }
        public static List<ReportItem> ReportItems { get; set; }

        public static string Groups;

        public class WalletItem
        {
            public string Symbol { get; set; }
            public string Type { get; set; }
            public DateTime OpenDate { get; set; }
            public double OpenPrice { get; set; }
            public double CurrentPrice { get; set; }
            public double Gain { get; set; }
            public double GainPrc { get; set; }
        }
        public static List<WalletItem> WalletItems { get; set; }

        static bool IsClActive(string point)
        {
            bool isClActive = false;

            double extraDaysToEnd = double.Parse(point.Split('+')[1].Split(';')[0],
                Data.numberFormat);
            DateTime lineEndDate = DateTime.ParseExact(point.Split('+')[0],
                Data.dateTimeFormat, CultureInfo.InvariantCulture);
            DateTime current = DateTime.Today;
            int days = (current - lineEndDate).Days;
            if (days - (int)extraDaysToEnd < 0)
                isClActive = true;

            return isClActive;
        }

        static void UpdateOnList(int cls, int clsActive, string sdKey)
        {
            string info = " " + cls + "/" + clsActive;
            if (cls == 0 && clsActive == 0)
                info = "";

            foreach (var symbolInfo in SymbolsInfoList)
            {
                if (symbolInfo.FullName == sdKey)
                {
                    symbolInfo.InfoName = symbolInfo.FullName + info;
                    break;
                }
            }
        }

        public static void UpdateInfoNames(string fullName)
        {
            foreach (var sd in SymbolsDrawings)
            {
                if (sd.Key != fullName)
                    continue;

                int cls = sd.Value.chartLines.Count;
                if (cls == 0)
                {
                    foreach (var symbolInfo in SymbolsInfoList)
                    {
                        if (symbolInfo.FullName == sd.Key)
                        {
                            symbolInfo.InfoName = symbolInfo.FullName;
                            break;
                        }
                    }
                    continue;
                }

                int clsActive = 0;
                foreach (var cl in sd.Value.chartLines)
                {
                    if (Misc.BrushToString(cl.color) == "Red" || Misc.BrushToString(cl.color) == "Lime")
                    {
                        Chart.ChartLine.DataToSerialize temp = cl.SerializeToJson(cl.GetDrawingInfo());
                        if (IsClActive(temp.StartPointDV) || IsClActive(temp.EndPointDV))
                            clsActive++;
                    }
                }

                UpdateOnList(cls, clsActive, sd.Key);
            }
        }

        public static void UpdateInfoNamesOnLoad()
        {
            foreach (var sd in SymbolsDrawingsToSerialize)
            {
                int cls = sd.Value.chartLines.Count;
                if (cls == 0)
                    continue;

                int clsActive = 0;
                foreach (var cl in sd.Value.chartLines)
                {
                    if (cl.Color == "Red" || cl.Color == "Lime")
                    {
                        if (IsClActive(cl.StartPointDV) || IsClActive(cl.EndPointDV))
                            clsActive++;
                    }
                }

                UpdateOnList(cls, clsActive, sd.Key);
            }
        }

        public DataViewModel()
        {
            Drive.CreateService();

            Data.numberFormat.NumberGroupSeparator = ""; // thousands
            Data.numberFormat.NumberDecimalSeparator = ".";
            Data.dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern;

            SymbolsDrawings = new Dictionary<string, Chart>();
            SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
            SDDs = new Dictionary<string, List<Data.SymbolDayData>>();

            ReportItems = new List<ReportItem>();
            WalletItems = new List<WalletItem>();

            LoadSymbolsInfoList();
            FilterSymbolInfoList("", "wszystko");

            // try to load symbols drawings
            // create default dir
            string folderId = Drive.CreateDirectory("temp");
            string fileId = Drive.GetFileId("charts.json");
            if (fileId != "")
            {
                string input = Drive.DownloadFile(fileId, "charts.json");

                SymbolsDrawingsToSerialize =
                    JsonConvert.DeserializeObject<Dictionary<string, Chart.DataToSerialize>>(input);

                // in case of empty file the result of deserialization will be null,
                // so create new object
                if (SymbolsDrawingsToSerialize == null)
                    SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();

                UpdateInfoNamesOnLoad();
            }
        }

        public struct retData
        {
            public string reportString;
        }

        // NOTE: this function is adapted to be run in a thread
        static retData CreateOneReport(Dictionary<string, List<Data.SymbolDayData>> sdds, Data.SymbolInfo symbolInfo)
        {
            string report = "";

            if (SymbolsDrawingsToSerialize.ContainsKey(symbolInfo.FullName) == false)
                return new retData();

            var sdd = GetSymbolData(sdds, symbolInfo);

            foreach (var line in SymbolsDrawingsToSerialize[symbolInfo.FullName].chartLines)
            {
                if (line.Color != "Lime" && line.Color != "Red")
                    continue;

                DateTime lineStartDate = DateTime.ParseExact(line.StartPointDV.Split('+')[0],
                    Data.dateTimeFormat, CultureInfo.InvariantCulture);
                DateTime lineEndDate = DateTime.ParseExact(line.EndPointDV.Split('+')[0],
                    Data.dateTimeFormat, CultureInfo.InvariantCulture);
                double extraDaysFromStart = double.Parse(line.StartPointDV.Split('+')[1].Split(';')[0],
                    Data.numberFormat);
                double extraDaysToEnd = double.Parse(line.EndPointDV.Split('+')[1].Split(';')[0],
                    Data.numberFormat);

                if (lineEndDate.AddDays(extraDaysToEnd) < sdd[0].Date)
                    continue;

                // find sdd at start of line
                var sddIt = 0;
                while (sdd[sddIt].Date > lineStartDate)
                    sddIt++;

                double numDays = sddIt - extraDaysFromStart + extraDaysToEnd;

                double startVal = double.Parse(line.StartPointDV.Split(';')[1],
                    Data.numberFormat);
                double endVal = double.Parse(line.EndPointDV.Split(';')[1],
                    Data.numberFormat);
                double step = (endVal - startVal) / numDays;

                double lineValAtSdd0 = startVal + step * sddIt;

                if (sdd[0].Low < lineValAtSdd0 && lineValAtSdd0 < sdd[0].Hi)
                {
                    string e = "";

                    report += "NAME: " + symbolInfo.FullName + " with ";
                    if (step > 0)
                        e += "ASCENDING line ";
                    else
                        e += "DESCENDING line ";
                    e += "crossed at value " + lineValAtSdd0.ToString("F2", Data.numberFormat);

                    ReportItem ri = new ReportItem();
                    ri.Symbol = symbolInfo.FullName;
                    ri.Date = sdd[0].Date;
                    ri.Event = e;
                    ReportItems.Add(ri);

                    report += e + "\n";
                }
            }

            var rd = new retData();
            rd.reportString = report;
            return rd;
        }

        public void GenerateReport()
        {
            string report = DateTime.Today.ToString("dd-MM-yyyy") + "\n" + "\n";

            var tasks = new Task<retData>[SymbolsInfoList.Count];
            int i = 0;
            foreach (var symbolInfo in SymbolsInfoList)
            {
                // var handle = Task.Factory.StartNew(() => CreateOneReport(SDDs, symbolInfo));
                //tasks[i] = handle;
                var result = CreateOneReport(SDDs, symbolInfo);
                report += result.reportString;
                i++;
            }

            /*
            Task.WaitAll(tasks);
            var results = Task.WhenAll(tasks);
            foreach (var result in results.Result)
            {
                report += result.reportString;
            }   
            */

            Drive.SaveReportFile(report);
        }

        public void SaveGroups()
        {
            Drive.SaveFile(Groups, "groups.txt");

            Console.WriteLine("Groups: " + Groups);
        }

        public void LoadGroups()
        {
            string groupsFileId = Drive.GetFileId("groups.txt");
            if (groupsFileId != "")
            {
                string input = Drive.DownloadFile(groupsFileId, "groups.txt");
                Groups = input;
            }

            Console.WriteLine("Groups: " + Groups);
        }

        private void LoadSymbolsInfoList()
        {
            // try to load from disk
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_symbols_" + today + ".html";

            if (MainWindow.testMode)
                filename = "stocker_symbols_00-00-0000.html";

            string folderId = Drive.CreateDirectory("temp");
            string fileId = Drive.GetFileId(filename);
            if (fileId != "")
            {
                string content = Drive.DownloadFile(fileId, filename);
                SymbolsInfoList = JsonConvert.DeserializeObject<List<Data.SymbolInfo>>(content);
            }
            else
            {
                if (SymbolsInfoList == null)
                {
                    // load from web
                    SymbolsInfoList = new List<Data.SymbolInfo>(Sources.GetSymbolsFromWeb());

                    // save to disk
                    string output = JsonConvert.SerializeObject(SymbolsInfoList, Formatting.Indented);
                    Drive.UploadFile(folderId, filename, output);                    
                }
            }

            VisibleSymbolsInfoList = new List<Data.SymbolInfo>();
        }

        public void FilterSymbolInfoList(string filter, string group)
        {
            VisibleSymbolsInfoList.Clear();

            bool defaultFilter = filter.Equals("...") || filter.Equals("");
            bool defaultGroup = group.Equals("wszystko");

            // copy all if no filter or group
            if (defaultFilter && defaultGroup)
            {
                foreach (var x in SymbolsInfoList)
                    VisibleSymbolsInfoList.Add(x);
            }
            else
            {
                // copy only filtered and in group
                Debug.WriteLine("Filter: " + filter + " Group: " + group);
                
                foreach (var x in SymbolsInfoList)
                {
                    if (x.FullName.Contains(filter.ToUpper()) || defaultFilter)
                    {
                        if (defaultGroup)
                            VisibleSymbolsInfoList.Add(x);
                        else
                        {
                            if (x.Group != null && x.Group.Contains(group))
                                VisibleSymbolsInfoList.Add(x);
                        }
                    }
                }
            }
        }

        public static void SetCurrentDrawing(Chart currentChart)
        {
            CurrentDrawing = currentChart;
        }

        public static List<Data.SymbolDayData> GetSymbolData(Dictionary<string, List<Data.SymbolDayData>> sdds, Data.SymbolInfo si)
        {
            string symbolName = si.ShortName;

            if (sdds.ContainsKey(symbolName))
                return sdds[symbolName];

            string csv = "";
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_" + today + "_" + symbolName + ".csv";

            if (MainWindow.testMode)
                filename = "stocker_00-00-0000_AAA.csv";
            
            string fileId = Drive.GetFileId(filename);
            if (fileId != "")
            {
                csv = Drive.DownloadFile(fileId, filename);
            }
            else
            {
                csv = Sources.GetHtml(symbolName);

                string folderId = Drive.CreateDirectory("temp");
                Drive.UploadFile(folderId, filename, csv);
            }            

            if (csv == "Przekroczony dzienny limit wywolan")
            {
                MessageBox.Show(csv, "ERROR");
            }

            List<Data.SymbolDayData> result = new List<Data.SymbolDayData>();
            
            foreach (string line in csv.Split('\n'))
            {
                if (line.Length == 0) continue;
                if (line[0] == 'D') continue;
                                
                string[] data = line.Split(',');
                Debug.Assert(data.Count() >= 5);

                DateTime date = DateTime.ParseExact(data[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);

                // discard old data for perf
                if (date.Year < 2016)
                    continue;

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
            sdds.Add(symbolName, result);

            return result;
        }
    }
}
