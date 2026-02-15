using PCBuddy.ViewModels;
using WinUIEx;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PCBuddy.Views
{
    public sealed partial class FirstRunView : WindowEx
    {
        internal FirstRunViewModel ViewModel { get; set; }

        public FirstRunView()
        {
            InitializeComponent();
            ViewModel = new FirstRunViewModel();
        }
    }
}
