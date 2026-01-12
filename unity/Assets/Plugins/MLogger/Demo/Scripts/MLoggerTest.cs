using UnityEngine;

namespace MLogger
{
    public class MLoggerTest : MonoBehaviour
    {
        [Header("Test Settings")] [Tooltip("Whether to auto-run tests on Start")]
        public bool autoRunOnStart = false;

        [Tooltip("Test log message")] public string testMessage = "MLogger Test Message";

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

        private void TestInitialization()
        {
            Debug.Log("[Test] Checking initialization status...");
            if (MLoggerManager.IsInitialized)
            {
                Debug.Log($"[Test] MLogger is initialized. Log path: {MLoggerManager.CurrentConfig?.logPath}");
            }
            else
            {
                Debug.LogWarning("[Test] MLogger is not initialized");
            }
        }

        private void TestLogLevels()
        {
            Debug.Log("[Test] Testing log levels...");
            Debug.Log($"[Test] Info: {testMessage}");
            Debug.LogWarning($"[Test] Warning: {testMessage}");
            Debug.LogError($"[Test] Error: {testMessage}");
            Debug.Log("[Test] Log levels test complete");
        }

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
            Debug.Log("[Test] Exception logging test complete");
        }

        private void TestLogLevelSettings()
        {
            Debug.Log("[Test] Testing log level settings...");
            var currentLevel = MLoggerManager.GetLogLevel();
            Debug.Log($"[Test] Current log level: {currentLevel}");
            MLoggerManager.SetLogLevel(LogLevel.Debug);
            Debug.Log($"[Test] Set log level to Debug: {MLoggerManager.GetLogLevel()}");
            MLoggerManager.SetLogLevel(LogLevel.Warn);
            Debug.Log($"[Test] Set log level to Warn: {MLoggerManager.GetLogLevel()}");
            MLoggerManager.SetLogLevel(currentLevel);
            Debug.Log("[Test] Log level settings test complete");
        }

        private void TestFlush()
        {
            Debug.Log("[Test] Testing flush...");
            MLoggerManager.Flush();
            Debug.Log("[Test] Flush test complete");
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
