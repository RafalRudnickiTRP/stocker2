﻿using System;
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

            public SymbolDayData(DateTime date, float open, float hi, float low,float close,uint volume)
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
            public string FullName { get; }
            public string ShortName { get; }

            public SymbolInfo(string fullName, string shortName)
            {
                FullName = fullName;
                ShortName = shortName;

                SymbolInfoList.Add(this);
            }
        }

        #region Members

        public static List<SymbolInfo> SymbolInfoList = new List<SymbolInfo>();

        #endregion

        public static List<SymbolInfo> GetSymbolsFromWeb()
        {
            GetSymbolDataFromWeb("Aaa");

            List<SymbolInfo> symbols = new List<SymbolInfo>();

            HtmlWeb web = new HtmlWeb();
            int page = 1;
            int added = 0;

            while (true)
            {
                string url = "http://stooq.pl/t/?i=513&v=1&l=" + page.ToString();
                HtmlDocument doc = web.Load(url);

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

        public static List<SymbolDayData> GetSymbolDataFromWeb(string symbolName)
        {
            string csv = "";
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_" + today + "_" + symbolName + ".csv";
            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                using (StreamReader reader = new StreamReader(mydocpath + @"\" + filename))
                {
                    // Read the stream to a string, and write the string to the console.
                    csv = reader.ReadToEnd();
                }
            } catch (Exception e)
            {
            }

            if (csv == "")
            {
                string url = "http://stooq.pl/q/d/l/?s=" + symbolName + "&i=d";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                csv = sr.ReadToEnd();

                using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\" + filename))
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
                Debug.Assert(data.Length == 6);

                DateTime date = DateTime.ParseExact(data[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                float open = float.Parse(data[1], CultureInfo.InvariantCulture);
                float hi = float.Parse(data[2], CultureInfo.InvariantCulture);
                float low = float.Parse(data[3], CultureInfo.InvariantCulture);
                float close = float.Parse(data[4], CultureInfo.InvariantCulture);
                uint volume = uint.Parse(data[5]);

                SymbolDayData sdd = new SymbolDayData(date, open, hi, low, close, volume);

                result.Add(sdd);
            }

            return result;
        }

    }    

    public class DataViewModel
    {
        public List<Data.SymbolInfo> SymbolsInfoList { get; set; }

        public Dictionary<string, Chart> SymbolsDrawings { get; set; }
        public Chart CurrentDrawing { get; set;  }
        
        public Dictionary<string, Chart.DataToSerialize> SymbolsDrawingsToSerialize { get; set; }

        public string SerializeToJson()
        {
            SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();

            foreach (KeyValuePair<string, Chart> pairSymbolsDrawings in SymbolsDrawings)
            {
                Chart.DataToSerialize data = pairSymbolsDrawings.Value.SerializeToJson();
                string key = pairSymbolsDrawings.Key;
                SymbolsDrawingsToSerialize.Add(key, data);
            }

            string output = JsonConvert.SerializeObject(SymbolsDrawingsToSerialize, Formatting.Indented);
            return output;
        }

        public void DeserializeFromJson(string input)
        {
            SymbolsDrawingsToSerialize = new Dictionary<string, Chart.DataToSerialize>();
            SymbolsDrawingsToSerialize = JsonConvert.DeserializeObject<Dictionary<string, Chart.DataToSerialize>>(input);
            
            foreach (Data.SymbolInfo symbolInfo in SymbolsInfoList)
            {

            }
        }

        public DataViewModel()
        {
            SymbolsDrawings = new Dictionary<string, Chart>();

            //  SymbolsInfoList = new List<Data.SymbolInfo>(Data.GetSymbolsFromWeb());

            // debug
            SymbolsInfoList = new List<Data.SymbolInfo>();
            SymbolsInfoList.Add(new Data.SymbolInfo("DOMDEV", "DOM"));
            SymbolsInfoList.Add(new Data.SymbolInfo("11BIT", "11B"));
            SymbolsInfoList.Add(new Data.SymbolInfo("KGHM", "KGH"));
        }

        public void SetCurrentDrawing(Chart currentChart)
        {
            CurrentDrawing = currentChart;
        }
    }
}
