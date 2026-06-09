using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EQModeChangeSimulator
{
    public static class LocalFileLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, DateTime> LastWriteTimes =
            new Dictionary<string, DateTime>();

        public static void Info(string module, string message)
        {
            Write("INFO", module, message, 2);
        }

        public static void Warn(string module, string message)
        {
            Write("WARN", module, message, 2);
        }

        public static void Error(string module, string message)
        {
            Write("ERROR", module, message, 2);
        }

        public static void LogOnceInWindow(
            string key,
            string level,
            string module,
            string message,
            int seconds)
        {
            Write(level, module, message, seconds, key);
        }

        private static void Write(
            string level,
            string module,
            string message,
            int dedupeSeconds,
            string key = null)
        {
            try
            {
                var now = DateTime.Now;
                var dedupeKey = key ?? (level + "\n" + module + "\n" + message);

                lock (SyncRoot)
                {
                    DateTime last;
                    if (LastWriteTimes.TryGetValue(dedupeKey, out last) &&
                        (now - last).TotalSeconds < dedupeSeconds)
                    {
                        return;
                    }

                    LastWriteTimes[dedupeKey] = now;

                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(logDir);

                    var filePath = Path.Combine(logDir, "cim_" + now.ToString("yyyyMMdd") + ".log");
                    var line = string.Format(
                        "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] {3}{4}",
                        now,
                        Normalize(level),
                        Normalize(module),
                        message ?? string.Empty,
                        Environment.NewLine);

                    File.AppendAllText(filePath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Local logging must never affect CIM business flow.
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "GENERAL" : value.Trim();
        }
    }
}
