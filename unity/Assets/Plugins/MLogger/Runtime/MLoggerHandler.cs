using UnityEngine;

namespace MLogger
{
    /// <summary>
    /// Unity ILogHandler implementation that forwards Unity logs to native layer. Handles both formatted logs and exceptions with fallback to default handler.
    /// </summary>
    public class MLoggerHandler : ILogHandler
    {
        private readonly ILogHandler _defaultHandler;
        private readonly bool _alsoLogToUnity;

        public MLoggerHandler(bool alsoLogToUnity = true)
        {
            _defaultHandler = Debug.unityLogger.logHandler;
            _alsoLogToUnity = alsoLogToUnity;
        }

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
                    _defaultHandler.LogFormat(LogType.Error, context,
                        "[MLogger] Failed to log to native: {0}", e.Message);
                }
            }

            if (_alsoLogToUnity)
            {
                _defaultHandler.LogFormat(logType, context, format, args);
            }
        }

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
