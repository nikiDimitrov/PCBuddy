using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCBuddy.Services
{
    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double RamUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkSent { get; set; }
        public double NetworkReceived { get; set; }
        public double CpuTemperature { get; set; }
        public double GpuTemperature { get; set; }
        public double GpuUsage { get; set; }
        public List<GpuInfo> GpuList { get; set; } = new();
    }
    
    public class GpuInfo
    {
        public string Name { get; set; } = "";
        public string Usage { get; set; } = "0%";
        public double UsageValue { get; set; }
    }

    public class MetricHistory
    {
        public List<double> CpuHistory { get; set; } = new();
        public List<double> RamHistory { get; set; } = new();
        public List<double> DiskHistory { get; set; } = new();
        public List<double> GpuHistory { get; set; } = new();
        public List<List<double>> GpuHistories { get; set; } = new();
        
        public int MaxPoints { get; set; } = 60;
        
        public void AddMetric(SystemMetrics metric)
        {
            CpuHistory.Add(metric.CpuUsage);
            RamHistory.Add(metric.RamUsage);
            DiskHistory.Add(metric.DiskUsage);
            GpuHistory.Add(metric.GpuUsage);
            
            while (GpuHistories.Count < metric.GpuList.Count)
            {
                GpuHistories.Add(new List<double>());
            }
            
            for (int i = 0; i < metric.GpuList.Count; i++)
            {
                if (i < GpuHistories.Count)
                {
                    GpuHistories[i].Add(metric.GpuList[i].UsageValue);
                }
            }
            
            if (CpuHistory.Count > MaxPoints)
            {
                CpuHistory.RemoveAt(0);
                RamHistory.RemoveAt(0);
                DiskHistory.RemoveAt(0);
                GpuHistory.RemoveAt(0);
                foreach (var gh in GpuHistories)
                {
                    if (gh.Count > 0) gh.RemoveAt(0);
                }
            }
        }
        
        public void Clear()
        {
            CpuHistory.Clear();
            RamHistory.Clear();
            DiskHistory.Clear();
            GpuHistory.Clear();
            foreach (var gh in GpuHistories)
            {
                gh.Clear();
            }
        }
    }

    public static class PerformanceMonitorService
    {
        private static DateTime _lastNetworkCheck = DateTime.MinValue;
        private static long _lastBytesSent = 0;
        private static long _lastBytesReceived = 0;

        public static async Task<SystemMetrics> GetCurrentMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new SystemMetrics
                {
                    Timestamp = DateTime.Now
                };

                try
                {
                    var script = @"(Get-CimInstance Win32_Processor -Property LoadPercentage).LoadPercentage;$os=Get-CimInstance Win32_OperatingSystem -Property TotalVisibleMemorySize,FreePhysicalMemory;([math]::Round((($os.TotalVisibleMemorySize-$os.FreePhysicalMemory)/$os.TotalVisibleMemorySize)*100,1));(Get-CimInstance Win32_PerfFormattedData_Tcpip_NetworkInterface -Property BytesReceivedPersec -MaxResultCount 1).BytesReceivedPersec/1MB";
                    var result = RunPowerShell(script);
                    var lines = result.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 1 && double.TryParse(lines[0].Trim(), out var cpu)) metrics.CpuUsage = cpu;
                    if (lines.Length >= 2 && double.TryParse(lines[1].Trim(), out var ram)) metrics.RamUsage = ram;
                    if (lines.Length >= 3 && double.TryParse(lines[2].Trim(), out var net)) metrics.NetworkReceived = net;
                    
                    var diskScript = @"$d=Get-CimInstance Win32_LogicalDisk -Property Size,FreeSpace -Filter 'DriveType=3';if($d.Size){$t=($d|Measure-Object -Property Size -Sum).Sum;$f=($d|Measure-Object -Property FreeSpace -Sum).Sum;if($t){[math]::Round((($t-$f)/$t)*100,1)}}else{0}";
                    var diskResult = RunPowerShell(diskScript);
                    if (double.TryParse(diskResult.Trim(), out var disk)) metrics.DiskUsage = disk;
                    
                    var gpuScript = @"(Get-CimInstance Win32_VideoController -Property Name).Name";
                    var gpuResult = RunPowerShell(gpuScript);
                    var gpuLines = gpuResult.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    var allGpuUtilsScript = @"(Get-Counter '\GPU Engine(*engtype_3D)' -EA SilentlyContinue | Select-Object -ExpandProperty CounterSamples | ForEach-Object { $_.CookedValue })";
                    var allGpuUtilsResult = RunPowerShell(allGpuUtilsScript);
                    var allGpuUtils = allGpuUtilsResult.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => { double.TryParse(s.Trim(), out var v); return v; })
                        .Where(v => v > 0)
                        .ToList();
                    
                    int gpuIndex = 0;
                    foreach (var gpuName in gpuLines)
                    {
                        var name = gpuName.Trim();
                        if (!string.IsNullOrEmpty(name) && !name.Contains("Microsoft Basic"))
                        {
                            double usage = 0;
                            if (allGpuUtils.Count > 0)
                            {
                                if (gpuIndex < allGpuUtils.Count)
                                    usage = allGpuUtils[gpuIndex];
                                else
                                    usage = allGpuUtils[0];
                            }
                            metrics.GpuList.Add(new GpuInfo { Name = name, Usage = $"{usage}%", UsageValue = usage });
                            gpuIndex++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Metrics error: {ex.Message}");
                }

                return metrics;
            });
        }

        private static double GetCpuUsage()
        {
            try
            {
                var script = @"
                    $cpu = Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average
                    [math]::Round($cpu.Average, 1)
                ";
                
                var result = RunPowerShell(script);
                if (double.TryParse(result.Trim(), out var usage))
                    return usage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CPU error: {ex.Message}");
            }
            return 0;
        }

        private static double GetRamUsage()
        {
            try
            {
                var script = @"
                    $os = Get-CimInstance Win32_OperatingSystem
                    [math]::Round((($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize) * 100, 1)
                ";
                
                var result = RunPowerShell(script);
                if (double.TryParse(result.Trim(), out var usage))
                    return usage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RAM error: {ex.Message}");
            }
            return 0;
        }

        private static double GetDiskUsage()
        {
            try
            {
                var script = @"
                    $disks = Get-CimInstance Win32_LogicalDisk -Filter ""DriveType=3""
                    $total = ($disks | Measure-Object -Property Size -Sum).Sum
                    $free = ($disks | Measure-Object -Property FreeSpace -Sum).Sum
                    if ($total -and $total -gt 0) {
                        [math]::Round((($total - $free) / $total) * 100, 1)
                    } else { 0 }
                ";
                
                var result = RunPowerShell(script);
                if (double.TryParse(result.Trim(), out var usage))
                    return usage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disk error: {ex.Message}");
            }
            return 0;
        }

        private static double GetGpuUsage()
        {
            try
            {
                var script = @"
                    try {
                        $gpu = Get-CimInstance MSAcpi_ThermalZoneTemperature -Namespace ""root/WMI"" -ErrorAction Stop | Select-Object -First 1
                        if ($gpu) {
                            $temp = [math]::Round(($gpu.CurrentTemperature - 2732) / 10, 1)
                            Write-Output $temp
                        } else { Write-Output '0' }
                    } catch {
                        Write-Output '0'
                    }
                ";
                
                var result = RunPowerShell(script);
                if (double.TryParse(result.Trim(), out var temp))
                    return temp;
            }
            catch { }
            return 0;
        }

        private static (double sent, double received) GetNetworkUsage()
        {
            try
            {
                var script = @"
                    $nics = Get-CimInstance Win32_PerfFormattedData_Tcpip_NetworkInterface -ErrorAction SilentlyContinue
                    if ($nics) {
                        $sent = ($nics | Measure-Object -Property BytesSentPersec -Sum).Sum
                        $recv = ($nics | Measure-Object -Property BytesReceivedPersec -Sum).Sum
                        [math]::Round($sent / 1MB, 2), [math]::Round($recv / 1MB, 2)
                    } else { '0', '0' }
                ";
                
                var result = RunPowerShell(script);
                var parts = result.Trim().Split(',');
                if (parts.Length >= 2 && double.TryParse(parts[0], out var sent) && double.TryParse(parts[1], out var recv))
                    return (sent, recv);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}");
            }
            return (0, 0);
        }

        private static string RunPowerShell(string script)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "";
            
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
            var output = process.StandardOutput.ReadToEnd();
            
            if (!process.HasExited)
                process.Kill();
            process.WaitForExit(3000);
            
            return output;
        }

        public static async Task StartMonitoringAsync(Func<SystemMetrics, Task> onMetricsUpdate, int intervalMs = 1000, CancellationToken token = default)
        {
            System.Diagnostics.Debug.WriteLine("=== StartMonitoringAsync started ===");
            
            int count = 0;
            while (!token.IsCancellationRequested)
            {
                count++;
                var metrics = await GetCurrentMetricsAsync();
                System.Diagnostics.Debug.WriteLine($"Tick {count}: CPU={metrics.CpuUsage}%");
                await onMetricsUpdate(metrics);
                await Task.Delay(intervalMs, token);
            }
        }
    }
}
