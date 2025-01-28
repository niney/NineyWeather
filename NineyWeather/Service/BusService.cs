using ClosedXML.Excel;
using NineyWeather.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Maps;

namespace NineyWeather.Service
{
    internal class BusService
    {
        private static readonly ResourceLoader RESOURCES = new ResourceLoader("apiResources");
        private readonly string BUS_SERVICE_KEY = RESOURCES.GetString("busServiceKey");
        
        public async Task<List<BusStopLocation>> LoadBusStopLocations()
        {
            var busStopLocations = new List<BusStopLocation>();

            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await folder.GetFolderAsync("Assets");
                var file = await assetsFolder.GetFileAsync("서울시버스정류소위치정보(20241209).xlsx");

                using (var stream = await file.OpenStreamForReadAsync())
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        try
                        {
                            if (TryParseBusStopLocation(row, out var busStopLocation))
                            {
                                busStopLocations.Add(busStopLocation);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing row: {row.RowNumber()}. Exception: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading file. Exception: {ex.Message}");
            }

            return busStopLocations;
        }

        private bool TryParseBusStopLocation(IXLRow row, out BusStopLocation location)
        {
            location = null;
            try
            {
                var busStopId = row.Cell(1).GetValue<string>().Trim();
                var arsId = row.Cell(2).GetValue<string>().Trim();
                var busStopName = row.Cell(3).GetValue<string>().Trim();

                if (double.TryParse(row.Cell(5).GetValue<string>().Trim(), out double latitude) &&
                    double.TryParse(row.Cell(4).GetValue<string>().Trim(), out double longitude))
                {
                    location = new BusStopLocation
                    {
                        BusStopId = busStopId,
                        ArsId = arsId,
                        BusStopName = busStopName,
                        Latitude = latitude,
                        Longitude = longitude
                    };
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing bus stop location: {ex.Message}");
            }
            return false;
        }

        public async Task<Dictionary<string, List<BusStop>>> LoadBusStopsByRoute()
        {
            var busStops = new Dictionary<string, List<BusStop>>();

            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await folder.GetFolderAsync("Assets");
                var file = await assetsFolder.GetFileAsync("서울시버스노선별정류소정보(20241209).xlsx");

                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1);

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
                                System.Diagnostics.Debug.WriteLine($"Error parsing row: {row.RowNumber()}. Exception: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading file. Exception: {ex.Message}");
            }

            return busStops;
        }

        public async Task<Dictionary<string, BusArrivalInfo>> LoadBusArrivalInfo()
        {
            var busArrivalInfos = new Dictionary<string, BusArrivalInfo>();

            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var assetsFolder = await folder.GetFolderAsync("Assets");
                var file = await assetsFolder.GetFileAsync("getLowArrInfoByStId.xml");

                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var xdoc = XDocument.Load(stream);
                    var items = xdoc.Descendants("itemList");

                    foreach (var item in items)
                    {
                        var busRouteAbrv = item.Element("busRouteAbrv")?.Value;
                        if (busRouteAbrv != null)
                        {
                            var busArrivalInfo = new BusArrivalInfo
                            {
                                Arrmsg1 = item.Element("arrmsg1")?.Value,
                                Arrmsg2 = item.Element("arrmsg2")?.Value,
                                ArsId = item.Element("arsId")?.Value,
                                StId = item.Element("stId")?.Value
                            };

                            busArrivalInfos[busRouteAbrv] = busArrivalInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading XML file. Exception: {ex.Message}");
            }

            return busArrivalInfos;
        }

        public async Task<Dictionary<string, BusArrivalInfo>> LoadBusArrivalInfoFromApi(string stId)
        {
            var busArrivalInfos = new Dictionary<string, BusArrivalInfo>();
            var cacheDirectory = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            var cacheFilePath = Path.Combine(cacheDirectory, $"BusArrivalInfo_{stId}.cache");
            var cacheDuration = TimeSpan.FromMinutes(1);

            try
            {
                // Delete expired cache files
                var cacheFiles = Directory.GetFiles(cacheDirectory, "BusArrivalInfo_*.cache");
                foreach (var file in cacheFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (DateTime.Now - fileInfo.LastWriteTime >= cacheDuration)
                    {
                        File.Delete(file);
                    }
                }

                // Check if the current cache file is valid
                if (File.Exists(cacheFilePath))
                {
                    var cacheFileInfo = new FileInfo(cacheFilePath);
                    if (DateTime.Now - cacheFileInfo.LastWriteTime < cacheDuration)
                    {
                        var cachedData = await File.ReadAllTextAsync(cacheFilePath);
                        busArrivalInfos = DeserializeBusArrivalInfo(cachedData);
                        return busArrivalInfos;
                    }
                }

                // Fetch data from API
                var apiUrl = $"http://ws.bus.go.kr/api/rest/arrive/getLowArrInfoByStId?serviceKey={BUS_SERVICE_KEY}&stId={stId}";
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var xdoc = XDocument.Parse(response);
                    var items = xdoc.Descendants("itemList");

                    foreach (var item in items)
                    {
                        var busRouteAbrv = item.Element("busRouteAbrv")?.Value;
                        if (busRouteAbrv != null)
                        {
                            var busArrivalInfo = new BusArrivalInfo
                            {
                                Arrmsg1 = item.Element("arrmsg1")?.Value,
                                Arrmsg2 = item.Element("arrmsg2")?.Value,
                                ArsId = item.Element("arsId")?.Value,
                                StId = item.Element("stId")?.Value,
                                MkTm = item.Element("mkTm")?.Value,
                                RouteType = int.TryParse(item.Element("routeType")?.Value, out var routeType) ? routeType : 0
                            };

                            busArrivalInfos[busRouteAbrv] = busArrivalInfo;
                        }
                    }

                    var serializedData = SerializeBusArrivalInfo(busArrivalInfos);
                    await File.WriteAllTextAsync(cacheFilePath, serializedData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching data from API. Exception: {ex.Message}");
            }

            return busArrivalInfos;
        }

        private string SerializeBusArrivalInfo(Dictionary<string, BusArrivalInfo> busArrivalInfos)
        {
            var xdoc = new XDocument(
                new XElement("BusArrivalInfos",
                    busArrivalInfos.Select(kvp =>
                        new XElement("BusArrivalInfo",
                            new XElement("busRouteAbrv", kvp.Key),
                            new XElement("arrmsg1", kvp.Value.Arrmsg1),
                            new XElement("arrmsg2", kvp.Value.Arrmsg2),
                            new XElement("arsId", kvp.Value.ArsId),
                            new XElement("stId", kvp.Value.StId),
                            new XElement("mkTm", kvp.Value.MkTm),
                            new XElement("routeType", kvp.Value.RouteType) // routeType 필드 추가
                        )
                    )
                )
            );
            return xdoc.ToString();
        }

        private Dictionary<string, BusArrivalInfo> DeserializeBusArrivalInfo(string data)
        {
            var xdoc = XDocument.Parse(data);
            var busArrivalInfos = new Dictionary<string, BusArrivalInfo>();
            var items = xdoc.Descendants("BusArrivalInfo");

            foreach (var item in items)
            {
                var busRouteAbrv = item.Element("busRouteAbrv")?.Value;
                if (busRouteAbrv != null)
                {
                    var busArrivalInfo = new BusArrivalInfo
                    {
                        Arrmsg1 = item.Element("arrmsg1")?.Value,
                        Arrmsg2 = item.Element("arrmsg2")?.Value,
                        ArsId = item.Element("arsId")?.Value,
                        StId = item.Element("stId")?.Value,
                        MkTm = item.Element("mkTm")?.Value,
                        RouteType = int.TryParse(item.Element("routeType")?.Value, out var routeType) ? routeType : 0
                    };

                    busArrivalInfos[busRouteAbrv] = busArrivalInfo;
                }
            }

            return busArrivalInfos;
        }

        public List<MapIcon> CreateMapIcons(List<BusStopLocation> busStopLocations)
        {
            var streamReference = CreateBusStopIcon();

            return busStopLocations.Select(location => new MapIcon
            {
                Location = new Geopoint(new BasicGeoposition
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                }),
                Title = location.BusStopName,
                NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 0.5),
                ZIndex = 0,
                Tag = location.ArsId,
                Image = streamReference,
                CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible
            }).ToList();
        }

