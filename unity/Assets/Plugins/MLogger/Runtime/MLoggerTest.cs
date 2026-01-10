using UnityEngine;

namespace MLogger
{
    /// <summary>
    /// MLogger Unity测试
    /// </summary>
    public class MLoggerTest : MonoBehaviour
    {
        [Header("Test Settings")] [Tooltip("是否在 Start 时自动运行测试")]
        public bool autoRunOnStart = false;

        [Tooltip("测试日志消息")] public string testMessage = "MLogger Test Message";

        private void Start()
        {
            if (autoRunOnStart)
            {
                RunTests();
            }
        }

        [ContextMenu("Run Tests")]
        public void RunTests()
        {
            Debug.Log("=== MLogger Test Suite ===");

            TestInitialization();
            TestLogLevels();
            TestExceptionLogging();
            TestLogLevelSettings();
            TestFlush();

            Debug.Log("=== MLogger Test Suite Complete ===");
        }

        /// <summary>
        /// 测试初始化状态
        /// </summary>
        private void TestInitialization()
        {
            Debug.Log("[Test] Checking initialization status...");
            if (MLoggerManager.IsInitialized)
            {
                Debug.Log($"[Test] ✓ MLogger is initialized. Log path: {MLoggerManager.CurrentConfig?.logPath}");
            }
            else
            {
                Debug.LogWarning("[Test] ✗ MLogger is not initialized");
            }
        }

        /// <summary>
        /// 测试各种日志级别
        /// </summary>
        private void TestLogLevels()
        {
            Debug.Log("[Test] Testing log levels...");

            Debug.Log($"[Test] Info: {testMessage}");
            Debug.LogWarning($"[Test] Warning: {testMessage}");
            Debug.LogError($"[Test] Error: {testMessage}");

            Debug.Log("[Test] ✓ Log levels test complete");
        }

        /// <summary>
        /// 测试异常日志
        /// </summary>
        private void TestExceptionLogging()
        {
            Debug.Log("[Test] Testing exception logging...");

            try
            {
                throw new System.Exception("Test Exception for MLogger");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            Debug.Log("[Test] ✓ Exception logging test complete");
        }

        /// <summary>
        /// 测试日志级别设置
        /// </summary>
        private void TestLogLevelSettings()
        {
            Debug.Log("[Test] Testing log level settings...");

            var currentLevel = MLoggerManager.GetLogLevel();
            Debug.Log($"[Test] Current log level: {currentLevel}");

            // 测试设置不同的日志级别
            MLoggerManager.SetLogLevel(LogLevel.Debug);
            Debug.Log($"[Test] Set log level to Debug: {MLoggerManager.GetLogLevel()}");

            MLoggerManager.SetLogLevel(LogLevel.Warn);
            Debug.Log($"[Test] Set log level to Warn: {MLoggerManager.GetLogLevel()}");

            // 恢复原级别
            MLoggerManager.SetLogLevel(currentLevel);

            Debug.Log("[Test] ✓ Log level settings test complete");
        }

        /// <summary>
        /// 测试刷新功能
        /// </summary>
        private void TestFlush()
        {
            Debug.Log("[Test] Testing flush...");
            MLoggerManager.Flush();
            Debug.Log("[Test] ✓ Flush test complete");
        }

        [ContextMenu("Manual Initialize")]
        public void ManualInitialize()
        {
            var config = MLoggerConfig.CreateDefault();
            config.logPath = System.IO.Path.Combine(Application.persistentDataPath, "test.log");
            var success = MLoggerManager.Initialize(config);
            Debug.Log($"[Test] Manual initialization: {(success ? "Success" : "Failed")}");
        }

        [ContextMenu("Manual Shutdown")]
        public void ManualShutdown()
        {
            MLoggerManager.Shutdown();
            Debug.Log("[Test] Manual shutdown complete");
        }
    }
}