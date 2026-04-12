using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PCBuddy.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCBuddy.Views
{
    public sealed partial class EventLogView : Page
    {
        private ObservableCollection<EventLogEntry> _allEvents = new();
        private ObservableCollection<EventLogEntry> _filteredEvents = new();
        private CancellationTokenSource? _cts;
        private int _loadVersion = 0;

        public EventLogView()
        {
            InitializeComponent();
            EventListView.ItemsSource = _filteredEvents;
            RefreshButton.Click += RefreshButton_Click;
            _ = LoadEventsAsync("System");
        }

        private void ApplyFilters()
        {
            var levelFilter = LevelFilter.SelectedIndex switch
            {
                1 => "Error",
                2 => "Warning",
                3 => "Info",
                _ => null
            };
            var searchText = SearchBox.Text?.ToLower() ?? "";

            _filteredEvents.Clear();

            var filtered = _allEvents.AsEnumerable();
            if (!string.IsNullOrEmpty(levelFilter))
            {
                filtered = filtered.Where(e => e.Level == levelFilter);
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(e => 
                    e.Source.ToLower().Contains(searchText) || 
                    e.Message.ToLower().Contains(searchText));
            }

            foreach (var entry in filtered)
            {
                _filteredEvents.Add(entry);
            }

            UpdateStatus();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_allEvents.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allEvents.Count > 0)
            {
                ApplyFilters();
            }
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
                    _ => "System"
                };
                await LoadEventsAsync(selectedLog);
            }
        }

        private void UpdateStatus()
        {
            int errors = _filteredEvents.Count(e => e.Level == "Error");
            int warnings = _filteredEvents.Count(e => e.Level == "Warning");
            int showing = _filteredEvents.Count;
            int total = _allEvents.Count;
            StatusText.Text = showing == total 
                ? $"Showing {showing} events ({errors} errors, {warnings} warnings)"
                : $"Showing {showing} of {total} events ({errors} errors, {warnings} warnings)";
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
            _allEvents.Clear();
            _filteredEvents.Clear();

            try
            {
                var entries = logType switch
                {
                    "Application" => await EventLogService.GetApplicationEventsAsync(1000, token),
                    _ => await EventLogService.GetSystemEventsAsync("System", 1000, token)
                };

                if (token.IsCancellationRequested || currentVersion != _loadVersion) return;

                foreach (var entry in entries)
                {
                    if (currentVersion != _loadVersion) return;
                    _allEvents.Add(entry);
                }

                ApplyFilters();
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
