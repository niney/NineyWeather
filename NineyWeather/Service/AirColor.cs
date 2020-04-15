using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace NineyWeather.Service
{
    class AirColor
    {
        private static readonly int redRange = 150;
        private static readonly int greenRange = 20;
        private static readonly int blueRange = 255;

        private readonly double redRate = 255.0 / redRange;
        private readonly double greenRate = 255.0 / greenRange;
        private readonly double blueRate = 255.0 / blueRange;

        public Brush CalSolidBrush(int dust)
        {
            byte red = (byte)(dust * this.redRate);
            if (dust > redRange) {
                red = 0;
            }
            byte green = (byte)(((dust * this.greenRate) - 255) * -1);
            if (dust > greenRange) {
                green = 0;
            }
            byte blue = (byte)(((dust * this.blueRate) - 255) * -1);
            if (dust > blueRange) {
                blue = 0;
            }
            //System.Diagnostics.Debug.WriteLine("dust: {0}, r : {1}, g : {2}, b : {3}", dust, red, green, blue);
            return new SolidColorBrush(Color.FromArgb(255, red, green, blue));
        }
    }
}
