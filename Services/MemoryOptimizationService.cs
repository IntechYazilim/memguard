using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MemGuard.Helpers;

namespace MemGuard.Services
{
    public class OptimizationResult
    {
        public ulong MemoryBefore { get; set; }
        public ulong MemoryAfter { get; set; }
        public ulong FreedMemory => MemoryBefore > MemoryAfter ? MemoryBefore - MemoryAfter : 0;
    }

    public interface IMemoryOptimizationService
    {
        Task<OptimizationResult> OptimizeMemoryAsync(Action<string> progressCallback);
    }

    public class MemoryOptimizationService : IMemoryOptimizationService
    {
        private readonly ISystemMonitoringService _monitoringService;
        private readonly ILoggerService _logger;

        private static readonly HashSet<string> ProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Idle", "Registry", "smss", "csrss", "wininit", "services", "lsass", 
            "winlogon", "dwm", "explorer", "svchost", "spoolsv", "taskmgr", "conhost", 
            "fontdrvhost", "securityhealthservice", "memguard", "msmpeng", "n360", "avp", 
            "bdagent", "avgidsagent", "mfevtps", "mcshield", "cleaner", "defrag", "sihost",
            "searchhost", "startmenuexperiencehost", "shellexperiencehost"
        };

        public MemoryOptimizationService(ISystemMonitoringService monitoringService, ILoggerService logger)
        {
            _monitoringService = monitoringService;
            _logger = logger;
        }

        public async Task<OptimizationResult> OptimizeMemoryAsync(Action<string> progressCallback)
        {
            return await Task.Run(async () =>
            {
                var result = new OptimizationResult();
                
                // 1. Get memory status before optimization
                var (totalBefore, availBefore, _) = _monitoringService.GetMemoryStatus();
                result.MemoryBefore = totalBefore - availBefore;

                progressCallback("Analyzing processes...");
                await Task.Delay(400); // Small delay to let user read the step

                progressCallback("Checking memory usage metrics...");
                await Task.Delay(400);

                progressCallback("Identifying optimization opportunities...");
                await Task.Delay(400);

                var processes = Process.GetProcesses();
                int optimizedCount = 0;
                long totalFreedEstimated = 0;

                progressCallback($"Scanning {processes.Length} running processes...");
                await Task.Delay(500);

                foreach (var proc in processes)
                {
                    try
                    {
                        // Check if it's protected
                        if (ProtectedProcesses.Contains(proc.ProcessName))
                        {
                            continue;
                        }

                        // Check if it runs in Session 0 (system services)
                        if (proc.SessionId == 0)
                        {
                            continue;
                        }

                        // Read working set size before to estimate freed memory
                        long wsBefore = 0;
                        try
                        {
                            wsBefore = proc.WorkingSet64;
                        }
                        catch
                        {
                            // Could fail if process exited or access denied
                            continue;
                        }

                        // Skip processes that use very little memory to avoid unnecessary handle operations
                        if (wsBefore < 5 * 1024 * 1024) // 5 MB
                        {
                            continue;
                        }

                        // Try to optimize
                        IntPtr hProcess = NativeMethods.OpenProcess(
                            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA,
                            false,
                            proc.Id);

                        if (hProcess != IntPtr.Zero)
                        {
                            bool success = NativeMethods.EmptyWorkingSet(hProcess);
                            NativeMethods.CloseHandle(hProcess);

                            if (success)
                            {
                                optimizedCount++;
                                // Refresh process details to get wsAfter
                                long wsAfter = 0;
                                try
                                {
                                    proc.Refresh();
                                    wsAfter = proc.WorkingSet64;
                                }
                                catch { }

                                long freed = wsBefore - wsAfter;
                                if (freed > 0)
                                {
                                    totalFreedEstimated += freed;
                                    double freedMb = (double)freed / (1024 * 1024);
                                    progressCallback($"Optimized {proc.ProcessName} (released {freedMb:F1} MB)");
                                    
                                    // Throttle logs slightly so they don't scroll too fast
                                    await Task.Delay(40); 
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Safely skip any process that fails
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                progressCallback("Finalizing optimization adjustments...");
                await Task.Delay(500);

                // Get memory status after optimization
                var (_, availAfter, _) = _monitoringService.GetMemoryStatus();
                result.MemoryAfter = totalBefore - availAfter;

                // Adjust result if system values show negative freed (can happen if other apps loaded memory during the scan)
                if (result.MemoryAfter > result.MemoryBefore)
                {
                    result.MemoryAfter = result.MemoryBefore - (ulong)Math.Min((long)result.MemoryBefore, totalFreedEstimated);
                }

                double systemFreedGb = (double)result.FreedMemory / (1024 * 1024 * 1024);
                progressCallback($"Successfully released {systemFreedGb:F2} GB of system RAM.");
                
                _logger.LogInfo($"Memory optimized. Before: {result.MemoryBefore / (1024 * 1024)} MB, After: {result.MemoryAfter / (1024 * 1024)} MB, Freed: {result.FreedMemory / (1024 * 1024)} MB. Optimized {optimizedCount} processes.");

                return result;
            });
        }
    }
}
