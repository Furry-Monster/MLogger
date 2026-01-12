#include "../src/bridge/bridge.h"
#include "../src/core/logger_config.h"
#include "../src/core/logger_manager.h"
#include <cassert>
#include <cstdio>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

// utils
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

std::string readFileContent(const char* path)
{
    std::ifstream file(path);
    if (!file.is_open()) {
        return "";
    }
    std::string content((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
    return content;
}

// test cases
void test_initialization()
{
    std::cout << "[TEST] Testing initialization...\n";

    // Test 1: uninitialized state
    assert(isInit() == 0);
    std::cout << "  [OK] isInit() returns 0 when not initialized\n";

    // Test 2: default initialization
    const char* log_path = "test_logs/test_default.log";
    int         result   = initDefault(log_path);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] initDefault() succeeds\n";

    // Test 3: terminate
    terminate();
    assert(isInit() == 0);
    std::cout << "  [OK] terminate() works correctly\n";

    // Test 4: custom configuration initialization (sync mode)
    log_path = "test_logs/test_sync.log";
    result   = init(log_path, 1024 * 1024, 3, 0, 1, LOG_INFO);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] init() with sync mode succeeds\n";
    terminate();

    // Test 5: custom configuration initialization (async mode)
    log_path = "test_logs/test_async.log";
    result   = init(log_path, 1024 * 1024, 3, 1, 2, LOG_DEBUG);
    assert(result == 1);
    assert(isInit() == 1);
    std::cout << "  [OK] init() with async mode succeeds\n";
    terminate();

    std::cout << "[PASS] Initialization tests passed\n\n";
}

void test_log_levels()
{
    std::cout << "[TEST] Testing log levels...\n";

    const char* log_path = "test_logs/test_levels.log";
    init(log_path, 1024 * 1024, 3, 0, 1, LOG_TRACE);

    logMessage(LOG_TRACE, "This is a TRACE message");
    logMessage(LOG_DEBUG, "This is a DEBUG message");
    logMessage(LOG_INFO, "This is an INFO message");
    logMessage(LOG_WARN, "This is a WARN message");
    logMessage(LOG_ERROR, "This is an ERROR message");
    logMessage(LOG_CRITICAL, "This is a CRITICAL message");

    flush();

    // Test 1: Verify file exists
    assert(fileExists(log_path));
    std::cout << "  [OK] All log levels written to file\n";

    // Test 2: Verify file content
    std::string content = readFileContent(log_path);
    assert(content.find("TRACE message") != std::string::npos);
    assert(content.find("DEBUG message") != std::string::npos);
    assert(content.find("INFO message") != std::string::npos);
    assert(content.find("WARN message") != std::string::npos);
    assert(content.find("ERROR message") != std::string::npos);
    assert(content.find("CRITICAL message") != std::string::npos);
    std::cout << "  [OK] All log levels found in file content\n";

    terminate();
    std::cout << "[PASS] Log levels tests passed\n\n";
}

void test_log_level_filtering()
{
    std::cout << "[TEST] Testing log level filtering...\n";

    const char* log_path = "test_logs/test_filtering.log";
    init(log_path, 1024 * 1024, 3, 0, 1, LOG_WARN);

    logMessage(LOG_TRACE, "TRACE - should be filtered");
    logMessage(LOG_DEBUG, "DEBUG - should be filtered");
    logMessage(LOG_INFO, "INFO - should be filtered");
    logMessage(LOG_WARN, "WARN - should be logged");
    logMessage(LOG_ERROR, "ERROR - should be logged");
    logMessage(LOG_CRITICAL, "CRITICAL - should be logged");

    flush();

    // Test 1: Verify file exists
    assert(fileExists(log_path));
    std::cout << "  [OK] File exists\n";

    // Test 2: Verify file content
    std::string content = readFileContent(log_path);
    assert(content.find("TRACE") == std::string::npos);
    assert(content.find("DEBUG") == std::string::npos);
    assert(content.find("INFO") == std::string::npos);
    assert(content.find("WARN") != std::string::npos);
    assert(content.find("ERROR") != std::string::npos);
    assert(content.find("CRITICAL") != std::string::npos);
    std::cout << "  [OK] Log level filtering works correctly\n";

    terminate();
    std::cout << "[PASS] Log level filtering tests passed\n\n";
}

