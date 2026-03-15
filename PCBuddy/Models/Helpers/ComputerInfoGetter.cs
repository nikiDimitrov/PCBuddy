using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
namespace PCBuddy.Models.Helpers
{
    /// <summary>
    /// Gets the computer information using WMI queries and other methods, and returns a Computer object with the gathered information.
    /// </summary>
    public static class ComputerInfoGetter
    {
        public static async Task<Computer> GetComputerInfo(UserProfile userProfile)
        {
            var processorTask = RunWmiAsync(GetProcessorInfo);
            var graphicsTask = RunWmiAsync(GetGraphicsAdapters);
            var memoryTask = RunWmiAsync(GetMemoryInfo);
            var storageTask = RunWmiAsync(GetStorageDisks);

            await Task.WhenAll(processorTask, graphicsTask, memoryTask);

            var computer =
                new Computer()
                {
                    Processor = processorTask.Result,
                    GPUs = graphicsTask.Result,
                    MemoryInfo = memoryTask.Result,
                    StorageDisks = storageTask.Result
                };

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

        #region GraphicsAdapter Methods

        private static List<GraphicsAdapter> GetGraphicsAdapters()
        {
            var adapters = new List<GraphicsAdapter>();

            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                //ignore software fallback adapter
                if (name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase))
                    continue;

                var manufacturer = obj["AdapterCompatibility"]?.ToString() ?? "Unknown";

                var adapter = new GraphicsAdapter
                {
                    Manufacturer = manufacturer,
                    ModelName = name,

                    VideoMemory = obj["AdapterRAM"] != null
                        ? Convert.ToInt32((uint)obj["AdapterRAM"] / (1024 * 1024))
                        : 0,

                    AdapterType = DetectAdapterType(name, manufacturer),

                    Driver = new Driver
                    {
                        Provider = obj["AdapterCompatibility"]?.ToString(),
                        Version = obj["DriverVersion"]?.ToString(),
                        Date = ParseManagementDate(obj["DriverDate"])
                    },

                    IsMobileVariant =
                        name.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Laptop", StringComparison.OrdinalIgnoreCase)
                };

                adapters.Add(adapter);
            }

            return adapters;
        }

        private static GraphicsAdapterType DetectAdapterType(string name, string manufacturer)
        {
            if (manufacturer?.Contains("Intel", StringComparison.OrdinalIgnoreCase) is true)
                return GraphicsAdapterType.Integrated;

            if (manufacturer?.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) is true ||
                manufacturer?.Contains("AMD", StringComparison.OrdinalIgnoreCase) is true ||
                manufacturer?.Contains("ATI", StringComparison.OrdinalIgnoreCase) is true)
                return GraphicsAdapterType.Dedicated;

            return GraphicsAdapterType.Unknown;
        }

        #endregion

        #region Memory Methods

        private static MemoryInfo GetMemoryInfo()
        {
            var memoryInfo = new MemoryInfo();

            memoryInfo.MemorySticks = GetMemorySticks();

            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PhysicalMemoryArray");

            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["MemoryDevices"] != null)
                    memoryInfo.TotalMemorySlots = Convert.ToInt32(obj["MemoryDevices"]);

                if (obj["MaxCapacity"] != null)
                    memoryInfo.MaxSupportedMemory =
                        Convert.ToInt32((uint)obj["MaxCapacity"] / 1024); //KB → MB

                break;
            }

            return memoryInfo;
        }

        private static List<MemoryStick> GetMemorySticks()
        {
            var sticks = new List<MemoryStick>();

            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PhysicalMemory");

            var slotIndex = 0;

            foreach (ManagementObject obj in searcher.Get())
            {
                var stick = new MemoryStick
                {
                    SlotIndex = slotIndex++,

                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    ModelName = obj["PartNumber"]?.ToString(),

                    CapacityMB = obj["Capacity"] != null
                        ? Convert.ToInt32((ulong)obj["Capacity"] / (1024 * 1024))
                        : 0,

                    Frequency = obj["ConfiguredClockSpeed"] != null
                        ? Convert.ToInt32(obj["ConfiguredClockSpeed"])
                        : 0,

                    MemoryType = MapMemoryType(obj["SMBIOSMemoryType"]),

                    FormFactor = MapFormFactor(obj["FormFactor"]),

                    IsECC = DetectECC(obj),

                    ChannelIndex = -1
                };

                sticks.Add(stick);
            }

            return sticks;
        }

        private static MemoryType MapMemoryType(object value)
        {
            if (value is null)
                return MemoryType.Unknown;

            var memoryType = Convert.ToInt32(value);

            return
                Enum.IsDefined(typeof(MemoryType), memoryType)
                ? (MemoryType)memoryType
                : MemoryType.Unknown;
        }

        private static MemoryFormFactor MapFormFactor(object value)
        {
            if (value is null)
                return MemoryFormFactor.DIMM;

            var formFactor = Convert.ToInt32(value);

            return 
                Enum.IsDefined(typeof(MemoryFormFactor), formFactor)
                ? (MemoryFormFactor)formFactor
                : MemoryFormFactor.Unknown;
        }

        private static bool DetectECC(ManagementObject obj)
        {
            if (obj["TotalWidth"] is null || 
                obj["DataWidth"] is null)
                return false;

            var totalWidth = Convert.ToInt32(obj["TotalWidth"]);
            var dataWidth = Convert.ToInt32(obj["DataWidth"]);

            return totalWidth > dataWidth;
        }

        #endregion

        #region StorageDisk Methods

        private static List<StorageDisk> GetStorageDisks()
        {
            var disks = new List<StorageDisk>();

            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDrive");

            foreach (ManagementObject disk in searcher.Get())
            {
                var storageDisk = new StorageDisk
                {
                    Manufacturer = disk["Manufacturer"]?.ToString(),
                    ModelName = disk["Model"]?.ToString(),

                    Interface = disk["InterfaceType"]?.ToString(),

                    FirmwareRevision = disk["FirmwareRevision"]?.ToString(),

                    CapacityMB = disk["Size"] != null
                        ? Convert.ToInt32((ulong)disk["Size"] / (1024 * 1024))
                        : 0
                };

                // Get partitions
                var partitions = disk.GetRelated("Win32_DiskPartition");

                foreach (ManagementObject partition in partitions)
                {
                    var logicalDisks = partition.GetRelated("Win32_LogicalDisk");

                    foreach (ManagementObject logical in logicalDisks)
                    {
                        if (logical["FreeSpace"] != null)
                        {
                            storageDisk.FreeSpaceMB +=
                                Convert.ToInt32((ulong)logical["FreeSpace"] / (1024 * 1024));
                        }
                    }
                }

                disks.Add(storageDisk);
            }

            return disks;
        }

        #endregion

        #region Helper Methods

        private static Task<T> RunWmiAsync<T>(Func<T> func)
        {
            return Task.Run(func);
        }

        private static DateTime ParseManagementDate(object value)
        {
            if (value == null)
                return DateTime.MinValue;

            return ManagementDateTimeConverter.ToDateTime(value.ToString());
        }

        #endregion
    }
}
