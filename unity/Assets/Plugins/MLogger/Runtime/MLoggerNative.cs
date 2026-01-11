using System;
using System.Runtime.InteropServices;

namespace MLogger
{
    /// <summary>
    /// Log level enumeration
    /// </summary>
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
    /// P/Invoke interface mapping for Native library
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
        private const string DllName = "mlogger_linux"; // Default
#endif

        #region ========== Initialization Functions ==========

        /// <summary>
        /// Initialize log system with custom configuration
        /// </summary>
        /// <param name="log_path">Log file path</param>
        /// <param name="max_file_size">Maximum file size (bytes)</param>
        /// <param name="max_files">Maximum number of files</param>
        /// <param name="async_mode">Whether to use async mode (1=async, 0=sync)</param>
        /// <param name="thread_pool_size">Thread pool size (async mode)</param>
        /// <param name="min_log_level">Minimum log level</param>
        /// <returns>1=success, 0=failure</returns>
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
        /// Initialize log system with default configuration
        /// </summary>
        /// <param name="log_path">Log file path</param>
        /// <returns>1=success, 0=failure</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int initDefault(
            [MarshalAs(UnmanagedType.LPStr)] string log_path
        );

        #endregion

        #region ========== Log Functions ==========

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="log_level">Log level</param>
        /// <param name="message">Log message</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logMessage(
            int log_level,
            [MarshalAs(UnmanagedType.LPStr)] string message
        );

        /// <summary>
        /// Log exception information
        /// </summary>
        /// <param name="exception_type">Exception type</param>
        /// <param name="message">Exception message</param>
        /// <param name="stack_trace">Stack trace</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logException(
            [MarshalAs(UnmanagedType.LPStr)] string exception_type,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            [MarshalAs(UnmanagedType.LPStr)] string stack_trace
        );

        #endregion

        #region ========== Control Functions ==========

        /// <summary>
        /// Flush log buffer, ensure all logs are written to disk
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flush();

        /// <summary>
        /// Set log level
        /// </summary>
        /// <param name="log_level">Log level</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void setLogLevel(int log_level);

        /// <summary>
        /// Get current log level
        /// </summary>
        /// <returns>Log level</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getLogLevel();

        /// <summary>
        /// Check if log system is initialized
        /// </summary>
        /// <returns>1=initialized, 0=not initialized</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int isInit();

        /// <summary>
        /// Terminate log system and cleanup resources
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void terminate();

        #endregion
    }
}
