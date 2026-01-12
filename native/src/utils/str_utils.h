#ifndef STR_UTILS_H
#define STR_UTILS_H

#include <string>

namespace mlogger
{
namespace utils
{

std::string formatExceptionMessage(const char* exception_type, const char* message,
                                   const char* stack_trace);
std::string safeString(const char* str);

}   // namespace utils
}   // namespace mlogger

#endif   // STR_UTILS_H
