using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PCBuddy.ViewModels;

namespace PCBuddy.Views
{
    public sealed partial class SystemInfoView : Page
    {
        private readonly SystemInfoViewModel _viewModel;

        public SystemInfoView()
        {
            InitializeComponent();
            _viewModel = new SystemInfoViewModel();
            DataContext = _viewModel;
        }

        public SystemInfoView(string partType) : this()
        {
            _viewModel.SetPartType(partType);
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string partType)
            {
                _viewModel.SetPartType(partType);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
