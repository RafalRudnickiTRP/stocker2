using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

using Newtonsoft.Json;

namespace WpfApplication3
{
    public class Trigger
    {
        public enum Type
        {
            Nothing,

            CrossUpLineWithTrend,
            CrossDownLineWithTrend,

            // others could be:
            TouchUpLineWithTrend,
            TouchDownLineWithTrend,
            TouchUpLineAgainstTrend,
            TouchDownLineAgainstTrend,
        }

        public Trigger(Type type)
        {

        }

        public static Type Check(Chart.ChartLine line, Data.SymbolDayData sdd)
        {                       
            bool cross = Misc.LineValueOnSdd(line, sdd);

            bool upCandle = sdd.Open > sdd.Close;
            bool downCandle = !upCandle;

            bool upLine = line.getP1().X < line.getP1().X ?
                line.getP1().Y < line.getP1().Y : line.getP1().Y > line.getP1().Y;
            bool downLine = !upLine;

            if (cross && upCandle && upLine)
                return Type.CrossUpLineWithTrend;
            else if (cross && downCandle && downLine)
                return Type.CrossDownLineWithTrend;

            return Type.Nothing;
        }
    }
}
