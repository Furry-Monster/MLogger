#ifndef STR_UTILS_H
#define STR_UTILS_H

#include <string>

namespace mlogger
{

std::string formatExceptionMessage(const char* exception_type, const char* message,
                                   const char* stack_trace);
std::string safeString(const char* str);

}   // namespace mlogger

#endif   // STR_UTILS_H
