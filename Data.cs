using System;
using System.Collections.Generic;
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
    class Data
    {
        struct SymbolDayData
        {
            public DateTime date;
            public float open, hi, low, close;
            public uint volume; 
        }

        public static List<string> GetSymbolsFromWeb()
        {
            GetSymbolDataFromWeb("Aaa");

            List<string> symbols = new List<string>();

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
                    symbols.Add(node.InnerText);
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

        static List<SymbolDayData> GetSymbolDataFromWeb(string symbolName)
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
}
