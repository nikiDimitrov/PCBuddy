using System.Collections.Generic;
using System.Linq;

namespace PCBuddy.Models.ComputerInfo
{
    /// <summary>
    /// Class that holds the information for the computer's memory configuration.
    /// </summary>
    public class MemoryInfo
    {
        public List<MemoryStick> MemorySticks { get; set; } = new();

        public int MaxSupportedMemory { get; set; }

        public int TotalMemorySlots { get; set; }

        public int UsedMemorySlots => MemorySticks.Count;

        public int AvailableSlots => TotalMemorySlots - UsedMemorySlots;

        public int TotalInstalledMemory => MemorySticks.Sum(stick => stick.CapacityMB);

        public bool HasMixedMemoryTypes => 
            MemorySticks
            ?.Select(s => s.MemoryType)
            ?.Distinct()
            ?.Count() > 1;

        public bool HasMixedFrequencies =>
            MemorySticks
            ?.Select(s => s.Frequency)
            ?.Distinct()
            ?.Count() > 1;
    }
}
