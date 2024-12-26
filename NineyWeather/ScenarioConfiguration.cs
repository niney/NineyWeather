using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace NineyWeather
{
    public partial class MainPage : Page
    {
        List<Scenario> scenarios = new List<Scenario>
        {
            new Scenario() { Title="날씨", ClassType=typeof(WeatherPage)},
            new Scenario() { Title="음식추천", ClassType=typeof(FoodPage)}
        };

        public class Scenario
        {
            public string Title { get; set; }
            public Type ClassType { get; set; }
        }
    }
}
