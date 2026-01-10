cmake_minimum_required(VERSION 3.20 FATAL_ERROR)

project(MLogger
    VERSION 1.0.0
    LANGUAGES CXX C
)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# build with position independent code for shared library linking
set(SPDLOG_BUILD_PIC ON CACHE BOOL "Build position independent code" FORCE)

add_subdirectory(external/spdlog)

include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/src
    ${CMAKE_CURRENT_SOURCE_DIR}/external/spdlog/include
)

file(GLOB_RECURSE SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/*.h"
)

if(WIN32) # .dll
    add_library(MLogger SHARED ${SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_win"
        PREFIX ""
    )
elseif(ANDROID) # .so
    add_library(MLogger SHARED ${SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_android"
    )
elseif(APPLE) # .dylib or .bundle
    add_library(MLogger SHARED ${SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_macos"
    )
else() # .so
    add_library(MLogger SHARED ${SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_linux"
    )
endif()

target_link_libraries(MLogger PRIVATE spdlog::spdlog)

if(WIN32)
    target_compile_definitions(MLogger PRIVATE
        _CRT_SECURE_NO_WARNINGS
        NOMINMAX
    )
    set_target_properties(MLogger PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
elseif(ANDROID)
    target_compile_definitions(MLogger PRIVATE
        ANDROID
    )
endif()

if(MSVC)
    target_compile_options(MLogger PRIVATE
        /W4
        /permissive-
    )
else()
    target_compile_options(MLogger PRIVATE
        -Wall
        -Wextra
        -Wpedantic
    )
endif()

install(TARGETS MLogger
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

# Test executable
option(BUILD_TESTS "Build test executable" ON)
if(BUILD_TESTS)
    add_executable(test_mlogger tests/test_mlogger.cpp)
    target_link_libraries(test_mlogger PRIVATE MLogger)
    target_include_directories(test_mlogger PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src)

    add_executable(test_simple tests/test_simple.cpp)
    target_link_libraries(test_simple PRIVATE MLogger)
    target_include_directories(test_simple PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src)

    # C interface test
    add_executable(test_c_interface tests/test_c_interface.c)
    target_link_libraries(test_c_interface PRIVATE MLogger)
    target_include_directories(test_c_interface PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src)

    if(MSVC)
        target_compile_options(test_mlogger PRIVATE /W4 /permissive-)
        target_compile_options(test_simple PRIVATE /W4 /permissive-)
        target_compile_options(test_c_interface PRIVATE /W4)
    else()
        target_compile_options(test_mlogger PRIVATE -Wall -Wextra -Wpedantic)
        target_compile_options(test_simple PRIVATE -Wall -Wextra -Wpedantic)
        target_compile_options(test_c_interface PRIVATE -Wall -Wextra -Wpedantic)
    endif()
endif()
