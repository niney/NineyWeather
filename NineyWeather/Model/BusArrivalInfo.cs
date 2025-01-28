// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

using System;
using System.ComponentModel;

namespace NineyWeather
{
    public class BusArrivalInfo : INotifyPropertyChanged
    {
        private string arrmsg1;
        private string arrmsg2;
        private TimeSpan? remainingTime1;
        private TimeSpan? remainingTime2;
        private bool isFavorite;

        public string Arrmsg1
        {
            get => arrmsg1;
            set
            {
                if (arrmsg1 != value)
                {
                    arrmsg1 = value;
                    RemainingTime1 = ParseRemainingTime(arrmsg1);
                    OnPropertyChanged(nameof(Arrmsg1));
                }
            }
        }

        public string Arrmsg2
        {
            get => arrmsg2;
            set
            {
                if (arrmsg2 != value)
                {
                    arrmsg2 = value;
                    RemainingTime2 = ParseRemainingTime(arrmsg2);
                    OnPropertyChanged(nameof(Arrmsg2));
                }
            }
        }

        public string ArsId { get; set; }
        public string StId { get; set; }
        public string RtNm { get; set; }
        public string MkTm { get; set; }
        public int RouteType { get; set; }

        public TimeSpan? RemainingTime1
        {
            get => remainingTime1;
            set
            {
                if (remainingTime1 != value)
                {
                    remainingTime1 = value;
                    OnPropertyChanged(nameof(RemainingTime1));
                }
            }
        }

        public TimeSpan? RemainingTime2
        {
            get => remainingTime2;
            set
            {
                if (remainingTime2 != value)
                {
                    remainingTime2 = value;
                    OnPropertyChanged(nameof(RemainingTime2));
                }
            }
        }
        public bool IsFavorite { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static TimeSpan? ParseRemainingTime(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            var match = System.Text.RegularExpressions.Regex.Match(message, @"(\d+)분\s*(\d+)초");
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[1].Value);
                int seconds = int.Parse(match.Groups[2].Value);
                return new TimeSpan(0, minutes, seconds);
            }

            return null;
        }
    }

}
