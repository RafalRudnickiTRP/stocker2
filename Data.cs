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

namespace WpfApplication3
{
    public class Data
    {
        public class SymbolDayData
        {
            public DateTime date { get; set; }
            public float open { get; set; }
            public float hi { get; set; }
            public float low { get; set; }
            public float close { get; set; }
            public uint volume { get; set; }
        }

        public class SymbolInfo
        {
            public string FullName { get; }
            public string ShortName { get; }

            public SymbolInfo(string fullName, string shortName)
            {
                FullName = fullName;
                ShortName = shortName;
            }
        }

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
            List<SymbolDayData> result = new List<SymbolDayData>();

            symbolName = "dom";
            string url = "http://stooq.pl/q/d/l/?s=" + symbolName + "&i=d";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string csv = sr.ReadToEnd();

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

                SymbolDayData sdd = new SymbolDayData();
                sdd.date = DateTime.ParseExact(data[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                sdd.open = float.Parse(data[1], CultureInfo.InvariantCulture);
                sdd.hi = float.Parse(data[2], CultureInfo.InvariantCulture);
                sdd.low = float.Parse(data[3], CultureInfo.InvariantCulture);
                sdd.close = float.Parse(data[4], CultureInfo.InvariantCulture);
                sdd.volume = uint.Parse(data[5]);

                result.Add(sdd);
            }

            return result;
        }

    }    

    public class DataViewModel
    {
        public List<Data.SymbolInfo> SymbolsInfoList { get; set; }

        public DataViewModel()
        {
            SymbolsInfoList = new List<Data.SymbolInfo>(Data.GetSymbolsFromWeb());
        }
    }
}
