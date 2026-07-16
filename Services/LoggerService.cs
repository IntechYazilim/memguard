using System;
using System.IO;
using System.Text;

namespace MemGuard.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public interface ILoggerService
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
        string GetLogFilePath();
    }

    public class LoggerService : ILoggerService
    {
        private static readonly object _lock = new();
        private readonly string _logDir;
        private readonly string _logFile;

        public LoggerService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDir = Path.Combine(appData, "MemGuard", "logs");
            _logFile = Path.Combine(_logDir, "app.log");

            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
            catch
            {
                // Fallback to local execution directory if AppData fails
                _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                _logFile = Path.Combine(_logDir, "app.log");
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
        }

        public string GetLogFilePath() => _logFile;

        public void LogInfo(string message) => WriteLog(LogLevel.Info, message);

        public void LogWarning(string message) => WriteLog(LogLevel.Warning, message);

        public void LogError(string message, Exception? ex = null)
        {
            var sb = new StringBuilder();
            sb.Append(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {ex.GetType().Name} - {ex.Message}");
                sb.AppendLine();
                sb.Append(ex.StackTrace);
            }
            WriteLog(LogLevel.Error, sb.ToString());
        }

        private void WriteLog(LogLevel level, string message)
        {
            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFile, logLine, Encoding.UTF8);
                }
                catch
                {
                    // Fail silently to prevent crashing the optimizer if logger fails
                }
            }
        }
    }
}
