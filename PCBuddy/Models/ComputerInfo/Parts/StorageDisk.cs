using PCBuddy.Models.Enums;

namespace PCBuddy.Models.ComputerInfo
{
    public class StorageDisk : Part
    {
        /// <summary>
        /// Interface of the storage disk - SATA III, SATA II or other.
        /// </summary>
        public string Interface { get; set; }

        public string FirmwareRevision { get; set; }

        public int CapacityMB { get; set; }

        public int FreeSpaceMB { get; set; }
    }
}
