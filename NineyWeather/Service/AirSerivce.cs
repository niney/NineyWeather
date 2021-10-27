using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NineyWeather.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace NineyWeather.Service
{    
    class AirSerivce
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly ResourceLoader RESOURCES = new ResourceLoader("apiResources");
        private const string KAKAKO_URL = "https://dapi.kakao.com/v2/local/{0}/{1}.json";
        private const string KAKAKO_AUTH_KEY = "Authorization";
        private readonly string KAKAKO_AUTH_VAL = RESOURCES.GetString("kakaoAuthKey");
        private readonly string AIRKOREA_API_KEY = RESOURCES.GetString("airServiceKey");

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
            AirMesure airMesure = new AirMesure();
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


                    var airkoreaResult = await this.RequestAirkorea(httpClient, result.documents[0].x, result.documents[0].y);
                    if (airkoreaResult != null)
                    {
                        return airkoreaResult;
                    }

                    var kweatherResult = await this.RequestKweather(httpClient);
                    DateTime now = new DateTime();
                    kweatherResult.DataTime = now.ToString("yyyy-MM-dd H:mm");
                    return kweatherResult;

                } catch (Exception e)
                {
                    Logger.Error(e.Message);
                    airMesure.Provider = ProviderName.None;
                    airMesure.KhaiValue = "0";
                    airMesure.Pm10Value = "0";
                    airMesure.Pm25Value = "0";
                    airMesure.DataTime = DateTime.Now.ToString("MMMM dd일 tt h:mm:ss");
                }
            }
            return airMesure;
        }

        private async Task<AirMesure> RequestAirkorea(HttpClient httpClient, string x, string y)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(2000);
            try
            {
                string airServiceKey = AIRKOREA_API_KEY;
                // 미세먼지 정보
                var uri = new Uri(string.Format("http://openapi.airkorea.or.kr/openapi/services/rest/MsrstnInfoInqireSvc/getNearbyMsrstnList?ServiceKey={0}&tmX={1}&tmY={2}", airServiceKey, x, y));
                var response = await httpClient.GetAsync(uri).AsTask(cancellationTokenSource.Token);
                var responseBody = await response.Content.ReadAsStringAsync();

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
                response = await httpClient.GetAsync(uri).AsTask(cancellationTokenSource.Token);
                responseBody = await response.Content.ReadAsStringAsync();

                xml = new XmlDocument();
                xml.LoadXml(responseBody);
                var xn = xml.SelectSingleNode("/response/body/items/item");

                return new AirMesure
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
                    DataTime = xn["dataTime"].InnerText,
                    Provider = ProviderName.AirKorea
                };
            } catch (Exception e)
            {
                Logger.Error(e.Message + ", timeout RequestAirkorea");
                return null;
            }
        }

        private async Task<AirMesure> RequestKweather(HttpClient httpClient)
        {
            Uri uri = new Uri("http://kweather.co.kr/air/data/dataJSON/AIR_DONG_DATA_KIOT_1100000000.json");
            var response  = await httpClient.GetAsync(uri);
            var responseBody = await response.Content.ReadAsStringAsync();

            JObject json = JObject.Parse(responseBody);
            var query = from s in json["list"]["guList"].SelectMany(gu => gu["dongList"]).Where(gu => gu.Value<string>("dCode") == "1147054000")
                    select new AirMesure 
                    {
                        MangName = (string)s["dName"],
                        Pm10Value = (string)s["pm10Value"],
                        Pm25Value = (string)s["pm25Value"],
                        Pm10Grade1h = (string)s["pm10GradeH"],
                        Pm25Grade1h = (string)s["pm25GradeH"],
                        Provider = ProviderName.Kweather
                    };

            return query.ToList()[0];
        }
    }
}
