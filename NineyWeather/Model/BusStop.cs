// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    public class BusStop
    {
        public string RouteId { get; set; }
        public string RouteName { get; set; }
        public int Sequence { get; set; }
        public string NodeId { get; set; }
        public string ArsId { get; set; }
        public string StopName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

}
