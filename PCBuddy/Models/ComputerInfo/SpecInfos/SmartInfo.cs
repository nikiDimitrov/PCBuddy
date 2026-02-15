using PCBuddy.Models.Enums;

namespace PCBuddy.Models.ComputerInfo.SpecInfos
{
    public class SmartInfo
    {
        public SMARTStatusCode StatusCode { get; set; }

        public int PowerOnHours { get; set; }

        public int BadSectors { get; set; }
    }
}
