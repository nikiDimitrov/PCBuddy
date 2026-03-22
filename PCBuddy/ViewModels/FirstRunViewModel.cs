using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using PCBuddy.Models;
using PCBuddy.Models.Enums;
using PCBuddy.Models.Helpers;
using PCBuddy.Models.Settings;
using System;
using System.Collections.ObjectModel;
using System.IO;
using static PCBuddy.PCBuddyConsts;

namespace PCBuddy.ViewModels
{
    public partial class FirstRunViewModel : ObservableObject
    {
        [ObservableProperty]
        private UserProfile? selectedProfile;

        public bool CanApplyProfile => SelectedProfile != null;

        public ObservableCollection<UserProfile> UserProfiles { get; set; }
            = new ObservableCollection<UserProfile>();

        public string WelcomeText { get; set; }

        public string ChooseProfileText { get; set; }

        public string ApplyProfileButtonText { get; set; }

        public FirstRunViewModel()
        {
            SetResources();
            SetUserProfiles();
        }

        partial void OnSelectedProfileChanged(UserProfile? value)
        {
            OnPropertyChanged(nameof(CanApplyProfile));
        }

        public void ProfileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = e.AddedItems[0] as UserProfile;
            SelectedProfile = selectedProfile;
        }

        [RelayCommand]
        private void ApplyProfile()
        {
            var computerInfo = ComputerInfoGetter.GetComputerInfo(SelectedProfile!);

            JsonSettingsManager.SaveComputerSettingsToFile(SelectedProfile!, computerInfo);
        }

        //TODO: make resources system from json
        private void SetResources()
        {
            WelcomeText = "Welcome To PCBuddy!";
            ChooseProfileText =
                "PCBuddy needs to know what do you want to do with this PC. Choose a profile!";
            ApplyProfileButtonText = "Apply Profile";
        }  

        /// <summary>
        /// Gets the icon and displayName from the Profile enums.
        /// </summary>
        /// <remarks>
        /// To add new profile simply add a new value in <see cref="UserProfileType"/>.
        /// Add the icon to assets/icons with the enum value in lowercase as the name.
        /// </remarks>
        private void SetUserProfiles()
        {
            var profileTypes = Enum.GetValues<UserProfileType>();

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
                    new UserProfile
                    (
                        displayName: profileType.ToString(),
                        iconPathForUI,
                        profileType
                    );

                UserProfiles.Add(profileOption);
            }
        }
    }
}
