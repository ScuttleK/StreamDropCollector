using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.IO;
using Core.Managers;

namespace Core.Logging
{
    public static class AppLogger
    {
        private static readonly object _sync = new();
        private static string? _logFilePath;

        public static string LogDirectoryPath => Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "Stream Drop Collector", "logs");

        public static string? LogFilePath => _logFilePath;

        public static void Initialize()
        {
            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(_logFilePath))
                    return;

                string baseDir = LogDirectoryPath;

                Directory.CreateDirectory(baseDir);

                string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                _logFilePath = Path.Combine(baseDir, $"app-{date}.log");

                Write("INFO", "Logger", $"Logger initialized. File={_logFilePath}");
            }
        }

        public static void Info(string scope, string message) => Write("INFO", scope, message);

        public static void Debug(string scope, string message)
        {
            if (!IsVerboseDebugEnabled())
                return;

            Write("DEBUG", scope, message);
        }

        public static void Warn(string scope, string message) => Write("WARN", scope, message);

        public static void Error(string scope, string message, Exception? ex = null)
        {
            if (ex == null)
            {
                Write("ERROR", scope, message);
                return;
            }

            StringBuilder sb = new StringBuilder(message);
            sb.Append(" | ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            Write("ERROR", scope, sb.ToString());
        }

        private static void Write(string level, string scope, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_logFilePath))
                    Initialize();

                string ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
                string line = $"[{ts}] [{level}] [{scope}] {message}";

                lock (_sync)
                {
                    File.AppendAllText(_logFilePath!, line + Environment.NewLine, Encoding.UTF8);
                }

                Trace.WriteLine(line);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AppLogger-Fallback] {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static bool IsVerboseDebugEnabled()
        {
            try
            {
                return UISettingsManager.Instance.VerboseDebugLogging;
            }
            catch
            {
                return false;
            }
        }
    }
}