#ifndef PATH_UTILS_H
#define PATH_UTILS_H

#include <filesystem>
#include <string>

namespace mlogger
{
namespace utils
{

bool        ensureDirectoryExists(const std::filesystem::path& file_path);
std::string normalizePath(const std::string& path);
bool        isValidPath(const std::string& path);

}   // namespace utils
}   // namespace mlogger

#endif   // PATH_UTILS_H
