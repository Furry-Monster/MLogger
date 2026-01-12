#include <stdio.h>
#include <stdlib.h>

#ifdef _WIN32
#    include <windows.h>
#    define sleep_ms(ms) Sleep(ms)
#else
#    include <unistd.h>
#    define sleep_ms(ms) usleep((ms) * 1000)
#endif

#include "../src/bridge/bridge.h"

// utils
int file_exists(const char* path)
{
#ifdef _WIN32
    return GetFileAttributesA(path) != INVALID_FILE_ATTRIBUTES;
#else
    return access(path, F_OK) == 0;
#endif
}

long get_file_size(const char* path)
{
    FILE* file = fopen(path, "r");
    if (!file) {
        return 0;
    }
    fseek(file, 0, SEEK_END);
    long size = ftell(file);
    fclose(file);
    return size;
}

// Test 1: Basic initialization
int test_initialization(void)
{
    printf("[TEST] Testing initialization...\n");

    // Test uninitialized state
    if (isInit() != 0) {
        printf("  [FAIL] isInit() should return 0 when not initialized\n");
        return 0;
    }
    printf("  [OK] isInit() returns 0 when not initialized\n");

    // Test default initialization
    const char* log_path = "test_logs/c_test_default.log";
    int         result   = initDefault(log_path);
    if (result != 1) {
        printf("  [FAIL] initDefault() failed\n");
        return 0;
    }
    if (isInit() != 1) {
        printf("  [FAIL] isInit() should return 1 after initialization\n");
        return 0;
    }
    printf("  [OK] initDefault() succeeds\n");

    // Cleanup
    terminate();
    if (isInit() != 0) {
        printf("  [FAIL] isInit() should return 0 after terminate()\n");
        return 0;
    }
    printf("  [OK] terminate() works correctly\n");

    // Test custom initialization (sync mode)
    log_path = "test_logs/c_test_sync.log";
    result   = init(log_path, 1024 * 1024, 3, 0, 1, LOG_INFO);
    if (result != 1) {
        printf("  [FAIL] init() with sync mode failed\n");
        return 0;
    }
    printf("  [OK] init() with sync mode succeeds\n");
    terminate();

    // Test custom initialization (async mode)
    log_path = "test_logs/c_test_async.log";
    result   = init(log_path, 1024 * 1024, 3, 1, 2, LOG_DEBUG);
    if (result != 1) {
        printf("  [FAIL] init() with async mode failed\n");
        return 0;
    }
    printf("  [OK] init() with async mode succeeds\n");
    terminate();

    printf("[PASS] Initialization tests passed\n\n");
    return 1;
}

// Test 2: Log levels
int test_log_levels(void)
{
    printf("[TEST] Testing log levels...\n");

    const char* log_path = "test_logs/c_test_levels.log";
    if (init(log_path, 1024 * 1024, 3, 0, 1, LOG_TRACE) != 1) {
        printf("  [FAIL] Failed to initialize logger\n");
        return 0;
    }

    // Test all log levels
    logMessage(LOG_TRACE, "This is a TRACE message from C");
    logMessage(LOG_DEBUG, "This is a DEBUG message from C");
    logMessage(LOG_INFO, "This is an INFO message from C");
    logMessage(LOG_WARN, "This is a WARN message from C");
    logMessage(LOG_ERROR, "This is an ERROR message from C");
    logMessage(LOG_CRITICAL, "This is a CRITICAL message from C");

    flush();

    // Verify file was created
    if (!file_exists(log_path)) {
        printf("  [FAIL] Log file was not created\n");
        terminate();
        return 0;
    }

    long file_size = get_file_size(log_path);
    if (file_size == 0) {
        printf("  [FAIL] Log file is empty\n");
        terminate();
        return 0;
    }

    printf("  [OK] All log levels written to file (size: %ld bytes)\n", file_size);
    terminate();

    printf("[PASS] Log levels tests passed\n\n");
    return 1;
}

// Test 3: Log level filtering
int test_log_level_filtering(void)
{
    printf("[TEST] Testing log level filtering...\n");

    const char* log_path = "test_logs/c_test_filtering.log";
    if (init(log_path, 1024 * 1024, 3, 0, 1, LOG_WARN) != 1) {
        printf("  [FAIL] Failed to initialize logger\n");
        return 0;
    }

    // Write logs at different levels
    logMessage(LOG_TRACE, "TRACE - should be filtered");
    logMessage(LOG_DEBUG, "DEBUG - should be filtered");
    logMessage(LOG_INFO, "INFO - should be filtered");
    logMessage(LOG_WARN, "WARN - should be logged");
    logMessage(LOG_ERROR, "ERROR - should be logged");
    logMessage(LOG_CRITICAL, "CRITICAL - should be logged");

    flush();

    long file_size = get_file_size(log_path);
    printf("  [OK] Log level filtering applied (file size: %ld bytes)\n", file_size);
    terminate();

    printf("[PASS] Log level filtering tests passed\n\n");
    return 1;
}