        public RandomAccessStreamReference CreateBusStopIcon(bool isSelected = false)
        {
            string svgString = isSelected
                ? @"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 20 20'>
            <circle cx='10' cy='10' r='9' fill='white' opacity='0.9'/>
            <circle cx='10' cy='10' r='8' fill='#e53935'/>
            <path d='M7 7h6v4H7z M6 12h8 M7 13h1 M12 13h1' stroke='white' stroke-width='1.2' fill='none'/>
            <circle cx='10' cy='10' r='9' stroke='#e53935' stroke-width='2' fill='none'/>
        </svg>"
                : @"<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' viewBox='0 0 20 20'>
            <circle cx='10' cy='10' r='9' fill='white' opacity='0.9'/>
            <circle cx='10' cy='10' r='8' fill='#1e88e5'/>
            <path d='M7 7h6v4H7z M6 12h8 M7 13h1 M12 13h1' stroke='white' stroke-width='1.2' fill='none'/>
        </svg>";

            var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteString(svgString);
                writer.StoreAsync().GetResults();
            }

            return RandomAccessStreamReference.CreateFromStream(stream);
        }

        public string ReplaceTimeInMessage(string message, TimeSpan newTime)
        {
            var match = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)분\s*(\d+)초");
            if (match.Success)
            {
                var newTimeString = $"{newTime.Minutes}분 {newTime.Seconds}초";
                return message.Replace(match.Value, newTimeString);
            }
            return message;
        }

        public string GetRouteTypeName(int routeType)
        {
            switch (routeType)
            {
                case 1:
                    return "공항";
                case 2:
                    return "마을";
                case 3:
                    return "간선";
                case 4:
                    return "지선";
                case 5:
                    return "순환";
                case 6:
                    return "광역";
                case 7:
                    return "인천";
                case 8:
                    return "경기";
                case 9:
                    return "폐지";
                case 0:
                    return "공용";
                default:
                    return "기타";
            }
        }

        public void SaveLastClickedMapIcon(BasicGeoposition position, string tag)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["LastClickedLatitude"] = position.Latitude;
            localSettings.Values["LastClickedLongitude"] = position.Longitude;
            localSettings.Values["LastClickedTag"] = tag;
        }

        public (BasicGeoposition Position, string Tag)? LoadLastClickedMapIcon()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("LastClickedLatitude") &&
                localSettings.Values.ContainsKey("LastClickedLongitude") &&
                localSettings.Values.ContainsKey("LastClickedTag"))
            {
                return (
                    new BasicGeoposition
                    {
                        Latitude = (double)localSettings.Values["LastClickedLatitude"],
                        Longitude = (double)localSettings.Values["LastClickedLongitude"]
                    },
                    (string)localSettings.Values["LastClickedTag"]
                );
            }
            return null;
        }
    }
}