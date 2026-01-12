using System;
using System.Runtime.InteropServices;

namespace MLogger
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    /// P/Invoke interface for the native MLogger logging library.
    /// Each static extern method corresponds to a C function in the platform-specific native DLL.
    /// Provides logging initialization, message logging, exception logging, flushing, log level management,
    /// status querying, and clean shutdown API for managed-to-unmanaged interop.
    /// </summary>
    internal static class MLoggerNative
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string DllName = "mlogger_win";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string DllName = "mlogger_macos";
#elif UNITY_ANDROID
        private const string DllName = "mlogger_android";
#elif UNITY_IOS
        private const string DllName = "mlogger_ios";
#elif UNITY_STANDALONE_LINUX
        private const string DllName = "mlogger_linux";
#else
        private const string DllName = "mlogger_linux";
#endif

        /// <summary>
        /// Initializes the MLogger native logging system with the specified configuration.
        /// </summary>
        /// <param name="log_path">Absolute or relative path for the main log file. Can be null to use default path.</param>
        /// <param name="max_file_size">Maximum size (in bytes) before log rotation occurs. 0 or a very high value disables rotation.</param>
        /// <param name="max_files">Maximum number of rotating log files to keep. Older files are deleted.</param>
        /// <param name="async_mode">1 to enable asynchronous logging, 0 for synchronous.</param>
        /// <param name="thread_pool_size">Thread pool size to use when async_mode is enabled.</param>
        /// <param name="min_log_level">Minimum log level; messages below this are discarded. (e.g. 0-Trace, 1-Debug, etc)</param>
        /// <returns>1 if initialized successfully, 0 otherwise (such as duplicate initialization or invalid config).</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int init(
            [MarshalAs(UnmanagedType.LPStr)] string log_path,
            UIntPtr max_file_size,
            int max_files,
            int async_mode,
            int thread_pool_size,
            int min_log_level
        );

        /// <summary>
        /// Initializes the MLogger with default configuration and log path.
        /// </summary>
        /// <param name="log_path">Absolute or relative path for the main log file. Can be null or empty to use hard-coded default.</param>
        /// <returns>1 if initialization succeeded, 0 if failed (already initialized or failure).</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int initDefault(
            [MarshalAs(UnmanagedType.LPStr)] string log_path
        );

        /// <summary>
        /// Logs a formatted text message to the native logger at the specified log level.
        /// </summary>
        /// <param name="log_level">Severity level for the message (0-Trace, 1-Debug, 2-Info, 3-Warn, 4-Error, 5-Critical).</param>
        /// <param name="message">Log message string (should be UTF-8/ASCII safe; long strings may be truncated natively).</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logMessage(
            int log_level,
            [MarshalAs(UnmanagedType.LPStr)] string message
        );

        /// <summary>
        /// Logs an exception record to the native logger at Error severity, including type, message, and stack trace.
        /// </summary>
        /// <param name="exception_type">Full type name of exception (e.g., System.Exception, ArgumentNullException).</param>
        /// <param name="message">Exception message.</param>
        /// <param name="stack_trace">Exception stack trace string.</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logException(
            [MarshalAs(UnmanagedType.LPStr)] string exception_type,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            [MarshalAs(UnmanagedType.LPStr)] string stack_trace
        );

        /// <summary>
        /// Immediately flushes all log buffers, forcing the native logger to write pending data to disk.
        /// Useful for ensuring logs are up-to-date during critical operations or shutdown.
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flush();

        /// <summary>
        /// Updates the logger's minimum log level at runtime.
        /// Messages below this level will be ignored.
        /// </summary>
        /// <param name="log_level">New minimum log level (0-Trace ... 5-Critical).</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void setLogLevel(int log_level);

        /// <summary>
        /// Gets the currently-active minimum log level for the logger.
        /// </summary>
        /// <returns>Current minimum log level (int).</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getLogLevel();

        /// <summary>
        /// Checks whether the native logger has been initialized.
        /// </summary>
        /// <returns>1 if the logger is initialized; 0 otherwise.</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int isInit();

        /// <summary>
        /// Shuts down the native logger and releases all native resources.
        /// After calling terminate(), the logger must be re-initialized to use again.
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void terminate();
    }
}