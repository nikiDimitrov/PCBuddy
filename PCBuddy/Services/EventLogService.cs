using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCBuddy.Services
{
    public class EventLogEntry
    {
        public DateTime TimeGenerated { get; set; }
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public string Level { get; set; } = "";
        public int EventId { get; set; }
    }

    public static class EventLogService
    {
        public static async Task<List<EventLogEntry>> GetSystemEventsAsync(string logName = "System", int maxEvents = 100, CancellationToken token = default)
        {
            return await Task.Run(() =>
            {
                var entries = new List<EventLogEntry>();
                token.ThrowIfCancellationRequested();

                try
                {
                    var psScript = $@"
                        Get-WinEvent -LogName '{logName}' -MaxEvents {maxEvents} 2>$null | ForEach-Object {{
                            $level = switch ($_.Level) {{ 
                                1 {{ 'Error' }} 
                                2 {{ 'Warning' }} 
                                3 {{ 'Info' }}
                                4 {{ 'Audit' }}
                                5 {{ 'Audit' }}
                                default {{ 'Info' }}
                            }}
                            [PSCustomObject]@{{
                                TimeGenerated = $_.TimeCreated.ToString('o')
                                Source = $_.ProviderName
                                Message = $_.Message
                                Level = $level
                                EventId = $_.Id
                            }}
                        }} | ConvertTo-Json -Compress
                    ";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                    {
                        entries.Add(new EventLogEntry
                        {
                            TimeGenerated = DateTime.Now,
                            Source = "Error",
                            Message = "Failed to start PowerShell process",
                            Level = "Error",
                            EventId = 0
                        });
                        return entries;
                    }

                    process.PriorityClass = ProcessPriorityClass.BelowNormal;
                    var output = process.StandardOutput.ReadToEnd();
                    
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.WaitForExit();

                    if (string.IsNullOrWhiteSpace(output) || output.Trim() == "null")
                    {
                        return entries;
                    }

                    if (output.Trim().StartsWith("["))
                    {
                        var jsonArray = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);
                        if (jsonArray != null)
                        {
                            foreach (var item in jsonArray)
                            {
                                entries.Add(ParseEventEntry(item));
                            }
                        }
                    }
                    else if (output.Trim().StartsWith("{"))
                    {
                        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(output);
                        if (jsonObject != null)
                        {
                            entries.Add(ParseEventEntry(jsonObject));
                        }
                    }
                }
                catch (Exception ex)
                {
                    entries.Add(new EventLogEntry
                    {
                        TimeGenerated = DateTime.Now,
                        Source = "Error",
                        Message = $"Failed to read event log: {ex.Message}",
                        Level = "Error",
                        EventId = 0
                    });
                }

                return entries;
            });
        }

        private static EventLogEntry ParseEventEntry(Dictionary<string, object> item)
        {
            var entry = new EventLogEntry();

            if (item.TryGetValue("TimeGenerated", out var time) && time != null)
            {
                if (DateTime.TryParse(time.ToString(), out var parsedTime))
                {
                    entry.TimeGenerated = parsedTime;
                }
            }

            if (item.TryGetValue("Source", out var source) && source != null)
            {
                entry.Source = source.ToString() ?? "";
            }

            if (item.TryGetValue("Message", out var message) && message != null)
            {
                entry.Message = message.ToString() ?? "";
            }

            if (item.TryGetValue("Level", out var level) && level != null)
            {
                entry.Level = level.ToString() ?? "Info";
            }

            if (item.TryGetValue("EventId", out var eventId) && eventId != null)
            {
                if (eventId is System.Text.Json.JsonElement jsonElement)
                {
                    entry.EventId = jsonElement.GetInt32();
                }
                else if (int.TryParse(eventId.ToString(), out var parsedId))
                {
                    entry.EventId = parsedId;
                }
            }

            return entry;
        }

        public static async Task<List<EventLogEntry>> GetApplicationEventsAsync(int maxEvents = 100, CancellationToken token = default)
        {
            return await GetSystemEventsAsync("Application", maxEvents, token);
        }

        public static async Task<List<EventLogEntry>> GetErrorEventsAsync(int maxEvents = 1000, CancellationToken token = default)
        {
            return await Task.Run(() =>
            {
                var entries = new List<EventLogEntry>();
                token.ThrowIfCancellationRequested();

                try
                {
                    var psScript = $@"
                        Get-WinEvent -LogName 'System' -MaxEvents 2000 2>$null | Where-Object {{ $_.Level -eq 1 -or $_.Level -eq 2 }} | Select-Object -First {maxEvents} | ForEach-Object {{
                            [PSCustomObject]@{{
                                TimeGenerated = $_.TimeCreated.ToString('o')
                                Source = $_.ProviderName
                                Message = $_.Message
                                Level = if ($_.Level -eq 1) {{ 'Error' }} else {{ 'Warning' }}
                                EventId = $_.Id
                            }}
                        }} | ConvertTo-Json -Compress
                    ";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return entries;

                    process.PriorityClass = ProcessPriorityClass.BelowNormal;
                    var output = process.StandardOutput.ReadToEnd();
                    
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.WaitForExit();

                    if (string.IsNullOrWhiteSpace(output) || output.Trim() == "null") return entries;

                    if (output.Trim().StartsWith("["))
                    {
                        var jsonArray = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);
                        if (jsonArray != null)
                        {
                            foreach (var item in jsonArray)
                            {
                                entries.Add(ParseEventEntry(item));
                            }
                        }
                    }
                    else if (output.Trim().StartsWith("{"))
                    {
                        var jsonObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(output);
                        if (jsonObject != null)
                        {
                            entries.Add(ParseEventEntry(jsonObject));
                        }
                    }
                }
                catch (Exception ex)
                {
                    entries.Add(new EventLogEntry
                    {
                        TimeGenerated = DateTime.Now,
                        Source = "Error",
                        Message = $"Failed to read event log: {ex.Message}",
                        Level = "Error",
                        EventId = 0
                    });
                }

                return entries;
            });
        }
    }
}
