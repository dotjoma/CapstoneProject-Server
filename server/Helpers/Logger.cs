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
        private static RichTextBox _rtbLogs = new RichTextBox(); // Reference to the RichTextBox for UI logging

        // Initialize the logger with a reference to the RichTextBox
        public static void Initialize(RichTextBox rtbConsole)
        {
            _rtbLogs = rtbConsole;

            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext() // Add contextual information
                .WriteTo.Console() // Write logs to the console
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "log-.txt"), // Log file path
                    rollingInterval: RollingInterval.Day, // Roll logs daily
                    fileSizeLimitBytes: maxFileSize, // Limit file size to 5 MB
                    retainedFileCountLimit: retainedFileCount, // Keep up to 10 log files
                    rollOnFileSizeLimit: true, // Roll files when size limit is reached
                    formatter: new CompactJsonFormatter() // Use JSON format for structured logging
                )
                .CreateLogger();
        }

        public static void Write(string type, string message)
        {
            try
            {
                // Log with structured data to Serilog (file and console)
                Log.Information("{Type}: {Message}", type, message);

                // Log to the RichTextBox (UI)
                LogMessage($"[{type}] {message}");
            }
            catch (Exception ex)
            {
                // Log errors if logging fails
                Console.Error.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public static void WriteError(string type, string message, Exception ex)
        {
            try
            {
                // Log errors with exception details to Serilog (file and console)
                Log.Error(ex, "{Type}: {Message}", type, message);

                // Log to the RichTextBox (UI)
                LogMessage($"[{type}] {message}: {ex.Message}");
            }
            catch (Exception logEx)
            {
                // Log errors if logging fails
                Console.Error.WriteLine($"Error writing to log file: {logEx.Message}");
            }
        }

        // Log a message to the RichTextBox (UI)
        private static void LogMessage(string message)
        {
            if (_rtbLogs.InvokeRequired)
            {
                _rtbLogs.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            _rtbLogs.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            _rtbLogs.ScrollToCaret();
        }

        public static void CloseAndFlush()
        {
            // Ensure all logs are written before application exits
            Log.CloseAndFlush();
        }
    }
}
