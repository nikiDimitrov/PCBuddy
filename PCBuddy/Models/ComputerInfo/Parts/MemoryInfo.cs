using System.Collections.Generic;
using System.Linq;

namespace PCBuddy.Models.ComputerInfo
{
    /// <summary>
    /// Class that holds the information for the computer's memory configuration.
    /// </summary>
    public class MemoryInfo
    {
        public List<MemoryStick> MemorySticks { get; set; }

        public int MaxSupportedMemory { get; set; }

        public int TotalMemorySlots { get; set; }

        public int UsedMemorySlots
        {
            get
            {
                if (MemorySticks is null)
                    return 0;

                return MemorySticks.Count;
            }
        }

        public int TotalInstalledMemory
        {
            get
            {
                if (MemorySticks is null)
                    return 0;

                return MemorySticks.Sum(stick => stick.CapacityMB);
            }
        }
    }
}
