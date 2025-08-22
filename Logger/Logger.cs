using System;
using System.IO;
using System.Threading;

namespace Peridot
{
    /// <summary>
    /// Log levels for categorizing log messages by severity
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    /// <summary>
    /// A flexible logging system that can output to console, file, or both.
    /// Thread-safe and supports multiple log levels with formatting.
    /// </summary>
    public static class Logger
    {
        private static LogLevel _minimumLogLevel = LogLevel.Debug;
        private static bool _logToConsole = true;
        private static bool _logToFile = false;
        private static string _logFilePath = "game.log";
        private static readonly object _lockObject = new object();
        private static bool _includeTimestamp = true;
        private static bool _includeThreadId = false;

        /// <summary>
        /// Gets or sets the minimum log level. Messages below this level will be ignored.
        /// </summary>
        public static LogLevel MinimumLogLevel
        {
            get => _minimumLogLevel;
            set => _minimumLogLevel = value;
        }

        /// <summary>
        /// Gets or sets whether to output log messages to the console.
        /// </summary>
        public static bool LogToConsole
        {
            get => _logToConsole;
            set => _logToConsole = value;
        }

        /// <summary>
        /// Gets or sets whether to output log messages to a file.
        /// </summary>
        public static bool LogToFile
        {
            get => _logToFile;
            set => _logToFile = value;
        }

        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        public static string LogFilePath
        {
            get => _logFilePath;
            set => _logFilePath = value ?? "game.log";
        }

        /// <summary>
        /// Gets or sets whether to include timestamps in log messages.
        /// </summary>
        public static bool IncludeTimestamp
        {
            get => _includeTimestamp;
            set => _includeTimestamp = value;
        }

        /// <summary>
        /// Gets or sets whether to include thread ID in log messages.
        /// </summary>
        public static bool IncludeThreadId
        {
            get => _includeThreadId;
            set => _includeThreadId = value;
        }

        /// <summary>
        /// Configures the logger with common settings.
        /// </summary>
        /// <param name="minLevel">Minimum log level to output</param>
        /// <param name="logToConsole">Whether to log to console</param>
        /// <param name="logToFile">Whether to log to file</param>
        /// <param name="filePath">Path to log file (optional)</param>
        public static void Configure(LogLevel minLevel, bool logToConsole = true, bool logToFile = false, string filePath = null)
        {
            _minimumLogLevel = minLevel;
            _logToConsole = logToConsole;
            _logToFile = logToFile;
            if (filePath != null)
                _logFilePath = filePath;
        }

        /// <summary>
        /// Logs a trace message (lowest priority, for detailed debugging)
        /// </summary>
        public static void Trace(string message) => Log(LogLevel.Trace, message);
        public static void Trace(string format, params object[] args) => Log(LogLevel.Trace, format, args);

        /// <summary>
        /// Logs a debug message (for debugging information)
        /// </summary>
        public static void Debug(string message) => Log(LogLevel.Debug, message);
        public static void Debug(string format, params object[] args) => Log(LogLevel.Debug, format, args);

        /// <summary>
        /// Logs an info message (general information)
        /// </summary>
        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Info(string format, params object[] args) => Log(LogLevel.Info, format, args);

        /// <summary>
        /// Logs a warning message (potentially harmful situations)
        /// </summary>
        public static void Warning(string message) => Log(LogLevel.Warning, message);
        public static void Warning(string format, params object[] args) => Log(LogLevel.Warning, format, args);

        /// <summary>
        /// Logs an error message (error events that might allow the application to continue)
        /// </summary>
        public static void Error(string message) => Log(LogLevel.Error, message);
        public static void Error(string format, params object[] args) => Log(LogLevel.Error, format, args);
        public static void Error(Exception exception, string message = null)
        {
            var errorMessage = message != null ? $"{message}: {exception}" : exception.ToString();
            Log(LogLevel.Error, errorMessage);
        }

        /// <summary>
        /// Logs a fatal message (very severe error events that will presumably lead the application to abort)
        /// </summary>
        public static void Fatal(string message) => Log(LogLevel.Fatal, message);
        public static void Fatal(string format, params object[] args) => Log(LogLevel.Fatal, format, args);
        public static void Fatal(Exception exception, string message = null)
        {
            var errorMessage = message != null ? $"{message}: {exception}" : exception.ToString();
            Log(LogLevel.Fatal, errorMessage);
        }

        /// <summary>
        /// Core logging method that handles the actual message output
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        private static void Log(LogLevel level, string message)
        {
            if (level < _minimumLogLevel)
                return;

            if (!_logToConsole && !_logToFile)
                return;

            var formattedMessage = FormatMessage(level, message);

            lock (_lockObject)
            {
                if (_logToConsole)
                {
                    WriteToConsole(level, formattedMessage);
                }

                if (_logToFile)
                {
                    WriteToFile(formattedMessage);
                }
            }
        }

        /// <summary>
        /// Core logging method with string formatting
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">The format string</param>
        /// <param name="args">The format arguments</param>
        private static void Log(LogLevel level, string format, params object[] args)
        {
            if (level < _minimumLogLevel)
                return;

            try
            {
                var message = string.Format(format, args);
                Log(level, message);
            }
            catch (FormatException)
            {
                // If string formatting fails, log the format string directly
                Log(LogLevel.Error, $"Log formatting failed for: {format}");
                Log(level, format);
            }
        }

        /// <summary>
        /// Formats the log message with timestamp, level, and thread info as configured
        /// </summary>
        private static string FormatMessage(LogLevel level, string message)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (_includeTimestamp)
            {
                parts.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }

            parts.Add($"[{level.ToString().ToUpper()}]");

            if (_includeThreadId)
            {
                parts.Add($"[T{Thread.CurrentThread.ManagedThreadId}]");
            }

            parts.Add(message);

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Writes a message to the console with appropriate coloring based on log level
        /// </summary>
        private static void WriteToConsole(LogLevel level, string message)
        {
            var originalColor = Console.ForegroundColor;
            
            try
            {
                Console.ForegroundColor = GetConsoleColor(level);
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        /// <summary>
        /// Gets the appropriate console color for each log level
        /// </summary>
        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.White,
                LogLevel.Info => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Writes a message to the log file
        /// </summary>
        private static void WriteToFile(string message)
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If we can't write to file, at least try to output to console
                if (_logToConsole)
                {
                    Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears the log file
        /// </summary>
        public static void ClearLogFile()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_logFilePath))
                    {
                        File.Delete(_logFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Error($"Failed to clear log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a timestamped log file name to avoid overwriting previous logs
        /// </summary>
        /// <param name="baseName">Base name for the log file (without extension)</param>
        /// <returns>A timestamped log file path</returns>
        public static string CreateTimestampedLogFile(string baseName = "game")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{baseName}_{timestamp}.log";
            _logFilePath = fileName;
            return fileName;
        }
    }
}
