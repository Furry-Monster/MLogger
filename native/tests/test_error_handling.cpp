#include "../src/bridge/bridge.h"
#include <cassert>
#include <cstdio>
#include <filesystem>
#include <iostream>
#include <string>

void test_error_handling()
{
    std::cout << "[TEST] Testing error handling...\n";

    // Test 1: Invalid path (null pointer)
    int result = 0;
    try {
        result = initDefault(nullptr);
        // Should handle gracefully (may return 0 or use default path)
        if (result == 1) {
            terminate();
        }
    } catch (...) {
        // Exception is acceptable for null pointer
    }
    std::cout << "  [OK] Null path handled gracefully\n";

    // Test 2: Empty path
    try {
        result = init("", 1024 * 1024, 3, 0, 1, LOG_INFO);
        // Should handle gracefully
        if (result == 1) {
            terminate();
        }
    } catch (...) {
        // Exception is acceptable for empty path
    }
    std::cout << "  [OK] Empty path handled gracefully\n";

    // Test 3: Invalid log level (out of range)
    const char* log_path = "test_logs/test_invalid_level.log";
    result               = initDefault(log_path);
    assert(result == 1);

    int original_level = getLogLevel();
    setLogLevel(-1);
    // Should not crash, level should remain valid
    int current_level = getLogLevel();
    assert(current_level >= LOG_TRACE && current_level <= LOG_CRITICAL);
    std::cout << "  [OK] Invalid log level handled gracefully\n";

    setLogLevel(99);
    current_level = getLogLevel();
    assert(current_level >= LOG_TRACE && current_level <= LOG_CRITICAL);
    std::cout << "  [OK] Out of range log level handled gracefully\n";
    terminate();

    // Test 4: Zero max_files
    log_path = "test_logs/test_zero_files.log";
    result   = init(log_path, 1024 * 1024, 0, 0, 1, LOG_INFO);
    // Should handle gracefully (may use default or reject)
    if (result == 1) {
        terminate();
    }
    std::cout << "  [OK] Zero max_files handled gracefully\n";

    // Test 5: Zero thread pool size in async mode
    log_path = "test_logs/test_zero_threads.log";
    result   = init(log_path, 1024 * 1024, 3, 1, 0, LOG_INFO);
    // Should handle gracefully
    if (result == 1) {
        terminate();
    }
    std::cout << "  [OK] Zero thread pool size handled gracefully\n";

    // Test 6: Very long path
    try {
        std::string long_path = "test_logs/";
        long_path.append(500, 'a');
        long_path += ".log";
        result = initDefault(long_path.c_str());
        // Should handle gracefully (may fail or truncate)
        if (result == 1) {
            terminate();
        }
    } catch (...) {
        // Exception is acceptable for very long path
    }
    std::cout << "  [OK] Very long path handled gracefully\n";

    // Test 7: Invalid characters in path (platform dependent)
    log_path = "test_logs/test<>invalid.log";
    result   = initDefault(log_path);
    // Should handle gracefully
    if (result == 1) {
        terminate();
    }
    std::cout << "  [OK] Invalid path characters handled gracefully\n";

    // Test 8: Logging before initialization
    terminate();
    assert(isInit() == 0);
    logMessage(LOG_INFO, "Should not crash");
    flush();
    std::cout << "  [OK] Logging before initialization handled gracefully\n";

    // Test 9: Multiple terminate calls
    initDefault("test_logs/test_multiple_terminate.log");
    terminate();
    terminate();
    terminate();
    assert(isInit() == 0);
    std::cout << "  [OK] Multiple terminate calls handled gracefully\n";

    // Test 10: Flush without initialization
    terminate();
    assert(isInit() == 0);
    flush();
    std::cout << "  [OK] Flush without initialization handled gracefully\n";

    // Test 11: Get log level without initialization
    int level = getLogLevel();
    assert(level >= LOG_TRACE && level <= LOG_CRITICAL);
    std::cout << "  [OK] Get log level without initialization returns valid value\n";

    // Test 12: Set log level without initialization
    terminate();
    setLogLevel(LOG_WARN);
    std::cout << "  [OK] Set log level without initialization handled gracefully\n";

    std::cout << "[PASS] Error handling tests passed\n\n";
}

int main()
{
    std::cout << "========================================\n";
    std::cout << "MLogger Error Handling Test Suite\n";
    std::cout << "========================================\n\n";

    std::filesystem::create_directories("test_logs");

    try {
        test_error_handling();

        std::cout << "========================================\n";
        std::cout << "All error handling tests passed! [OK]\n";
        std::cout << "========================================\n";

        return 0;
    } catch (const std::exception& e) {
        std::cerr << "\n[FAIL] Test failed with exception: " << e.what() << "\n";
        terminate();
        return 1;
    } catch (...) {
        std::cerr << "\n[FAIL] Test failed with unknown exception\n";
        terminate();
        return 1;
    }
}
