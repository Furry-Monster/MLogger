#include "../src/bridge/bridge.h"
#include <cassert>
#include <cstdio>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <limits>

bool fileExists(const char* path)
{
    return std::filesystem::exists(path);
}

size_t getFileSize(const char* path)
{
    if (!fileExists(path)) {
        return 0;
    }
    return std::filesystem::file_size(path);
}

void test_boundary_conditions()
{
    std::cout << "[TEST] Testing boundary conditions...\n";

    // Test 1: Extremely large file size
    const char* log_path        = "test_logs/test_large_size.log";
    size_t      very_large_size = 10ULL * 1024 * 1024 * 1024;   // 10GB
    int         result          = init(log_path, very_large_size, 5, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] Initialization with very large file size succeeds\n";
    terminate();

    // Test 2: Very small file size (1 byte)
    log_path = "test_logs/test_tiny_size.log";
    result   = init(log_path, 1, 1, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    logMessage(LOG_INFO, "Test message");
    flush();
    std::cout << "  [OK] Initialization with tiny file size succeeds\n";
    terminate();

    // Test 3: Maximum number of files
    log_path = "test_logs/test_max_files.log";
    result   = init(log_path, 1024, 100, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] Initialization with maximum files succeeds\n";
    terminate();

    // Test 4: Single file (no rotation)
    log_path = "test_logs/test_single_file.log";
    result   = init(log_path, 0, 1, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    logMessage(LOG_INFO, "Single file test");
    flush();
    assert(fileExists(log_path));
    std::cout << "  [OK] Single file mode works\n";
    terminate();

    // Test 5: Extreme log level values
    log_path = "test_logs/test_extreme_levels.log";
    result   = init(log_path, 1024 * 1024, 3, 0, 1, LOG_TRACE);
    assert(result == 1);
    setLogLevel(LOG_CRITICAL);
    assert(getLogLevel() == LOG_CRITICAL);
    setLogLevel(LOG_TRACE);
    assert(getLogLevel() == LOG_TRACE);
    std::cout << "  [OK] Extreme log level values handled correctly\n";
    terminate();

    // Test 6: Large thread pool size
    log_path = "test_logs/test_large_threadpool.log";
    result   = init(log_path, 1024 * 1024, 3, 1, 32, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] Large thread pool size accepted\n";
    terminate();

    // Test 7: Zero max_file_size (unlimited)
    log_path = "test_logs/test_unlimited_size.log";
    result   = init(log_path, 0, 3, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    logMessage(LOG_INFO, "Unlimited size test");
    flush();
    std::cout << "  [OK] Zero max_file_size (unlimited) works\n";
    terminate();

    std::cout << "[PASS] Boundary conditions tests passed\n\n";
}

int main()
{
    std::cout << "========================================\n";
    std::cout << "MLogger Boundary Conditions Test Suite\n";
    std::cout << "========================================\n\n";

    std::filesystem::create_directories("test_logs");

    try {
        test_boundary_conditions();

        std::cout << "========================================\n";
        std::cout << "All boundary tests passed! [OK]\n";
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
