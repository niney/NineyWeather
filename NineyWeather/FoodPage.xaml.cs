using NineyWeather.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class FoodPage : Page
    {
        public FoodPage()
        {
            this.InitializeComponent();

            FoodRecommendService foodService = new FoodRecommendService();
            this.foodList.ItemsSource = foodService.Request();
        }

        private async void foodList_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Set the desired remaining view.
            var options = new Windows.System.LauncherOptions();
            options.DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseLess;

            // Launch the URI
            FoodItem foodItem = e.ClickedItem as FoodItem;
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://www.10000recipe.com/recipe/view.html?seq={foodItem.id}"), options);
        }
    }
}
