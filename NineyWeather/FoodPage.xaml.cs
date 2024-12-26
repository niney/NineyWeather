using NineyWeather.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class FoodPage : Page
    {
        private readonly FoodRecommendService foodService = new FoodRecommendService();
        private bool isLoading = false;
        private int currentPage = 1;
        private readonly ObservableCollection<FoodItem> foods;
        private bool useFullScreenWebView = false; // 옵션 플래그

        public FoodPage()
        {
            this.InitializeComponent();
            foods = new ObservableCollection<FoodItem>(foodService.Request());
            this.foodListView.ItemsSource = foods;
            this.foodGridView.ItemsSource = foods;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CheckAndLoadMoreItems();
        }

        private void FoodList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FoodItem foodItem)
            {
                var recipeUri = $"https://www.10000recipe.com/recipe/view.html?seq={foodItem.Id}";

                if (useFullScreenWebView)
                {
                    var fullScreenView = new FullScreenWebView(recipeUri);
                    fullScreenView.Show();
                }
                else
                {
                    Frame.Navigate(typeof(RecipeDetailPage), recipeUri);
                }
            }
        }

        private void FoodList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var gridViewWidth = foodGridView.ActualWidth - foodGridView.Margin.Left; // - foodList.Padding.Left - foodList.Padding.Right;
            int columns = CalculateColumns(gridViewWidth);
            var itemWidth = gridViewWidth / columns;
            var itemHeight = itemWidth * 0.875;

            if (foodGridView.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = itemWidth;
                wrapGrid.ItemHeight = itemHeight;
            }
        }

        private int CalculateColumns(double gridViewWidth)
        {
            if (gridViewWidth > 1200)
                return 4;
            else if (gridViewWidth > 800)
                return 3;
            else
                return 2;
        }

        private void LoadMoreItems()
        {
            if (isLoading) return;

            isLoading = true;
            try
            {
                var newItems = foodService.RequestReco(currentPage);
                if (newItems != null && newItems.Any())
                {
                    foreach (var item in newItems)
                    {
                        foods.Add(item);
                    }
                    currentPage++;
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 50)
                {
                    LoadMoreItems();
                }
            }
        }

        private void CheckAndLoadMoreItems()
        {
            if (foodGridView.ItemsPanelRoot is ItemsWrapGrid)
            {
                var scrollViewer = foodGridView.GetDescendantsOfType<ScrollViewer>().FirstOrDefault();
                if (scrollViewer != null && scrollViewer.ScrollableHeight == 0)
                {
                    LoadMoreItems();
                }
            }
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            foodListView.Visibility = Visibility.Visible;
            foodGridView.Visibility = Visibility.Collapsed;
        }

        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            foodListView.Visibility = Visibility.Collapsed;
            foodGridView.Visibility = Visibility.Visible;
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static IEnumerable<T> GetDescendantsOfType<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var descendant in GetDescendantsOfType<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