// Test 4: Set/get log level
int test_set_get_log_level(void)
{
    printf("[TEST] Testing set/get log level...\n");

    const char* log_path = "test_logs/c_test_set_get.log";
    if (initDefault(log_path) != 1) {
        printf("  [FAIL] Failed to initialize logger\n");
        return 0;
    }

    // Get current log level
    int current_level = getLogLevel();
    if (current_level < LOG_TRACE || current_level > LOG_CRITICAL) {
        printf("  [FAIL] getLogLevel() returned invalid level: %d\n", current_level);
        terminate();
        return 0;
    }
    printf("  [OK] getLogLevel() returns valid level: %d\n", current_level);

    // Set different log levels
    setLogLevel(LOG_DEBUG);
    if (getLogLevel() != LOG_DEBUG) {
        printf("  [FAIL] setLogLevel(LOG_DEBUG) failed\n");
        terminate();
        return 0;
    }
    printf("  [OK] setLogLevel(LOG_DEBUG) works\n");

    setLogLevel(LOG_WARN);
    if (getLogLevel() != LOG_WARN) {
        printf("  [FAIL] setLogLevel(LOG_WARN) failed\n");
        terminate();
        return 0;
    }
    printf("  [OK] setLogLevel(LOG_WARN) works\n");

    setLogLevel(LOG_ERROR);
    if (getLogLevel() != LOG_ERROR) {
        printf("  [FAIL] setLogLevel(LOG_ERROR) failed\n");
        terminate();
        return 0;
    }
    printf("  [OK] setLogLevel(LOG_ERROR) works\n");

    terminate();
    printf("[PASS] Set/get log level tests passed\n\n");
    return 1;
}

// Test 5: Exception logging
int test_exception_logging(void)
{
    printf("[TEST] Testing exception logging...\n");

    const char* log_path = "test_logs/c_test_exception.log";
    if (init(log_path, 1024 * 1024, 3, 0, 1, LOG_ERROR) != 1) {
        printf("  [FAIL] Failed to initialize logger\n");
        return 0;
    }

    const char* exception_type = "System.Exception";
    const char* message        = "Test exception message from C";
    const char* stack_trace    = "at TestClass.TestMethod()\n  at Main()";

    logException(exception_type, message, stack_trace);
    flush();

    // Wait a bit for file system to sync
    sleep_ms(100);   // 100ms

    if (!file_exists(log_path)) {
        printf("  [FAIL] Exception log file was not created\n");
        terminate();
        return 0;
    }

    long file_size = get_file_size(log_path);
    if (file_size == 0) {
        printf("  [FAIL] Exception log file is empty\n");
        terminate();
        return 0;
    }

    printf("  [OK] Exception logging works (file size: %ld bytes)\n", file_size);
    terminate();

    printf("[PASS] Exception logging tests passed\n\n");
    return 1;
}

// Test 6: Flush operation
int test_flush(void)
{
    printf("[TEST] Testing flush operation...\n");

    const char* log_path = "test_logs/c_test_flush.log";
    if (initDefault(log_path) != 1) {
        printf("  [FAIL] Failed to initialize logger\n");
        return 0;
    }

    // Write some logs
    for (int i = 0; i < 10; ++i) {
        char buffer[128];
        snprintf(buffer, sizeof(buffer), "Flush test message %d", i);
        logMessage(LOG_INFO, buffer);
    }

    // Flush
    flush();

    // Wait a bit for file system to sync
    sleep_ms(100);   // 100ms

    long file_size = get_file_size(log_path);
    if (file_size == 0) {
        printf("  [FAIL] Log file is empty after flush\n");
        terminate();
        return 0;
    }

    printf("  [OK] Flush operation works (file size: %ld bytes)\n", file_size);
    terminate();

    printf("[PASS] Flush tests passed\n\n");
    return 1;
}

// Test 7: Reinitialization
int test_reinitialization(void)
{
    printf("[TEST] Testing reinitialization...\n");

    const char* log_path1 = "test_logs/c_test_reinit1.log";
    const char* log_path2 = "test_logs/c_test_reinit2.log";

    // First initialization
    if (initDefault(log_path1) != 1) {
        printf("  [FAIL] First initialization failed\n");
        return 0;
    }
    logMessage(LOG_INFO, "First initialization");
    if (isInit() != 1) {
        printf("  [FAIL] Logger should be initialized\n");
        terminate();
        return 0;
    }

    // Reinitialize with different path
    if (initDefault(log_path2) != 1) {
        printf("  [FAIL] Reinitialization failed\n");
        terminate();
        return 0;
    }
    logMessage(LOG_INFO, "Second initialization");
    if (isInit() != 1) {
        printf("  [FAIL] Logger should be initialized after reinit\n");
        terminate();
        return 0;
    }

    flush();

    // Verify both files exist
    if (!file_exists(log_path1)) {
        printf("  [FAIL] First log file was not created\n");
        terminate();
        return 0;
    }
    if (!file_exists(log_path2)) {
        printf("  [FAIL] Second log file was not created\n");
        terminate();
        return 0;
    }

    printf("  [OK] Reinitialization works correctly\n");
    terminate();

    printf("[PASS] Reinitialization tests passed\n\n");
    return 1;
}

int main(void)
{
    printf("========================================\n");
    printf("MLogger C Interface Test Suite\n");
    printf("========================================\n\n");

    // Create test logs directory
#ifdef _WIN32
    system("if not exist test_logs mkdir test_logs");
#else
    system("mkdir -p test_logs");
#endif

    int all_passed = 1;

    // Run all tests
    if (!test_initialization()) all_passed = 0;
    if (!test_log_levels()) all_passed = 0;
    if (!test_log_level_filtering()) all_passed = 0;
    if (!test_set_get_log_level()) all_passed = 0;
    if (!test_exception_logging()) all_passed = 0;
    if (!test_flush()) all_passed = 0;
    if (!test_reinitialization()) all_passed = 0;

    // Ensure all async operations complete
    flush();
    sleep_ms(200);   // Wait for async operations

    printf("========================================\n");
    if (all_passed) {
        printf("All tests passed! [OK]\n");
        printf("========================================\n");
        fflush(stdout);
        return 0;
    } else {
        printf("Some tests failed! [FAIL]\n");
        printf("========================================\n");
        fflush(stdout);
        return 1;
    }
}
