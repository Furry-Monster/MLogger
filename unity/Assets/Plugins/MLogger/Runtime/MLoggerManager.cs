using System;
using System.IO;
using UnityEngine;

namespace MLogger
{
    /// <summary>
    /// MLogger manager, handles initialization and lifecycle
    /// </summary>
    public static class MLoggerManager
    {
        private static MLoggerHandler _handler;

        /// <summary>
        /// Whether the logger is initialized
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Current configuration
        /// </summary>
        public static MLoggerConfig CurrentConfig { get; private set; }

        /// <summary>
        /// Auto-initialize at runtime (called when Unity starts)
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
        /// Initialize with configuration
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <returns>Whether initialization succeeded</returns>
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

            // Ensure log directory exists
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

            // Call Native initialization
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

                // Replace Unity log handler
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
        /// Initialize with default configuration
        /// </summary>
        /// <param name="logPath">Log file path (Optional)</param>
        /// <returns>Whether initialization succeeded</returns>
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
        /// Get default log path
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
        /// Load configuration (from PlayerPrefs or default values)
        /// </summary>
        private static MLoggerConfig LoadConfig()
        {
            // Can load from PlayerPrefs or ScriptableObject
            // Here we use default configuration
            return MLoggerConfig.CreateDefault();
        }

        /// <summary>
        /// Set log level
        /// </summary>
        /// <param name="level">Log level</param>
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
        /// Get current log level
        /// </summary>
        /// <returns>Log level</returns>
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
        /// Flush log buffer
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
        /// Shutdown and cleanup resources
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

                // Restore default log handler
                if (_handler != null)
                {
                    // Keep original logHandler, no longer force reset
                    _handler = null;
                }
            }
        }

        /// <summary>
        /// Auto cleanup when application quits
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            Application.quitting += OnApplicationQuitting;
        }

        /// <summary>
        /// Application quit callback
        /// </summary>
        private static void OnApplicationQuitting()
        {
            Shutdown();
        }
    }
}