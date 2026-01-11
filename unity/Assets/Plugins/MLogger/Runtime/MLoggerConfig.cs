using System;

namespace MLogger
{
    /// <summary>
    /// MLogger configuration class
    /// </summary>
    [Serializable]
    public class MLoggerConfig
    {
        /// <summary>
        /// Log file path
        /// </summary>
        public string logPath = "";

        /// <summary>
        /// Maximum file size (bytes), default 10MB
        /// </summary>
        public long maxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// Maximum number of files, default 5
        /// </summary>
        public int maxFiles = 5;

        /// <summary>
        /// Whether to use async mode, default true
        /// </summary>
        public bool asyncMode = true;

        /// <summary>
        /// Thread pool size (async mode), default 2
        /// </summary>
        public int threadPoolSize = 2;

        /// <summary>
        /// Minimum log level, default Info
        /// </summary>
        public LogLevel minLogLevel = LogLevel.Info;

        /// <summary>
        /// Whether to auto-initialize, default true
        /// </summary>
        public bool autoInitialize = true;

        /// <summary>
        /// Whether to also output to Unity console in Editor, default true
        /// </summary>
        public bool alsoLogToUnity = true;

        /// <summary>
        /// Create default configuration
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
        /// Get default log path
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
