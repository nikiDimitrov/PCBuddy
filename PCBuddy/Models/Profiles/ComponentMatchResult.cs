using PCBuddy.Models.Enums;

namespace PCBuddy.Models.Profiles
{
    public class ComponentMatchResult
    {
        public string ComponentName { get; set; }
        public RequirementStatus Status { get; set; }
        public string StatusMessage { get; set; }
        public string ActualValue { get; set; }
        public string RequiredValue { get; set; }
    }
}