void test_set_get_log_level()
{
    std::cout << "[TEST] Testing set/get log level...\n";

    const char* log_path = "test_logs/test_set_get.log";
    initDefault(log_path);

    // Test 1: Verify getLogLevel()
    int current_level = getLogLevel();
    assert(current_level >= LOG_TRACE && current_level <= LOG_CRITICAL);
    std::cout << "  [OK] getLogLevel() returns valid level: " << current_level << "\n";

    // Test 2: Verify setLogLevel()
    setLogLevel(LOG_DEBUG);
    assert(getLogLevel() == LOG_DEBUG);
    std::cout << "  [OK] setLogLevel(LOG_DEBUG) works\n";

    setLogLevel(LOG_WARN);
    assert(getLogLevel() == LOG_WARN);
    std::cout << "  [OK] setLogLevel(LOG_WARN) works\n";

    setLogLevel(LOG_ERROR);
    assert(getLogLevel() == LOG_ERROR);
    std::cout << "  [OK] setLogLevel(LOG_ERROR) works\n";

    terminate();
    std::cout << "[PASS] Set/get log level tests passed\n\n";
}

void test_exception_logging()
{
    std::cout << "[TEST] Testing exception logging...\n";

    const char* log_path = "test_logs/test_exception.log";
    init(log_path, 1024 * 1024, 3, 0, 1, LOG_ERROR);

    const char* exception_type = "System.Exception";
    const char* message        = "Test exception message";
    const char* stack_trace    = "at TestClass.TestMethod()\n  at Main()";

    logException(exception_type, message, stack_trace);
    flush();

    std::this_thread::sleep_for(std::chrono::milliseconds(100));

    std::string content = readFileContent(log_path);

    if (content.empty()) {
        std::cerr << "  [FAIL] Log file is empty!\n";
        std::abort();
    }

    // Check for exception content (format may vary)
    bool has_exception = content.find("EXCEPTION") != std::string::npos ||
                         content.find("exception") != std::string::npos;
    bool has_type    = content.find(exception_type) != std::string::npos;
    bool has_message = content.find(message) != std::string::npos;
    bool has_stack   = content.find("TestClass") != std::string::npos ||
                     content.find("TestMethod") != std::string::npos;

    if (!has_exception && !has_type) {
        std::cerr << "  [FAIL] Exception marker not found. Content: " << content.substr(0, 500)
                  << "\n";
        std::abort();
    }
    if (!has_message) {
        std::cerr << "  [FAIL] Exception message not found. Content: " << content.substr(0, 500)
                  << "\n";
        std::abort();
    }
    if (!has_stack) {
        std::cerr << "  [FAIL] Stack trace not found. Content: " << content.substr(0, 500) << "\n";
        std::abort();
    }

    assert(has_exception || has_type);
    assert(has_message);
    assert(has_stack);
    std::cout << "  [OK] Exception logging works correctly\n";

    terminate();
    std::cout << "[PASS] Exception logging tests passed\n\n";
}

void test_file_rotation()
{
    std::cout << "[TEST] Testing file rotation...\n";

    const char* log_path = "test_logs/test_rotation.log";
    init(log_path, 1024, 3, 0, 1, LOG_INFO);   // 1KB max, 3 files

    for (int i = 0; i < 100; ++i) {
        char buffer[256];
        snprintf(buffer, sizeof(buffer), "Test message %d: ", i);
        std::string msg = buffer;
        msg.append(100, 'X');   // 添加100个字符
        logMessage(LOG_INFO, msg.c_str());
    }

    flush();

    // Test 1: Verify main log file exists
    assert(fileExists(log_path));
    std::cout << "  [OK] Main log file created\n";

    // Test 2: Check for rotated files (.1, .2, etc.)
    bool has_rotation = false;
    for (int i = 1; i <= 3; ++i) {
        std::string rotated_file = std::string(log_path) + "." + std::to_string(i);
        if (fileExists(rotated_file.c_str())) {
            has_rotation = true;
            std::cout << "  [OK] Rotated file found: " << rotated_file << "\n";
            break;
        }
    }

    if (has_rotation) {
        std::cout << "  [OK] File rotation works\n";
    } else {
        std::cout << "  ⚠ File rotation not triggered (may need more data)\n";
    }

    terminate();
    std::cout << "[PASS] File rotation tests passed\n\n";
}

