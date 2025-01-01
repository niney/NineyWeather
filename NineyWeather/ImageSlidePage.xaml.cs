using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace NineyWeather
{
    public sealed partial class ImageSlidePage : Page
    {
        private ObservableCollection<BitmapImage> images = new ObservableCollection<BitmapImage>();
        private int currentIndex = 0;
        private const string FolderPathKey = "SelectedFolderPath";

        public ImageSlidePage()
        {
            this.InitializeComponent();
            LoadImagesFromSavedFolder();
        }

        private async void LoadImagesFromSavedFolder()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(FolderPathKey))
            {
                string folderPath = localSettings.Values[FolderPathKey] as string;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                    await LoadImagesFromFolder(folder);
                }
            }
            else
            {
                await PickAndLoadImagesFromFolder();
            }
        }

        private async System.Threading.Tasks.Task PickAndLoadImagesFromFolder()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values[FolderPathKey] = folder.Path;
                await LoadImagesFromFolder(folder);
            }
        }

        private async System.Threading.Tasks.Task LoadImagesFromFolder(StorageFolder folder)
        {
            var files = await folder.GetFilesAsync();
            var imageFiles = files.Where(file => file.FileType == ".jpg" || file.FileType == ".jpeg" || file.FileType == ".png");

            foreach (var file in imageFiles)
            {
                try
                {
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        images.Add(bitmapImage);
                    }
                }
                catch (Exception ex)
                {
                    // 예외 처리: 이미지 파일을 로드할 수 없는 경우
                    System.Diagnostics.Debug.WriteLine($"이미지 파일을 로드할 수 없습니다: {file.Name}, 오류: {ex.Message}");
                }
            }

            if (images.Count > 0)
            {
                DisplayImage();
            }
        }

        private void DisplayImage()
        {
            if (images.Count > 0)
            {
                try
                {
                    ImageControl.Source = images[currentIndex];
                    SlideInAnimation.Begin();
                }
                catch (Exception ex)
                {
                    // 예외 처리: 이미지 파일을 표시할 수 없는 경우
                    System.Diagnostics.Debug.WriteLine($"이미지 파일을 표시할 수 없습니다: {ex.Message}");
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (images.Count > 0)
            {
                SlideOutAnimation.Completed += (s, a) =>
                {
                    currentIndex = (currentIndex - 1 + images.Count) % images.Count;
                    DisplayImage();
                };
                SlideOutAnimation.Begin();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (images.Count > 0)
            {
                SlideOutAnimation.Completed += (s, a) =>
                {
                    currentIndex = (currentIndex + 1) % images.Count;
                    DisplayImage();
                };
                SlideOutAnimation.Begin();
            }
        }
    }
}