using System;

namespace MLogger
{
    /// <summary>
    /// MLogger 配置类
    /// </summary>
    [Serializable]
    public class MLoggerConfig
    {
        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string logPath = "";

        /// <summary>
        /// 最大文件大小（字节），默认 10MB
        /// </summary>
        public long maxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// 最大文件数量，默认 5
        /// </summary>
        public int maxFiles = 5;

        /// <summary>
        /// 是否使用异步模式，默认 true
        /// </summary>
        public bool asyncMode = true;

        /// <summary>
        /// 线程池大小（异步模式），默认 2
        /// </summary>
        public int threadPoolSize = 2;

        /// <summary>
        /// 最小日志级别，默认 Info
        /// </summary>
        public LogLevel minLogLevel = LogLevel.Info;

        /// <summary>
        /// 是否自动初始化，默认 true
        /// </summary>
        public bool autoInitialize = true;

        /// <summary>
        /// 是否在 Editor 中也输出到 Unity 控制台，默认 true
        /// </summary>
        public bool alsoLogToUnity = true;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static MLoggerConfig CreateDefault()
        {
            return new MLoggerConfig
            {
                logPath = GetDefaultLogPath(),
                maxFileSize = 10 * 1024 * 1024,
                maxFiles = 5,
                asyncMode = true,
                threadPoolSize = 2,
                minLogLevel = LogLevel.Info,
                autoInitialize = true,
                alsoLogToUnity = true
            };
        }

        /// <summary>
        /// 获取默认日志路径
        /// </summary>
        private static string GetDefaultLogPath()
        {
#if UNITY_EDITOR
            return System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Logs", "unity.log");
#elif UNITY_STANDALONE
            return System.IO.Path.Combine(UnityEngine.Application.dataPath, "Logs", "game.log");
#elif UNITY_ANDROID || UNITY_IOS
            return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "game.log");
#else
            return System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "game.log");
#endif
        }
    }
}