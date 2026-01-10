#include "logger_config.h"

namespace mlogger
{

bool LoggerConfig::isValid() const
{
    if (log_path.empty()) return false;
    if (max_file_size == 0) return false;
    if (max_files <= 0) return false;
    if (thread_pool_size <= 0) return false;
    if (min_log_level < 0 || min_log_level > 5) return false;

    return true;
}

}   // namespace mlogger
