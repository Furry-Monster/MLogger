using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MLogger
{
    public class MLoggerPerformanceTest : MonoBehaviour
    {
        [Header("Test Settings")] public int logCount = 10000;
        public bool autoRunOnStart = false;
        public string messageTemplate = "Performance test message {0} with some data: {1}";

        [Header("Test Options")] public bool testUnityDebug = true;
        public bool testMLogger = true;
        public bool testMultiThread = false;
        public int threadCount = 4;

        [Header("Results")] [SerializeField] private PerformanceTestResult unityDebugResult;
        [SerializeField] private PerformanceTestResult mloggerResult;

        private bool isRunning = false;

        [Serializable]
        public class PerformanceTestResult
        {
            public string testName;
            public long totalTimeMs;
            public double averageTimeMs;
            public long minTimeMs = long.MaxValue;
            public long maxTimeMs;
            public long memoryBefore;
            public long memoryAfter;
            public long memoryDelta;
            public int logCount;
            public double logsPerSecond;
            public bool success;

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"=== {testName} Performance Test Results ===");
                sb.AppendLine($"Total Time: {totalTimeMs} ms");
                sb.AppendLine($"Average Time: {averageTimeMs:F3} ms");
                sb.AppendLine($"Min Time: {minTimeMs} ms");
                sb.AppendLine($"Max Time: {maxTimeMs} ms");
                sb.AppendLine($"Memory Delta: {memoryDelta / 1024.0 / 1024.0:F2} MB");
                sb.AppendLine($"Log Count: {logCount}");
                sb.AppendLine($"Throughput: {logsPerSecond:F0} logs/sec");
                sb.AppendLine($"Status: {(success ? "Success" : "Failed")}");
                return sb.ToString();
            }
        }

        private void Start()
        {
            if (autoRunOnStart)
            {
                StartCoroutine(RunAllTestsCoroutine());
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            if (isRunning)
            {
                Debug.LogWarning("Test is already running, please wait for completion");
                return;
            }

            StartCoroutine(RunAllTestsCoroutine());
        }

        private IEnumerator RunAllTestsCoroutine()
        {
            isRunning = true;
            Debug.Log("=== Starting Performance Comparison Test ===");

            if (testMLogger && !MLoggerManager.IsInitialized)
            {
                Debug.LogWarning("MLogger not initialized, attempting to initialize with default config...");
                var config = MLoggerConfig.CreateDefault();
                MLoggerManager.Initialize(config);
                yield return new WaitForSeconds(0.1f);
            }

            if (testUnityDebug)
            {
                Debug.Log("--- Testing Unity Debug.Log ---");
                unityDebugResult = TestUnityDebug();
                yield return null;
            }

            if (testMLogger)
            {
                Debug.Log("--- Testing MLogger ---");
                mloggerResult = TestMLogger();
                yield return null;
            }

            PrintComparisonResults();

            isRunning = false;
            Debug.Log("=== Performance Comparison Test Complete ===");
        }

        private PerformanceTestResult TestUnityDebug()
        {
            var result = new PerformanceTestResult
            {
                testName = "Unity Debug.Log",
                logCount = logCount
            };

            var originalHandler = Debug.unityLogger.logHandler;
            var tempHandler = new UnityLogHandler();
            Debug.unityLogger.logHandler = tempHandler;

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                result.memoryBefore = GC.GetTotalMemory(false);

                var stopwatch = Stopwatch.StartNew();
                var times = new List<long>(logCount);

                for (int i = 0; i < logCount; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Debug.Log(string.Format(messageTemplate, i, UnityEngine.Random.Range(0, 1000)));
                    sw.Stop();
                    times.Add(sw.ElapsedTicks);
                }

                stopwatch.Stop();

                result.totalTimeMs = stopwatch.ElapsedMilliseconds;
                result.memoryAfter = GC.GetTotalMemory(false);
                result.memoryDelta = result.memoryAfter - result.memoryBefore;

                if (times.Count > 0)
                {
                    times.Sort();
                    result.minTimeMs = times[0] / TimeSpan.TicksPerMillisecond;
                    result.maxTimeMs = times[^1] / TimeSpan.TicksPerMillisecond;

                    var totalTicks = times.Sum();

                    result.averageTimeMs = (totalTicks / (double)times.Count) / TimeSpan.TicksPerMillisecond;
                }

                result.logsPerSecond = logCount / (result.totalTimeMs / 1000.0);
                result.success = true;
            }
            catch (Exception e)
            {
                result.success = false;
                Debug.LogError($"Unity Debug.Log test failed: {e.Message}");
            }
            finally
            {
                Debug.unityLogger.logHandler = originalHandler;
            }

            return result;
        }

        private PerformanceTestResult TestMLogger()
        {
            var result = new PerformanceTestResult
            {
                testName = "MLogger",
                logCount = logCount
            };

            if (!MLoggerManager.IsInitialized)
            {
                result.success = false;
                Debug.LogError("MLogger not initialized, cannot run test");
                return result;
            }

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                result.memoryBefore = GC.GetTotalMemory(false);

                var stopwatch = Stopwatch.StartNew();
                var times = new List<long>(logCount);

                for (int i = 0; i < logCount; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var message = string.Format(messageTemplate, i, UnityEngine.Random.Range(0, 1000));
                    MLoggerNative.logMessage((int)LogLevel.Info, message);
                    sw.Stop();
                    times.Add(sw.ElapsedTicks);
                }

                MLoggerNative.flush();
                System.Threading.Thread.Sleep(100);

                stopwatch.Stop();

                result.totalTimeMs = stopwatch.ElapsedMilliseconds;
                result.memoryAfter = GC.GetTotalMemory(false);
                result.memoryDelta = result.memoryAfter - result.memoryBefore;

                if (times.Count > 0)
                {
                    times.Sort();
                    result.minTimeMs = times[0] / TimeSpan.TicksPerMillisecond;
                    result.maxTimeMs = times[^1] / TimeSpan.TicksPerMillisecond;

                    var totalTicks = times.Sum();

                    result.averageTimeMs = (totalTicks / (double)times.Count) / TimeSpan.TicksPerMillisecond;
                }

                result.logsPerSecond = logCount / (result.totalTimeMs / 1000.0);
                result.success = true;
            }
            catch (Exception e)
            {
                result.success = false;
                Debug.LogError($"MLogger test failed: {e.Message}");
            }

            return result;
        }

        private void PrintComparisonResults()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n========================================");
            sb.AppendLine("Performance Comparison Results");
            sb.AppendLine("========================================");

            if (unityDebugResult != null)
            {
                sb.AppendLine(unityDebugResult.ToString());
            }

            if (mloggerResult != null)
            {
                sb.AppendLine(mloggerResult.ToString());
            }

            if (unityDebugResult != null && mloggerResult != null &&
                unityDebugResult.success && mloggerResult.success)
            {
                sb.AppendLine("\n--- Performance Comparison ---");
                var timeRatio = (double)mloggerResult.totalTimeMs / unityDebugResult.totalTimeMs;
                var speedup = unityDebugResult.totalTimeMs / (double)mloggerResult.totalTimeMs;
                var throughputRatio = mloggerResult.logsPerSecond / unityDebugResult.logsPerSecond;

                sb.AppendLine($"Time Ratio (MLogger/Unity): {timeRatio:F2}x");
                sb.AppendLine($"Speedup: {speedup:F2}x");
                sb.AppendLine($"Throughput Ratio (MLogger/Unity): {throughputRatio:F2}x");
                sb.AppendLine(
                    $"Memory Difference: {(mloggerResult.memoryDelta - unityDebugResult.memoryDelta) / 1024.0 / 1024.0:F2} MB");

                sb.AppendLine(speedup > 1.0
                    ? $"\n✓ MLogger is {speedup:F2}x faster than Unity Debug.Log"
                    : $"\n⚠ MLogger is {1.0 / speedup:F2}x slower than Unity Debug.Log");
            }

            sb.AppendLine("========================================\n");
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Test High Frequency (1000 logs)")]
        public void TestHighFrequency()
        {
            logCount = 1000;
            RunAllTests();
        }

        [ContextMenu("Test Medium Frequency (10000 logs)")]
        public void TestMediumFrequency()
        {
            logCount = 10000;
            RunAllTests();
        }

        [ContextMenu("Test Low Frequency (100000 logs)")]
        public void TestLowFrequency()
        {
            logCount = 100000;
            RunAllTests();
        }

        private class UnityLogHandler : ILogHandler
        {
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
            }
        }
    }
}