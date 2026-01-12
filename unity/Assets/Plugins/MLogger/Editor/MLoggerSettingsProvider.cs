using System.IO;
using UnityEditor;
using UnityEngine;
using MLogger;
using UnityEngine.UIElements;

namespace MLogger.Editor
{
    public class MLoggerSettingsProvider : SettingsProvider
    {
        private MLoggerSettings settings;
        private SerializedObject serializedSettings;

        private class Styles
        {
            public static readonly GUIContent LogPathLabel = new("Log Path", "Path to the log file");

            public static readonly GUIContent MaxFileSizeLabel =
                new("Max File Size (MB)", "Maximum size of each log file in megabytes");

            public static readonly GUIContent MaxFilesLabel = new("Max Files", "Maximum number of log files to keep");

            public static readonly GUIContent AsyncModeLabel =
                new("Async Mode", "Use asynchronous logging for better performance");

            public static readonly GUIContent ThreadPoolSizeLabel =
                new("Thread Pool Size", "Number of threads in the async thread pool");

            public static readonly GUIContent MinLogLevelLabel = new("Min Log Level", "Minimum log level to record");

            public static readonly GUIContent AutoInitializeLabel =
                new("Auto Initialize", "Automatically initialize logger when Unity starts");

            public static readonly GUIContent AlsoLogToUnityLabel =
                new("Also Log to Unity", "Also output logs to Unity console");
        }

        public MLoggerSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settings = MLoggerSettings.GetOrCreateSettings();
            serializedSettings = new SerializedObject(settings);
        }

        public override void OnGUI(string searchContext)
        {
            if (settings == null || serializedSettings == null)
            {
                EditorGUILayout.HelpBox("Failed to load MLogger settings.", MessageType.Error);
                return;
            }

            serializedSettings.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MLogger Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            var config = settings.Config;
            var newConfig = new MLoggerConfig
            {
                logPath = config.logPath,
                maxFileSize = config.maxFileSize,
                maxFiles = config.maxFiles,
                asyncMode = config.asyncMode,
                threadPoolSize = config.threadPoolSize,
                minLogLevel = config.minLogLevel,
                autoInitialize = config.autoInitialize,
                alsoLogToUnity = config.alsoLogToUnity
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.LogPathLabel, GUILayout.Width(150));
            newConfig.logPath = EditorGUILayout.TextField(newConfig.logPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.SaveFilePanel("Select Log File", Path.GetDirectoryName(newConfig.logPath),
                    "game",
                    "log");
                if (!string.IsNullOrEmpty(path))
                {
                    newConfig.logPath = path;
                }
            }

            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                newConfig.logPath = MLoggerConfig.CreateDefault().logPath;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            var maxFileSizeMB = newConfig.maxFileSize / (1024.0 * 1024.0);
            maxFileSizeMB = EditorGUILayout.Slider(Styles.MaxFileSizeLabel, (float)maxFileSizeMB, 1.0f, 1000.0f);
            newConfig.maxFileSize = (long)(maxFileSizeMB * 1024 * 1024);

            newConfig.maxFiles = EditorGUILayout.IntSlider(Styles.MaxFilesLabel, newConfig.maxFiles, 1, 50);

            EditorGUILayout.Space(5);

            newConfig.asyncMode = EditorGUILayout.Toggle(Styles.AsyncModeLabel, newConfig.asyncMode);

            EditorGUI.BeginDisabledGroup(!newConfig.asyncMode);
            newConfig.threadPoolSize =
                EditorGUILayout.IntSlider(Styles.ThreadPoolSizeLabel, newConfig.threadPoolSize, 1, 16);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            newConfig.minLogLevel = (LogLevel)EditorGUILayout.EnumPopup(Styles.MinLogLevelLabel, newConfig.minLogLevel);

            EditorGUILayout.Space(5);

            newConfig.autoInitialize = EditorGUILayout.Toggle(Styles.AutoInitializeLabel, newConfig.autoInitialize);
            newConfig.alsoLogToUnity = EditorGUILayout.Toggle(Styles.AlsoLogToUnityLabel, newConfig.alsoLogToUnity);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Default", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Configuration",
                        "Are you sure you want to reset all settings to default?", "Yes", "No"))
                {
                    config = MLoggerConfig.CreateDefault();
                    settings.Config = config;
                    serializedSettings.Update();
                }
            }

            if (GUILayout.Button("Open Log Directory", GUILayout.Height(30)))
            {
                var logPath = string.IsNullOrEmpty(newConfig.logPath)
                    ? MLoggerConfig.CreateDefault().logPath
                    : newConfig.logPath;
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
                    EditorUtility.DisplayDialog("Directory Not Found", $"Log directory does not exist:\n{logDir}",
                        "OK");
                }
            }

            if (GUILayout.Button("Flush Logs", GUILayout.Height(30)))
            {
                if (MLoggerManager.IsInitialized)
                {
                    MLoggerManager.Flush();
                    Debug.Log("[MLogger] Logs flushed successfully");
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Initialized",
                        "MLogger is not initialized. Logs cannot be flushed.",
                        "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Initialized", MLoggerManager.IsInitialized);
            if (MLoggerManager.IsInitialized)
            {
                EditorGUILayout.EnumPopup("Current Log Level", MLoggerManager.GetLogLevel());
                if (MLoggerManager.CurrentConfig != null)
                {
                    EditorGUILayout.TextField("Current Log Path", MLoggerManager.CurrentConfig.logPath);
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                settings.Config = newConfig;
                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateMLoggerSettingsProvider()
        {
            var provider = new MLoggerSettingsProvider("Project/MLogger", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
}