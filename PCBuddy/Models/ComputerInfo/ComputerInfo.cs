using System.Collections.Generic;

namespace PCBuddy.Models.ComputerInfo
{
    public class ComputerInfo
    {
        public string OS { get; set; }

        public Processor Processor { get; set; }

        public List<GraphicsAdapter> GPUs { get; set; }
        
        public MemoryInfo MemoryInfo { get; set; }

        
    }
}
