#include "path_utils.h"
#include <filesystem>

namespace mlogger
{
namespace utils
{

bool ensureDirectoryExists(const std::filesystem::path& file_path)
{
    std::filesystem::path dir = file_path.parent_path();
    if (dir.empty() || dir == ".") {
        return true;
    }

    if (std::filesystem::exists(dir)) {
        return std::filesystem::is_directory(dir);
    }

    try {
        return std::filesystem::create_directories(dir);
    } catch (const std::exception&) {
        return false;
    }
}

std::string normalizePath(const std::string& path)
{
    try {
        std::filesystem::path p(path);
        return p.lexically_normal().string();
    } catch (const std::exception&) {
        return path;
    }
}

bool isValidPath(const std::string& path)
{
    if (path.empty()) {
        return false;
    }

    try {
        std::filesystem::path p(path);
        std::filesystem::path parent = p.parent_path();
        if (!parent.empty() && parent != ".") {
            return std::filesystem::exists(parent) || std::filesystem::is_directory(parent);
        }
        return true;
    } catch (const std::exception&) {
        return false;
    }
}

}   // namespace utils
}   // namespace mlogger
