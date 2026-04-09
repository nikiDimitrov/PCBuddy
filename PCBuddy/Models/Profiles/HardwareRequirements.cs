using PCBuddy.Models.Enums;

namespace PCBuddy.Models.Profiles
{
    public class CpuRequirements
    {
        public int MinCores { get; set; }
        public int MinThreads { get; set; }
        public double MinBaseClockGhz { get; set; }
        public bool RequireVirtualization { get; set; }
    }

    public class GpuRequirements
    {
        public int MinVramMb { get; set; }
        public int RecommendedVramMb { get; set; }
    }

    public class RamRequirements
    {
        public int MinGb { get; set; }
        public int RecommendedGb { get; set; }
    }

    public class StorageRequirements
    {
        public int MinStorageGb { get; set; }
        public int RecommendedStorageGb { get; set; }
        public bool RequireSsd { get; set; }
    }

    public class ProfileRequirementsSet
    {
        public CpuRequirements Cpu { get; set; } = new();
        public GpuRequirements Gpu { get; set; } = new();
        public RamRequirements Ram { get; set; } = new();
        public StorageRequirements Storage { get; set; } = new();
    }

    public class AllProfileRequirements
    {
        public ProfileRequirementsSet Office { get; set; } = new();
        public ProfileRequirementsSet Gaming { get; set; } = new();
        public ProfileRequirementsSet Programming { get; set; } = new();
        public ProfileRequirementsSet Editing { get; set; } = new();

        public ProfileRequirementsSet GetRequirementsForProfile(UserProfileType profileType)
        {
            return profileType switch
            {
                UserProfileType.Office => Office,
                UserProfileType.Gaming => Gaming,
                UserProfileType.Programming => Programming,
                UserProfileType.Editing => Editing,
                _ => Office
            };
        }
    }
}
