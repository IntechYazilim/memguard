using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;
using MemGuard.Models;

namespace MemGuard.Services
{
    public interface IStartupService
    {
        Task<List<StartupItem>> GetStartupItemsAsync();
        Task<bool> ToggleStartupItemAsync(StartupItem item, bool enable);
    }

    public class StartupService : IStartupService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILoggerService _logger;
        private readonly string _disabledShortcutsFolder;

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public StartupService(ISettingsService settingsService, ILoggerService logger)
        {
            _settingsService = settingsService;
            _logger = logger;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _disabledShortcutsFolder = Path.Combine(appData, "MemGuard", "DisabledStartup");
            try
            {
                if (!Directory.Exists(_disabledShortcutsFolder))
                {
                    Directory.CreateDirectory(_disabledShortcutsFolder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create disabled shortcuts directory.", ex);
            }
        }

        public async Task<List<StartupItem>> GetStartupItemsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<StartupItem>();

                // 1. Scan Registry Current User (Enabled)
                ScanRegistryRun(Registry.CurrentUser, RunKeyPath, isLocalMachine: false, isEnabled: true, list);

                // 2. Scan Registry Local Machine (Enabled)
                ScanRegistryRun(Registry.LocalMachine, RunKeyPath, isLocalMachine: true, isEnabled: true, list);

                // 3. Scan Registry Current User (Disabled - in our settings)
                foreach (var disabled in _settingsService.Current.DisabledRegistryStartups)
                {
                    list.Add(new StartupItem
                    {
                        KeyName = disabled.KeyName,
                        Name = disabled.KeyName,
                        Command = disabled.Command,
                        Location = disabled.IsLocalMachine ? "Registry (Machine - Disabled)" : "Registry (User - Disabled)",
                        IsEnabled = false,
                        Impact = EstimateImpact(disabled.Command),
                        Publisher = ResolvePublisher(disabled.Command),
                        FilePath = disabled.RegistryPath
                    });
                }

                // 4. Scan User Startup Folder (Enabled)
                var userStartupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                ScanStartupFolder(userStartupPath, isLocalMachine: false, isEnabled: true, list);

                // 5. Scan Common Startup Folder (Enabled)
                var commonStartupPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                if (!string.IsNullOrEmpty(commonStartupPath))
                {
                    ScanStartupFolder(commonStartupPath, isLocalMachine: true, isEnabled: true, list);
                }

                // 6. Scan Disabled Startup Shortcuts backed up by MemGuard
                foreach (var disabled in _settingsService.Current.DisabledFolderStartups)
                {
                    list.Add(new StartupItem
                    {
                        KeyName = disabled.KeyName,
                        Name = disabled.Name,
                        Command = disabled.DisabledPath,
                        Location = disabled.IsLocalMachine ? "Startup Folder (System - Disabled)" : "Startup Folder (User - Disabled)",
                        IsEnabled = false,
                        Impact = EstimateImpact(disabled.DisabledPath),
                        Publisher = ResolvePublisher(disabled.DisabledPath),
                        FilePath = disabled.DisabledPath
                    });
                }

                return list;
            });
        }

        private void ScanRegistryRun(RegistryKey rootKey, string path, bool isLocalMachine, bool isEnabled, List<StartupItem> list)
        {
            try
            {
                using var key = rootKey.OpenSubKey(path, writable: false);
                if (key == null) return;

                foreach (var valueName in key.GetValueNames())
                {
                    var command = key.GetValue(valueName)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(command)) continue;

                    list.Add(new StartupItem
                    {
                        KeyName = valueName,
                        Name = valueName,
                        Command = command,
                        Location = isLocalMachine ? "Registry (Machine)" : "Registry (User)",
                        IsEnabled = isEnabled,
                        Impact = EstimateImpact(command),
                        Publisher = ResolvePublisher(command),
                        FilePath = path
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not scan registry key {rootKey.Name}\\{path}: {ex.Message}");
            }
        }

        private void ScanStartupFolder(string folderPath, bool isLocalMachine, bool isEnabled, List<StartupItem> list)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;

                var directory = new DirectoryInfo(folderPath);
                // Shortcuts usually have .lnk or .url extension, or could be executables placed directly
                var files = directory.GetFiles("*.*");
                foreach (var file in files)
                {
                    string command = file.FullName;
                    string name = Path.GetFileNameWithoutExtension(file.Name);

                    // Skip temporary desktop.ini files often present in startup folders
                    if (name.Equals("desktop", StringComparison.OrdinalIgnoreCase)) continue;

                    // For disabled files, retrieve the original name if we appended metadata, or just display the filename
                    string displayName = name;
                    string locationName = isEnabled 
                        ? (isLocalMachine ? "Startup Folder (System)" : "Startup Folder (User)") 
                        : "Startup Folder (Disabled)";

                    list.Add(new StartupItem
                    {
                        KeyName = file.Name,
                        Name = displayName,
                        Command = command,
                        Location = locationName,
                        IsEnabled = isEnabled,
                        Impact = EstimateImpact(command),
                        Publisher = ResolvePublisher(command),
                        FilePath = file.FullName
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not scan startup folder '{folderPath}': {ex.Message}");
            }
        }

        public async Task<bool> ToggleStartupItemAsync(StartupItem item, bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!enable)
                    {
                        if (item.Location.StartsWith("Registry"))
                        {
                            return DisableRegistryItem(item);
                        }
                        else if (item.Location.StartsWith("Startup Folder"))
                        {
                            return DisableFolderItem(item);
                        }
                    }
                    else
                    {
                        if (item.Location.Contains("Registry"))
                        {
                            return EnableRegistryItem(item);
                        }
                        else if (item.Location.Contains("Disabled"))
                        {
                            return EnableFolderItem(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to toggle startup item: {item.Name}", ex);
                }
                return false;
            });
        }

        private bool DisableRegistryItem(StartupItem item)
        {
            bool isMachine = item.Location.Contains("Machine");
            var rootKey = isMachine ? Registry.LocalMachine : Registry.CurrentUser;

            try
            {
                using (var key = rootKey.OpenSubKey(RunKeyPath, writable: true))
                {
                    if (key == null)
                    {
                        throw new UnauthorizedAccessException($"No write permissions on registry key {rootKey.Name}\\{RunKeyPath}");
                    }
                    key.DeleteValue(item.KeyName, throwOnMissingValue: false);
                }

                // Backup to configuration settings
                _settingsService.Update(s =>
                {
                    s.DisabledRegistryStartups.Add(new DisabledRegistryStartup
                    {
                        KeyName = item.KeyName,
                        Command = item.Command,
                        RegistryPath = RunKeyPath,
                        IsLocalMachine = isMachine
                    });
                });

                _logger.LogInfo($"Disabled registry startup item: {item.KeyName}");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"Access denied when trying to disable registry item: {item.KeyName}. Run as Administrator might be required.");
                throw;
            }
        }

        private bool EnableRegistryItem(StartupItem item)
        {
            // Find in backup
            var backup = _settingsService.Current.DisabledRegistryStartups
                .FirstOrDefault(x => x.KeyName.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase));

            if (backup == null) return false;

            var rootKey = backup.IsLocalMachine ? Registry.LocalMachine : Registry.CurrentUser;

            try
            {
                using (var key = rootKey.OpenSubKey(RunKeyPath, writable: true))
                {
                    if (key == null)
                    {
                        throw new UnauthorizedAccessException($"No write permissions on registry key {rootKey.Name}\\{RunKeyPath}");
                    }
                    key.SetValue(backup.KeyName, backup.Command);
                }

                // Remove from backup
                _settingsService.Update(s =>
                {
                    s.DisabledRegistryStartups.RemoveAll(x => x.KeyName.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase));
                });

                _logger.LogInfo($"Enabled registry startup item: {item.KeyName}");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"Access denied when trying to enable registry item: {item.KeyName}. Run as Administrator might be required.");
                throw;
            }
        }

        private bool DisableFolderItem(StartupItem item)
        {
            try
            {
                if (!File.Exists(item.FilePath)) return false;

                string destPath = Path.Combine(_disabledShortcutsFolder, item.KeyName);
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }

                File.Move(item.FilePath, destPath);
                _settingsService.Update(s =>
                {
                    s.DisabledFolderStartups.RemoveAll(x => x.KeyName.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase));
                    s.DisabledFolderStartups.Add(new DisabledFolderStartup
                    {
                        KeyName = item.KeyName,
                        Name = item.Name,
                        DisabledPath = destPath,
                        OriginalFolderPath = Path.GetDirectoryName(item.FilePath) ?? string.Empty,
                        IsLocalMachine = item.Location.Contains("System", StringComparison.OrdinalIgnoreCase)
                    });
                });
                _logger.LogInfo($"Disabled folder startup item: {item.Name} (Moved to {destPath})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to disable folder startup item: {item.Name}", ex);
                throw;
            }
        }

        private bool EnableFolderItem(StartupItem item)
        {
            try
            {
                var backup = _settingsService.Current.DisabledFolderStartups
                    .FirstOrDefault(x => x.KeyName.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase));

                if (backup == null || !File.Exists(backup.DisabledPath)) return false;

                string targetFolder = !string.IsNullOrWhiteSpace(backup.OriginalFolderPath)
                    ? backup.OriginalFolderPath
                    : Environment.GetFolderPath(backup.IsLocalMachine
                        ? Environment.SpecialFolder.CommonStartup
                        : Environment.SpecialFolder.Startup);

                Directory.CreateDirectory(targetFolder);
                string destPath = Path.Combine(targetFolder, item.KeyName);

                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }

                File.Move(backup.DisabledPath, destPath);
                _settingsService.Update(s =>
                {
                    s.DisabledFolderStartups.RemoveAll(x => x.KeyName.Equals(item.KeyName, StringComparison.OrdinalIgnoreCase));
                });
                _logger.LogInfo($"Enabled folder startup item: {item.Name} (Moved back to startup folder)");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to enable folder startup item: {item.Name}", ex);
                throw;
            }
        }

        private string EstimateImpact(string command)
        {
            var cmd = command.ToLowerInvariant();
            if (cmd.Contains("discord") || cmd.Contains("teams") || cmd.Contains("chrome") || 
                cmd.Contains("spotify") || cmd.Contains("steam") || cmd.Contains("onedrive") || 
                cmd.Contains("epicgames") || cmd.Contains("msedge") || cmd.Contains("slack"))
            {
                return "High";
            }
            if (cmd.Contains("update") || cmd.Contains("helper") || cmd.Contains("assistant") || 
                cmd.Contains("agent") || cmd.Contains("service") || cmd.Contains("launcher"))
            {
                return "Medium";
            }
            return "Low";
        }

        private string ResolvePublisher(string command)
        {
            try
            {
                // Try to extract executable path (strip quotes and parameters)
                string path = command.Trim();
                if (path.StartsWith("\""))
                {
                    int nextQuote = path.IndexOf("\"", 1);
                    if (nextQuote > 0)
                    {
                        path = path.Substring(1, nextQuote - 1);
                    }
                }
                else
                {
                    int spaceIdx = path.IndexOf(" ");
                    if (spaceIdx > 0)
                    {
                        path = path.Substring(0, spaceIdx);
                    }
                }

                if (File.Exists(path))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(path);
                    return versionInfo.CompanyName ?? "Unknown";
                }
            }
            catch { }

            // Heuristics for common startup paths
            var cmdLower = command.ToLowerInvariant();
            if (cmdLower.Contains("microsoft") || cmdLower.Contains("onedrive") || cmdLower.Contains("windows"))
                return "Microsoft Corporation";
            if (cmdLower.Contains("discord")) return "Discord Inc.";
            if (cmdLower.Contains("spotify")) return "Spotify AB";
            if (cmdLower.Contains("steam")) return "Valve Corporation";
            if (cmdLower.Contains("google") || cmdLower.Contains("chrome")) return "Google LLC";

            return "Unknown";
        }
    }
}
