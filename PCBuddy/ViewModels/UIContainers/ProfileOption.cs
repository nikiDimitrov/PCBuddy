using PCBuddy.Models.Enums;

namespace PCBuddy.ViewModels.UIContainers
{
    public class ProfileOption
    {
        public string DisplayName { get; set; }

        public string IconPath { get; set; }

        public UserProfile ProfileType { get; set; }

        public ProfileOption
        (
            string displayName, 
            string iconPath, 
            UserProfile profileType
        )
        {
            DisplayName = displayName;
            IconPath = iconPath;
            ProfileType = profileType;
        }
    }
}
