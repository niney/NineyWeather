using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NineyWeather
{
    public sealed partial class MainPage : Page
    {
        private const string SelectedNavItemKey = "SelectedNavItem";

        public MainPage()
        {
            this.InitializeComponent();

            // 마지막으로 선택한 NavigationViewItem 복원
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(SelectedNavItemKey))
            {
                var selectedItemTag = localSettings.Values[SelectedNavItemKey] as string;
                var selectedItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag.ToString() == selectedItemTag);

                if (selectedItem != null)
                {
                    NavView.SelectedItem = selectedItem;
                    NavigateToPage(selectedItemTag);
                }
            }
            else
            {
                // 초기 페이지로 날씨 페이지 설정
                ContentFrame.Navigate(typeof(WeatherPage));
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
                return;

            var invokedItemTag = (args.InvokedItemContainer as NavigationViewItem)?.Tag.ToString();
            if (invokedItemTag != null)
            {
                // 선택한 NavigationViewItem 저장
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[SelectedNavItemKey] = invokedItemTag;

                NavigateToPage(invokedItemTag);
            }
        }

        private void NavigateToPage(string tag)
        {
            switch (tag)
            {
                case "weather":
                    ContentFrame.Navigate(typeof(WeatherPage));
                    break;
                case "food":
                    ContentFrame.Navigate(typeof(FoodPage));
                    break;
                case "bus":
                    ContentFrame.Navigate(typeof(BusPage));
                    break;
                case "photos":
                    ContentFrame.Navigate(typeof(ImageSlidePage));
                    break;
            }
        }
    }
}