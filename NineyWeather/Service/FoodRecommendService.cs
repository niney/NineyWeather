using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NineyWeather.Service
{
    public class FoodRecommendService
    {
        public List<FoodItem> Request()
        {
            string sitesource = "";
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                sitesource = client.DownloadString("https://www.10000recipe.com/recipe/reco_list.html");
            }

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(sitesource);
            var nodes = doc.DocumentNode.SelectNodes("//*[@id='contents_area_full']/div/div/div");
            List<FoodItem> foods = new List<FoodItem>();
            foreach(var node in nodes)
            {
                var img = node.SelectSingleNode("*//img").Attributes["src"];
                var title = node.SelectSingleNode("*//div//h4").InnerText;
                var foodItem = new FoodItem();
                foodItem.Title = title;
                foodItem.Img = String.Format("http://cache.coolschool.co.kr/image/T/275X174?url={0}", HttpUtility.UrlEncode(img.Value));
                foods.Add(foodItem);
            }
            return foods;
        }
    }

    public class FoodItem
    {
        public string Title { get; set; }
        public string Img { get; set; }
    }
}
