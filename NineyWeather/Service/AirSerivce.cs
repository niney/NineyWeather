using Newtonsoft.Json;
using NineyWeather.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Windows.Data.Json;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NineyWeather.Service
{
    class AirSerivce
    {
        private const string KAKAKO_URL = "https://dapi.kakao.com/v2/local/{0}/{1}.json";
        private const string KAKAKO_AUTH_KEY = "Authorization";
        private const string KAKAKO_AUTH_VAL = "KakaoAK 0a9fc00e3366d781f02429831a3b953d";

        public async Task<string> Coord2Address(string lng, string lat)
        {
            var uriBuilder = new UriBuilder(string.Format(KAKAKO_URL, "geo", "coord2address"));
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["x"] = lng;
            parameters["y"] = lat;
            uriBuilder.Query = parameters.ToString();

            var addressName = "";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(KAKAKO_AUTH_KEY, KAKAKO_AUTH_VAL);
                var response = await httpClient.GetAsync(uriBuilder.Uri);
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
                var resultObj = JsonObject.Parse(responseBody);
                addressName = resultObj.GetNamedArray("documents").GetObjectAt(0).GetNamedObject("address").GetNamedString("address_name");
            }
            return addressName;
        }

        public async Task<AirMesure> Request(string addr)
        {
            List<string> info = new List<string>();
            AirMesure airMesure = null;
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // 주소-> 좌표
                    httpClient.DefaultRequestHeaders.Add(KAKAKO_AUTH_KEY, KAKAKO_AUTH_VAL);
                    var uri = new Uri(string.Format("https://dapi.kakao.com/v2/local/search/address.json?query={0}", HttpUtility.UrlEncode(addr)));
                    var response = await httpClient.GetAsync(uri);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<KkoResult>(responseBody);

                    // 키워드 검색 변환
                    if (result.documents.Count == 0)
                    {
                        uri = new Uri(string.Format("https://dapi.kakao.com/v2/local/search/keyword.json?query={0}", HttpUtility.UrlEncode(addr)));
                        response = await httpClient.GetAsync(uri);
                        responseBody = await response.Content.ReadAsStringAsync();
                        result = JsonConvert.DeserializeObject<KkoResult>(responseBody);
                    }

                    // tm 좌표 변환
                    uri = new Uri(string.Format("https://dapi.kakao.com/v2/local/geo/transcoord.json?x={0}&y={1}&output_coord=TM", result.documents[0].x, result.documents[0].y));
                    response = await httpClient.GetAsync(uri);
                    responseBody = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<KkoResult>(responseBody);

                    httpClient.DefaultRequestHeaders.Remove(KAKAKO_AUTH_KEY); // kakao 사용끝나면..

                    //string airServiceKey = "8h7b9a876gQhh2GZaSMXGEaFeqpFoX%2BLx2K6XXe67dGgKbwb45BMzPMTbmFfQuL59feW3%2FnXXbwLWss871elXA%3D%3D";
                    string airServiceKey = "KtUrDQ1aaGWFVpo7qKpsqBwsxgVBB6qCPhq2PlRt4lMC7cyYp0jJrRXRH3BFZ6AiJCxaHFWLxpkr73WvKy6uWw%3D%3D";
                    // 미세먼지 정보
                    uri = new Uri(string.Format("http://openapi.airkorea.or.kr/openapi/services/rest/MsrstnInfoInqireSvc/getNearbyMsrstnList?ServiceKey={0}&tmX={1}&tmY={2}", airServiceKey,result.documents[0].x, result.documents[0].y));
                    response = await httpClient.GetAsync(uri);
                    responseBody = await response.Content.ReadAsStringAsync();

                    var xml = new XmlDocument();
                    xml.LoadXml(responseBody);
                    var xnList = xml.SelectNodes("/response/body/items/item");

                    // 미세먼지 정보
                    var rltDnstyUrl = "http://openapi.airkorea.or.kr/openapi/services/rest/ArpltnInforInqireSvc/getMsrstnAcctoRltmMesureDnsty?" +
                        "serviceKey={0}" +
                        "&stationName={1}" +
                        "&dataTerm={2}" +
                        "&ver={3}"
                        ;
                    uri = new Uri(string.Format(rltDnstyUrl, airServiceKey, xnList[0]["stationName"].InnerText, "DAILY", "1.3"));
                    response = await httpClient.GetAsync(uri);
                    responseBody = await response.Content.ReadAsStringAsync();

                    xml = new XmlDocument();
                    xml.LoadXml(responseBody);
                    var xn = xml.SelectSingleNode("/response/body/items/item");
                    airMesure = new AirMesure
                    {
                        MangName = xn["mangName"].InnerText,
                        So2Value = xn["so2Value"].InnerText,
                        CoValue = xn["coValue"].InnerText,
                        Pm10Value = xn["pm10Value"].InnerText,
                        Pm25Value = xn["pm25Value"].InnerText,
                        KhaiValue = xn["khaiValue"].InnerText,
                        KhaiGrade = xn["khaiGrade"].InnerText,
                        Pm10Grade1h = xn["pm10Grade1h"].InnerText,
                        Pm25Grade1h = xn["pm25Grade1h"].InnerText,
                        dataTime = xn["dataTime"].InnerText
                    };

                } catch (Exception e)
                {
                    e.ToString();
                }
            }
            return airMesure;
        }
    }
}
