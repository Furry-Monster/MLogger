using UnityEngine;

namespace MLogger
{
    /// <summary>
    /// Unity ILogHandler 实现，将 Unity 日志转发到 Native 层
    /// </summary>
    public class MLoggerHandler : ILogHandler
    {
        private readonly ILogHandler _defaultHandler;
        private readonly bool _alsoLogToUnity;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="alsoLogToUnity">是否同时输出到 Unity 控制台</param>
        public MLoggerHandler(bool alsoLogToUnity = true)
        {
            // 保存默认处理器作为后备
            _defaultHandler = Debug.unityLogger.logHandler;
            _alsoLogToUnity = alsoLogToUnity;
        }

        /// <summary>
        /// 处理格式化日志
        /// </summary>
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            var message = args != null && args.Length > 0
                ? string.Format(format, args)
                : format;

            var level = MapLogType(logType);

            if (MLoggerManager.IsInitialized)
            {
                try
                {
                    MLoggerNative.logMessage((int)level, message);
                }
                catch (System.Exception e)
                {
                    // Native 调用失败时，回退到默认处理器
                    _defaultHandler.LogFormat(LogType.Error, context,
                        "[MLogger] Failed to log to native: {0}", e.Message);
                }
            }

            if (_alsoLogToUnity)
            {
                _defaultHandler.LogFormat(logType, context, format, args);
            }
        }

        /// <summary>
        /// 处理异常日志
        /// </summary>
        public void LogException(System.Exception exception, Object context)
        {
            if (MLoggerManager.IsInitialized && exception != null)
            {
                try
                {
                    var exceptionType = exception.GetType().FullName ?? "UnknownException";
                    var message = exception.Message ?? "";
                    var stackTrace = exception.StackTrace ?? "";

                    MLoggerNative.logException(exceptionType, message, stackTrace);
                }
                catch (System.Exception e)
                {
                    // Native 调用失败时，回退到默认处理器
                    _defaultHandler.LogException(
                        new System.Exception("[MLogger] Failed to log exception to native", e),
                        context);
                }
            }

            if (_alsoLogToUnity)
            {
                _defaultHandler.LogException(exception, context);
            }
        }

        /// <summary>
        /// 映射 Unity LogType 到 Native LogLevel
        /// </summary>
        private static LogLevel MapLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Error or LogType.Assert => LogLevel.Error,
                LogType.Warning => LogLevel.Warn,
                LogType.Log => LogLevel.Info,
                LogType.Exception => LogLevel.Critical,
                _ => LogLevel.Info
            };
        }
    }
}