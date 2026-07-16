using System;
using System.IO;
using System.Linq;
using MemGuard.Helpers;

namespace MemGuard.Services
{
    public interface ISystemMonitoringService
    {
        double GetCpuUsage();
        (ulong TotalMemoryBytes, ulong AvailableMemoryBytes, double UsagePercentage) GetMemoryStatus();
        (long TotalSizeBytes, long FreeSizeBytes, double UsagePercentage) GetDiskStatus();
        TimeSpan GetSystemUptime();
    }

    public class SystemMonitoringService : ISystemMonitoringService
    {
        private readonly ILoggerService _logger;
        private ulong _prevIdleTime;
        private ulong _prevKernelTime;
        private ulong _prevUserTime;

        public SystemMonitoringService(ILoggerService logger)
        {
            _logger = logger;
            // Initialize CPU metrics
            GetCpuUsage();
        }

        public double GetCpuUsage()
        {
            try
            {
                if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    return 0;
                }

                ulong currentIdle = ((ulong)idle.dwHighDateTime << 32) | (uint)idle.dwLowDateTime;
                ulong currentKernel = ((ulong)kernel.dwHighDateTime << 32) | (uint)kernel.dwLowDateTime;
                ulong currentUser = ((ulong)user.dwHighDateTime << 32) | (uint)user.dwLowDateTime;

                if (_prevIdleTime == 0 && _prevKernelTime == 0 && _prevUserTime == 0)
                {
                    _prevIdleTime = currentIdle;
                    _prevKernelTime = currentKernel;
                    _prevUserTime = currentUser;
                    return 0;
                }

                ulong idleDiff = currentIdle - _prevIdleTime;
                ulong kernelDiff = currentKernel - _prevKernelTime;
                ulong userDiff = currentUser - _prevUserTime;

                _prevIdleTime = currentIdle;
                _prevKernelTime = currentKernel;
                _prevUserTime = currentUser;

                ulong totalSys = kernelDiff + userDiff;
                if (totalSys == 0) return 0;

                // Kernel time includes Idle time in GetSystemTimes, so totalSys is the sum of kernel + user.
                // However, idleDiff represents the time the CPU was idle.
                // So CPU Load = (TotalTime - IdleTime) / TotalTime
                double cpu = (double)(totalSys - idleDiff) / totalSys * 100.0;
                return Math.Clamp(cpu, 0.0, 100.0);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error calculating CPU usage", ex);
                return 0;
            }
        }

        public (ulong TotalMemoryBytes, ulong AvailableMemoryBytes, double UsagePercentage) GetMemoryStatus()
        {
            try
            {
                var memStatus = new NativeMethods.MEMORYSTATUSEX();
                if (NativeMethods.GlobalMemoryStatusEx(memStatus))
                {
                    ulong total = memStatus.ullTotalPhys;
                    ulong avail = memStatus.ullAvailPhys;
                    ulong used = total - avail;
                    double pct = (double)used / total * 100.0;
                    return (total, avail, pct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading memory status", ex);
            }
            return (0, 0, 0);
        }

        public (long TotalSizeBytes, long FreeSizeBytes, double UsagePercentage) GetDiskStatus()
        {
            try
            {
                var systemDrivePath = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var drive = new DriveInfo(systemDrivePath);
                if (drive.IsReady)
                {
                    long total = drive.TotalSize;
                    long free = drive.TotalFreeSpace;
                    long used = total - free;
                    double pct = (double)used / total * 100.0;
                    return (total, free, pct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading disk status", ex);
            }
            return (0, 0, 0);
        }

        public TimeSpan GetSystemUptime()
        {
            try
            {
                // Environment.TickCount64 gets the number of milliseconds elapsed since the system started.
                return TimeSpan.FromMilliseconds(Environment.TickCount64);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching system uptime", ex);
                return TimeSpan.Zero;
            }
        }
    }
}
