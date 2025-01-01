using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    public class BusStop
    {
        public string RouteId { get; set; }
        public string RouteName { get; set; }
        public int Sequence { get; set; }
        public string NodeId { get; set; }
        public string ArsId { get; set; }
        public string StopName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class BusPage : Page
    {
        private Dictionary<string, List<BusStop>> busStops = new Dictionary<string, List<BusStop>>();
        private List<MapIcon> allMapIcons = new List<MapIcon>();
        private HashSet<MapIcon> currentMapIcons = new HashSet<MapIcon>();
        private DispatcherTimer updateTimer;

        public BusPage()
        {
            this.InitializeComponent();
            InitializeMap();
            InitializeUpdateTimer();
        }

        private async void InitializeMap()
        {
            MyMap.MapServiceToken = "YourMapServiceToken"; // 여기에 실제 맵 서비스 토큰을 입력하세요.
            var accessStatus = await Geolocator.RequestAccessAsync();

            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                var geoLocator = new Geolocator { DesiredAccuracyInMeters = 50 };
                Geoposition pos = await geoLocator.GetGeopositionAsync();

                MyMap.Center = pos.Coordinate.Point;
                MyMap.ZoomLevel = 12;
                MyMap.LandmarksVisible = true;
            }
            else
            {
                // 위치 접근이 허용되지 않은 경우 기본 위치 설정
                MyMap.Center = new Geopoint(new BasicGeoposition() { Latitude = 37.5665, Longitude = 126.9780 }); // 서울의 위도와 경도
                MyMap.ZoomLevel = 12;
                MyMap.LandmarksVisible = true;
            }

            MyMap.MapElementClick += MyMap_MapElementClick;

            await LoadBusStops();
            await Task.Run(() => LoadBusStopsByRoute());
        }

        private void InitializeUpdateTimer()
        {
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, object e)
        {
            UpdateMapIcons();
        }

        private async Task LoadBusStops()
        {
            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await folder.GetFolderAsync("Assets");
                var file = await assetsFolder.GetFileAsync("서울시버스정류소위치정보(20241209).xlsx");

                // 엑셀 파일 읽기
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트
                        var rows = worksheet.RowsUsed().Skip(1); // 첫 줄은 헤더이므로 건너뜁니다.

                        var mapIcons = new List<MapIcon>();

                        foreach (var row in rows)
                        {
                            try
                            {
                                var busStopId = row.Cell(1).GetValue<string>().Trim();
                                var arsId = row.Cell(2).GetValue<string>().Trim();
                                var busStopName = row.Cell(3).GetValue<string>().Trim();
                                if (double.TryParse(row.Cell(5).GetValue<string>().Trim(), out double latitude) && double.TryParse(row.Cell(4).GetValue<string>().Trim(), out double longitude))
                                {
                                    var location = new BasicGeoposition { Latitude = latitude, Longitude = longitude };
                                    var point = new Geopoint(location);

                                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        var mapIcon = new MapIcon
                                        {
                                            Location = point,
                                            Title = busStopName,
                                            ZIndex = 0,
                                            Tag = arsId
                                        };

                                        mapIcons.Add(mapIcon);
                                        //System.Diagnostics.Debug.WriteLine($"Parsed bus stop: {busStopName}");
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                // 각 줄의 파싱 중 발생한 예외 처리
                                System.Diagnostics.Debug.WriteLine($"Error parsing row: {row.RowNumber()}. Exception: {ex.Message}");
                            }
                        }

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            allMapIcons.AddRange(mapIcons);
                            UpdateMapIcons();
                            System.Diagnostics.Debug.WriteLine($"Loaded {mapIcons.Count} bus stops.");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 파일 읽기 중 발생한 예외 처리
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading file. Exception: {ex.Message}");
                });
            }
        }

        private async Task LoadBusStopsByRoute()
        {
            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await folder.GetFolderAsync("Assets");
                var file = await assetsFolder.GetFileAsync("서울시버스노선별정류소정보(20241209).xlsx");

                // 엑셀 파일 읽기
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트
                        var rows = worksheet.RowsUsed().Skip(1); // 첫 줄은 헤더이므로 건너뜁니다.

                        foreach (var row in rows)
                        {
                            try
                            {
                                var busStop = new BusStop
                                {
                                    RouteId = row.Cell(1).GetValue<string>().Trim(),
                                    RouteName = row.Cell(2).GetValue<string>().Trim(),
                                    Sequence = row.Cell(3).GetValue<int>(),
                                    NodeId = row.Cell(4).GetValue<string>().Trim(),
                                    ArsId = row.Cell(5).GetValue<string>().Trim(),
                                    StopName = row.Cell(6).GetValue<string>().Trim(),
                                    Longitude = row.Cell(7).GetValue<double>(),
                                    Latitude = row.Cell(8).GetValue<double>()
                                };

                                if (!busStops.ContainsKey(busStop.ArsId))
                                {
                                    busStops[busStop.ArsId] = new List<BusStop>();
                                }
                                busStops[busStop.ArsId].Add(busStop);
                            }
                            catch (Exception ex)
                            {
                                // 각 줄의 파싱 중 발생한 예외 처리
                                System.Diagnostics.Debug.WriteLine($"Error parsing row: {row.RowNumber()}. Exception: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 파일 읽기 중 발생한 예외 처리
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading file. Exception: {ex.Message}");
                });
            }
        }

        private void UpdateMapIcons()
        {
            if (MyMap.ZoomLevel < 15)
            {
                // 줌 레벨이 15 미만일 때는 아이콘을 모두 제거
                MyMap.MapElements.Clear();
                currentMapIcons.Clear();
                return;
            }

            var bounds = MyMap.GetBounds();

            var iconsToAdd = allMapIcons.Where(icon => bounds.Contains(icon.Location.Position)).ToList();
            var iconsToRemove = currentMapIcons.Except(iconsToAdd).ToList();

            foreach (var mapIcon in iconsToRemove)
            {
                MyMap.MapElements.Remove(mapIcon);
                currentMapIcons.Remove(mapIcon);
                System.Diagnostics.Debug.WriteLine($"Removed map icon: {mapIcon.Title}");
            }

            foreach (var mapIcon in iconsToAdd.Except(currentMapIcons))
            {
                MyMap.MapElements.Add(mapIcon);
                currentMapIcons.Add(mapIcon);
                System.Diagnostics.Debug.WriteLine($"Added map icon: {mapIcon.Title}");
            }
        }

        private void MyMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
        {
            foreach (var mapElement in args.MapElements)
            {
                if (mapElement is MapIcon mapIcon)
                {
                    TitleTextBlock.Text = mapIcon.Title;

                    if (busStops.TryGetValue(mapIcon.Tag as string, out var busStopList))
                    {
                        RouteNameListBox.Items.Clear();
                        foreach (var busStop in busStopList)
                        {
                            RouteNameListBox.Items.Add(busStop.RouteName);
                            System.Diagnostics.Debug.WriteLine($"Route: {busStop.RouteName}, Stop: {busStop.StopName}");
                        }
                    }

                    ShowDetails();
                }
            }
        }

        private void ShowDetails()
        {
            DetailsOverlay.Visibility = Visibility.Visible;
        }

        private void HideDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            DetailsOverlay.Visibility = Visibility.Collapsed;
        }
    }

    public static class MapControlExtensions
    {
        public static GeoboundingBox GetBounds(this MapControl mapControl)
        {
            mapControl.GetLocationFromOffset(new Windows.Foundation.Point(0, 0), out Geopoint nw);
            mapControl.GetLocationFromOffset(new Windows.Foundation.Point(mapControl.ActualWidth, mapControl.ActualHeight), out Geopoint se);

            return new GeoboundingBox(nw.Position, se.Position);
        }

        public static bool Contains(this GeoboundingBox box, BasicGeoposition position)
        {
            return position.Latitude <= box.NorthwestCorner.Latitude &&
                   position.Latitude >= box.SoutheastCorner.Latitude &&
                   position.Longitude >= box.NorthwestCorner.Longitude &&
                   position.Longitude <= box.SoutheastCorner.Longitude;
        }
    }
}
