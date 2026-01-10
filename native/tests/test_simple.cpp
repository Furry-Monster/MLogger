#include "../src/bridge/bridge.h"
#include <cstdlib>
#include <iostream>

int main()
{
    std::cout << "Testing MLogger initialization...\n";

    // Test 1: Default init
    std::cout << "Test 1: initDefault(\"test_logs/simple.log\")...\n";
    int result = initDefault("test_logs/simple.log");
    std::cout << "  Result: " << result << "\n";
    std::cout << "  isInit(): " << isInit() << "\n";

    if (result == 1 && isInit() == 1) {
        std::cout << "  ✓ Success!\n";
        logMessage(LOG_INFO, "Test message from simple test");
        flush();
        terminate();
    } else {
        std::cout << "  ✗ Failed!\n";
        return 1;
    }

    // Test 2: Custom init sync
    std::cout << "\nTest 2: init() sync mode...\n";
    result = init("test_logs/simple_sync.log", 1024 * 1024, 3, 0, 1, LOG_INFO);
    std::cout << "  Result: " << result << "\n";
    std::cout << "  isInit(): " << isInit() << "\n";

    if (result == 1 && isInit() == 1) {
        std::cout << "  ✓ Success!\n";
        terminate();
    } else {
        std::cout << "  ✗ Failed!\n";
        return 1;
    }

    // Test 3: Custom init async
    std::cout << "\nTest 3: init() async mode...\n";
    result = init("test_logs/simple_async.log", 1024 * 1024, 3, 1, 2, LOG_DEBUG);
    std::cout << "  Result: " << result << "\n";
    std::cout << "  isInit(): " << isInit() << "\n";

    if (result == 1 && isInit() == 1) {
        std::cout << "  ✓ Success!\n";
        terminate();
    } else {
        std::cout << "  ✗ Failed!\n";
        return 1;
    }

    std::cout << "\nAll simple tests passed!\n";
    return 0;
}
