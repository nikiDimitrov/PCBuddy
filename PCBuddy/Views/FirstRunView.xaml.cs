using Microsoft.UI.Xaml;
using PCBuddy.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PCBuddy.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FirstRunView : Window
{
    internal FirstRunViewModel ViewModel { get; set; }

    public FirstRunView()
    {
        InitializeComponent();
        ViewModel = new FirstRunViewModel();
    }
}
