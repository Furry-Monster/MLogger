#include "logger_manager.h"
#include "core/logger_config.h"
#include "utils/path_utils.h"
#include "utils/str_utils.h"
#include <filesystem>
#include <iostream>
#include <mutex>
#include <spdlog/async.h>
#include <spdlog/sinks/rotating_file_sink.h>

namespace mlogger
{

LoggerManager& LoggerManager::getInstance()
{
    static LoggerManager instance;
    return instance;
}

bool LoggerManager::initialize(const LoggerConfig& config)
{
    if (!config.isValid()) {
        return false;
    }

    bool need_terminate = false;
    {
        std::lock_guard<std::mutex> lock(mutex_);
        if (initialized_) need_terminate = true;
    }
    if (need_terminate) terminate();

    std::lock_guard<std::mutex> lock(mutex_);

    try {
        // prepare directory
        if (!utils::ensureDirectoryExists(config.log_path)) {
            throw std::runtime_error("Failed to create log directory");
        }

        // prepare configs
        auto rotating_sink = std::make_shared<spdlog::sinks::rotating_file_sink_mt>(
            config.log_path, config.max_file_size, config.max_files);
        if (rotating_sink == nullptr) {
            throw std::runtime_error("Failed to create rotating file sink");
        }
        spdlog::level::level_enum min_level = convertLogLevel(config.min_log_level);
        rotating_sink->set_level(min_level);
        async_mode_ = config.async_mode;

        // create specific logger
        if (async_mode_) {
            auto tp = spdlog::thread_pool();
            if (tp == nullptr) {
                spdlog::init_thread_pool(8192, config.thread_pool_size);
            }
            tp = spdlog::thread_pool();
            if (tp == nullptr) {
                throw std::runtime_error("Failed to create thread pool");
            }

            async_logger_ =
                std::make_shared<spdlog::async_logger>("mlogger",
                                                       rotating_sink,
                                                       spdlog::thread_pool(),
                                                       spdlog::async_overflow_policy::block);
            if (async_logger_ == nullptr) {
                throw std::runtime_error("Failed to create async logger");
            }

            async_logger_->set_level(min_level);
            async_logger_->flush_on(spdlog::level::err);
            spdlog::register_logger(async_logger_);

            logger_ = async_logger_;
        } else {
            logger_ = std::make_shared<spdlog::logger>("mlogger", rotating_sink);
            if (logger_ == nullptr) {
                throw std::runtime_error("Failed to create logger");
            }

            logger_->set_level(min_level);
            logger_->flush_on(spdlog::level::err);
            spdlog::register_logger(logger_);
        }

        initialized_ = true;
        return true;
    } catch (const std::exception& e) {
        initialized_ = false;
        reportError("initialize", e.what());
        return false;
    } catch (...) {
        initialized_ = false;
        reportError("initialize", "Unknown exception occurred during initialization");
        return false;
    }
}

bool LoggerManager::initialize(const std::string& log_path)
{
    LoggerConfig config(log_path);
    return initialize(config);
}

void LoggerManager::log(int level, const char* message)
{
    if (!initialized_ || !message) {
        return;
    }

    std::lock_guard<std::mutex> lock(mutex_);
    if (!logger_) {
        return;
    }

    spdlog::level::level_enum spdlog_level = convertLogLevel(level);

    try {
        switch (spdlog_level) {
        case spdlog::level::trace: logger_->trace(message); break;
        case spdlog::level::debug: logger_->debug(message); break;
        case spdlog::level::info: logger_->info(message); break;
        case spdlog::level::warn: logger_->warn(message); break;
        case spdlog::level::err: logger_->error(message); break;
        case spdlog::level::critical: logger_->critical(message); break;
        default: throw std::runtime_error("Invalid log level");
        }
    } catch (const std::exception& e) {
        reportError("log", e.what());
    } catch (...) {
        reportError("log", "Unknown exception occurred while logging");
    }
}

void LoggerManager::logException(const char* exception_type, const char* message,
                                 const char* stack_trace)
{
    if (!initialized_) {
        return;
    }

    std::lock_guard<std::mutex> lock(mutex_);
    if (!logger_) {
        return;
    }

    try {
        std::string full_message =
            utils::formatExceptionMessage(exception_type, message, stack_trace);
        logger_->error(full_message);
    } catch (const std::exception& e) {
        reportError("logException", e.what());
    } catch (...) {
        reportError("logException", "Unknown exception occurred while logging exception");
    }
}

void LoggerManager::flush()
{
    if (!initialized_) {
        return;
    }

    std::lock_guard<std::mutex> lock(mutex_);
    if (!logger_) {
        return;
    }

    try {
        logger_->flush();
    } catch (const std::exception& e) {
        reportError("flush", e.what());
    } catch (...) {
        reportError("flush", "Unknown exception occurred while flushing");
    }
}

int LoggerManager::getLogLevel() const
{
    if (!initialized_) {
        return 2;   // Default to INFO
    }

    std::lock_guard<std::mutex> lock(mutex_);
    if (!logger_) {
        return 2;
    }

    try {
        return convertToInt(logger_->level());
    } catch (const std::exception& e) {
        reportError("getLogLevel", e.what());
        return 2;   // Default to INFO on error
    } catch (...) {
        reportError("getLogLevel", "Unknown exception occurred while getting log level");
        return 2;   // Default to INFO on error
    }
}

void LoggerManager::setLogLevel(int level)
{
    if (!initialized_) {
        return;
    }

    std::lock_guard<std::mutex> lock(mutex_);
    if (!logger_) {
        return;
    }

    try {
        spdlog::level::level_enum spdlog_level = convertLogLevel(level);
        logger_->set_level(spdlog_level);
    } catch (const std::exception& e) {
        reportError("setLogLevel", e.what());
    } catch (...) {
        reportError("setLogLevel", "Unknown exception occurred while setting log level");
    }
}

void LoggerManager::setErrorCallback(ErrorCallback callback)
{
    std::lock_guard<std::mutex> lock(mutex_);
    error_callback_ = std::move(callback);
}

bool LoggerManager::isInitialized() const
{
    std::lock_guard<std::mutex> lock(mutex_);
    return initialized_;
}

void LoggerManager::terminate()
{
    std::lock_guard<std::mutex> lock(mutex_);

    // flush before terminating
    if (logger_ && !async_mode_) {
        try {
            logger_->flush();
        } catch (const std::exception& e) {
            reportError("terminate::flush", e.what());
        } catch (...) {
            reportError("terminate::flush", "Unknown exception during flush");
        }
    } else if (async_logger_) {
        try {
            async_logger_->flush();
        } catch (const std::exception& e) {
            reportError("terminate::flush_async", e.what());
        } catch (...) {
            reportError("terminate::flush_async", "Unknown exception during async flush");
        }
    }

    // unregister logger from spdlog registry
    if (logger_) {
        try {
            spdlog::drop("mlogger");
        } catch (const std::exception& e) {
            reportError("terminate::drop", e.what());
        } catch (...) {
            reportError("terminate::drop", "Unknown exception during logger drop");
        }
    }

    // reset shared ptrs
    if (async_logger_) async_logger_.reset();
    if (logger_) logger_.reset();

    initialized_ = false;
    async_mode_  = false;
}

LoggerManager::~LoggerManager() noexcept
{
    terminate();
}

void LoggerManager::reportError(const char* function_name, const char* error_message) const
{
    if (error_callback_) {
        try {
            error_callback_(error_message, function_name);
        } catch (...) {
            // NOTE: if callback itself throws, fall back to stderr
            std::cerr << "[MLogger Error in " << function_name << "] " << error_message
                      << std::endl;
        }
    } else {
        std::cerr << "[MLogger Error in " << function_name << "] " << error_message << std::endl;
    }
}

spdlog::level::level_enum LoggerManager::convertLogLevel(int level)
{
    switch (level) {
    case 0: return spdlog::level::trace;
    case 1: return spdlog::level::debug;
    case 2: return spdlog::level::info;
    case 3: return spdlog::level::warn;
    case 4: return spdlog::level::err;
    case 5: return spdlog::level::critical;
    default: throw std::invalid_argument("Invalid log level int val: " + std::to_string(level));
    }
}

int LoggerManager::convertToInt(spdlog::level::level_enum level)
{
    switch (level) {
    case spdlog::level::trace: return 0;
    case spdlog::level::debug: return 1;
    case spdlog::level::info: return 2;
    case spdlog::level::warn: return 3;
    case spdlog::level::err: return 4;
    case spdlog::level::critical: return 5;
    default:
        throw std::invalid_argument("Invalid log level enum: " +
                                    std::to_string(static_cast<int>(level)));
    }
}

}   // namespace mlogger
