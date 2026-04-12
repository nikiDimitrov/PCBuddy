using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        public int MaxPoints { get; set; } = 60;
        
        public void AddMetric(SystemMetrics metric)
        {
            CpuHistory.Add(metric.CpuUsage);
            RamHistory.Add(metric.RamUsage);
            DiskHistory.Add(metric.DiskUsage);
            GpuHistory.Add(metric.GpuUsage);
            
            if (CpuHistory.Count > MaxPoints)
            {
                CpuHistory.RemoveAt(0);
                RamHistory.RemoveAt(0);
                DiskHistory.RemoveAt(0);
                GpuHistory.RemoveAt(0);
            }
        }
        
        public void Clear()
        {
            CpuHistory.Clear();
            RamHistory.Clear();
            DiskHistory.Clear();
            GpuHistory.Clear();
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
                    var script = @"
                        $cpu=(Get-CimInstance Win32_Processor).LoadPercentage
                        $os=Get-CimInstance Win32_OperatingSystem
                        $ram=[math]::Round((($os.TotalVisibleMemorySize-$os.FreePhysicalMemory)/$os.TotalVisibleMemorySize)*100,1)
                        $d=Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3' | Select-Object -First 1
                        $disk=if($d.Size){[math]::Round((($d.Size-$d.FreeSpace)/$d.Size)*100,1)}else{0}
                        $n=Get-CimInstance Win32_PerfFormattedData_Tcpip_NetworkInterface -EA SilentlyContinue | Select-Object -First 1
                        $net=if($n){[math]::Round($n.BytesReceivedPersec/1MB,2)}else{0}
                        $gpuNames=(Get-CimInstance Win32_VideoController).Name
                        $gpuResults=@()
                        foreach($gpu in $gpuNames) {
                            $usage=0
                            try {
                                $counter=Get-Counter ""\GPU Engine(*engtype_3D)"" -EA Stop
                                $usage=[math]::Round(($counter.CounterSamples | Measure-Object -Property CookedValue -Maximum).Maximum,0)
                            } catch { $usage=0 }
                            $gpuResults+=$gpu + '|||' + $usage
                        }
                        $gpuResultsStr=$gpuResults -join '|||GPU|||'
                        ""$cpu|$ram|$disk|$net|$gpuResultsStr""
                    ";
                    var result = RunPowerShell(script);
                    var parts = result.Trim().Split(new[] { '|' }, 5);
                    if (parts.Length >= 4)
                    {
                        if (double.TryParse(parts[0], out var cpu)) metrics.CpuUsage = cpu;
                        if (double.TryParse(parts[1], out var ram)) metrics.RamUsage = ram;
                        if (double.TryParse(parts[2], out var disk)) metrics.DiskUsage = disk;
                        if (double.TryParse(parts[3], out var net)) metrics.NetworkReceived = net;
                        
                        if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4]))
                        {
                            var gpuEntries = parts[4].Split(new[] { "|||GPU|||" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var entry in gpuEntries)
                            {
                                var gpuParts = entry.Split(new[] { "|||" }, StringSplitOptions.None);
                                if (gpuParts.Length >= 2)
                                {
                                    var name = gpuParts[0].Trim();
                                    if (double.TryParse(gpuParts[1], out var usage))
                                    {
                                        metrics.GpuList.Add(new GpuInfo { Name = name, Usage = $"{usage}%", UsageValue = usage });
                                    }
                                }
                            }
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
