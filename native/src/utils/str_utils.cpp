#include "str_utils.h"

namespace mlogger
{

std::string formatExceptionMessage(const char* exception_type, const char* message,
                                   const char* stack_trace)
{
    std::string result = "[EXCEPTION] ";

    if (exception_type) {
        result += exception_type;
        result += ": ";
    }

    if (message) {
        result += message;
    }

    if (stack_trace) {
        result += "\n";
        result += stack_trace;
    }

    return result;
}

std::string safeString(const char* str)
{
    return str ? std::string(str) : std::string();
}

}   // namespace mlogger