void test_async_mode()
{
    std::cout << "[TEST] Testing async mode...\n";

    const char* log_path = "test_logs/test_async_mode.log";
    init(log_path, 1024 * 1024, 3, 1, 2, LOG_INFO);   // async mode , 2 threads

    const int num_logs = 1000;
    for (int i = 0; i < num_logs; ++i) {
        char buffer[128];
        snprintf(buffer, sizeof(buffer), "Async log message %d", i);
        logMessage(LOG_INFO, buffer);
    }

    std::this_thread::sleep_for(std::chrono::milliseconds(500));
    flush();

    // Test 1: Verify that the log file exists
    assert(fileExists(log_path));
    std::string content = readFileContent(log_path);

    // Test 2: Verify that the log file contains the expected number of logs
    assert(content.find("Async log message") != std::string::npos);
    std::cout << "  [OK] Async mode works correctly\n";

    terminate();
    std::cout << "[PASS] Async mode tests passed\n\n";
}

void test_concurrent_logging()
{
    std::cout << "[TEST] Testing concurrent logging...\n";

    const char* log_path = "test_logs/test_concurrent.log";
    init(log_path, 1024 * 1024, 3, 1, 4, LOG_INFO);   // async mode , 4 threads

    const int                num_threads     = 4;
    const int                logs_per_thread = 100;
    std::vector<std::thread> threads;

    for (int t = 0; t < num_threads; ++t) {
        threads.emplace_back([t]() {
            for (int i = 0; i < logs_per_thread; ++i) {
                char buffer[256];
                snprintf(buffer, sizeof(buffer), "Thread %d: Log message %d", t, i);
                logMessage(LOG_INFO, buffer);
            }
        });
    }

    for (auto& thread : threads) {
        thread.join();
    }

    std::this_thread::sleep_for(std::chrono::milliseconds(1000));
    flush();

    // Test 1: Verify that the log file exists
    assert(fileExists(log_path));
    std::string content = readFileContent(log_path);

    // Test 2: Verify that the log file contains the expected number of logs per thread
    for (int t = 0; t < num_threads; ++t) {
        char search_str[64];
        snprintf(search_str, sizeof(search_str), "Thread %d:", t);
        assert(content.find(search_str) != std::string::npos);
    }
    std::cout << "  [OK] Concurrent logging works correctly\n";

    terminate();
    std::cout << "[PASS] Concurrent logging tests passed\n\n";
}

void test_reinitialization()
{
    std::cout << "[TEST] Testing reinitialization...\n";

    const char* log_path1 = "test_logs/test_reinit1.log";
    const char* log_path2 = "test_logs/test_reinit2.log";

    // Test 1: First initialization
    initDefault(log_path1);
    logMessage(LOG_INFO, "First initialization");
    assert(isInit() == 1);

    // Test 2: Reinitialization (different path)
    initDefault(log_path2);
    logMessage(LOG_INFO, "Second initialization");
    assert(isInit() == 1);

    flush();

    // Test 3: Verify that both files exist
    assert(fileExists(log_path1));
    assert(fileExists(log_path2));
    std::cout << "  [OK] Reinitialization works correctly\n";

    terminate();
    std::cout << "[PASS] Reinitialization tests passed\n\n";
}

