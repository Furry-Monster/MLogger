#include "bridge.h"
#include "core/logger_config.h"
#include "core/logger_manager.h"
#include <cstring>

using namespace mlogger;

#ifdef __cplusplus
extern "C" {
#endif   // __cplusplus extern begin

EXPORT_API int init(const char* log_path, size_t max_file_size, int max_files, int async_mode,
                    int thread_pool_size, int min_log_level)
{
    LoggerConfig config;
    config.log_path         = log_path;
    config.max_file_size    = max_file_size;
    config.max_files        = max_files;
    config.async_mode       = (async_mode != 0);
    config.thread_pool_size = thread_pool_size;
    config.min_log_level    = min_log_level;

    LoggerManager& manager = LoggerManager::getInstance();
    return manager.initialize(config) ? 1 : 0;
}

EXPORT_API int initDefault(const char* log_path)
{
    LoggerManager& manager = LoggerManager::getInstance();
    return manager.initialize(log_path) ? 1 : 0;
}

EXPORT_API void logMessage(int log_level, const char* message)
{
    LoggerManager& manager = LoggerManager::getInstance();
    manager.log(log_level, message);
}

EXPORT_API void logException(const char* exception_type, const char* message,
                             const char* stack_trace)
{
    LoggerManager& manager = LoggerManager::getInstance();
    manager.logException(exception_type, message, stack_trace);
}

EXPORT_API void flush()
{
    LoggerManager& manager = LoggerManager::getInstance();
    manager.flush();
}

EXPORT_API void setLogLevel(int log_level)
{
    LoggerManager& manager = LoggerManager::getInstance();
    manager.setLogLevel(log_level);
}

EXPORT_API int getLogLevel()
{
    LoggerManager& manager = LoggerManager::getInstance();
    return manager.getLogLevel();
}

EXPORT_API int isInit()
{
    LoggerManager& manager = LoggerManager::getInstance();
    return manager.isInitialized() ? 1 : 0;
}

EXPORT_API void terminate()
{
    LoggerManager& manager = LoggerManager::getInstance();
    manager.terminate();
}

#ifdef __cplusplus
}
#endif   // __cplusplus extern end
