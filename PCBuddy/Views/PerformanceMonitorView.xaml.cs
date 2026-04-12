using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PCBuddy.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace PCBuddy.Views
{
    public sealed partial class PerformanceMonitorView : Page
    {
        private CancellationTokenSource? _cts;
        private bool _isMonitoring = false;
        private MetricHistory _history = new();
        private readonly object _lock = new();

        public PerformanceMonitorView()
        {
            InitializeComponent();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Button clicked!");
            
            if (_isMonitoring)
            {
                StopMonitoring();
            }
            else
            {
                StartMonitoring();
            }
        }

        private async void StartMonitoring()
        {
            _isMonitoring = true;
            _cts = new CancellationTokenSource();
            _history.Clear();

            StartStopButton.Content = "Stop Monitoring";
            StatusText.Text = "Starting...";
            
            try
            {
                await PerformanceMonitorService.StartMonitoringAsync(OnMetricsUpdate, 25, _cts.Token);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void StopMonitoring()
        {
            _isMonitoring = false;
            _cts?.Cancel();

            if (StartStopButton != null)
                StartStopButton.Content = "Start Monitoring";
            if (StatusText != null)
            {
                StatusText.Text = "Stopped";
                StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 158, 158, 158));
            }
        }

        private async Task OnMetricsUpdate(SystemMetrics metrics)
        {
            try
            {
                StatusText.Text = $"Live - CPU: {metrics.CpuUsage:F0}% RAM: {metrics.RamUsage:F0}%";
                StatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
                UpdateGauges(metrics);
                UpdateHistory(metrics);
                DrawGraphs();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateGauges(SystemMetrics metrics)
        {
            if (CpuProgress != null && RamProgress != null && DiskProgress != null)
            {
                CpuProgress.Value = Math.Min(100, Math.Max(0, metrics.CpuUsage));
                RamProgress.Value = Math.Min(100, Math.Max(0, metrics.RamUsage));
                DiskProgress.Value = Math.Min(100, Math.Max(0, metrics.DiskUsage));
            }
            
            if (CpuPercentText != null) CpuPercentText.Text = $"{metrics.CpuUsage:F0}%";
            if (CpuTempText != null) CpuTempText.Text = metrics.CpuTemperature > 0 ? $"Temp: {metrics.CpuTemperature:F0}°C" : "Temp: N/A";
            
            if (RamPercentText != null) RamPercentText.Text = $"{metrics.RamUsage:F0}%";
            if (RamUsedText != null) RamUsedText.Text = $"Used: {metrics.RamUsage:F0}%";
            
            if (DiskPercentText != null) DiskPercentText.Text = $"{metrics.DiskUsage:F0}%";
            if (DiskUsedText != null) DiskUsedText.Text = $"Total: {metrics.DiskUsage:F0}%";
            
            if (GpuListPanel != null && metrics.GpuList != null && metrics.GpuList.Count > 0)
            {
                GpuListPanel.ItemsSource = metrics.GpuList;
            }
            
            if (NetworkDownText != null) NetworkDownText.Text = $"{metrics.NetworkReceived:F1} MB/s";
            if (NetworkUpText != null) NetworkUpText.Text = $"{metrics.NetworkSent:F1} MB/s";

            if (CpuProgress != null) UpdateGaugeColor(CpuProgress, metrics.CpuUsage);
            if (RamProgress != null) UpdateGaugeColor(RamProgress, metrics.RamUsage);
            if (DiskProgress != null) UpdateGaugeColor(DiskProgress, metrics.DiskUsage);
        }

        private void UpdateGaugeColor(ProgressBar bar, double value)
        {
            if (value >= 90)
            {
                bar.Foreground = new SolidColorBrush(Color.FromArgb(255, 244, 67, 54));
            }
            else if (value >= 70)
            {
                bar.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 152, 0));
            }
            else
            {
                bar.Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
            }
        }

        private void UpdateHistory(SystemMetrics metrics)
        {
            lock (_lock)
            {
                _history.AddMetric(metrics);
            }
        }

        private void DrawGraphs()
        {
            lock (_lock)
            {
                DrawGraph(CpuGraph, _history.CpuHistory, "#4FC3F7");
                DrawGraph(RamGraph, _history.RamHistory, "#81C784");
                DrawGraph(DiskGraph, _history.DiskHistory, "#FFB74D");
            }
        }

        private void DrawGraph(Canvas canvas, System.Collections.Generic.List<double> data, string colorHex)
        {
            if (canvas == null || data == null) return;
            
            canvas.Children.Clear();

            if (data.Count < 2) return;

            var color = Color.FromArgb(255, 
                Convert.ToByte(colorHex.Substring(1, 2), 16),
                Convert.ToByte(colorHex.Substring(3, 2), 16),
                Convert.ToByte(colorHex.Substring(5, 2), 16));

            var transparentColor = Color.FromArgb(50, 
                Convert.ToByte(colorHex.Substring(1, 2), 16),
                Convert.ToByte(colorHex.Substring(3, 2), 16),
                Convert.ToByte(colorHex.Substring(5, 2), 16));

            var width = canvas.ActualWidth;
            var height = canvas.ActualHeight;

            if (width <= 0 || height <= 0) return;

            var brush = new SolidColorBrush(color);
            var polyline = new Polyline
            {
                Stroke = brush,
                StrokeThickness = 2
            };

            double xStep = width / Math.Max(data.Count - 1, 1);

            for (int i = 0; i < data.Count; i++)
            {
                var x = i * xStep;
                var y = height - (data[i] / 100.0 * height);
                polyline.Points.Add(new Windows.Foundation.Point(x, y));
            }

            canvas.Children.Add(polyline);

            var area = new Polyline
            {
                Fill = new SolidColorBrush(transparentColor),
                Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                StrokeThickness = 0
            };

            area.Points.Add(new Windows.Foundation.Point(0, height));
            foreach (var point in polyline.Points)
            {
                area.Points.Add(point);
            }
            area.Points.Add(new Windows.Foundation.Point((data.Count - 1) * xStep, height));

            canvas.Children.Insert(0, area);

            var gridLine = new Line
            {
                X1 = 0,
                Y1 = height * 0.5,
                X2 = width,
                Y2 = height * 0.5,
                Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 4 }
            };
            canvas.Children.Add(gridLine);

            var valueLine = new Line
            {
                X1 = width - 2,
                Y1 = 0,
                X2 = width - 2,
                Y2 = height,
                Stroke = brush,
                StrokeThickness = 1
            };
            canvas.Children.Add(valueLine);
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_isMonitoring)
            {
                StopMonitoring();
            }
        }
    }
}
