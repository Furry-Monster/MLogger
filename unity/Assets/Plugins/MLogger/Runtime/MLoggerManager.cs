using System;
using System.IO;
using UnityEngine;

namespace MLogger
{
    /// <summary>
    /// MLogger 管理器，处理初始化和生命周期
    /// </summary>
    public static class MLoggerManager
    {
        private static MLoggerHandler _handler;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// 当前配置
        /// </summary>
        public static MLoggerConfig CurrentConfig { get; private set; }

        /// <summary>
        /// 运行时自动初始化（在 Unity 启动时调用）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            var config = LoadConfig();
            if (config.autoInitialize)
            {
                Initialize(config);
            }
        }

        /// <summary>
        /// 使用配置初始化
        /// </summary>
        /// <param name="config">配置对象</param>
        /// <returns>是否成功</returns>
        public static bool Initialize(MLoggerConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[MLogger] Config is null");
                return false;
            }

            if (IsInitialized)
            {
                Debug.LogWarning("[MLogger] Already initialized. Reinitializing...");
                Shutdown();
            }

            // 确保日志目录存在
            var logPath = config.logPath;
            if (string.IsNullOrEmpty(logPath))
            {
                logPath = GetDefaultLogPath();
            }

            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MLogger] Failed to create log directory: {e.Message}");
                    return false;
                }
            }

            // 调用 Native 初始化
            var result = 0;
            try
            {
                if (config.maxFileSize > 0 && config.maxFiles > 0)
                {
                    result = MLoggerNative.init(
                        logPath,
                        new UIntPtr((ulong)config.maxFileSize),
                        config.maxFiles,
                        config.asyncMode ? 1 : 0,
                        config.threadPoolSize,
                        (int)config.minLogLevel
                    );
                }
                else
                {
                    result = MLoggerNative.initDefault(logPath);
                }
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError($"[MLogger] Native library not found: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MLogger] Failed to initialize native logger: {e.Message}");
                return false;
            }

            if (result == 1)
            {
                IsInitialized = true;
                CurrentConfig = config;

                // 替换 Unity 日志处理器
                _handler = new MLoggerHandler(config.alsoLogToUnity);
                Debug.unityLogger.logHandler = _handler;

                Debug.Log($"[MLogger] Initialized successfully. Log path: {logPath}");
                return true;
            }
            else
            {
                Debug.LogError("[MLogger] Failed to initialize native logger (returned 0)");
                return false;
            }
        }

        /// <summary>
        /// 使用默认配置初始化
        /// </summary>
        /// <param name="logPath">日志文件路径（Optional）</param>
        /// <returns>是否成功</returns>
        public static bool InitializeDefault(string logPath = null)
        {
            var config = MLoggerConfig.CreateDefault();
            if (!string.IsNullOrEmpty(logPath))
            {
                config.logPath = logPath;
            }

            return Initialize(config);
        }

        /// <summary>
        /// 获取默认日志路径
        /// </summary>
        private static string GetDefaultLogPath()
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "..", "Logs", "unity.log");
#elif UNITY_STANDALONE
            return Path.Combine(Application.dataPath, "Logs", "game.log");
#elif UNITY_ANDROID || UNITY_IOS
            return Path.Combine(Application.persistentDataPath, "game.log");
#else
            return Path.Combine(Application.persistentDataPath, "game.log");
#endif
        }

        /// <summary>
        /// 加载配置（从 PlayerPrefs 或默认值）
        /// </summary>
        private static MLoggerConfig LoadConfig()
        {
            // 可以从 PlayerPrefs 或 ScriptableObject 加载
            // 这里使用默认配置
            return MLoggerConfig.CreateDefault();
        }

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="level">日志级别</param>
        public static void SetLogLevel(LogLevel level)
        {
            if (!IsInitialized)
                return;

            try
            {
                MLoggerNative.setLogLevel((int)level);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MLogger] Failed to set log level: {e.Message}");
            }
        }

        /// <summary>
        /// 获取当前日志级别
        /// </summary>
        /// <returns>日志级别</returns>
        public static LogLevel GetLogLevel()
        {
            if (!IsInitialized)
                return LogLevel.Info;

            try
            {
                var level = MLoggerNative.getLogLevel();
                return (LogLevel)level;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MLogger] Failed to get log level: {e.Message}");
            }

            return LogLevel.Info;
        }

        /// <summary>
        /// 刷新日志缓冲区
        /// </summary>
        public static void Flush()
        {
            if (!IsInitialized)
                return;

            try
            {
                MLoggerNative.flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MLogger] Failed to flush: {e.Message}");
            }
        }

        /// <summary>
        /// 关闭并清理资源
        /// </summary>
        public static void Shutdown()
        {
            if (!IsInitialized)
                return;

            try
            {
                Flush();
                MLoggerNative.terminate();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MLogger] Error during shutdown: {e.Message}");
            }
            finally
            {
                IsInitialized = false;
                CurrentConfig = null;

                // 恢复默认日志处理器
                if (_handler != null)
                {
                    // 仍然提示不能，保留原始 logHandler，不再强制重置
                    _handler = null;
                }
            }
        }

        /// <summary>
        /// 应用退出时自动清理
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            Application.quitting += OnApplicationQuitting;
        }

        /// <summary>
        /// 应用退出 Callback
        /// </summary>
        private static void OnApplicationQuitting()
        {
            Shutdown();
        }
    }
}