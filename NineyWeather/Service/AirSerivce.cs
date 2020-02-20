using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace App1.Service
{
    class AirSerivce
    {
        public async Task<List<string>> Request(string addr)
        {
            List<string> info = new List<string>();
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    // 주소-> 좌표
                    httpClient.DefaultRequestHeaders.Add("Authorization", "KakaoAK 0a9fc00e3366d781f02429831a3b953d");
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

                    //string airServiceKey = "8h7b9a876gQhh2GZaSMXGEaFeqpFoX%2BLx2K6XXe67dGgKbwb45BMzPMTbmFfQuL59feW3%2FnXXbwLWss871elXA%3D%3D";
                    string airServiceKey = "BHNLDvjF4oORm8ohVGD95jDWYQhzcQS31q9micS6eSGAFuWnhz0otvLqLQVXnOkMVRfbk9NFkdeC%2FSJJnAUvIQ%3D%3D";
                    // 미세먼지 정보
                    uri = new Uri(string.Format("http://openapi.airkorea.or.kr/openapi/services/rest/MsrstnInfoInqireSvc/getNearbyMsrstnList?ServiceKey={0}&tmX={1}&tmY={2}", airServiceKey,result.documents[0].x, result.documents[0].y));
                    response = await httpClient.GetAsync(uri);
                    responseBody = await response.Content.ReadAsStringAsync();

                    var xml = new XmlDocument();
                    xml.LoadXml(responseBody);
                    var xnList = xml.SelectNodes("/response/body/items/item");
                    foreach (XmlNode xn in xnList)
                    {
                        info.Add(String.Format("측정소: {0}, 주소 : {1}, 거리, {2}", xn["stationName"].InnerText, xn["addr"].InnerText, xn["tm"].InnerText));
                    }

                    // 미세먼지 정보
                    var rltDnstyUrl = "http://openapi.airkorea.or.kr/openapi/services/rest/ArpltnInforInqireSvc/getMsrstnAcctoRltmMesureDnsty?" +
                        "ServiceKey={0}" +
                        "&stationName={1}" +
                        "&dataTerm={2}"
                        ;
                    uri = new Uri(string.Format(rltDnstyUrl, airServiceKey, xnList[0]["stationName"].InnerText, "1"));
                    response = await httpClient.GetAsync(uri);
                    responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine(responseBody);

                } catch (Exception e)
                {
                    e.ToString();
                }
            }
            return info;
        }
    }
}
