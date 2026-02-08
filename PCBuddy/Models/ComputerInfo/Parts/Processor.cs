namespace PCBuddy.Models.ComputerInfo
{
    public class Processor : Part
    {
        public string Architecture { get; set; }

        public string Generation { get; set; }

        public int PhysicalCoreCount { get; set; }

        public int ThreadCount { get; set; }

        public double BaseClockGhz { get; set; }

        public double BoostClockGhz { get; set; }

        public bool HasIntegratedGraphics { get; set; }

        public bool HasVirtualization { get; set; }
    }
}
