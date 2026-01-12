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
    /// P/Invoke interface for native logging library. All methods map to C functions via DllImport.
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

        Critical = 5
    }

    /// <summary>
    /// P/Invoke interface for native logging library. All methods map to C functions via DllImport.
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
        /// Initialize logger with custom configuration. Returns 1 on success, 0 on failure.
        /// </summary>
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
        /// Initialize logger with default configuration. Returns 1 on success, 0 on failure.
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int initDefault(
            [MarshalAs(UnmanagedType.LPStr)] string log_path
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logMessage(
            int log_level,
            [MarshalAs(UnmanagedType.LPStr)] string message
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logException(
            [MarshalAs(UnmanagedType.LPStr)] string exception_type,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            [MarshalAs(UnmanagedType.LPStr)] string stack_trace
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flush();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void setLogLevel(int log_level);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getLogLevel();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int isInit();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void terminate();
    }
}
