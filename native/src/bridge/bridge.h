#ifndef BRIDGE_H
#define BRIDGE_H

#ifdef __cplusplus
extern "C" {
#endif   // __cplusplus extern begin

#ifdef _WIN32
#    ifdef BUILDING_DLL
#        define EXPORT_API __declspec(dllexport)
#    else
#        define EXPORT_API __declspec(dllimport)
#    endif
#elif defined(__GNUC__) || defined(__clang__)
#    define EXPORT_API __attribute__((visibility("default")))
#else
#    define EXPORT_API
#endif

typedef enum {
    LOG_TRACE    = 0,
    LOG_DEBUG    = 1,
    LOG_INFO     = 2,
    LOG_WARN     = 3,
    LOG_ERROR    = 4,
    LOG_CRITICAL = 5
} LogLevel;

EXPORT_API int initLogger(const char*);

#ifdef __cplusplus
}
#endif   // __cplusplus extern end

#endif   // !BRIDGE_H