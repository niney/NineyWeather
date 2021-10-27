using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NineyWeather.Model
{
    enum ProviderName
    {
        None, AirKorea, Kweather,
    }

    class AirMesure
    {
        public string MangName { get; set; }
        public string So2Value { get; set; }
        public string CoValue { get; set; }
        public string Pm10Value { get; set; }
        public string Pm25Value { get; set; }
        public string KhaiValue { get; set; }
        public string KhaiGrade { get; set; }
        public string Pm10Grade1h { get; set; }
        public string Pm25Grade1h { get; set; }
        public string DataTime { get; set; }
        public ProviderName Provider { get; set; }
    }
}
