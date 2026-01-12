using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MLogger.Editor
{
    public class MLoggerLogViewer : EditorWindow
    {
        private Vector2 scrollPosition;
        private string logContent = "";
        private string searchText = "";
        private bool autoRefresh = false;
        private double lastRefreshTime = 0;
        private const double refreshInterval = 2.0;

        [MenuItem("Tools/MLogger/Log Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MLoggerLogViewer>("MLogger Log Viewer");
            window.minSize = new Vector2(600, 400);
            window.RefreshLog();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshLog();
            }

            if (GUILayout.Button("Open Directory", EditorStyles.toolbarButton))
            {
                OpenLogDirectory();
            }

            if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Log", "This will clear the displayed log content. Continue?",
                        "Yes",
                        "No"))
                {
                    logContent = "";
                }
            }

            GUILayout.FlexibleSpace();

            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchText = EditorGUILayout.TextField(searchText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            var displayContent = logContent;
            if (!string.IsNullOrEmpty(searchText))
            {
                displayContent = FilterLogContent(logContent, searchText);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.TextArea(displayContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Lines: {GetLineCount(displayContent)}", GUILayout.Width(100));
            EditorGUILayout.LabelField(MLoggerManager.IsInitialized ? "Status: Initialized" : "Status: Not Initialized",
                GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        private void Update()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                RefreshLog();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private void RefreshLog()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var logPath = string.IsNullOrEmpty(config.logPath) ? MLoggerConfig.CreateDefault().logPath : config.logPath;

            if (File.Exists(logPath))
            {
                try
                {
                    using var reader = new StreamReader(logPath);
                    logContent = reader.ReadToEnd();
                }
                catch (Exception e)
                {
                    logContent = $"Error reading log file: {e.Message}";
                }
            }
            else
            {
                logContent = $"Log file not found: {logPath}\n\nMake sure MLogger is initialized and has written logs.";
            }

            lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private static void OpenLogDirectory()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var logPath = string.IsNullOrEmpty(config.logPath) ? MLoggerConfig.CreateDefault().logPath : config.logPath;
            var logDir = Path.GetDirectoryName(logPath);

            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Application.dataPath;
            }

            if (Directory.Exists(logDir))
            {
                EditorUtility.RevealInFinder(logDir);
            }
            else
            {
                EditorUtility.DisplayDialog("Directory Not Found", $"Log directory does not exist:\n{logDir}", "OK");
            }
        }

        private static string FilterLogContent(string content, string search)
        {
            if (string.IsNullOrEmpty(search))
                return content;

            var lines = content.Split('\n');
            var filtered = System.Linq.Enumerable.Where(lines, line =>
                line.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

            return string.Join("\n", filtered);
        }

        private static int GetLineCount(string content)
        {
            return string.IsNullOrEmpty(content)
                ? 0
                : content.Split('\n').Length;
        }
    }
}