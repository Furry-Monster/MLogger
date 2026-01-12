using System;
using System.IO;
using UnityEngine;

namespace MLogger
{
    public static class MLoggerManager
    {
        private static MLoggerHandler _handler;

        public static bool IsInitialized { get; private set; } = false;

        public static MLoggerConfig CurrentConfig { get; private set; }

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
        /// Initialize logger with configuration. Handles directory creation, native library initialization, and Unity log handler setup.
        /// </summary>
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

        public static bool InitializeDefault(string logPath = null)
        {
            var config = MLoggerConfig.CreateDefault();
            if (!string.IsNullOrEmpty(logPath))
            {
                config.logPath = logPath;
            }

            return Initialize(config);
        }

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

        private static MLoggerConfig LoadConfig()
        {
            var settings = MLoggerSettings.Instance;
            if (settings != null && settings.Config != null)
            {
                return new MLoggerConfig
                {
                    logPath = settings.Config.logPath,
                    maxFileSize = settings.Config.maxFileSize,
                    maxFiles = settings.Config.maxFiles,
                    asyncMode = settings.Config.asyncMode,
                    threadPoolSize = settings.Config.threadPoolSize,
                    minLogLevel = settings.Config.minLogLevel,
                    autoInitialize = settings.Config.autoInitialize,
                    alsoLogToUnity = settings.Config.alsoLogToUnity
                };
            }

            return MLoggerConfig.CreateDefault();
        }

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
        /// Shutdown logger and cleanup resources. Flushes logs, terminates native library, and restores Unity log handler.
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
                if (_handler != null)
                {
                    _handler = null;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            Shutdown();
        }
    }
}
