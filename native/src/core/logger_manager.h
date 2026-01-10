#ifndef LOGGER_MANAGER_H
#define LOGGER_MANAGER_H

#include "logger_config.h"
#include <functional>
#include <memory>
#include <mutex>
#include <spdlog/async.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <spdlog/spdlog.h>

namespace mlogger
{

using ErrorCallback = std::function<void(const char*, const char*)>;

class LoggerManager
{
public:
    static LoggerManager& getInstance();

    bool initialize(const LoggerConfig& config);
    bool initialize(const std::string& log_path);
    bool isInitialized() const;
    void terminate();

    void log(int level, const char* message);
    void logException(const char* exception_type, const char* message, const char* stack_trace);

    void flush();

    int  getLogLevel() const;
    void setLogLevel(int level);
    void setErrorCallback(ErrorCallback callback);

    LoggerManager(const LoggerManager&)            = delete;
    LoggerManager& operator=(const LoggerManager&) = delete;
    LoggerManager(LoggerManager&&)                 = delete;
    LoggerManager& operator=(LoggerManager&&)      = delete;

private:
    LoggerManager() = default;
    ~LoggerManager();

    std::shared_ptr<spdlog::logger>       logger_;
    std::shared_ptr<spdlog::async_logger> async_logger_;
    bool                                  initialized_ = false;
    bool                                  async_mode_  = false;
    ErrorCallback                         error_callback_;
    mutable std::mutex                    mutex_;

    void reportError(const char* function_name, const char* error_message) const;

    static spdlog::level::level_enum convertLogLevel(int level);
    static int                       convertToInt(spdlog::level::level_enum level);
};

}   // namespace mlogger

#endif   // LOGGER_MANAGER_H
