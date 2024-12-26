using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

            var foodList = this.Parsing(sitesource, "//*[@class='rcp_m_list2']//*[@class='col-xs-3']");
            foodList = foodList.Concat(this.Parsing(sitesource2, "//*[@class='rcp_m_list2']//*[@class='common_sp_list_li']")).ToList();
            foodList.Shuffle();
            return foodList;
        }

        public List<FoodItem> RequestReco(int page)
        {
            string sitesource = this.RequestRecipe("https://www.10000recipe.com/recipe/reco_list.html?page=" + page);

            var foodList = this.Parsing(sitesource, "//*[@class='rcp_m_list2']//*[@class='col-xs-3']");
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

        private List<FoodItem> Parsing(String source, String firstCls)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(source);
            var nodes = doc.DocumentNode.SelectNodes(firstCls);
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
                foodItem.Id = id;
                foodItem.Title = title;
                foodItem.Img = img.Value;
                foods.Add(foodItem);
            }
            return foods;
        }

        public RecipeData GetRecipeData(string html)
        {
            RecipeData recipeData = new RecipeData();

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 1. id가 main_thumbs인 img 태그의 src 속성 가져오기
                HtmlNode mainThumbsImg = doc.DocumentNode.SelectSingleNode("//img[@id='main_thumbs']");
                recipeData.MainThumbsSrc = mainThumbsImg?.GetAttributeValue("src", null);

                // 2. id가 divConfirmedMaterialArea인 div 태그 아래의 재료 정보 가져오기
                HtmlNode ingredientsArea = doc.DocumentNode.SelectSingleNode("//div[@id='divConfirmedMaterialArea']");
                if (ingredientsArea != null)
                {
                    recipeData.Ingredients = new List<Ingredient>();
                    var ingredientLiNodes = ingredientsArea.SelectNodes(".//li");
                    if (ingredientLiNodes != null)
                    {
                        foreach (var liNode in ingredientLiNodes)
                        {
                            var nameNode = liNode.SelectSingleNode(".//div[@class='ingre_list_name']");
                            var quantityNode = liNode.SelectSingleNode(".//span[@class='ingre_list_ea']");

                            // 재료명에서 \n 제거 및 공백 2칸 이상을 1칸으로 변경
                            string name = nameNode?.InnerText.Trim().Replace("\n", "").Replace("\r", "");
                            name = Regex.Replace(name, @"\s{2,}", " ");

                            if (nameNode != null && quantityNode != null)
                            {
                                recipeData.Ingredients.Add(new Ingredient
                                {
                                    Name = name,
                                    Quantity = quantityNode.InnerText.Trim()
                                });
                            }
                        }
                    }
                }

                // 3. id가 obx_recipe_step_start인 div 태그 아래의 조리 단계 정보 가져오기
                HtmlNode stepsArea = doc.DocumentNode.SelectSingleNode("//div[@id='obx_recipe_step_start']");
                if (stepsArea != null)
                {
                    recipeData.Steps = new List<RecipeStep>();
                    var stepDivNodes = stepsArea.SelectNodes("./div[starts-with(@id, 'stepDiv')]");
                    if (stepDivNodes != null)
                    {
                        foreach (var stepDivNode in stepDivNodes)
                        {
                            string stepId = stepDivNode.Id;
                            string stepNumber = stepId.Replace("stepDiv", "");

                            var descNode = stepDivNode.SelectSingleNode(".//div[@class='media-body']");
                            var imgNode = descNode?.NextSibling?.SelectSingleNode(".//img");

                            recipeData.Steps.Add(new RecipeStep
                            {
                                Step = stepNumber,
                                Description = descNode?.InnerText.Trim(),
                                Image = imgNode?.GetAttributeValue("src", null)
                            });
                        }
                    }
                }

                // 4. id가 contents_area_full인 div 태그 아래의 조리명, 조리 내용 요약 가져오기
                HtmlNode contentsAreaFull = doc.DocumentNode.SelectSingleNode("//div[@id='contents_area_full']");
                if (contentsAreaFull != null)
                {
                    // 조리명
                    var recipeNameNode = contentsAreaFull.SelectSingleNode(".//div[@class='view2_summary st3']//h3");
                    recipeData.RecipeName = recipeNameNode?.InnerText.Trim();

                    // 조리내용 요약
                    var summaryNode = contentsAreaFull.SelectSingleNode(".//div[@class='view2_summary_in']");
                    recipeData.Summary = summaryNode?.InnerText.Trim();
                }
            }
            catch (Exception ex)
            {
                // 예외 발생 시 로그를 남기거나 기본값을 설정할 수 있습니다.
                // 예: 로그 남기기
                Console.WriteLine($"Error parsing recipe data: {ex.Message}");
                // 예: 기본값 설정
                recipeData = new RecipeData
                {
                    MainThumbsSrc = null,
                    Ingredients = new List<Ingredient>(),
                    Steps = new List<RecipeStep>(),
                    RecipeName = "Unknown",
                    Summary = "No summary available"
                };
            }

            return recipeData;
        }

        public RecipeData RequestDetail(string url)
        {
            string sitesource = this.RequestRecipe(url);
            RecipeData data = GetRecipeData(sitesource);
            return data;
        }
    }

    public class FoodItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Img { get; set; }
    }

    // Ingredient 클래스 (재료명, 재료양)
    public class Ingredient
    {
        public string Name { get; set; }
        public string Quantity { get; set; }
    }

    // Step 클래스 (조리 단계, 조리 설명, 조리 이미지)
    public class RecipeStep
    {
        public string Step { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }

    public class RecipeData
    {
        public string MainThumbsSrc { get; set; }
        public List<Ingredient> Ingredients { get; set; }
        public List<RecipeStep> Steps { get; set; }
        public string RecipeName { get; set; }
        public string Summary { get; set; }
    }

}
