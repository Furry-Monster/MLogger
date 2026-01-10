using System;
using System.Runtime.InteropServices;

namespace MLogger
{
    /// <summary>
    /// 日志级别枚举
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
    /// Native 库的 P/Invoke 接口映射
    /// 映射所有 C 接口函数到 C#
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
        private const string DllName = "mlogger_linux"; // 默认
#endif

        #region ========== 初始化函数 ==========

        /// <summary>
        /// 使用自定义配置初始化日志系统
        /// </summary>
        /// <param name="log_path">日志文件路径</param>
        /// <param name="max_file_size">最大文件大小（字节）</param>
        /// <param name="max_files">最大文件数量</param>
        /// <param name="async_mode">是否使用异步模式 (1=异步, 0=同步)</param>
        /// <param name="thread_pool_size">线程池大小（异步模式）</param>
        /// <param name="min_log_level">最小日志级别</param>
        /// <returns>1=成功, 0=失败</returns>
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
        /// 使用默认配置初始化日志系统
        /// </summary>
        /// <param name="log_path">日志文件路径</param>
        /// <returns>1=成功, 0=失败</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int initDefault(
            [MarshalAs(UnmanagedType.LPStr)] string log_path
        );

        #endregion

        #region ========== 日志函数 ==========

        /// <summary>
        /// 记录日志消息
        /// </summary>
        /// <param name="log_level">日志级别</param>
        /// <param name="message">日志消息</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logMessage(
            int log_level,
            [MarshalAs(UnmanagedType.LPStr)] string message
        );

        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="exception_type">异常类型</param>
        /// <param name="message">异常消息</param>
        /// <param name="stack_trace">堆栈跟踪</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void logException(
            [MarshalAs(UnmanagedType.LPStr)] string exception_type,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            [MarshalAs(UnmanagedType.LPStr)] string stack_trace
        );

        #endregion

        #region ========== 控制函数 ==========

        /// <summary>
        /// 刷新日志缓冲区，确保所有日志写入磁盘
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void flush();

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="log_level">日志级别</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void setLogLevel(int log_level);

        /// <summary>
        /// 获取当前日志级别
        /// </summary>
        /// <returns>日志级别</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int getLogLevel();

        /// <summary>
        /// 检查日志系统是否已初始化
        /// </summary>
        /// <returns>1=已初始化, 0=未初始化</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int isInit();

        /// <summary>
        /// 终止日志系统并清理资源
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void terminate();

        #endregion
    }
}