using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Windows;

namespace WpfApplication3
{
    public partial class DataViewModel
    {
        public string SerializeToJson()
        {
            foreach (KeyValuePair<string, Chart> pairSymbolsDrawings in SymbolsDrawings)
            {
                Chart.DataToSerialize data = pairSymbolsDrawings.Value.SerializeToJson();
                string key = pairSymbolsDrawings.Key;

                if (SymbolsDrawingsToSerialize.ContainsKey(key))
                    SymbolsDrawingsToSerialize.Remove(key);

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

            public DataToSerialize SerializeToJson(DrawingInfo drawingInfo)
            {
                // dates 
                var P1DT = Misc.PixelToSdd(drawingInfo, getP1());
                var P2DT = Misc.PixelToSdd(drawingInfo, getP2());

                // values
                double P1ValY = Math.Round(Misc.RemapRangePixToVal(getP1().Y, drawingInfo), 6);
                double P2ValY = Math.Round(Misc.RemapRangePixToVal(getP2().Y, drawingInfo), 6);

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
