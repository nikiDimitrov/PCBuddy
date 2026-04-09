using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PCBuddy.Views
{
    public sealed partial class DashboardView : Page
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void CpuCard_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SystemInfoView), "CPU");
        }

        private void GpuCard_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SystemInfoView), "GPU");
        }

        private void RamCard_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SystemInfoView), "RAM");
        }

        private void StorageCard_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SystemInfoView), "Storage");
        }
    }
}
