using Microsoft.UI.Xaml;
using PCBuddy.Views;

namespace PCBuddy
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public new Window? MainWindow { get; set; }

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new FirstRunView();
            MainWindow.Activate();
        }
    }
}
