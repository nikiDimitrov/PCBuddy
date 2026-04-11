using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using PCBuddy.Models;
using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Profiles;
using PCBuddy.Models.Settings;
using PCBuddy.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PCBuddy.ViewModels
{
    public partial class SystemInfoViewModel : ObservableObject
    {
        [ObservableProperty]
        private Computer? _computer;

        [ObservableProperty]
        private UserProfile? _currentProfile;

        [ObservableProperty]
        private ProfileInspectionResult? _inspectionResult;

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private ObservableCollection<PartDetailItem> _details = new();

        public SystemInfoViewModel()
        {
            LoadData();
        }

        public void SetPartType(string partType)
        {
            Title = partType;
            LoadPartDetails(partType);
        }

        private void LoadData()
        {
            try
            {
                var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var settingsPath = Path.Combine(folder, "PCBuddy", "settings.json");

                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<JsonSettingsObject>(json);

                    if (settings != null)
                    {
                        Computer = settings.Computer;
                        CurrentProfile = settings.Profile;

                        if (Computer != null && CurrentProfile != null)
                        {
                            InspectionResult = ProfileAnalyzerService.AnalyzeComputerAgainstProfile(
                                Computer,
                                CurrentProfile.ProfileType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private void LoadPartDetails(string partType)
        {
            Details.Clear();

            switch (partType)
            {
                case "CPU":
                    LoadCpuDetails();
                    break;
                case "GPU":
                    LoadGpuDetails();
                    break;
                case "RAM":
                    LoadRamDetails();
                    break;
                case "Storage":
                    LoadStorageDetails();
                    break;
            }
        }

        private void LoadCpuDetails()
        {
            if (Computer?.Processor == null) return;
            var cpu = Computer.Processor;

            Details.Add(new PartDetailItem("Name", $"{cpu.Manufacturer} {cpu.ModelName}"));
            Details.Add(new PartDetailItem("Architecture", cpu.Architecture));
            Details.Add(new PartDetailItem("Generation", cpu.Generation));
            Details.Add(new PartDetailItem("Physical Cores", cpu.PhysicalCoreCount.ToString()));
            Details.Add(new PartDetailItem("Threads", cpu.ThreadCount.ToString()));
            Details.Add(new PartDetailItem("Base Clock", $"{cpu.BaseClockGhz:F1} GHz"));
            Details.Add(new PartDetailItem("Boost Clock", $"{cpu.BoostClockGhz:F1} GHz"));
            Details.Add(new PartDetailItem("Integrated Graphics", cpu.HasIntegratedGraphics ? "Yes" : "No"));
            Details.Add(new PartDetailItem("Virtualization", cpu.HasVirtualizationEnabled ? "Enabled" : "Disabled"));

            if (InspectionResult?.CpuResult != null)
            {
                Details.Add(new PartDetailItem("", ""));
                Details.Add(new PartDetailItem("Requirements", "", isHeader: true));
                foreach (var result in InspectionResult.CpuResult)
                {
                    var statusIcon = result.Status switch
                    {
                        RequirementStatus.Pass => "✓",
                        RequirementStatus.Warning => "⚠",
                        RequirementStatus.Fail => "✗",
                        _ => ""
                    };
                    var requirement = result.Status == RequirementStatus.Pass ? $" (need {result.RequiredValue})" : "";
                    Details.Add(new PartDetailItem(result.ComponentName, $"{statusIcon} {result.StatusMessage}{requirement}"));
                }
            }
        }

        private void LoadGpuDetails()
        {
            if (Computer?.GPUs == null) return;

            foreach (var gpu in Computer.GPUs)
            {
                Details.Add(new PartDetailItem($"GPU {Computer.GPUs.IndexOf(gpu) + 1}", "", isHeader: true));
                Details.Add(new PartDetailItem("Name", $"{gpu.Manufacturer} {gpu.ModelName}"));
                Details.Add(new PartDetailItem("Type", gpu.AdapterType.ToString()));
                Details.Add(new PartDetailItem("VRAM", $"{gpu.VideoMemory} MB"));
            }

            if (InspectionResult?.GpuResult != null)
            {
                Details.Add(new PartDetailItem("", ""));
                Details.Add(new PartDetailItem("Requirements", "", isHeader: true));
                foreach (var result in InspectionResult.GpuResult)
                {
                    var statusIcon = result.Status switch
                    {
                        RequirementStatus.Pass => "✓",
                        RequirementStatus.Warning => "⚠",
                        RequirementStatus.Fail => "✗",
                        _ => ""
                    };
                    var requirement = result.Status == RequirementStatus.Pass ? $" (need {result.RequiredValue})" : "";
                    Details.Add(new PartDetailItem(result.ComponentName, $"{statusIcon} {result.StatusMessage}{requirement}"));
                }
            }
        }

        private void LoadRamDetails()
        {
            if (Computer?.MemoryInfo == null) return;
            var mem = Computer.MemoryInfo;

            var totalGb = mem.TotalInstalledMemory / 1024;
            Details.Add(new PartDetailItem("Total RAM", $"{totalGb} GB"));
            Details.Add(new PartDetailItem("Memory Slots", $"{mem.UsedMemorySlots} / {mem.TotalMemorySlots} used"));
            Details.Add(new PartDetailItem("Available Slots", mem.AvailableSlots.ToString()));
            Details.Add(new PartDetailItem("Max Supported", $"{mem.MaxSupportedMemory / 1024} GB"));

            foreach (var stick in mem.MemorySticks)
            {
                Details.Add(new PartDetailItem("", ""));
                Details.Add(new PartDetailItem($"Stick {mem.MemorySticks.IndexOf(stick) + 1}", "", isHeader: true));
                Details.Add(new PartDetailItem("Capacity", $"{stick.CapacityMB / 1024} GB"));
                Details.Add(new PartDetailItem("Type", $"{stick.MemoryType}"));
                Details.Add(new PartDetailItem("Speed", $"{stick.Frequency} MHz"));
                Details.Add(new PartDetailItem("Manufacturer", stick.Manufacturer));
            }

            if (InspectionResult?.RamResult != null)
            {
                Details.Add(new PartDetailItem("", ""));
                Details.Add(new PartDetailItem("Requirements", "", isHeader: true));
                foreach (var result in InspectionResult.RamResult)
                {
                    var statusIcon = result.Status switch
                    {
                        RequirementStatus.Pass => "✓",
                        RequirementStatus.Warning => "⚠",
                        RequirementStatus.Fail => "✗",
                        _ => ""
                    };
                    var requirement = result.Status == RequirementStatus.Pass ? $" (need {result.RequiredValue})" : "";
                    Details.Add(new PartDetailItem(result.ComponentName, $"{statusIcon} {result.StatusMessage}{requirement}"));
                }
            }
        }

        private void LoadStorageDetails()
        {
            if (Computer?.StorageDisks == null) return;

            foreach (var disk in Computer.StorageDisks)
            {
                Details.Add(new PartDetailItem($"Disk {Computer.StorageDisks.IndexOf(disk) + 1}", "", isHeader: true));
                Details.Add(new PartDetailItem("Model", $"{disk.Manufacturer} {disk.ModelName}"));
                Details.Add(new PartDetailItem("Interface", disk.Interface));
                Details.Add(new PartDetailItem("Capacity", $"{disk.CapacityMB / 1024} GB"));
                Details.Add(new PartDetailItem("Free Space", $"{disk.FreeSpaceMB / 1024} GB"));
                Details.Add(new PartDetailItem("Firmware", disk.FirmwareRevision));
            }

            if (InspectionResult?.StorageResult != null)
            {
                Details.Add(new PartDetailItem("", ""));
                Details.Add(new PartDetailItem("Requirements", "", isHeader: true));
                foreach (var result in InspectionResult.StorageResult)
                {
                    var statusIcon = result.Status switch
                    {
                        RequirementStatus.Pass => "✓",
                        RequirementStatus.Warning => "⚠",
                        RequirementStatus.Fail => "✗",
                        _ => ""
                    };
                    var requirement = result.Status == RequirementStatus.Pass ? $" (need {result.RequiredValue})" : "";
                    Details.Add(new PartDetailItem(result.ComponentName, $"{statusIcon} {result.StatusMessage}{requirement}"));
                }
            }
        }
    }

    public class PartDetailItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public bool IsHeader { get; set; }

        public PartDetailItem(string label, string value, bool isHeader = false)
        {
            Label = label;
            Value = value;
            IsHeader = isHeader;
        }
    }
}
