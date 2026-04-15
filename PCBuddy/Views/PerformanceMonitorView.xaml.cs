using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PCBuddy.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;

namespace PCBuddy.Views
{
    public class GpuGraphData
    {
        public string Name { get; set; } = "";
        public Canvas GraphCanvas { get; set; } = new Canvas { Height = 80, Background = new SolidColorBrush(Color.FromArgb(255, 37, 37, 37)) };
        public TextBlock? UsageText { get; set; }
    }

    public sealed partial class PerformanceMonitorView : Page
    {
        private CancellationTokenSource? _cts;
        private bool _isMonitoring = false;
        private MetricHistory _history = new();
        private readonly object _lock = new();
        private List<GpuGraphData> _gpuGraphs = new();

        public PerformanceMonitorView()
        {
            InitializeComponent();
            CpuGraph.SizeChanged += Graph_SizeChanged;
            RamGraph.SizeChanged += Graph_SizeChanged;
            DiskGraph.SizeChanged += Graph_SizeChanged;
        }

        private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGraphs();
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
                await PerformanceMonitorService.StartMonitoringAsync(OnMetricsUpdate, 0, _cts.Token);
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
            
            if (metrics.GpuList != null && metrics.GpuList.Count > 0)
            {
                while (_gpuGraphs.Count < metrics.GpuList.Count)
                {
                    var gpuIdx = _gpuGraphs.Count;
                    var gpuData = new GpuGraphData { Name = metrics.GpuList[gpuIdx].Name };
                    _gpuGraphs.Add(gpuData);
                    
                    var listContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                    var nameText = new TextBlock 
                    { 
                        Text = metrics.GpuList[gpuIdx].Name, 
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), 
                        FontSize = 12, 
                        TextWrapping = TextWrapping.Wrap 
                    };
                    var usageText = new TextBlock 
                    { 
                        Text = metrics.GpuList[gpuIdx].Usage, 
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 186, 104, 200)), 
                        FontSize = 12, 
                        FontWeight = new FontWeight { Weight = 700 } 
                    };
                    gpuData.UsageText = usageText;
                    listContainer.Children.Add(nameText);
                    listContainer.Children.Add(usageText);
                    GpuListPanel.Children.Add(listContainer);
                    
                    var graphContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
                    graphContainer.Children.Add(new TextBlock 
                    { 
                        Text = metrics.GpuList[gpuIdx].Name, 
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 186, 104, 200)), 
                        FontSize = 12, 
                        Margin = new Thickness(0, 0, 0, 4) 
                    });
                    var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                    gpuData.GraphCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                    gpuData.GraphCanvas.VerticalAlignment = VerticalAlignment.Top;
                    grid.Children.Add(gpuData.GraphCanvas);
                    gpuData.GraphCanvas.Height = 80;
                    gpuData.GraphCanvas.SizeChanged += Graph_SizeChanged;
                    graphContainer.Children.Add(grid);
                    GpuGraphsPanel.Children.Add(graphContainer);
                }
                
                for (int i = 0; i < metrics.GpuList.Count; i++)
                {
                    _gpuGraphs[i].Name = metrics.GpuList[i].Name;
                    if (_gpuGraphs[i].UsageText != null)
                    {
                        _gpuGraphs[i].UsageText.Text = metrics.GpuList[i].Usage;
                    }
                }
            }
            
            if (NetworkDownText != null) NetworkDownText.Text = $"{metrics.NetworkReceived:F1} KB/s";
            if (NetworkUpText != null) NetworkUpText.Text = $"{metrics.NetworkSent:F1} KB/s";

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
                
                for (int i = 0; i < _gpuGraphs.Count && i < _history.GpuHistories.Count; i++)
                {
                    DrawGraph(_gpuGraphs[i].GraphCanvas, _history.GpuHistories[i], "#BA68C8");
                }
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
                StrokeThickness = 2,
                Clip = null
            };

            double xStep = width / Math.Max(data.Count - 1, 1);

            for (int i = 0; i < data.Count; i++)
            {
                var x = Math.Min(i * xStep, width);
                var y = Math.Max(0, Math.Min(height - (data[i] / 100.0 * height), height));
                polyline.Points.Add(new Windows.Foundation.Point(x, y));
            }

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
            area.Points.Add(new Windows.Foundation.Point(width, height));

            canvas.Children.Add(area);
            canvas.Children.Add(polyline);

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
            
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Width = width,
                Height = height
            };
            canvas.Children.Add(border);
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
