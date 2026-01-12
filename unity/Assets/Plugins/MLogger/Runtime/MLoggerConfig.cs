using System;

namespace MLogger
{
    [Serializable]
    public class MLoggerConfig
    {
        public string logPath = "";
        public long maxFileSize = 10 * 1024 * 1024;
        public int maxFiles = 5;
        public bool asyncMode = true;
        public int threadPoolSize = 2;
        public LogLevel minLogLevel = LogLevel.Info;
        public bool autoInitialize = true;
        public bool alsoLogToUnity = true;

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