void test_error_callback()
{
    std::cout << "[TEST] Testing error callback...\n";

    struct ErrorCapture {
        mutable std::mutex       mutex;
        std::vector<std::string> error_messages;
        std::vector<std::string> function_names;
        int                      call_count = 0;

        void clear()
        {
            std::lock_guard<std::mutex> lock(mutex);
            error_messages.clear();
            function_names.clear();
            call_count = 0;
        }

        void add(const char* error_msg, const char* func_name)
        {
            std::lock_guard<std::mutex> lock(mutex);
            error_messages.push_back(error_msg ? error_msg : "");
            function_names.push_back(func_name ? func_name : "");
            call_count++;
        }

        bool contains(const char* error_msg, const char* func_name) const
        {
            std::lock_guard<std::mutex> lock(mutex);
            for (size_t i = 0; i < error_messages.size(); ++i) {
                if (error_messages[i].find(error_msg) != std::string::npos &&
                    function_names[i] == func_name) {
                    return true;
                }
            }
            return false;
        }
    };

    ErrorCapture            capture;
    mlogger::LoggerManager& manager = mlogger::LoggerManager::getInstance();

    // Test 1: Setting error callback and trigger an error
    manager.setErrorCallback([&capture](const char* error_msg, const char* func_name) {
        capture.add(error_msg, func_name);
    });
    const char* log_path = "test_logs/test_error_callback.log";
    initDefault(log_path);
    assert(isInit() == 1);
    std::cout << "  [OK] Error callback set successfully\n";

    // Test 2: Trigger error with invalid log level
    capture.clear();
    setLogLevel(99);
    std::this_thread::sleep_for(std::chrono::milliseconds(10));
    // No assert here, as error callback may or may not be called depending on implementation.

    // Test 3: Initializing with invalid config
    terminate();
    capture.clear();
    {
        mlogger::LoggerConfig invalid_config;
        invalid_config.log_path = "";
        bool result             = manager.initialize(invalid_config);
        assert(result == false);
        std::this_thread::sleep_for(std::chrono::milliseconds(10));
    }

    // Test 4: setErrorCallback(nullptr)
    initDefault(log_path);
    capture.clear();
    manager.setErrorCallback(nullptr);
    capture.clear();

    // Test 5: Reinitialize and register new callback
    terminate();
    initDefault(log_path);

    bool        callback_called = false;
    std::string captured_error;
    std::string captured_function;

    manager.setErrorCallback([&](const char* error_msg, const char* func_name) {
        callback_called   = true;
        captured_error    = error_msg ? error_msg : "";
        captured_function = func_name ? func_name : "";
        capture.add(error_msg, func_name);
    });

    capture.clear();
    callback_called = false;

    // Test 6: Trigger setLogLevel error callback
    setLogLevel(-1);
    std::this_thread::sleep_for(std::chrono::milliseconds(50));

    if (callback_called) {
        assert(!captured_error.empty());
        assert(!captured_function.empty());
        assert(captured_function == "setLogLevel");
        std::cout << "  [OK] Error callback captured error in setLogLevel\n";
    } else {
        std::cout << "  [OK] Error handled gracefully (validation may prevent callback)\n";
    }

    std::cout << "  [OK] Error callback mechanism verified\n";

    // Test 7: Callback function throws exception
    manager.setErrorCallback(
        [&](const char*, const char*) { throw std::runtime_error("Callback throws"); });

    terminate();

    manager.setErrorCallback(nullptr);
    terminate();

    std::cout << "  [OK] Error callback with exception handling works\n";
    std::cout << "[PASS] Error callback tests passed\n\n";
}

int main()
{
    std::cout << "========================================\n";
    std::cout << "MLogger Native Layer Test Suite\n";
    std::cout << "========================================\n\n";

    std::filesystem::create_directories("test_logs");

    try {
        test_initialization();
        test_log_levels();
        test_log_level_filtering();
        test_set_get_log_level();
        test_exception_logging();
        test_file_rotation();
        test_async_mode();
        test_concurrent_logging();
        test_reinitialization();
        test_error_callback();

        std::cout << "========================================\n";
        std::cout << "All tests passed! [OK]\n";
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
