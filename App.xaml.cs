using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;

namespace MemGuard;

public partial class App : Application
{
    private const string SingleInstancePrefix = @"Local\MemGuard_SingleInstance_";
    private const string RestoreSignalPrefix = @"Local\MemGuard_RestoreSignal_";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _restoreSignal;
    private RegisteredWaitHandle? _restoreWaitHandle;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        var instanceKey = BuildInstanceKey();
        var mutexName = SingleInstancePrefix + instanceKey;
        var signalName = RestoreSignalPrefix + instanceKey;

        try
        {
            _instanceMutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out var createdNew);
            _ownsMutex = createdNew;
            if (!createdNew)
            {
                SignalRunningInstance(signalName);
                Shutdown();
                return;
            }

            _restoreSignal = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
            _restoreWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                _restoreSignal,
                (_, _) => Dispatcher.BeginInvoke(new Action(RestoreExistingWindow)),
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            if (e.Args.Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)))
            {
                mainWindow.Show();
                mainWindow.StartHiddenInTray();
                return;
            }

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            WriteStartupError(ex);
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _restoreWaitHandle?.Unregister(null);
        _restoreSignal?.Dispose();

        if (_instanceMutex != null)
        {
            if (_ownsMutex)
            {
                _instanceMutex.ReleaseMutex();
            }

            _instanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    private static string BuildInstanceKey()
    {
        var exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(exePath.ToLowerInvariant()));
        return Convert.ToHexString(hash);
    }

    private static void SignalRunningInstance(string signalName)
    {
        try
        {
            using var restoreSignal = EventWaitHandle.OpenExisting(signalName);
            restoreSignal.Set();
        }
        catch
        {
            // If the signal is unavailable, just exit quietly.
        }
    }

    private void RestoreExistingWindow()
    {
        if (MainWindow is MainWindow window)
        {
            window.RestoreFromExternalLaunch();
        }
    }

    private static void WriteStartupError(Exception ex)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDir = Path.Combine(appData, "MemGuard", "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "startup-errors.log");
            var lines = new[]
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Startup failure",
                ex.ToString(),
                string.Empty
            };
            File.AppendAllLines(logPath, lines);
        }
        catch
        {
            // Swallow logging failures.
        }
    }
}
