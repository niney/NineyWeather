using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NineyWeather.Model
{
    class BusStopLocation
    {
        public string BusStopId { get; set; }
        public string ArsId { get; set; }
        public string BusStopName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
