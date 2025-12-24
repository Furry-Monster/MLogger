// MLogger - Unity Plugin for spdlog
// 头文件占位符

#ifndef MLOGGER_H
#define MLOGGER_H

#ifdef __cplusplus
extern "C" {
#endif

// 初始化日志器
void InitLogger(const char *logPath);

// 记录日志
void NativeLog(int level, const char *message);

// 刷新日志
void FlushLog();

#ifdef __cplusplus
}
#endif

#endif // MLOGGER_H
