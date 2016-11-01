using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace WpfApplication3
{
    public class Misc
    {
        public static string BrushToString(Brush br)
        {
            var map = new Dictionary<Brush, string>
            {
                { Brushes.Black, "Black" },
                { Brushes.Lime, "Lime" },
                { Brushes.Blue, "Blue" },
                { Brushes.Red, "Red" },
            };

            return map[br];
        }

        public static Brush StringToBrush(string br)
        {
            var map = new Dictionary<string, Brush>
            {
                { "Black", Brushes.Black },
                { "Lime", Brushes.Lime },
                { "Blue", Brushes.Blue },
                { "Red", Brushes.Red },
            };
            
            return map[br];
        }
    }
}