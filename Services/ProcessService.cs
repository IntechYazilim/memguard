using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemGuard.Models;

namespace MemGuard.Services
{
    public interface IProcessService
    {
        Task<List<ProcessItem>> GetProcessesAsync();
        Task<bool> KillProcessAsync(int pid);
        bool IsProcessCritical(string processName);
    }

    public class ProcessService : IProcessService
    {
        private readonly ILoggerService _logger;
        
        // Cache process paths and publishers to optimize query speeds (FileSystem I/O can be slow)
        private readonly Dictionary<string, string> _publisherCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _pathCache = new(StringComparer.OrdinalIgnoreCase);
        
        // Track CPU usage times
        private Dictionary<int, (TimeSpan ProcessorTime, DateTime Timestamp)> _cpuHistory = new();

        private static readonly HashSet<string> CriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Idle", "Registry", "smss", "csrss", "wininit", "services", "lsass", 
            "winlogon", "dwm", "explorer", "svchost", "spoolsv", "taskmgr", "conhost", 
            "fontdrvhost", "securityhealthservice", "memguard", "sihost",
            "searchhost", "startmenuexperiencehost", "shellexperiencehost"
        };

        public ProcessService(ILoggerService logger)
        {
            _logger = logger;
        }

        public bool IsProcessCritical(string processName)
        {
            return CriticalProcessNames.Contains(processName) || 
                   processName.Equals(Process.GetCurrentProcess().ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<ProcessItem>> GetProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var currentProcesses = Process.GetProcesses();
                var items = new List<ProcessItem>();
                var nextCpuHistory = new Dictionary<int, (TimeSpan ProcessorTime, DateTime Timestamp)>();
                
                var now = DateTime.UtcNow;
                int processorCount = Environment.ProcessorCount;

                foreach (var proc in currentProcesses)
                {
                    try
                    {
                        var name = proc.ProcessName;
                        var pid = proc.Id;
                        
                        // We skip System Idle Process (Id 0) from termination list, but we can list it.
                        bool isCritical = IsProcessCritical(name) || proc.SessionId == 0 || pid == 0;

                        long memBytes = 0;
                        try
                        {
                            memBytes = proc.WorkingSet64;
                        }
                        catch
                        {
                            // Access denied to memory stats of some system processes
                        }

                        DateTime? startTime = null;
                        try
                        {
                            startTime = proc.StartTime;
                        }
                        catch
                        {
                            // Can throw access denied for system processes
                        }

                        // Calculate CPU usage
                        double cpuPct = 0;
                        try
                        {
                            var currentProcTime = proc.TotalProcessorTime;
                            nextCpuHistory[pid] = (currentProcTime, now);

                            if (_cpuHistory.TryGetValue(pid, out var oldRecord))
                            {
                                double totalMs = (currentProcTime - oldRecord.ProcessorTime).TotalMilliseconds;
                                double elapsedMs = (now - oldRecord.Timestamp).TotalMilliseconds;
                                if (elapsedMs > 100) // avoid division by zero / tiny values
                                {
                                    cpuPct = (totalMs / (elapsedMs * processorCount)) * 100.0;
                                }
                            }
                        }
                        catch
                        {
                            // Catch protected processes
                        }

                        // Resolve Path and Publisher with caching
                        string execPath = string.Empty;
                        string publisher = "Microsoft Corporation"; // default fallback for system

                        if (!isCritical || (name.Equals("explorer", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!_pathCache.TryGetValue(name, out execPath!))
                            {
                                try
                                {
                                    execPath = proc.MainModule?.FileName ?? string.Empty;
                                    _pathCache[name] = execPath;
                                }
                                catch
                                {
                                    execPath = string.Empty;
                                }
                            }

                            if (!string.IsNullOrEmpty(execPath))
                            {
                                if (!_publisherCache.TryGetValue(execPath, out publisher!))
                                {
                                    try
                                    {
                                        var versionInfo = FileVersionInfo.GetVersionInfo(execPath);
                                        publisher = versionInfo.CompanyName ?? "Unknown";
                                        _publisherCache[execPath] = publisher;
                                    }
                                    catch
                                    {
                                        publisher = "Unknown";
                                    }
                                }
                            }
                            else
                            {
                                publisher = "Unknown";
                            }
                        }

                        items.Add(new ProcessItem
                        {
                            Id = pid,
                            Name = name,
                            MemoryBytes = memBytes,
                            CpuUsage = Math.Clamp(cpuPct, 0.0, 100.0),
                            ExecutablePath = execPath,
                            Publisher = publisher,
                            StartTime = startTime,
                            IsCritical = isCritical
                        });
                    }
                    catch
                    {
                        // Ignore individual process failures
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                _cpuHistory = nextCpuHistory;
                return items.OrderByDescending(x => x.MemoryBytes).ToList();
            });
        }

        public async Task<bool> KillProcessAsync(int pid)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var proc = Process.GetProcessById(pid);
                    if (IsProcessCritical(proc.ProcessName) || proc.SessionId == 0)
                    {
                        _logger.LogWarning($"Attempted to kill critical system process: {proc.ProcessName} (PID: {pid}). Action blocked.");
                        return false;
                    }

                    proc.Kill();
                    _logger.LogInfo($"User closed process: {proc.ProcessName} (PID: {pid})");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to terminate process PID: {pid}", ex);
                    return false;
                }
            });
        }
    }
}
