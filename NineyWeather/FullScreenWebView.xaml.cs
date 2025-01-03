﻿using System;
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

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace NineyWeather
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class FullScreenWebView : UserControl
    {
        private Popup _popup;

        public FullScreenWebView(string url)
        {
            this.InitializeComponent();

            this.Width = Window.Current.Bounds.Width;
            this.Height = Window.Current.Bounds.Height;

            NineyWebView.Navigate(new Uri(url));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }
        }

        public void Show()
        {
            _popup = new Popup
            {
                Child = this,
                IsLightDismissEnabled = true
            };
            _popup.IsOpen = true;
        }
    }
}
