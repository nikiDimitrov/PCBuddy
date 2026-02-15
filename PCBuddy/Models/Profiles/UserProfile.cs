using PCBuddy.Models.Enums;

namespace PCBuddy.Models
{
    public class UserProfile
    {
        public string DisplayName { get; set; }

        public string IconPath { get; set; }

        public UserProfileType ProfileType { get; set; }

        public UserProfile
        (
            string displayName, 
            string iconPath, 
            UserProfileType profileType
        )
        {
            DisplayName = displayName;
            IconPath = iconPath;
            ProfileType = profileType;
        }
    }
}
