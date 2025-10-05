using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NineyWeather
{
    public sealed partial class RoulettePage : Page
    {
        private readonly Dictionary<string, string[]> cuisineMenus = new Dictionary<string, string[]>
        {
            {
                "한식", new string[]
                {
                    "김치찌개", "된장찌개", "비빔밥", "떡볶이", "삼겹살",
                    "부대찌개", "칼국수", "순두부찌개", "국밥", "돼지갈비",
                    "냉면", "보쌈", "제육볶음", "닭갈비", "갈비탕"
                }
            },
            {
                "중식", new string[]
                {
                    "짜장면", "짬뽕", "탕수육", "마파두부", "양장피",
                    "깐풍기", "볶음밥", "울면", "팔보채", "유린기",
                    "고추잡채", "동파육", "양꼬치", "마라탕", "훠궈"
                }
            },
            {
                "양식", new string[]
                {
                    "파스타", "피자", "스테이크", "햄버거", "리조또",
                    "샐러드", "오믈렛", "치킨", "샌드위치", "그라탕",
                    "라자냐", "필라프", "감자튀김", "파에야", "비프스튜"
                }
            }
        };

        private string[] currentFoodItems;
        private int numSegments;
        private double segAngle;           // 각 세그먼트의 각도 (라디안)
        private double startAngle = -Math.PI / 2; // 시작각도 (위쪽이 -90°)
        private bool spinning = false;
        private double spinVelocity = 0;   // 회전 속도 (rad/ms)
        private double deceleration = 0.000002; // 감속 (rad/ms²)
        private DateTime lastRenderTime;
        private readonly Random random = new Random();
        private DispatcherTimer animationTimer;

        public RoulettePage()
        {
            this.InitializeComponent();

            // 초기 메뉴 설정 (한식)
            UpdateMenuItems("한식");

            // CanvasControl의 Draw 이벤트에 핸들러 등록
            wheelCanvas.Draw += WheelCanvas_Draw;

            // 애니메이션 타이머 초기화
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(16); // 약 60fps
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void UpdateMenuItems(string cuisine)
        {
            if (cuisineMenus.TryGetValue(cuisine, out string[] menuItems))
            {
                currentFoodItems = menuItems;
                numSegments = currentFoodItems.Length;
                segAngle = 2 * Math.PI / numSegments;

                // 룰렛 다시 그리기
                if (wheelCanvas != null)
                {
                    wheelCanvas.Invalidate();
                }
            }
        }

        private void CuisineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cuisineComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedCuisine = selectedItem.Content.ToString();
                UpdateMenuItems(selectedCuisine);
            }
        }

        private void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (spinning) return;

            spinning = true;
            spinButton.IsEnabled = false;
            resultTextBlock.Opacity = 0;
            resultTextBlock.Text = string.Empty;
            lastRenderTime = DateTime.Now;

            // 초기 회전 속도 설정
            spinVelocity = 0.005 + random.NextDouble() * 0.003;

            // 애니메이션 타이머 시작
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, object e)
        {
            DateTime currentTime = DateTime.Now;
            double dt = (currentTime - lastRenderTime).TotalMilliseconds;
            lastRenderTime = currentTime;

            // 감속 후 속도 업데이트
            spinVelocity = Math.Max(spinVelocity - deceleration * dt, 0);
            startAngle += spinVelocity * dt;

            // CanvasControl 강제 갱신
            wheelCanvas.Invalidate();

            // 회전이 멈추면 애니메이션 종료
            if (spinVelocity <= 0)
            {
                animationTimer.Stop();
                spinning = false;
                spinButton.IsEnabled = true;

                double pointerAngle = -Math.PI / 2;
                double relativeAngle = Mod(pointerAngle - startAngle, 2 * Math.PI);
                int winningIndex = (int)(relativeAngle / segAngle);
                resultTextBlock.Text = "오늘의 점심 메뉴: " + currentFoodItems[winningIndex];
                resultTextBlock.Opacity = 1;
            }
        }

        private double Mod(double n, double m)
        {
            return ((n % m) + m) % m;
        }

        private void WheelCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            CanvasDrawingSession ds = args.DrawingSession;
            float width = (float)sender.ActualWidth;
            float height = (float)sender.ActualHeight;
            float centerX = width / 2;
            float centerY = height / 2;
            float radius = Math.Min(centerX, centerY) - 30;

            // 각 세그먼트를 그리기
            for (int i = 0; i < numSegments; i++)
            {
                double angle = startAngle + i * segAngle;
                double hue = i * 360.0 / numSegments;
                Color fillColor = HslToColor(hue, 0.7, 0.6);

                // 파이 조각(세그먼트) 그리기
                using (var builder = new CanvasPathBuilder(sender))
                {
                    builder.BeginFigure(new Vector2(centerX, centerY));
                    builder.AddArc(new Vector2(centerX, centerY),
                                   radius, radius,
                                   (float)angle,
                                   (float)segAngle);
                    builder.EndFigure(CanvasFigureLoop.Closed);
                    using (CanvasGeometry geometry = CanvasGeometry.CreatePath(builder))
                    {
                        ds.FillGeometry(geometry, fillColor);
                        ds.DrawGeometry(geometry, Colors.White, 1);
                    }
                }

                // 텍스트 그리기
                float textRadius = radius * 0.7f;
                float textAngle = (float)(angle + segAngle / 2);
                float textX = centerX + textRadius * (float)Math.Cos(textAngle);
                float textY = centerY + textRadius * (float)Math.Sin(textAngle);

                using (var textFormat = new CanvasTextFormat
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = CanvasHorizontalAlignment.Center,
                    VerticalAlignment = CanvasVerticalAlignment.Center,
                    FontFamily = "Malgun Gothic"
                })
                {
                    using (var textLayout = new CanvasTextLayout(ds, currentFoodItems[i], textFormat, 0, 0))
                    {
                        Matrix3x2 originalTransform = ds.Transform;
                        ds.Transform = Matrix3x2.CreateTranslation(-textX, -textY) *
                                     Matrix3x2.CreateRotation(textAngle + (float)Math.PI / 2) *
                                     Matrix3x2.CreateTranslation(textX, textY);

                        using (var blackBrush = new CanvasSolidColorBrush(ds, Colors.Black))
                        {
                            ds.DrawTextLayout(
                                textLayout,
                                new Vector2(
                                    textX - (float)(textLayout.LayoutBounds.Width / 2),
                                    textY - (float)(textLayout.LayoutBounds.Height / 2)
                                ),
                                blackBrush
                            );
                        }

                        ds.Transform = originalTransform;
                    }
                }
            }

            // 중앙 원 그리기
            float centerCircleRadius = radius * 0.1f;
            ds.FillCircle(centerX, centerY, centerCircleRadius, Colors.White);
            ds.DrawCircle(centerX, centerY, centerCircleRadius, Colors.Gray);

            // 외곽 원 그리기
            ds.DrawCircle(centerX, centerY, radius, Colors.White, 2);

            // 포인터 그리기
            float pointerTipY = 10;
            float pointerWidth = 20;
            float pointerHeight = 25;
            using (var pointerBuilder = new CanvasPathBuilder(sender))
            {
                pointerBuilder.BeginFigure(new Vector2(centerX - pointerWidth / 2, pointerTipY));
                pointerBuilder.AddLine(new Vector2(centerX + pointerWidth / 2, pointerTipY));
                pointerBuilder.AddLine(new Vector2(centerX, pointerTipY + pointerHeight));
                pointerBuilder.EndFigure(CanvasFigureLoop.Closed);
                using (var pointerGeometry = CanvasGeometry.CreatePath(pointerBuilder))
                {
                    ds.FillGeometry(pointerGeometry, Colors.Red);
                    ds.DrawGeometry(pointerGeometry, Colors.DarkRed, 2);
                }
            }
        }

        private Color HslToColor(double h, double s, double l)
        {
            h = h % 360;
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;
            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);
            return Color.FromArgb(255, r, g, b);
        }
    }
}