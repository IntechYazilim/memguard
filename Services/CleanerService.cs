using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MemGuard.Models;

namespace MemGuard.Services
{
    public interface ICleanerService
    {
        Task<List<CleanCategory>> GetCategoriesAsync();
        Task ScanCategoryAsync(CleanCategory category);
        Task<long> CleanCategoryAsync(CleanCategory category);
    }

    public class CleanerService : ICleanerService
    {
        private readonly ILoggerService _logger;

        // P/Invoke for Recycle Bin
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        public CleanerService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task<List<CleanCategory>> GetCategoriesAsync()
        {
            return await Task.Run(() =>
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                var userTemp = Path.GetTempPath();

                var categories = new List<CleanCategory>
                {
                    new()
                    {
                        Key = "WinTemp",
                        Name = "Windows Temporary Files",
                        Description = "Temporary files created by Windows system services. Safe to delete.",
                        Targets = new List<string> { winTemp }
                    },
                    new()
                    {
                        Key = "UserTemp",
                        Name = "User Temporary Files",
                        Description = "Temporary files created by running applications under your profile.",
                        Targets = new List<string> { userTemp }
                    },
                    new()
                    {
                        Key = "ThumbCache",
                        Name = "Thumbnail Cache",
                        Description = "Image and folder preview database files created by Windows Explorer.",
                        Targets = new List<string> { Path.Combine(localAppData, "Microsoft", "Windows", "Explorer") }
                    },
                    new()
                    {
                        Key = "ShaderCache",
                        Name = "DirectX Shader Cache",
                        Description = "Graphics pipeline precompiled shaders. Re-generated automatically by GPU drivers.",
                        Targets = new List<string>
                        {
                            Path.Combine(localAppData, "D3DSCache"),
                            Path.Combine(localAppData, "NVIDIA", "DXCache"),
                            Path.Combine(localAppData, "AMD", "DxCache")
                        }
                    },
                    new()
                    {
                        Key = "RecycleBin",
                        Name = "Recycle Bin",
                        Description = "Files you deleted that are kept in the trash storage.",
                        Targets = new List<string>() // Handled specifically via shell API
                    },
                    new()
                    {
                        Key = "AppCache",
                        Name = "Application Cache",
                        Description = "Cache files from browsers (Chrome, Edge) and apps (Spotify). Sessions and passwords are untouched.",
                        Targets = new List<string>
                        {
                            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache"),
                            Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache"),
                            Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"),
                            Path.Combine(localAppData, "Spotify", "Storage")
                        }
                    }
                };

                return categories;
            });
        }

        public async Task ScanCategoryAsync(CleanCategory category)
        {
            await Task.Run(() =>
            {
                category.Status = "Scanning...";
                long totalSize = 0;
                int totalFiles = 0;

                if (category.Key == "RecycleBin")
                {
                    try
                    {
                        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
                        int hr = SHQueryRecycleBin(null, ref info);
                        if (hr == 0)
                        {
                            totalSize = info.i64Size;
                            totalFiles = (int)info.i64NumItems;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error scanning Recycle Bin", ex);
                    }
                }
                else
                {
                    foreach (var target in category.Targets)
                    {
                        if (!Directory.Exists(target)) continue;

                        try
                        {
                            var dirInfo = new DirectoryInfo(target);
                            var result = ScanDirectory(dirInfo, category.Key == "ThumbCache");
                            totalSize += result.Size;
                            totalFiles += result.Files;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error scanning directory: {target}", ex);
                        }
                    }
                }

                category.SizeBytes = totalSize;
                category.FileCount = totalFiles;
                category.Status = totalSize == 0 ? "Clean" : category.SizeFormatted;
            });
        }

        public async Task<long> CleanCategoryAsync(CleanCategory category)
        {
            return await Task.Run(() =>
            {
                category.Status = "Cleaning...";
                long bytesFreed = 0;

                if (category.Key == "RecycleBin")
                {
                    try
                    {
                        // Get size before emptying
                        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
                        SHQueryRecycleBin(null, ref info);
                        long sizeBefore = info.i64Size;

                        int hr = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                        if (hr == 0)
                        {
                            bytesFreed = sizeBefore;
                            _logger.LogInfo("Recycle Bin emptied successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error clearing Recycle Bin", ex);
                    }
                }
                else
                {
                    foreach (var target in category.Targets)
                    {
                        if (!Directory.Exists(target)) continue;

                        try
                        {
                            var dirInfo = new DirectoryInfo(target);
                            bytesFreed += CleanDirectory(dirInfo, category.Key == "ThumbCache");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error cleaning directory: {target}", ex);
                        }
                    }
                }

                category.SizeBytes = 0;
                category.FileCount = 0;
                category.Status = "Cleaned";
                
                _logger.LogInfo($"Cleaner category '{category.Name}' executed. Released {(bytesFreed / (1024.0 * 1024)):F1} MB.");
                return bytesFreed;
            });
        }

        private (long Size, int Files) ScanDirectory(DirectoryInfo dir, bool filterThumbcache)
        {
            long size = 0;
            int files = 0;

            try
            {
                // Scan files in current directory
                var fileSearch = filterThumbcache ? "thumbcache_*.db" : "*";
                var fileInfos = dir.GetFiles(fileSearch, SearchOption.TopDirectoryOnly);
                foreach (var file in fileInfos)
                {
                    try
                    {
                        size += file.Length;
                        files++;
                    }
                    catch { }
                }

                // Scan subdirectories recursively
                var subDirs = dir.GetDirectories();
                foreach (var subDir in subDirs)
                {
                    var subResult = ScanDirectory(subDir, filterThumbcache);
                    size += subResult.Size;
                    files += subResult.Files;
                }
            }
            catch
            {
                // Access denied or directory removed
            }

            return (size, files);
        }

        private long CleanDirectory(DirectoryInfo dir, bool filterThumbcache)
        {
            long freed = 0;

            try
            {
                var fileSearch = filterThumbcache ? "thumbcache_*.db" : "*";
                var fileInfos = dir.GetFiles(fileSearch, SearchOption.TopDirectoryOnly);
                foreach (var file in fileInfos)
                {
                    try
                    {
                        long len = file.Length;
                        file.Delete();
                        freed += len;
                    }
                    catch (IOException)
                    {
                        // File is locked/in-use, skip silently
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Access denied, skip silently
                    }
                    catch
                    {
                        // Other errors, skip silently
                    }
                }

                var subDirs = dir.GetDirectories();
                foreach (var subDir in subDirs)
                {
                    freed += CleanDirectory(subDir, filterThumbcache);

                    // Try to delete directory if it is now empty and not a thumbnail folder
                    if (!filterThumbcache)
                    {
                        try
                        {
                            subDir.Delete(false);
                        }
                        catch
                        {
                            // Ignore if folder is locked or not empty
                        }
                    }
                }
            }
            catch
            {
                // Access denied to directory
            }

            return freed;
        }
    }
}
