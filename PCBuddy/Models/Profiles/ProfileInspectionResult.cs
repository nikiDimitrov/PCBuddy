using PCBuddy.Models.Enums;
using System.Collections.Generic;

namespace PCBuddy.Models.Profiles
{
    public class ProfileInspectionResult
    {
        public UserProfileType ProfileType { get; set; }
        public bool OverallPass { get; set; }
        public List<ComponentMatchResult> CpuResult { get; set; } = new();
        public List<ComponentMatchResult> GpuResult { get; set; } = new();
        public List<ComponentMatchResult> RamResult { get; set; } = new();
        public List<ComponentMatchResult> StorageResult { get; set; } = new();
    }
}
