using CommunityToolkit.Mvvm.ComponentModel;

namespace PCBuddy.ViewModels
{
    public class FirstRunViewModel : ObservableObject
    {
        public string WelcomeText { get; set; }

        public FirstRunViewModel()
        {
            WelcomeText = "Welcome To PCBuddy!";
        }
    }
}
