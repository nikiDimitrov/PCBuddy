using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Enums;
using System;
using System.Management;
using System.Runtime.InteropServices;
namespace PCBuddy.Models.Helpers
{
    /// <summary>
    /// Gets the computer information using WMI queries and other methods, and returns a Computer object with the gathered information.
    /// </summary>
    public static class ComputerInfoGetter
    {
        public static Computer GetComputerInfo(UserProfile userProfile)
        {
            var computer = new Computer();

            var processor = GetProcessorInfo();

            return computer;
        }

        #region Processor Methods

        private static Processor GetProcessorInfo()
        {
            var processor = new Processor();

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                var architecture = obj["Architecture"] != null
                    ? (CpuArchitecture)Convert.ToUInt16(obj["Architecture"])
                    : CpuArchitecture.X86;

                return new Processor
                {
                    Architecture = architecture.ToString(),
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    ModelName = obj["Name"]?.ToString(),
                    PhysicalCoreCount = Convert.ToInt32(obj["NumberOfCores"]),
                    ThreadCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"]),
                    BaseClockGhz = Convert.ToDouble(obj["MaxClockSpeed"]) / 1000.0
                };
            }

            return null;
        }

        #endregion
    }
}
