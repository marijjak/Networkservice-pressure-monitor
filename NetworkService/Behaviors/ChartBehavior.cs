using NetworkService.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Behaviors
{
    public static class ChartBehavior
    {
        public static readonly DependencyProperty MeasurementsProperty =
            DependencyProperty.RegisterAttached(
                "Measurements",
                typeof(IEnumerable<MeasurementEntry>),
                typeof(ChartBehavior),
                new PropertyMetadata(null, OnMeasurementsChanged));

        public static void SetMeasurements(DependencyObject o, IEnumerable<MeasurementEntry> v) =>
            o.SetValue(MeasurementsProperty, v);

        public static IEnumerable<MeasurementEntry> GetMeasurements(DependencyObject o) =>
            (IEnumerable<MeasurementEntry>)o.GetValue(MeasurementsProperty);

        private static readonly DependencyProperty HookedProperty =
            DependencyProperty.RegisterAttached(
                "Hooked", typeof(bool), typeof(ChartBehavior), new PropertyMetadata(false));

        private static void OnMeasurementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Canvas canvas)) return;

            if (!(bool)canvas.GetValue(HookedProperty))
            {
                canvas.SetValue(HookedProperty, true);
                canvas.SizeChanged += (s, _) => Draw(canvas);
                canvas.Loaded += (s, _) => Draw(canvas);
            }
            Draw(canvas);
        }

        private static void Draw(Canvas canvas)
        {
            canvas.Children.Clear();

            var data = GetMeasurements(canvas)?.ToList();
            if (data == null || data.Count == 0)
            {
                DrawEmptyState(canvas);
                return;
            }

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            if (w < 20 || h < 20) return;

            double offsetX = 42;
            double offsetY = 20;
            double bottomPad = 28;
            double rightPad = 10;
            double chartW = w - offsetX - rightPad;
            double chartH = h - offsetY - bottomPad;

            double maxMeasured = data.Max(m => m.Value);
            double maxVal = System.Math.Max(20.0, System.Math.Ceiling(maxMeasured * 1.15 / 5) * 5);

            var gridColor = Color.FromRgb(241, 245, 249);
            var axisColor = Color.FromRgb(148, 163, 184);

            for (int tick = 0; tick <= 4; tick++)
            {
                double tickVal = maxVal * tick / 4;
                double tickY = offsetY + chartH - (tickVal / maxVal) * chartH;

                canvas.Children.Add(new Line
                {
                    X1 = offsetX, Y1 = tickY,
                    X2 = offsetX + chartW, Y2 = tickY,
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = 1
                });

                var yLabel = new TextBlock
                {
                    Text = tickVal.ToString("0"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(axisColor)
                };
                Canvas.SetRight(yLabel, w - offsetX + 4);
                Canvas.SetTop(yLabel, tickY - 7);
                canvas.Children.Add(yLabel);
            }

            double minValidY = offsetY + chartH - (16.0 / maxVal) * chartH;
            double maxValidY = offsetY + chartH - (5.0 / maxVal) * chartH;
            double rangeHeight = maxValidY - minValidY;
            if (rangeHeight > 0)
            {
                var rangeBand = new Rectangle
                {
                    Width = chartW,
                    Height = rangeHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(22, 79, 70, 229))
                };
                Canvas.SetLeft(rangeBand, offsetX);
                Canvas.SetTop(rangeBand, minValidY);
                canvas.Children.Add(rangeBand);

                canvas.Children.Add(new Line
                {
                    X1 = offsetX, Y1 = minValidY, X2 = offsetX + chartW, Y2 = minValidY,
                    Stroke = new SolidColorBrush(Color.FromArgb(90, 79, 70, 229)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 3 }
                });
                canvas.Children.Add(new Line
                {
                    X1 = offsetX, Y1 = maxValidY, X2 = offsetX + chartW, Y2 = maxValidY,
                    Stroke = new SolidColorBrush(Color.FromArgb(90, 79, 70, 229)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 3 }
                });
            }

            canvas.Children.Add(new Line
            {
                X1 = offsetX, Y1 = offsetY, X2 = offsetX, Y2 = offsetY + chartH,
                Stroke = new SolidColorBrush(axisColor), StrokeThickness = 1.5
            });
            canvas.Children.Add(new Line
            {
                X1 = offsetX, Y1 = offsetY + chartH,
                X2 = offsetX + chartW, Y2 = offsetY + chartH,
                Stroke = new SolidColorBrush(axisColor), StrokeThickness = 1.5
            });

            int count = data.Count;
            double slotW = chartW / count;
            double barW = slotW * 0.5;
            double barOffset = (slotW - barW) / 2;

            var validColor = Color.FromRgb(79, 70, 229);
            var invalidColor = Color.FromRgb(217, 119, 6);

            for (int i = 0; i < count; i++)
            {
                var entry = data[i];
                bool oor = entry.IsOutOfRange;
                double barH = System.Math.Min((entry.Value / maxVal) * chartH, chartH);
                double x = offsetX + i * slotW + barOffset;
                double y = offsetY + chartH - barH;

                var barColor = oor ? invalidColor : validColor;

                var rect = new Rectangle
                {
                    Width = barW,
                    Height = System.Math.Max(barH, 2),
                    Fill = new SolidColorBrush(barColor),
                    RadiusX = 3, RadiusY = 3
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                canvas.Children.Add(rect);

                var valText = new TextBlock
                {
                    Text = $"{entry.Value:0.#} MP",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(oor ? invalidColor : validColor)
                };
                Canvas.SetLeft(valText, x - 2);
                Canvas.SetTop(valText, System.Math.Max(y - 18, offsetY));
                canvas.Children.Add(valText);

                var timeText = new TextBlock
                {
                    Text = entry.Time.ToString("HH:mm"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(axisColor)
                };
                Canvas.SetLeft(timeText, x + barW / 2 - 15);
                Canvas.SetTop(timeText, offsetY + chartH + 6);
                canvas.Children.Add(timeText);
            }
        }

        private static void DrawEmptyState(Canvas canvas)
        {
            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;

            var msg = new TextBlock
            {
                Text = "Odaberite entitet za prikaz merenja",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Canvas.SetLeft(msg, w / 2 - 130);
            Canvas.SetTop(msg, h / 2 - 10);
            canvas.Children.Add(msg);
        }
    }
}
