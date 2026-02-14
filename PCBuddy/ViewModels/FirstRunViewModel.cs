using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCBuddy.Models.Enums;
using PCBuddy.ViewModels.UIContainers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using static PCBuddy.PCBuddyConsts;

namespace PCBuddy.ViewModels
{
    public partial class FirstRunViewModel : ObservableObject
    {
        [ObservableProperty]
        private ProfileOption? selectedProfile;

        public ObservableCollection<ProfileOption> ProfileOptions { get; set; }
            = new ObservableCollection<ProfileOption>();

        public string WelcomeText { get; set; }

        public string ChooseProfileText { get; set; }

        public FirstRunViewModel()
        {
            SetResources();
            SetProfileOptions();
        }

        [RelayCommand]
        private void SelectProfile(ProfileOption option)
        {
            SelectedProfile = option;
        }

        //TODO: make resources system from json
        private void SetResources()
        {
            WelcomeText = "Welcome To PCBuddy!";
            ChooseProfileText =
                "PCBuddy needs to know what do you want to do with this PC. Choose a profile.";
        }  

        /// <summary>
        /// Gets the icon and displayName from the Profile enums.
        /// </summary>
        /// <remarks>
        /// To add new profile simply add a new value in <see cref="UserProfile"/>.
        /// Add the icon to assets/icons with the enum value in lowercase as the name.
        /// </remarks>
        private void SetProfileOptions()
        {
            var profileTypes = Enum.GetValues<UserProfile>();

            foreach(var profileType in profileTypes)
            {
                var displayName =
                    profileType.ToString().ToLowerInvariant();

                var iconPathForUI = 
                    $"{ICON_PATH_FOR_UI}/{displayName}.svg";

                var iconPathApp = $"{ICON_PATH}\\{displayName}.svg";

                var fullPath = Path.Combine(AppContext.BaseDirectory, iconPathApp);

                if (!File.Exists(fullPath))
                    continue;

                var profileOption =
                    new ProfileOption
                    (
                        displayName: profileType.ToString(),
                        iconPathForUI,
                        profileType
                    );

                ProfileOptions.Add(profileOption);
            }
        }
    }
}
