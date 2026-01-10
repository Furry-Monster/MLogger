#ifndef BRIDGE_H
#define BRIDGE_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif   // __cplusplus extern begin

#if defined(_WIN32) || defined(_WIN64)
// Windows
#    ifdef BUILDING_DLL
#        define EXPORT_API __declspec(dllexport)
#    else
#        define EXPORT_API __declspec(dllimport)
#    endif
#elif defined(__APPLE__) && defined(__MACH__)
// macOS / iOS
#    ifdef BUILDING_DLL
#        define EXPORT_API __attribute__((visibility("default")))
#    else
#        define EXPORT_API __attribute__((visibility("default")))
#    endif
#elif defined(__linux__) || defined(__unix__)
// Linux/Unix
#    define EXPORT_API __attribute__((visibility("default")))
// Android (ARM/ARM64/X86), WebGL (Emscripten), etc.
// currently it's just a placeholder.
#elif defined(__ANDROID__)
#    define EXPORT_API __attribute__((visibility("default")))
#elif defined(EMSCRIPTEN)
#    define EXPORT_API __attribute__((visibility("default")))
#else
#    error "Unsupported platform: EXPORT_API macro is not defined for this platform."
#endif

typedef enum {
    LOG_TRACE    = 0,
    LOG_DEBUG    = 1,
    LOG_INFO     = 2,
    LOG_WARN     = 3,
    LOG_ERROR    = 4,
    LOG_CRITICAL = 5
} LogLevel;

EXPORT_API int init(const char* log_path, size_t max_file_size, int max_files, int async_mode,
                    int thread_pool_size, int min_log_level);

EXPORT_API int initDefault(const char* log_path);

EXPORT_API void logMessage(int log_level, const char* message);

EXPORT_API void logException(const char* exception_type, const char* message,
                             const char* stack_trace);

EXPORT_API void flush();

EXPORT_API void setLogLevel(int log_level);

EXPORT_API int getLogLevel();

EXPORT_API int isInit();

EXPORT_API void terminate();


#ifdef __cplusplus
}
#endif   // __cplusplus extern end

#endif   // !BRIDGE_H
