using System;
using System.Linq;
using System.Threading;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NineyWeather
{
    public sealed partial class MainPage : Page
    {
        private const string SelectedNavItemKey = "SelectedNavItem";
        private DispatcherTimer timer;

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

            // 타이머 설정
            SetTimer();
        }

        private void SetTimer()
        {
            if (timer != null)
            {
                timer.Stop();
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;

            if ((currentDay == DayOfWeek.Tuesday && currentTime >= new TimeSpan(19, 10, 0) && currentTime <= new TimeSpan(19, 40, 0)) ||
                (currentDay == DayOfWeek.Saturday && currentTime >= new TimeSpan(11, 0, 0) && currentTime <= new TimeSpan(11, 30, 0)) ||
                (currentDay == DayOfWeek.Sunday && currentTime >= new TimeSpan(14, 0, 0) && currentTime <= new TimeSpan(14, 30, 0)))
            {
                //if (!(ContentFrame.Content is BusPage))
                //{
                    //ContentFrame.Navigate(typeof(BusPage));
                //}
            }
            else if (currentTime >= new TimeSpan(11, 0, 0) && currentTime <= new TimeSpan(13, 0, 0))
            {
                if (!(ContentFrame.Content is FoodPage))
                {
                    ContentFrame.Navigate(typeof(FoodPage));
                }
            }
            else
            {
                if (!(ContentFrame.Content is WeatherPage))
                {
                    ContentFrame.Navigate(typeof(WeatherPage));
                }
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

                // 타이머 재설정
                SetTimer();
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
                case "lunchRoulette":
                    ContentFrame.Navigate(typeof(RoulettePage));
                    break;
            }
        }
    }
}