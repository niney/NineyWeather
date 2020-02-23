using NineyWeather.Service;
using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x412에 나와 있습니다.

namespace NineyWeather
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string lat, lng;
        private readonly AirSerivce airSerivce = new AirSerivce();

        public MainPage()
        {
            this.InitializeComponent();
            this.DoSearch();
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
            var airMesure = await this.airSerivce.Request(addressName); // "서울특별시 강남구 역삼로92길 7"

            this.txtCity.Text = addressName;
            //this.txtSo2Value.Text = $"아황산가스 농도 {airMesure.So2Value}";
            //this.txtCo2Value.Text = $"일산화탄소 농도 {airMesure.CoValue}";
            this.txtKhaiValue.Text = $"통합대기환경지수 {airMesure.KhaiValue}";
            this.txtPm10Value.Text = $"미세먼지(PM10) 농도 {airMesure.Pm10Value}";
            this.txtPm25Value.Text = $"초미세먼지(PM25) 농도 {airMesure.Pm25Value}";
            this.txtTime.Text = airMesure.dataTime;

            var imgUrl = "http://m.airkorea.or.kr/images/ico_station4_";
            // 통합대기환경지수
            this.imgWeather.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.KhaiGrade}.png", UriKind.Absolute));
            // 미세먼지
            this.imgPm10Value.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.Pm10Grade1h}.png", UriKind.Absolute));
            // 초미세먼지
            this.imgPm25Value.Source = new BitmapImage(new Uri($"{imgUrl}{airMesure.Pm25Grade1h}.png", UriKind.Absolute));

            this.progressRing.IsActive = false;
        }

        private async void BtnGetWeather_Click(object sender, RoutedEventArgs e)
        {
            this.DoSearch();
        }
    }
}
