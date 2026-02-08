using PCBuddy.Models.Enums;

namespace PCBuddy.Models.ComputerInfo
{
    public class MemoryStick : Part
    {
        public int SlotIndex { get; set; }

        public int ChannelIndex { get; set; }

        public string MemoryType { get; set; }

        public int Frequency { get; set; }

        public int CapacityMB { get; set; }

        public MemoryFormFactor FormFactor { get; set; }

        public bool IsECC { get; set; }
    }
}
