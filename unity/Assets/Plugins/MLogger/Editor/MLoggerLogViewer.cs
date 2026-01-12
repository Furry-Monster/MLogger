using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MLogger.Editor
{
    public class MLoggerLogViewer : EditorWindow
    {
        private Vector2 scrollPosition;
        private string logContent = "";
        private string[] logLines = Array.Empty<string>();
        private string searchText = "";
        private bool useRegex = false;
        private bool autoRefresh = false;
        private double lastRefreshTime = 0;
        private const double refreshInterval = 2.0;

        private readonly Dictionary<LogLevel, bool> levelFilters = new()
        {
            { LogLevel.Trace, true },
            { LogLevel.Debug, true },
            { LogLevel.Info, true },
            { LogLevel.Warn, true },
            { LogLevel.Error, true },
            { LogLevel.Critical, true }
        };

        private readonly LogStatistics statistics = new();
        private string currentLogPath = "";
        private long currentFileSize = 0;
        private readonly List<string> availableLogFiles = new();
        private int selectedFileIndex = 0;

        private string cachedFilteredContent = "";
        private string lastSearchText = "";
        private bool lastUseRegex = false;
        private Dictionary<LogLevel, bool> lastLevelFilters = new()
        {
            { LogLevel.Trace, true },
            { LogLevel.Debug, true },
            { LogLevel.Info, true },
            { LogLevel.Warn, true },
            { LogLevel.Error, true },
            { LogLevel.Critical, true }
        };
        private bool filtersChanged = true;
        private const int maxDisplayLines = 10000;
        private bool isLoading = false;
        private double lastFileListRefresh = 0;
        private const double fileListRefreshInterval = 5.0;

        private class LogStatistics
        {
            public int totalLines = 0;
            public int traceCount = 0;
            public int debugCount = 0;
            public int infoCount = 0;
            public int warnCount = 0;
            public int errorCount = 0;
            public int criticalCount = 0;

            public void Reset()
            {
                totalLines = 0;
                traceCount = 0;
                debugCount = 0;
                infoCount = 0;
                warnCount = 0;
                errorCount = 0;
                criticalCount = 0;
            }
        }

        [MenuItem("Window/MLogger/Log Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MLoggerLogViewer>("MLogger Log Viewer");
            window.minSize = new Vector2(800, 500);
            window.RefreshLog();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawSearchBar();
            DrawLogContent();
            DrawStatusBar();
        }

        private void DrawToolbar()
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

            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
            {
                ShowExportMenu();
            }

            if (GUILayout.Button("Clean", EditorStyles.toolbarButton))
            {
                ShowCleanMenu();
            }

            if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Log",
                        "This will clear the displayed log content. Continue?", "Yes", "No"))
                {
                    logContent = "";
                    logLines = Array.Empty<string>();
                    statistics.Reset();
                }
            }

            GUILayout.FlexibleSpace();

            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            DrawFileSelector();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filter by Level:", GUILayout.Width(100));

            levelFilters[LogLevel.Trace] = GUILayout.Toggle(levelFilters[LogLevel.Trace], "Trace",
                EditorStyles.miniButton);
            levelFilters[LogLevel.Debug] = GUILayout.Toggle(levelFilters[LogLevel.Debug], "Debug",
                EditorStyles.miniButton);
            levelFilters[LogLevel.Info] = GUILayout.Toggle(levelFilters[LogLevel.Info], "Info",
                EditorStyles.miniButton);
            levelFilters[LogLevel.Warn] = GUILayout.Toggle(levelFilters[LogLevel.Warn], "Warn",
                EditorStyles.miniButton);
            levelFilters[LogLevel.Error] = GUILayout.Toggle(levelFilters[LogLevel.Error], "Error",
                EditorStyles.miniButton);
            levelFilters[LogLevel.Critical] = GUILayout.Toggle(levelFilters[LogLevel.Critical], "Critical",
                EditorStyles.miniButton);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
            {
                foreach (var key in levelFilters.Keys.ToList())
                {
                    levelFilters[key] = true;
                }
            }

            if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(40)))
            {
                foreach (var key in levelFilters.Keys.ToList())
                {
                    levelFilters[key] = false;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchText = EditorGUILayout.TextField(searchText);
            useRegex = GUILayout.Toggle(useRegex, "Regex", EditorStyles.miniButton, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogContent()
        {
            EditorGUILayout.Space(5);

            if (isLoading)
            {
                EditorGUILayout.HelpBox("Loading log file...", MessageType.Info);
                return;
            }

            CheckFilterChanges();
            UpdateCachedContent();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (!string.IsNullOrEmpty(cachedFilteredContent))
            {
                var style = new GUIStyle(EditorStyles.textArea)
                {
                    richText = true,
                    wordWrap = true
                };
                EditorGUILayout.TextArea(cachedFilteredContent, style, GUILayout.ExpandHeight(true));
            }
            else
            {
                EditorGUILayout.LabelField("No logs match the current filters.");
            }

            EditorGUILayout.EndScrollView();
        }

        private void CheckFilterChanges()
        {
            if (searchText != lastSearchText || useRegex != lastUseRegex)
            {
                filtersChanged = true;
                lastSearchText = searchText;
                lastUseRegex = useRegex;
            }

            foreach (var kvp in levelFilters)
            {
                if (!lastLevelFilters.ContainsKey(kvp.Key) || lastLevelFilters[kvp.Key] != kvp.Value)
                {
                    filtersChanged = true;
                    break;
                }
            }

            if (filtersChanged)
            {
                lastLevelFilters = new Dictionary<LogLevel, bool>(levelFilters);
            }
        }

        private void UpdateCachedContent()
        {
            if (!filtersChanged && !string.IsNullOrEmpty(cachedFilteredContent))
                return;

            filtersChanged = false;

            EditorApplication.delayCall += () =>
            {
                var filteredLines = GetFilteredLines();
                if (filteredLines.Length == 0)
                {
                    cachedFilteredContent = "";
                    return;
                }

                var linesToProcess = filteredLines.Length > maxDisplayLines
                    ? filteredLines.Skip(filteredLines.Length - maxDisplayLines).ToArray()
                    : filteredLines;

                var coloredLines = linesToProcess.Select(ApplyColorToLine).ToArray();
                cachedFilteredContent = string.Join("\n", coloredLines);

                if (filteredLines.Length > maxDisplayLines)
                {
                    cachedFilteredContent = $"[Showing last {maxDisplayLines} of {filteredLines.Length} lines]\n" + cachedFilteredContent;
                }

                Repaint();
            };
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Lines: {statistics.totalLines}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Size: {FormatFileSize(currentFileSize)}", GUILayout.Width(120));

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField($"Trace: {statistics.traceCount}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Debug: {statistics.debugCount}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Info: {statistics.infoCount}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Warn: {statistics.warnCount}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Error: {statistics.errorCount}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Critical: {statistics.criticalCount}", GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(
                MLoggerManager.IsInitialized ? "Status: Initialized" : "Status: Not Initialized",
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

        private void DrawFileSelector()
        {
            if (EditorApplication.timeSinceStartup - lastFileListRefresh > fileListRefreshInterval)
            {
                RefreshAvailableFiles();
                lastFileListRefresh = EditorApplication.timeSinceStartup;
            }

            if (availableLogFiles.Count > 1)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Log File:", GUILayout.Width(70));

                var newIndex = EditorGUILayout.Popup(selectedFileIndex,
                    availableLogFiles.Select(f => Path.GetFileName(f)).ToArray());

                if (newIndex != selectedFileIndex)
                {
                    selectedFileIndex = newIndex;
                    if (selectedFileIndex < availableLogFiles.Count)
                    {
                        currentLogPath = availableLogFiles[selectedFileIndex];
                        LoadLogFile(currentLogPath);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void RefreshAvailableFiles()
        {
            availableLogFiles.Clear();

            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var baseLogPath = string.IsNullOrEmpty(config.logPath)
                ? MLoggerConfig.CreateDefault().logPath
                : config.logPath;

            var logDir = Path.GetDirectoryName(baseLogPath);
            var baseFileName = Path.GetFileNameWithoutExtension(baseLogPath);
            var extension = Path.GetExtension(baseLogPath);

            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return;

            try
            {
                var files = Directory.GetFiles(logDir, $"{baseFileName}*{extension}")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                if (File.Exists(baseLogPath))
                {
                    availableLogFiles.Add(baseLogPath);
                }

                foreach (var file in files)
                {
                    if (file != baseLogPath && !availableLogFiles.Contains(file))
                    {
                        availableLogFiles.Add(file);
                    }
                }

                if (availableLogFiles.Count == 0 && File.Exists(baseLogPath))
                {
                    availableLogFiles.Add(baseLogPath);
                }

                if (selectedFileIndex >= availableLogFiles.Count)
                {
                    selectedFileIndex = 0;
                }

                if (availableLogFiles.Count > 0 && string.IsNullOrEmpty(currentLogPath))
                {
                    currentLogPath = availableLogFiles[0];
                }
            }
            catch
            {
                // ignored
            }
        }

        private void RefreshLog()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            if (string.IsNullOrEmpty(currentLogPath))
            {
                currentLogPath = string.IsNullOrEmpty(config.logPath)
                    ? MLoggerConfig.CreateDefault().logPath
                    : config.logPath;
            }

            LoadLogFile(currentLogPath);
        }

        private void LoadLogFile(string logPath)
        {
            currentLogPath = logPath;
            isLoading = true;
            filtersChanged = true;
            cachedFilteredContent = "";

            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (File.Exists(logPath))
                    {
                        var fileInfo = new FileInfo(logPath);
                        currentFileSize = fileInfo.Length;

                        if (fileInfo.Length > 50 * 1024 * 1024)
                        {
                            LoadLargeFile(logPath);
                        }
                        else
                        {
                            using var reader = new StreamReader(logPath);
                            logContent = reader.ReadToEnd();
                            logLines = logContent.Split('\n');
                        }

                        UpdateStatisticsAsync();
                    }
                    else
                    {
                        logContent = $"Log file not found: {logPath}\n\nMake sure MLogger is initialized and has written logs.";
                        logLines = Array.Empty<string>();
                        statistics.Reset();
                        currentFileSize = 0;
                    }
                }
                catch (Exception e)
                {
                    logContent = $"Error reading log file: {e.Message}";
                    logLines = Array.Empty<string>();
                }
                finally
                {
                    isLoading = false;
                    Repaint();
                }

                lastRefreshTime = EditorApplication.timeSinceStartup;
            };
        }

        private void LoadLargeFile(string logPath)
        {
            var lines = new List<string>();
            using var reader = new StreamReader(logPath);
            string line;
            var lineCount = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineCount++;
                if (lineCount > maxDisplayLines * 2)
                {
                    lines.RemoveAt(0);
                }
                lines.Add(line);
            }

            logLines = lines.ToArray();
            logContent = string.Join("\n", logLines);
        }

        private void UpdateStatisticsAsync()
        {
            EditorApplication.delayCall += () =>
            {
                statistics.Reset();
                statistics.totalLines = logLines.Length;

                var batchSize = 1000;
                var processed = 0;

                void ProcessBatch()
                {
                    var end = Math.Min(processed + batchSize, logLines.Length);
                    for (var i = processed; i < end; i++)
                    {
                        var level = DetectLogLevel(logLines[i]);
                        switch (level)
                        {
                            case LogLevel.Trace:
                                statistics.traceCount++;
                                break;
                            case LogLevel.Debug:
                                statistics.debugCount++;
                                break;
                            case LogLevel.Info:
                                statistics.infoCount++;
                                break;
                            case LogLevel.Warn:
                                statistics.warnCount++;
                                break;
                            case LogLevel.Error:
                                statistics.errorCount++;
                                break;
                            case LogLevel.Critical:
                                statistics.criticalCount++;
                                break;
                        }
                    }

                    processed = end;

                    if (processed < logLines.Length)
                    {
                        EditorApplication.delayCall += ProcessBatch;
                    }
                    else
                    {
                        Repaint();
                    }
                }

                ProcessBatch();
            };
        }

        private static LogLevel DetectLogLevel(string line)
        {
            if (string.IsNullOrEmpty(line))
                return LogLevel.Info;

            var upperLine = line.ToUpperInvariant();
            if (upperLine.Contains("[TRACE]") || upperLine.Contains("TRACE"))
                return LogLevel.Trace;
            if (upperLine.Contains("[DEBUG]") || upperLine.Contains("DEBUG"))
                return LogLevel.Debug;
            if (upperLine.Contains("[INFO]") || upperLine.Contains("INFO"))
                return LogLevel.Info;
            if (upperLine.Contains("[WARN]") || upperLine.Contains("WARNING") || upperLine.Contains("WARN"))
                return LogLevel.Warn;
            if (upperLine.Contains("[ERROR]") || upperLine.Contains("ERROR"))
                return LogLevel.Error;
            if (upperLine.Contains("[CRITICAL]") || upperLine.Contains("CRITICAL") || upperLine.Contains("FATAL"))
                return LogLevel.Critical;

            return LogLevel.Info;
        }

        private string[] GetFilteredLines()
        {
            IEnumerable<string> filtered = logLines;
            var filteredList = filtered
                .Where(line => levelFilters[DetectLogLevel(line)])
                .ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                if (useRegex)
                {
                    try
                    {
                        var regex = new Regex(searchText, RegexOptions.IgnoreCase);
                        filteredList = filteredList.Where(line => regex.IsMatch(line)).ToList();
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    filteredList = filteredList
                        .Where(line => line.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }
            }

            return filteredList.ToArray();
        }

        private static string ApplyColorToLine(string line)
        {
            var level = DetectLogLevel(line);
            var color = GetColorForLevel(level);
            return $"<color={color}>{line}</color>";
        }

        private static string GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => "#808080",
                LogLevel.Debug => "#A0A0A0",
                LogLevel.Info => "#FFFFFF",
                LogLevel.Warn => "#FFAA00",
                LogLevel.Error => "#FF0000",
                LogLevel.Critical => "#FF00FF",
                _ => "#FFFFFF"
            };
        }

        private static string FormatFileSize(long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F2} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F2} MB",
                _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
            };
        }

        private static void OpenLogDirectory()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var logPath = string.IsNullOrEmpty(config.logPath)
                ? MLoggerConfig.CreateDefault().logPath
                : config.logPath;
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

        private void ShowExportMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Export as Text"), false, () => ExportLog(false));
            menu.AddItem(new GUIContent("Export as CSV"), false, () => ExportLog(true));
            menu.ShowAsContext();
        }

        private void ExportLog(bool asCsv)
        {
            var filteredLines = GetFilteredLines();
            if (filteredLines.Length == 0)
            {
                EditorUtility.DisplayDialog("Export", "No logs to export with current filters.", "OK");
                return;
            }

            var extension = asCsv ? "csv" : "txt";
            var path = EditorUtility.SaveFilePanel("Export Log", "", "mlogger_export", extension);
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                if (asCsv)
                {
                    var csvLines = new List<string> { "Level,Message,Timestamp" };
                    foreach (var line in filteredLines)
                    {
                        var level = DetectLogLevel(line);
                        var cleanLine = line.Replace("\"", "\"\"");
                        csvLines.Add($"\"{level}\",\"{cleanLine}\",\"\"");
                    }

                    File.WriteAllLines(path, csvLines);
                }
                else
                {
                    File.WriteAllLines(path, filteredLines);
                }

                EditorUtility.DisplayDialog("Export", $"Log exported successfully to:\n{path}", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export log:\n{e.Message}", "OK");
            }
        }

        private void ShowCleanMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clean by Count"), false, CleanLogsByCount);
            menu.AddItem(new GUIContent("Clean by Size"), false, CleanLogsBySize);
            menu.AddItem(new GUIContent("Clean by Age"), false, CleanLogsByAge);
            menu.ShowAsContext();
        }

        private void CleanLogsByCount()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var maxFiles = config.maxFiles;

            if (EditorUtility.DisplayDialog("Clean Logs",
                    $"This will keep only the {maxFiles} most recent log files. Continue?", "Yes", "No"))
            {
                CleanLogFiles(maxFiles, 0, 0);
            }
        }

        private void CleanLogsBySize()
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var maxFileSize = config.maxFileSize * config.maxFiles;

            if (EditorUtility.DisplayDialog("Clean Logs",
                    $"This will delete log files until total size is below {FormatFileSize(maxFileSize)}. Continue?",
                    "Yes", "No"))
            {
                CleanLogFiles(0, maxFileSize, 0);
            }
        }

        private void CleanLogsByAge()
        {
            if (EditorUtility.DisplayDialog("Clean Logs",
                    "This will delete log files older than 7 days. Continue?", "Yes", "No"))
            {
                const int days = 7;
                CleanLogFiles(0, 0, days);
            }
        }

        private void CleanLogFiles(int maxCount, long maxTotalSize, int maxAgeDays)
        {
            var config = MLoggerSettings.Instance?.Config ?? MLoggerConfig.CreateDefault();
            var baseLogPath = string.IsNullOrEmpty(config.logPath)
                ? MLoggerConfig.CreateDefault().logPath
                : config.logPath;

            var logDir = Path.GetDirectoryName(baseLogPath);
            var baseFileName = Path.GetFileNameWithoutExtension(baseLogPath);
            var extension = Path.GetExtension(baseLogPath);

            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
            {
                EditorUtility.DisplayDialog("Clean Failed", "Log directory not found.", "OK");
                return;
            }

            try
            {
                var files = Directory.GetFiles(logDir, $"{baseFileName}*{extension}")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                var filesToDelete = new List<FileInfo>();
                var totalSize = 0L;
                var cutoffDate = DateTime.Now.AddDays(-maxAgeDays);

                foreach (var file in files)
                {
                    var shouldDelete = false;

                    if (maxCount > 0 && filesToDelete.Count + 1 > maxCount)
                    {
                        shouldDelete = true;
                    }
                    else if (maxTotalSize > 0)
                    {
                        if (totalSize + file.Length > maxTotalSize)
                        {
                            shouldDelete = true;
                        }
                        else
                        {
                            totalSize += file.Length;
                        }
                    }
                    else if (maxAgeDays > 0 && file.LastWriteTime < cutoffDate)
                    {
                        shouldDelete = true;
                    }

                    if (shouldDelete)
                    {
                        filesToDelete.Add(file);
                    }
                }

                if (filesToDelete.Count == 0)
                {
                    EditorUtility.DisplayDialog("Clean", "No log files need to be cleaned.", "OK");
                    return;
                }

                var deletedCount = 0;
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file.FullName);
                        deletedCount++;
                    }
                    catch
                    {
                        // ignored
                    }
                }

                EditorUtility.DisplayDialog("Clean", $"Deleted {deletedCount} log file(s).", "OK");
                RefreshAvailableFiles();
                RefreshLog();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Clean Failed", $"Failed to clean logs:\n{e.Message}", "OK");
            }
        }
    }
}