using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PCBuddy.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace PCBuddy.Views
{
    public sealed partial class EventLogView : Page
    {
        private ObservableCollection<EventLogEntry> _events = new();
        private CancellationTokenSource? _cts;
        private int _loadVersion = 0;

        public EventLogView()
        {
            InitializeComponent();
            EventListView.ItemsSource = _events;
            RefreshButton.Click += RefreshButton_Click;
            _ = LoadEventsAsync("System");
        }

        private void EventListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EventListView.SelectedItem is EventLogEntry entry)
            {
                DetailTime.Text = entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss");
                DetailLevel.Text = entry.Level;
                DetailSource.Text = entry.Source;
                DetailMessage.Text = entry.Message;
                DetailPanel.Visibility = Visibility.Visible;
            }
        }

        private void CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            EventListView.SelectedItem = null;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedLog = LogSelector.SelectedIndex switch
            {
                1 => "Application",
                2 => "Errors",
                _ => "System"
            };
            await LoadEventsAsync(selectedLog);
        }

        private async void LogSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogSelector.SelectedItem is ComboBoxItem item)
            {
                var selectedLog = item.Content?.ToString() switch
                {
                    "Application" => "Application",
                    "Errors Only" => "Errors",
                    _ => "System"
                };
                await LoadEventsAsync(selectedLog);
            }
        }

        private async Task LoadEventsAsync(string logType)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var currentVersion = ++_loadVersion;

            LoadingBar.Visibility = Visibility.Visible;
            StatusText.Text = "Loading events...";
            RefreshButton.IsEnabled = false;
            _events.Clear();

            try
            {
                var entries = logType switch
                {
                    "Application" => await EventLogService.GetApplicationEventsAsync(1000, token),
                    "Errors" => await EventLogService.GetErrorEventsAsync(1000, token),
                    _ => await EventLogService.GetSystemEventsAsync("System", 1000, token)
                };

                if (token.IsCancellationRequested || currentVersion != _loadVersion) return;

                foreach (var entry in entries)
                {
                    if (currentVersion != _loadVersion) return;
                    _events.Add(entry);
                }

                int errors = 0, warnings = 0;
                foreach (var entry in _events)
                {
                    if (entry.Level == "Error") errors++;
                    else if (entry.Level == "Warning") warnings++;
                }

                StatusText.Text = $"Loaded {_events.Count} events ({errors} errors, {warnings} warnings)";
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Cancelled";
            }
            catch (Exception ex)
            {
                if (currentVersion == _loadVersion)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
            finally
            {
                if (currentVersion == _loadVersion)
                {
                    LoadingBar.Visibility = Visibility.Collapsed;
                    RefreshButton.IsEnabled = true;
                }
            }
        }
    }
}
