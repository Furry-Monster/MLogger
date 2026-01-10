#ifndef LOGGER_CONFIG_H
#define LOGGER_CONFIG_H

#include <cstddef>
#include <string>

namespace mlogger
{

struct LoggerConfig final {
    std::string log_path;
    size_t      max_file_size    = 10 * 1024 * 1024;   // 10MB default
    int         max_files        = 5;
    bool        async_mode       = true;
    int         thread_pool_size = 1;
    int         min_log_level    = 2;   // filter LOG_INFO by default

    LoggerConfig() = default;
    explicit LoggerConfig(const std::string& path)
        : log_path(path)
    {
    }

    bool isValid() const;
};

}   // namespace mlogger

#endif   // LOGGER_CONFIG_H
