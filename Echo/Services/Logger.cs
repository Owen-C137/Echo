using System;
using System.Diagnostics;
using System.IO;

namespace Echo.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public static class Logger
    {
        private static string? _logFilePath;
        private static readonly object _lockObj = new object();

        public static void Initialize()
        {
            try
            {
                // Get the application directory
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _logFilePath = Path.Combine(appDir, "Logs.txt");

                // Create or clear the log file
                lock (_lockObj)
                {
                    File.WriteAllText(_logFilePath, $"=== Echo Application Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }

                Info("Logger initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        private static void Log(LogLevel level, string message)
        {
            if (string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";

                lock (_lockObj)
                {
                    File.AppendAllText(_logFilePath, logEntry);
                }

                // Also output to debug console
                System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public static void Error(string message, Exception ex)
        {
            Log(LogLevel.Error, $"{message} - Exception: {ex.Message}");
            Log(LogLevel.Error, $"Stack Trace: {ex.StackTrace}");
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void OpenLogFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFilePath,
                        UseShellExecute = true
                    });
                    Info("Log file opened");
                }
            }
            catch (Exception ex)
            {
                Error("Failed to open log file", ex);
            }
        }
    }
}
