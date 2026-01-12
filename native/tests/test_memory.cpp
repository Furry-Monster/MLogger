#include "../src/bridge/bridge.h"
#include <cassert>
#include <chrono>
#include <cstdio>
#include <filesystem>
#include <iostream>
#include <string>
#include <thread>

void test_memory_operations()
{
    std::cout << "[TEST] Testing memory operations...\n";

    // Test 1: Many initializations and terminations (check for leaks)
    for (int i = 0; i < 100; ++i) {
        char path[128];
        snprintf(path, sizeof(path), "test_logs/test_memory_%d.log", i);
        initDefault(path);
        logMessage(LOG_INFO, "Memory test");
        flush();
        terminate();
    }
    std::cout << "  [OK] Multiple init/terminate cycles completed (100 cycles)\n";

    // Test 2: Long running with many logs
    const char* log_path = "test_logs/test_memory_long.log";
    init(log_path, 1024 * 1024, 3, 1, 4, LOG_INFO);

    for (int i = 0; i < 50000; ++i) {
        char buffer[128];
        snprintf(buffer, sizeof(buffer), "Memory test message %d", i);
        logMessage(LOG_INFO, buffer);
        if (i % 10000 == 0) {
            flush();
        }
    }
    flush();
    terminate();
    std::cout << "  [OK] Long running operation with many logs completed (50000 messages)\n";

    // Test 3: Large message strings
    log_path = "test_logs/test_memory_large_msg.log";
    init(log_path, 10 * 1024 * 1024, 3, 0, 1, LOG_INFO);

    for (int i = 0; i < 100; ++i) {
        std::string large_msg(100000, 'Z');
        logMessage(LOG_INFO, large_msg.c_str());
    }
    flush();
    terminate();
    std::cout << "  [OK] Large message strings handled correctly (100 messages of 100KB each)\n";

    // Test 4: Rapid memory allocation/deallocation
    log_path = "test_logs/test_memory_rapid.log";
    for (int cycle = 0; cycle < 50; ++cycle) {
        init(log_path, 1024 * 1024, 3, 1, 2, LOG_INFO);
        for (int i = 0; i < 1000; ++i) {
            char buffer[256];
            snprintf(buffer, sizeof(buffer), "Rapid cycle %d message %d", cycle, i);
            logMessage(LOG_INFO, buffer);
        }
        flush();
        terminate();
    }
    std::cout << "  [OK] Rapid memory allocation/deallocation completed (50 cycles)\n";

    std::cout << "[PASS] Memory operations tests passed\n";
    std::cout << "  Note: Use valgrind, AddressSanitizer, or similar tools for actual memory leak "
                 "detection\n\n";
}

void test_edge_cases()
{
    std::cout << "[TEST] Testing edge cases...\n";

    // Test 1: Logging with empty message
    const char* log_path = "test_logs/test_edge_empty.log";
    initDefault(log_path);
    logMessage(LOG_INFO, "");
    logMessage(LOG_INFO, nullptr);
    flush();
    std::cout << "  [OK] Empty/null messages handled gracefully\n";
    terminate();

    // Test 2: Exception logging with null/empty parameters
    log_path = "test_logs/test_edge_exception.log";
    initDefault(log_path);
    logException(nullptr, nullptr, nullptr);
    logException("", "", "");
    logException("Exception", nullptr, nullptr);
    flush();
    std::cout << "  [OK] Exception logging with null/empty parameters handled gracefully\n";
    terminate();

    // Test 3: Rapid log level changes
    log_path = "test_logs/test_edge_rapid_level.log";
    initDefault(log_path);
    for (int i = 0; i < 100; ++i) {
        setLogLevel(static_cast<LogLevel>(i % 6));
        logMessage(LOG_INFO, "Rapid level change");
    }
    flush();
    std::cout << "  [OK] Rapid log level changes handled correctly (100 changes)\n";
    terminate();

    std::cout << "[PASS] Edge cases tests passed\n\n";
}

int main()
{
    std::cout << "========================================\n";
    std::cout << "MLogger Memory & Edge Cases Test Suite\n";
    std::cout << "========================================\n\n";

    std::filesystem::create_directories("test_logs");

    try {
        test_memory_operations();
        test_edge_cases();

        std::cout << "========================================\n";
        std::cout << "All memory and edge case tests passed! [OK]\n";
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
