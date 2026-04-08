using System.Collections.Generic;

namespace PCBuddy.Models.ComputerInfo
{
    public class Computer
    {
        public string OS { get; set; }

        public Processor Processor { get; set; }

        public List<GraphicsAdapter> GPUs { get; set; }

        public List<StorageDisk> StorageDisks { get; set; }
        
        public MemoryInfo MemoryInfo { get; set; }
    }
}
