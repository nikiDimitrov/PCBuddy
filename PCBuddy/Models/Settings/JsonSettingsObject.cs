using PCBuddy.Models.ComputerInfo;
using System.Text.Json.Serialization;

namespace PCBuddy.Models.Settings
{
    [JsonSerializable(typeof(JsonSettingsObject))]
    public class JsonSettingsObject
    {
        public UserProfile Profile { get; set; }

        public Computer Computer { get; set; }
    }
}
