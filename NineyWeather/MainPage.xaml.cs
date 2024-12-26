using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NineyWeather
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;

            // 초기 페이지로 날씨 페이지 설정
            ContentFrame.Navigate(typeof(WeatherPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
                return;

            switch (args.InvokedItem?.ToString())
            {
                case "날씨":
                    ContentFrame.Navigate(typeof(WeatherPage));
                    break;
                case "음식추천":
                    ContentFrame.Navigate(typeof(FoodPage));
                    break;
            }
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
    }
}