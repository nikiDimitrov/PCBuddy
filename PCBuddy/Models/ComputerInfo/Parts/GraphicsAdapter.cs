using PCBuddy.Models.Enums;

namespace PCBuddy.Models.ComputerInfo
{
    public class GraphicsAdapter : Part
    {
        public GraphicsAdapterType AdapterType { get; set; }

        public int VideoMemory { get; set; }

        public Driver Driver { get; set; }

        public bool IsVulkanSupported { get; set; }

        public bool IsOpenCLSupported { get; set; }

        public bool IsMobileVariant { get; set; }
    }
}
