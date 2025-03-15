using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Helpers
{
    public class Logger
    {
        private static readonly string logDirectory = "logs";
        private const long MAX_FILE_SIZE = 5; // 5MB

        public static void Write(string type, string message)
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string baseFileName = DateTime.Now.ToString("yyyy-MM-dd");
                string logFilePath = Path.Combine(logDirectory, $"{baseFileName}.log");

                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    if (fileInfo.Length >= MAX_FILE_SIZE * 1024 * 1024)
                    {
                        int index = 1;
                        while (File.Exists(Path.Combine(logDirectory, $"{baseFileName}_{index}.log")))
                        {
                            index++;
                        }
                        logFilePath = Path.Combine(logDirectory, $"{baseFileName}_{index}.log");
                    }
                }

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ {type} ] : {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
    }
}
