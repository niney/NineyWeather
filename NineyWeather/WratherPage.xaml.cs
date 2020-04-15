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
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Media.Imaging;
using NineyWeather.Service;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class WratherPage : Page
    {

        private string lat, lng;
        private readonly AirSerivce airSerivce = new AirSerivce();
        private readonly AirColor airColor = new AirColor();
        private DispatcherTimer currentTimeTimer;
        private DispatcherTimer airTimer;

        public WratherPage()
        {
            this.InitializeComponent();
            this.DoSearch();
            this.CurrentTime();
            this.AirTimerStart();
        }

        public async void DoSearch()
        {
            this.progressRing.IsActive = true;
            var geoLocator = new Geolocator();
            geoLocator.DesiredAccuracy = PositionAccuracy.High;
            // 나의 위치 정보 가져오기
            Geoposition pos = await geoLocator.GetGeopositionAsync();
            this.lng = pos.Coordinate.Point.Position.Longitude.ToString();
            this.lat = pos.Coordinate.Point.Position.Latitude.ToString();

            //var addressName = await this.airSerivce.Coord2Address(this.lng, this.lat);
            var addressName = "서울시 양천구 목4동";
            var airMesure = await this.airSerivce.Request(addressName);

            this.txtCity.Text = addressName;
            //this.txtSo2Value.Text = $"아황산가스 농도 {airMesure.So2Value}";
            //this.txtCo2Value.Text = $"일산화탄소 농도 {airMesure.CoValue}";
            this.txtKhaiValue.Text = $"통합대기환경지수 {airMesure.KhaiValue}";
            this.txtPm10Value.Text = $"미세먼지(PM10) 농도 {airMesure.Pm10Value}";
            this.txtPm25Value.Text = $"초미세먼지(PM25) 농도 {airMesure.Pm25Value}";
            // "2020-03-12 24:00"
            var dateTime = airMesure.dataTime;
            if (dateTime.Contains("24:"))
            {
                dateTime = dateTime.Replace("24:", "00:");
            }
            this.txtTime.Text = DateTime.Parse(dateTime).ToString("MMMM dd일 tt h:mm:ss");
            this.txtUpdateTime.Text = String.Format(" 갱신 : {0}", DateTime.Now.ToString("tt h:mm:ss"));

            var imgUrl = "http://m.airkorea.or.kr/images/ico_station4_";
            // 통합대기환경지수
            this.imgWeather.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.KhaiGrade}.png", UriKind.Absolute));
            // 미세먼지
            this.imgPm10Value.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.Pm10Grade1h}.png", UriKind.Absolute));
            // 초미세먼지
            this.imgPm25Value.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.Pm25Grade1h}.png", UriKind.Absolute));
            // background #0099CC
            this.mGrid.Background = this.airColor.CalSolidBrush(int.Parse(airMesure.KhaiValue));

            this.progressRing.IsActive = false;
        }

        private void BtnGetWeather_Click(object sender, RoutedEventArgs e)
        {
            this.DoSearch();
        }

        private void CurrentTime()
        {
            currentTimeTimer = new DispatcherTimer();
            currentTimeTimer.Tick += CurrentTimeCallback;
            currentTimeTimer.Interval = TimeSpan.FromMilliseconds(1000);
            currentTimeTimer.Start();
        }

        private void CurrentTimeCallback(object sender, object args)
        {
            this.txtCurrentTime.Text = DateTime.Now.ToString("yyyy년 MMMM dd일 dddd tt h:mm:ss");
        }

        private void AirTimerStart()
        {
            this.airTimer = new DispatcherTimer();
            this.airTimer.Tick += AirTimerCallback;
            this.airTimer.Interval = TimeSpan.FromMinutes(15);
            this.airTimer.Start();
        }

        private void FoodRecommend_Click(object sender, RoutedEventArgs e)
        {
            var foodRecommendService = new FoodRecommendService();
            foodRecommendService.Request();
        }

        private void AirTimerCallback(object sender, object args)
        {
            this.DoSearch();
        }
    }
}
