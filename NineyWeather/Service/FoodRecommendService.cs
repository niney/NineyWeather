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
            string sitesource = this.RequestRecipe("https://www.10000recipe.com/recipe/reco_list.html");
            string sitesource2 = this.RequestRecipe("https://www.10000recipe.com/recipe/list.html?q=%EC%88%98%EB%AF%B8");

            var foodList = this.Parsing(sitesource);
            foodList = foodList.Concat(this.Parsing(sitesource2)).ToList();
            foodList.Shuffle();
            return foodList;
        }

        private string RequestRecipe(String url)
        {
            string sitesource = "";
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                sitesource = client.DownloadString(url);
            }

            return sitesource;
        }

        private List<FoodItem> Parsing(String source)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(source);
            var nodes = doc.DocumentNode.SelectNodes("//*[@class='rcp_m_list2']//*[@class='col-xs-3']");
            List<FoodItem> foods = new List<FoodItem>();
            foreach (var node in nodes)
            {
                var aTagNode = node.SelectSingleNode("a");
                if (node.SelectSingleNode("a") == null)
                {
                    continue;
                }
                var id = aTagNode.Attributes["href"].Value.Split("/")[2];
                var imgNode = node.SelectSingleNode("*//*[@class='thumbs_hb']//img");
                if (imgNode == null)
                {
                    var imgNodes = node.SelectNodes("*//img");
                    if (imgNodes.Count > 2)
                    {
                        imgNode = imgNodes[2];
                    }
                    else if (imgNodes.Count > 1)
                    {
                        imgNode = imgNodes[1];
                    }
                }
                var img = imgNode.Attributes["src"];
                var title = node.SelectSingleNode("*//div//h4").InnerText;
                var foodItem = new FoodItem();
                foodItem.id = id;
                foodItem.Title = title;
                foodItem.Img = String.Format("http://cache.coolschool.co.kr/image/T/320X180?url={0}", HttpUtility.UrlEncode(img.Value));
                foods.Add(foodItem);
            }
            return foods;
        }
    }

    public class FoodItem
    {
        public string id { get; set; }
        public string Title { get; set; }
        public string Img { get; set; }
    }
}
