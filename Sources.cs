using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfApplication3
{
    class Sources
    {
        public static Data.SymbolDayData GetSdd(Data.SymbolInfo si, out string time)
        {
            time = "";
            Console.WriteLine("GetSdd: {0}", si.FullName);

            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = new HtmlDocument();

                string data = "http://stooq.pl/q/?s=" + si.ShortName;
                doc = web.Load(data);

                HtmlNodeCollection symbolNodes = doc.DocumentNode.SelectNodes("//*/font[@id=\"f18\"]");
                string currentPrice = symbolNodes[1].InnerText;
                HtmlNodeCollection hiCol = doc.DocumentNode.SelectNodes("//*/span[@id='aq_"
                    + si.ShortName.ToLower(CultureInfo.InvariantCulture) + "_h']");
                string hi = hiCol[0].InnerText;
                HtmlNodeCollection lowCol = doc.DocumentNode.SelectNodes("//*/span[@id='aq_"
                    + si.ShortName.ToLower(CultureInfo.InvariantCulture) + "_l']");
                string low = lowCol[0].InnerText;
                HtmlNodeCollection openCol = doc.DocumentNode.SelectNodes("//*/span[@id='aq_"
                    + si.ShortName.ToLower(CultureInfo.InvariantCulture) + "_o']");
                string open = openCol[0].InnerText;

                HtmlNodeCollection timeCol = doc.DocumentNode.SelectNodes("//*/span[@id='aqdat']");
                time = timeCol[0].InnerText.Remove(timeCol[0].InnerText.IndexOf("CET") + 3);
            
                Data.SymbolDayData sdd = new Data.SymbolDayData(DateTime.Today,
                    float.Parse(open, CultureInfo.InvariantCulture),
                    float.Parse(hi, CultureInfo.InvariantCulture),
                    float.Parse(low, CultureInfo.InvariantCulture),
                    float.Parse(currentPrice, CultureInfo.InvariantCulture),
                    0); //TODO vol maybe in future
                return sdd;
            }
            catch (Exception)
            {
                Debug.Assert(false);
            }

            return null;
        }

        public static List<Data.SymbolInfo> GetSymbolsFromWeb()
        {
            List<Data.SymbolInfo> symbols = new List<Data.SymbolInfo>();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = new HtmlDocument();
            int page = 1;
            int added = 0;

            Console.WriteLine("GetSymbolsFromWeb()");

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
                    Data.SymbolInfo si = new Data.SymbolInfo(fullName, shortName);
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

            // manually added
            {
                Data.SymbolInfo intel = new Data.SymbolInfo("_US_INTEL", "INTC.US");
                symbols.Add(intel);

                Data.SymbolInfo usdpln = new Data.SymbolInfo("_FX_USDPLN", "USDPLN");
                symbols.Add(usdpln);
                Data.SymbolInfo eurpln = new Data.SymbolInfo("_FX_EURPLN", "EURPLN");
                symbols.Add(eurpln);
                Data.SymbolInfo chfpln = new Data.SymbolInfo("_FX_CHFPLN", "CHFPLN");
                symbols.Add(chfpln);
                Data.SymbolInfo gbppln = new Data.SymbolInfo("_FX_GBPPLN", "GBPPLN");
                symbols.Add(gbppln);
            }

            return symbols;
        }

        public static string GetHtml(string symbolName)
        {
            Console.WriteLine("GetHtml: " + symbolName);

            string url = "http://stooq.pl/q/d/l/?s=" + symbolName + "&i=d";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            return sr.ReadToEnd();
        }
    }
}
