using Microsoft.VisualBasic;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Helpers
{
    public static class Logger
    {
        private static readonly string logDirectory = "logs";
        private const long maxFileSize = 5 * 1024 * 1024; // 5 MB
        private const int retainedFileCount = 10; // Keep up to 10 log files
        private static RichTextBox? _rtbLogs;

        public static event Action<string>? OnLogMessage;
        public static RichTextBox? LogControl => _rtbLogs;

        public static void Initialize(RichTextBox rtbConsole)
        {
            _rtbLogs = rtbConsole;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: maxFileSize,
                    retainedFileCountLimit: retainedFileCount,
                    rollOnFileSizeLimit: true,
                    formatter: new CompactJsonFormatter()
                )
                .CreateLogger();
        }

        public static void Write(string type, string message)
        {
            try
            {
                string formattedMessage = $"[{type}] {message}";
                Log.Information(formattedMessage);
                OnLogMessage?.Invoke(formattedMessage);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing to log: {ex.Message}");
            }
        }

        public static void WriteError(string type, string message, Exception ex)
        {
            try
            {
                Log.Error(ex, "{Type}: {Message}", type, message);
                LogMessage($"[{type}] {message}: {ex.Message}");
            }
            catch (Exception logEx)
            {
                Console.Error.WriteLine($"Error writing to log file: {logEx.Message}");
            }
        }

        private static void LogMessage(string message)
        {
            try
            {
                string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                // File logging (existing)
                Log.Information(formattedMessage);

                // RichTextBox logging (existing)
                if (_rtbLogs != null && !_rtbLogs.IsDisposed)
                {
                    if (_rtbLogs.InvokeRequired)
                    {
                        _rtbLogs.Invoke(new Action(() => LogMessage(message)));
                    }
                    else
                    {
                        _rtbLogs.AppendText($"{formattedMessage}{Environment.NewLine}");
                        _rtbLogs.ScrollToCaret();
                    }
                }

                // Notify SystemLogs form
                OnLogMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in LogMessage: {ex.Message}");
            }
        }

        public static void ClearLogs()
        {
            if (_rtbLogs != null && !_rtbLogs.IsDisposed)
            {
                if (_rtbLogs.InvokeRequired)
                {
                    _rtbLogs.Invoke(new Action(ClearLogs));
                }
                else
                {
                    _rtbLogs.Clear();
                }
            }
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}