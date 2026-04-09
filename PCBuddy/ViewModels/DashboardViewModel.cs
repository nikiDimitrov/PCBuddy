using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using PCBuddy.Models;
using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Enums;
using PCBuddy.Models.Profiles;
using PCBuddy.Models.Settings;
using PCBuddy.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PCBuddy.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private Computer? _computer;

        [ObservableProperty]
        private UserProfile? _currentProfile;

        [ObservableProperty]
        private ProfileInspectionResult? _inspectionResult;

        [ObservableProperty]
        private string _cpuStatus = "Loading...";
        [ObservableProperty]
        private string _gpuStatus = "Loading...";
        [ObservableProperty]
        private string _ramStatus = "Loading...";

        [ObservableProperty]
        private string _cpuName = "";
        [ObservableProperty]
        private string _gpuName = "";
        [ObservableProperty]
        private string _ramTotal = "";
        [ObservableProperty]
        private string _storageInfo = "";

        [ObservableProperty]
        private string _cpuStatusColor = "#808080";
        [ObservableProperty]
        private string _gpuStatusColor = "#808080";
        [ObservableProperty]
        private string _ramStatusColor = "#808080";

        [ObservableProperty]
        private bool _cpuPass;
        [ObservableProperty]
        private bool _gpuPass;
        [ObservableProperty]
        private bool _ramPass;
        [ObservableProperty]
        private bool _storagePass = true;

        [ObservableProperty]
        private bool _overallPass;

        [ObservableProperty]
        private string _summaryMessage = "";

        [ObservableProperty]
        private bool _isLoading = true;

        public DashboardViewModel()
        {
            LoadData();
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

                            UpdateStatusFromResult();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateStatusFromResult()
        {
            if (InspectionResult == null) return;

            var cpuResults = InspectionResult.CpuResult;
            var gpuResults = InspectionResult.GpuResult;
            var ramResults = InspectionResult.RamResult;

            // Set part names
            CpuName = Computer?.Processor != null 
                ? $"{Computer.Processor.Manufacturer} {Computer.Processor.ModelName}" 
                : "Unknown";
            
            var gpus = Computer?.GPUs?
                .Select(g => $"{g.ModelName}")
                .ToList() ?? new List<string>();
            GpuName = string.Join("\n", gpus);
            
            var totalRamGb = Computer?.MemoryInfo?.TotalInstalledMemory / 1024 ?? 0;
            RamTotal = $"{totalRamGb} GB";

            var storageDisks = Computer?.StorageDisks?
                .Select(d => $"{d.ModelName} ({d.CapacityMB / 1024} GB)")
                .ToList() ?? new List<string>();
            StorageInfo = string.Join("\n", storageDisks);

            // CPU status
            var cpuFailCount = cpuResults.Count(r => r.Status == RequirementStatus.Fail);
            var cpuWarnCount = cpuResults.Count(r => r.Status == RequirementStatus.Warning);
            CpuPass = cpuFailCount == 0;
            if (cpuFailCount > 0)
            {
                CpuStatus = "Needs attention";
                CpuStatusColor = "#DC5040";
            }
            else if (cpuWarnCount > 0)
            {
                CpuStatus = "Could be better";
                CpuStatusColor = "#FFC107";
            }
            else
            {
                CpuStatus = "Good";
                CpuStatusColor = "#2DB44B";
            }

            // GPU status
            var gpuFailCount = gpuResults.Count(r => r.Status == RequirementStatus.Fail);
            var gpuWarnCount = gpuResults.Count(r => r.Status == RequirementStatus.Warning);
            GpuPass = gpuFailCount == 0;
            if (gpuFailCount > 0)
            {
                GpuStatus = "Needs attention";
                GpuStatusColor = "#DC5040";
            }
            else if (gpuWarnCount > 0)
            {
                GpuStatus = "Could be better";
                GpuStatusColor = "#FFC107";
            }
            else
            {
                GpuStatus = "Good";
                GpuStatusColor = "#2DB44B";
            }

            // RAM status
            var ramFailCount = ramResults.Count(r => r.Status == RequirementStatus.Fail);
            var ramWarnCount = ramResults.Count(r => r.Status == RequirementStatus.Warning);
            RamPass = ramFailCount == 0;
            if (ramFailCount > 0)
            {
                RamStatus = "Needs attention";
                RamStatusColor = "#DC5040";
            }
            else if (ramWarnCount > 0)
            {
                RamStatus = "Could be better";
                RamStatusColor = "#FFC107";
            }
            else
            {
                RamStatus = "Good";
                RamStatusColor = "#2DB44B";
            }

            StoragePass = true;

            OverallPass = CpuPass && GpuPass && RamPass && StoragePass;
            SummaryMessage = OverallPass
                ? "Your PC meets the requirements for your selected profile"
                : "Your PC does not fully meet the requirements for your selected profile";
        }

        [RelayCommand]
        private void Refresh()
        {
            IsLoading = true;
            LoadData();
        }
    }
}
