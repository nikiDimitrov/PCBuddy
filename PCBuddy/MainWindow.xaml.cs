using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PCBuddy.Views;

namespace PCBuddy
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardView));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardView));
                        break;
                    case "Drivers":
                        ContentFrame.Navigate(typeof(DriversView));
                        break;
                    case "EventLog":
                        ContentFrame.Navigate(typeof(EventLogView));
                        break;
                }
            }
        }

        private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(DashboardView))
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
            else if (e.SourcePageType == typeof(DriversView))
            {
                NavView.SelectedItem = NavView.MenuItems[1];
            }
            else if (e.SourcePageType == typeof(EventLogView))
            {
                NavView.SelectedItem = NavView.MenuItems[2];
            }
        }
    }
}
