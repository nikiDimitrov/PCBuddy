using Newtonsoft.Json;
using PCBuddy.Models.ComputerInfo;
using PCBuddy.Models.Enums;
using PCBuddy.Models.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PCBuddy.Services
{
    public static class ProfileAnalyzerService
    {
        private static AllProfileRequirements _cachedRequirements;

        public static ProfileInspectionResult AnalyzeComputerAgainstProfile(
            Computer computer,
            UserProfileType profileType)
        {
            var requirements = GetRequirements();
            var profileRequirements = requirements.GetRequirementsForProfile(profileType);

            var result = new ProfileInspectionResult
            {
                ProfileType = profileType,
                OverallPass = true
            };

            result.CpuResult = AnalyzeCpu(computer.Processor, profileRequirements.Cpu);
            result.GpuResult = AnalyzeGpu(computer.GPUs, profileRequirements.Gpu);
            result.RamResult = AnalyzeRam(computer.MemoryInfo, profileRequirements.Ram);
            result.StorageResult = AnalyzeStorage(computer.StorageDisks, profileRequirements.Storage);

            result.OverallPass = !result.CpuResult.Any(r => r.Status == RequirementStatus.Fail) &&
                                 !result.GpuResult.Any(r => r.Status == RequirementStatus.Fail) &&
                                 !result.RamResult.Any(r => r.Status == RequirementStatus.Fail) &&
                                 !result.StorageResult.Any(r => r.Status == RequirementStatus.Fail);

            return result;
        }

        private static List<ComponentMatchResult> AnalyzeCpu(Processor cpu, CpuRequirements req)
        {
            var results = new List<ComponentMatchResult>();

            if (cpu == null)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "CPU",
                    Status = RequirementStatus.Fail,
                    StatusMessage = "No CPU detected",
                    ActualValue = "N/A",
                    RequiredValue = $"{req.MinCores} cores, {req.MinBaseClockGhz} GHz"
                });
                return results;
            }

            results.Add(new ComponentMatchResult
            {
                ComponentName = "CPU Cores",
                Status = cpu.PhysicalCoreCount >= req.MinCores ? RequirementStatus.Pass : RequirementStatus.Fail,
                StatusMessage = cpu.PhysicalCoreCount >= req.MinCores 
                    ? $"Great! {cpu.PhysicalCoreCount} cores handles multitasking and parallel workloads efficiently"
                    : $"Need {req.MinCores}+ cores. More cores help with multitasking, faster rendering, and running multiple apps smoothly",
                ActualValue = $"{cpu.PhysicalCoreCount} cores",
                RequiredValue = $"{req.MinCores} cores"
            });

            results.Add(new ComponentMatchResult
            {
                ComponentName = "CPU Threads",
                Status = cpu.ThreadCount >= req.MinThreads ? RequirementStatus.Pass : RequirementStatus.Fail,
                StatusMessage = cpu.ThreadCount >= req.MinThreads 
                    ? $"Excellent! {cpu.ThreadCount} threads provide smooth performance in demanding applications"
                    : $"Need {req.MinThreads}+ threads. More threads improve performance in video editing, streaming, and background tasks",
                ActualValue = $"{cpu.ThreadCount} threads",
                RequiredValue = $"{req.MinThreads} threads"
            });

            results.Add(new ComponentMatchResult
            {
                ComponentName = "CPU Clock",
                Status = cpu.BaseClockGhz >= req.MinBaseClockGhz ? RequirementStatus.Pass : RequirementStatus.Fail,
                StatusMessage = cpu.BaseClockGhz >= req.MinBaseClockGhz 
                    ? $"Your {cpu.BaseClockGhz:F1} GHz ensures snappy responsiveness"
                    : $"Need {req.MinBaseClockGhz:F1}+ GHz. Higher clock speeds mean faster single-threaded performance and quicker app launches",
                ActualValue = $"{cpu.BaseClockGhz:F1} GHz",
                RequiredValue = $"{req.MinBaseClockGhz:F1} GHz"
            });

            if (req.RequireVirtualization)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "Virtualization",
                    Status = cpu.HasVirtualizationEnabled ? RequirementStatus.Pass : RequirementStatus.Fail,
                    StatusMessage = cpu.HasVirtualizationEnabled 
                        ? "Virtualization is enabled - you can run virtual machines and Docker containers"
                        : "Virtualization is disabled. Enable it in BIOS to run virtual machines, Android emulators, or WSL2",
                    ActualValue = cpu.HasVirtualizationEnabled ? "Enabled" : "Disabled",
                    RequiredValue = "Enabled"
                });
            }

            return results;
        }

        private static List<ComponentMatchResult> AnalyzeGpu(List<GraphicsAdapter> gpus, GpuRequirements req)
        {
            var results = new List<ComponentMatchResult>();

            if (gpus == null || gpus.Count == 0)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "GPU",
                    Status = RequirementStatus.Fail,
                    StatusMessage = "No GPU detected",
                    ActualValue = "N/A",
                    RequiredValue = $"{req.MinVramMb} MB VRAM"
                });
                return results;
            }

            var primaryGpu = gpus.FirstOrDefault(g => g.AdapterType != GraphicsAdapterType.Integrated) ?? gpus.First();
            var vramMb = primaryGpu.VideoMemory;

            if (vramMb >= req.RecommendedVramMb)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "GPU VRAM",
                    Status = RequirementStatus.Pass,
                    StatusMessage = $"Excellent! {vramMb} MB handles high-resolution textures and multiple displays without stuttering",
                    ActualValue = $"{vramMb} MB",
                    RequiredValue = $"{req.RecommendedVramMb} MB (recommended)"
                });
            }
            else if (vramMb >= req.MinVramMb)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "GPU VRAM",
                    Status = RequirementStatus.Warning,
                    StatusMessage = $"{vramMb} MB works, but {req.RecommendedVramMb} MB would prevent texture pop-in and improve 4K gaming/editing",
                    ActualValue = $"{vramMb} MB",
                    RequiredValue = $"{req.RecommendedVramMb} MB (recommended)"
                });
            }
            else
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "GPU VRAM",
                    Status = RequirementStatus.Fail,
                    StatusMessage = $"Need {req.MinVramMb} MB. VRAM stores textures and frames - low VRAM causes stuttering in games and renders",
                    ActualValue = $"{vramMb} MB",
                    RequiredValue = $"{req.MinVramMb} MB"
                });
            }

            return results;
        }

        private static List<ComponentMatchResult> AnalyzeRam(MemoryInfo memory, RamRequirements req)
        {
            var results = new List<ComponentMatchResult>();

            if (memory == null)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "RAM",
                    Status = RequirementStatus.Fail,
                    StatusMessage = "No RAM info available",
                    ActualValue = "N/A",
                    RequiredValue = $"{req.MinGb} GB"
                });
                return results;
            }

            var totalGb = memory.TotalInstalledMemory / 1024;

            if (totalGb >= req.RecommendedGb)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "RAM",
                    Status = RequirementStatus.Pass,
                    StatusMessage = $"Great! {totalGb} GB lets you run heavy apps smoothly with room for browser tabs and background tasks",
                    ActualValue = $"{totalGb} GB",
                    RequiredValue = $"{req.RecommendedGb} GB (recommended)"
                });
            }
            else if (totalGb >= req.MinGb)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "RAM",
                    Status = RequirementStatus.Warning,
                    StatusMessage = $"{totalGb} GB is enough, but {req.RecommendedGb} GB would prevent slowdowns when multitasking",
                    ActualValue = $"{totalGb} GB",
                    RequiredValue = $"{req.RecommendedGb} GB (recommended)"
                });
            }
            else
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "RAM",
                    Status = RequirementStatus.Fail,
                    StatusMessage = $"Need {req.MinGb} GB. RAM is your system's workspace - low RAM causes freezing and slow performance",
                    ActualValue = $"{totalGb} GB",
                    RequiredValue = $"{req.MinGb} GB"
                });
            }

            return results;
        }

        private static List<ComponentMatchResult> AnalyzeStorage(List<StorageDisk> disks, StorageRequirements req)
        {
            var results = new List<ComponentMatchResult>();

            if (disks == null || disks.Count == 0)
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "Storage",
                    Status = RequirementStatus.Fail,
                    StatusMessage = "No storage detected",
                    ActualValue = "N/A",
                    RequiredValue = $"{req.MinStorageGb} GB"
                });
                return results;
            }

            var totalGb = disks.Sum(d => d.CapacityMB) / 1024;
            var hasSsd = disks.Any(d => 
                d.Interface?.ToLower().Contains("nvme") == true || 
                d.Interface?.ToLower().Contains("ssd") == true ||
                d.ModelName?.ToLower().Contains("nvme") == true ||
                d.ModelName?.ToLower().Contains("ssd") == true ||
                d.ModelName?.ToLower().Contains("solid state") == true ||
                d.ModelName?.ToLower().Contains("pc711") == true ||
                d.ModelName?.ToLower().Contains("pm9") == true ||
                d.ModelName?.ToLower().Contains("rzvls") == true ||
                (d.ModelName?.ToLower().Contains("samsung") == true && d.ModelName?.ToLower().Contains("disk") != true) ||
                (d.ModelName?.ToLower().Contains("wd") == true && (d.ModelName?.ToLower().Contains("sn") == true || d.ModelName?.ToLower().Contains("blue") == true)) ||
                d.ModelName?.ToLower().Contains("crucial p") == true ||
                d.ModelName?.ToLower().Contains("kingston a") == true ||
                d.ModelName?.ToLower().Contains("adata xpg") == true ||
                d.ModelName?.ToLower().Contains("teamgroup mp") == true
            );

            var ssdGb = disks.Where(d => 
                d.Interface?.ToLower().Contains("nvme") == true || 
                d.Interface?.ToLower().Contains("ssd") == true ||
                d.ModelName?.ToLower().Contains("nvme") == true ||
                d.ModelName?.ToLower().Contains("ssd") == true ||
                d.ModelName?.ToLower().Contains("solid state") == true ||
                d.ModelName?.ToLower().Contains("pc711") == true ||
                d.ModelName?.ToLower().Contains("pm9") == true ||
                d.ModelName?.ToLower().Contains("rzvls") == true ||
                (d.ModelName?.ToLower().Contains("samsung") == true && d.ModelName?.ToLower().Contains("disk") != true) ||
                (d.ModelName?.ToLower().Contains("wd") == true && (d.ModelName?.ToLower().Contains("sn") == true || d.ModelName?.ToLower().Contains("blue") == true)) ||
                d.ModelName?.ToLower().Contains("crucial p") == true ||
                d.ModelName?.ToLower().Contains("kingston a") == true ||
                d.ModelName?.ToLower().Contains("adata xpg") == true ||
                d.ModelName?.ToLower().Contains("teamgroup mp") == true
            ).Sum(d => d.CapacityMB) / 1024;

            var storageNote = "";
            if (hasSsd && ssdGb > 0 && ssdGb < totalGb)
            {
                storageNote = $" ({ssdGb} GB SSD + {totalGb - ssdGb} GB HDD)";
            }
            else if (hasSsd)
            {
                storageNote = " (SSD)";
            }

            if (totalGb >= req.RecommendedStorageGb && (!req.RequireSsd || hasSsd))
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "Storage",
                    Status = RequirementStatus.Pass,
                    StatusMessage = $"Great! {totalGb} GB{storageNote} provides fast load times and ample space for your projects",
                    ActualValue = $"{totalGb} GB{storageNote}",
                    RequiredValue = $"{req.RecommendedStorageGb} GB (recommended)"
                });
            }
            else if (totalGb >= req.MinStorageGb)
            {
                var message = "";
                if (req.RequireSsd && !hasSsd)
                {
                    message = $"{totalGb} GB is enough storage, but an SSD is recommended for fast load times and responsiveness";
                }
                else
                {
                    message = $"{totalGb} GB is enough, but {req.RecommendedStorageGb} GB gives you breathing room for large files";
                }

                results.Add(new ComponentMatchResult
                {
                    ComponentName = "Storage",
                    Status = req.RequireSsd && !hasSsd ? RequirementStatus.Warning : RequirementStatus.Pass,
                    StatusMessage = message,
                    ActualValue = $"{totalGb} GB{storageNote}",
                    RequiredValue = $"{req.RecommendedStorageGb} GB (recommended)"
                });
            }
            else
            {
                results.Add(new ComponentMatchResult
                {
                    ComponentName = "Storage",
                    Status = RequirementStatus.Fail,
                    StatusMessage = $"Need {req.MinStorageGb} GB. Storage holds your OS, apps, and files - low space slows everything down",
                    ActualValue = $"{totalGb} GB{storageNote}",
                    RequiredValue = $"{req.MinStorageGb} GB"
                });
            }

            return results;
        }

        public static AllProfileRequirements GetRequirements()
        {
            if (_cachedRequirements != null)
                return _cachedRequirements;

            var dataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data", "profile-requirements.json");

            if (File.Exists(dataPath))
            {
                try
                {
                    var json = File.ReadAllText(dataPath);
                    _cachedRequirements = JsonConvert.DeserializeObject<AllProfileRequirements>(json);
                    return _cachedRequirements;
                }
                catch
                {
                    // Fall through to embedded defaults
                }
            }

            _cachedRequirements = GetEmbeddedDefaults();
            return _cachedRequirements;
        }

        private static AllProfileRequirements GetEmbeddedDefaults()
        {
            return new AllProfileRequirements
            {
                Office = new ProfileRequirementsSet
                {
                    Cpu = new CpuRequirements { MinCores = 4, MinThreads = 4, MinBaseClockGhz = 2.0 },
                    Gpu = new GpuRequirements { MinVramMb = 1024, RecommendedVramMb = 2048 },
                    Ram = new RamRequirements { MinGb = 4, RecommendedGb = 8 },
                    Storage = new StorageRequirements { MinStorageGb = 128, RecommendedStorageGb = 256, RequireSsd = false }
                },
                Gaming = new ProfileRequirementsSet
                {
                    Cpu = new CpuRequirements { MinCores = 6, MinThreads = 6, MinBaseClockGhz = 3.5 },
                    Gpu = new GpuRequirements { MinVramMb = 6144, RecommendedVramMb = 8192 },
                    Ram = new RamRequirements { MinGb = 16, RecommendedGb = 32 },
                    Storage = new StorageRequirements { MinStorageGb = 512, RecommendedStorageGb = 1024, RequireSsd = true }
                },
                Programming = new ProfileRequirementsSet
                {
                    Cpu = new CpuRequirements { MinCores = 4, MinThreads = 8, MinBaseClockGhz = 3.0 },
                    Gpu = new GpuRequirements { MinVramMb = 2048, RecommendedVramMb = 4096 },
                    Ram = new RamRequirements { MinGb = 16, RecommendedGb = 32 },
                    Storage = new StorageRequirements { MinStorageGb = 256, RecommendedStorageGb = 512, RequireSsd = true }
                },
                Editing = new ProfileRequirementsSet
                {
                    Cpu = new CpuRequirements { MinCores = 6, MinThreads = 12, MinBaseClockGhz = 3.5 },
                    Gpu = new GpuRequirements { MinVramMb = 4096, RecommendedVramMb = 8192 },
                    Ram = new RamRequirements { MinGb = 32, RecommendedGb = 64 },
                    Storage = new StorageRequirements { MinStorageGb = 512, RecommendedStorageGb = 2048, RequireSsd = true }
                }
            };
        }

        public static void ClearCache()
        {
            _cachedRequirements = null;
        }
    }
}
