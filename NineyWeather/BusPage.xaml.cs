using NineyWeather.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace NineyWeather
{
    public sealed partial class BusPage : Page
    {
        #region Fields and Constants
        private static readonly ResourceLoader RESOURCES = new ResourceLoader("apiResources");
        private readonly string BING_MAP_SERVICE_KEY = RESOURCES.GetString("bingMapServiceKey");
        private const string FAVORITES_SETTINGS_KEY = "BusFavorites";

        // 서비스
        private readonly BusService busService = new BusService();

        // 지도 관련
        private Dictionary<string, List<BusStop>> busStops = new Dictionary<string, List<BusStop>>();
        private List<MapIcon> allMapIcons = new List<MapIcon>();
        private HashSet<MapIcon> currentMapIcons = new HashSet<MapIcon>();
        private MapIcon selectedMapIcon;

        // 타이머
        private DispatcherTimer updateTimer;
        private DispatcherTimer busArrivalInfoTimer;

        // 캐시와 즐겨찾기
        private Dictionary<string, Dictionary<string, BusArrivalInfo>> busArrivalInfoCache = new Dictionary<string, Dictionary<string, BusArrivalInfo>>();
        private HashSet<string> favoriteBusRoutes;
        #endregion

        #region Initialization
        public BusPage()
        {
            this.InitializeComponent();
            InitializeMap();
            InitializeUpdateTimer();
            LoadFavorites();
        }

        private async void InitializeMap()
        {
            await InitializeMapLocation();
            MyMap.MapElementClick += MyMap_MapElementClick;
            await LoadBusStops();
            this.busStops = await busService.LoadBusStopsByRoute();
            SettingLoadLastMapIcon();
        }

        private async Task InitializeMapLocation()
        {
            MyMap.MapServiceToken = BING_MAP_SERVICE_KEY;
            var accessStatus = await Geolocator.RequestAccessAsync();

            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                var geoLocator = new Geolocator { DesiredAccuracyInMeters = 50 };
                var pos = await geoLocator.GetGeopositionAsync();
                MyMap.Center = pos.Coordinate.Point;
            }
            else
            {
                MyMap.Center = new Geopoint(new BasicGeoposition()
                {
                    Latitude = 37.5665,
                    Longitude = 126.9780
                });
            }

            MyMap.ZoomLevel = 12;
            MyMap.LandmarksVisible = true;
        }
        #endregion

        #region Map Operations
        private async Task LoadBusStops()
        {
            try
            {
                var busStopLocations = await busService.LoadBusStopLocations();
                var mapIcons = busService.CreateMapIcons(busStopLocations);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    allMapIcons.AddRange(mapIcons);
                    UpdateMapIcons();
                    System.Diagnostics.Debug.WriteLine($"Loaded {mapIcons.Count} bus stops.");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading bus stops: {ex.Message}");
                });
            }
        }

        private void UpdateMapIcons()
        {
            if (MyMap.ZoomLevel < 15)
            {
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

        private async void SettingLoadLastMapIcon()
        {
            var lastClickedMapIcon = busService.LoadLastClickedMapIcon();
            if (lastClickedMapIcon.HasValue)
            {
                MyMap.Center = new Geopoint(lastClickedMapIcon.Value.Position);
                MyMap.ZoomLevel = 18;

                var mapIcon = allMapIcons.FirstOrDefault(icon => icon.Tag as string == lastClickedMapIcon.Value.Tag);
                if (mapIcon != null)
                {
                    await HandleMapIconClick(mapIcon);
                }
            }
        }

        private async Task HandleMapIconClick(MapIcon mapIcon)
        {
            if (selectedMapIcon != null)
            {
                selectedMapIcon.Image = busService.CreateBusStopIcon(false);
            }

            selectedMapIcon = mapIcon;
            mapIcon.Image = busService.CreateBusStopIcon(true);

            busService.SaveLastClickedMapIcon(mapIcon.Location.Position, mapIcon.Tag as string);

            TitleTextBlock.Text = mapIcon.Title;
            await UpdateBusArrivalInfo(mapIcon.Tag as string);
            ShowDetails();
        }
        #endregion

        #region Timer Operations
        private void InitializeUpdateTimer()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void StartBusArrivalInfoTimer()
        {
            if (busArrivalInfoTimer != null)
            {
                busArrivalInfoTimer.Stop();
            }

            string arsId = selectedMapIcon?.Tag as string;

            busArrivalInfoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(40)
            };
            busArrivalInfoTimer.Tick += async (sender, e) => await UpdateBusArrivalInfo(arsId);
            busArrivalInfoTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, object e)
        {
            UpdateMapIcons();
            UpdateBusArrivalTimes();
        }
        #endregion

        #region Bus Arrival Information
        private async Task UpdateBusArrivalInfo(string arsId)
        {
            if (string.IsNullOrEmpty(arsId) || !busStops.TryGetValue(arsId, out var busStopList))
                return;

            var nodeId = busStopList[0].NodeId;
            var busArrivalInfos = await GetBusArrivalInfos(nodeId);
            if (busArrivalInfos == null) return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var allBusArrivalInfos = busArrivalInfos
                    .Where(x => busStopList.Any(b => b.RouteName == x.Key))
                    .Select(x =>
                    {
                        x.Value.RtNm = x.Key;
                        x.Value.IsFavorite = favoriteBusRoutes.Contains(x.Key);
                        return x.Value;
                    })
                    .ToList();

                var groupedBusArrivalInfos = new List<object>();

                var favorites = allBusArrivalInfos.Where(x => x.IsFavorite).ToList();
                if (favorites.Any())
                {
                    groupedBusArrivalInfos.Add(new { Name = "즐겨찾기", Items = favorites });
                }

                var nonFavorites = allBusArrivalInfos.Where(x => !x.IsFavorite)
                    .GroupBy(info => info.RouteType)
                    .Select(g => new { Name = GetRouteTypeName(g.Key), Items = g.ToList() });

                groupedBusArrivalInfos.AddRange(nonFavorites);

                RouteNameListView.ItemsSource = groupedBusArrivalInfos;
            });

            StartBusArrivalInfoTimer();
        }

        private async Task<Dictionary<string, BusArrivalInfo>> GetBusArrivalInfos(string nodeId)
        {
            try
            {
                var busArrivalInfos = await busService.LoadBusArrivalInfoFromApi(nodeId);
                var newMkTm = busArrivalInfos.Values.FirstOrDefault()?.MkTm;

                if (!IsSameArrivalInfo(nodeId, newMkTm))
                {
                    busArrivalInfoCache[nodeId] = busArrivalInfos;
                }
                else if (!busArrivalInfoCache.ContainsKey(nodeId))
                {
                    busArrivalInfoCache[nodeId] = busArrivalInfos;
                }
                else
                {
                    busArrivalInfos = busArrivalInfoCache[nodeId];
                }

                return busArrivalInfos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"버스 도착 정보 조회 실패: {ex.Message}");
                return null;
            }
        }

        private void UpdateBusArrivalTimes()
        {
            foreach (var nodeId in busArrivalInfoCache.Keys)
            {
                var arrivalInfos = busArrivalInfoCache[nodeId];
                foreach (var routeName in arrivalInfos.Keys)
                {
                    var busInfo = arrivalInfos[routeName];

                    if (busInfo.RemainingTime1.HasValue)
                    {
                        var newTime1 = busInfo.RemainingTime1.Value.Subtract(TimeSpan.FromSeconds(1));
                        busInfo.Arrmsg1 = busService.ReplaceTimeInMessage(busInfo.Arrmsg1, newTime1);
                    }

                    if (busInfo.RemainingTime2.HasValue)
                    {
                        var newTime2 = busInfo.RemainingTime2.Value.Subtract(TimeSpan.FromSeconds(1));
                        busInfo.Arrmsg2 = busService.ReplaceTimeInMessage(busInfo.Arrmsg2, newTime2);
                    }
                }
            }
        }

        private bool IsSameArrivalInfo(string nodeId, string newMkTm)
        {
            var currentTag = RouteNameListView.Tag as Dictionary<string, string> ?? new Dictionary<string, string>();
            bool isSame = currentTag.TryGetValue(nodeId, out var currentMkTm) && currentMkTm == newMkTm;

            currentTag[nodeId] = newMkTm;
            RouteNameListView.Tag = currentTag;

            return isSame;
        }

        private string GetRouteTypeName(int routeType)
        {
            return busService.GetRouteTypeName(routeType);
        }
        #endregion

        #region Favorites
        private void LoadFavorites()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var favoritesString = localSettings.Values[FAVORITES_SETTINGS_KEY] as string ?? "";
            favoriteBusRoutes = new HashSet<string>(favoritesString.Split(',').Where(s => !string.IsNullOrEmpty(s)));
        }

        private void SaveFavorites()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[FAVORITES_SETTINGS_KEY] = string.Join(",", favoriteBusRoutes);
        }
        #endregion

        #region UI Event Handlers
        private async void MyMap_MapElementClick(MapControl sender, MapElementClickEventArgs args)
        {
            if (!(args.MapElements.FirstOrDefault() is MapIcon mapIcon)) return;
            await HandleMapIconClick(mapIcon);
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var busInfo = button.DataContext as BusArrivalInfo;

            if (busInfo.IsFavorite)
            {
                favoriteBusRoutes.Remove(busInfo.RtNm);
            }
            else
            {
                favoriteBusRoutes.Add(busInfo.RtNm);
            }

            busInfo.IsFavorite = !busInfo.IsFavorite;
            SaveFavorites();

            var arsId = selectedMapIcon?.Tag as string;
            if (!string.IsNullOrEmpty(arsId))
            {
                _ = UpdateBusArrivalInfo(arsId);
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
        #endregion
    }

    #region Extensions
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
    #endregion
}