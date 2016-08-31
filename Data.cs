using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace WpfApplication3
{
    class Data
    {
        struct SymbolDayData
        {
            public DateTime date;
            public double open, hi, low, close;
            public uint volume; 
        }

        public static List<string> GetSymbolsFromWeb()
        {
            { 
            List<SymbolDayData> results = new List<SymbolDayData>();
            HtmlWeb weba = new HtmlWeb();

            string urla = "http://stooq.pl/q/d/l/?s=dom&i=d";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(urla);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                StreamReader sr = new StreamReader(resp.GetResponseStream());
                string resultsa = sr.ReadToEnd();
                sr.Close();
            }


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

        List<SymbolDayData> GetSymbolDataFromWeb(string symbolName)
        {
            List<SymbolDayData> results = new List<SymbolDayData>();
            HtmlWeb web = new HtmlWeb();

            string url = "http://stooq.pl/q/d/l/?s=dom&i=d";
            HtmlDocument doc = web.Load(url);

            return results;
        }
    }    
}
