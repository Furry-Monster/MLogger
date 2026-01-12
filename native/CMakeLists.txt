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

set(SPDLOG_BUILD_PIC ON CACHE BOOL "Build position independent code" FORCE)
add_subdirectory(external/spdlog)

set(MLOGGER_SOURCES
    src/core/logger_config.cpp
    src/core/logger_config.h
    src/core/logger_manager.cpp
    src/core/logger_manager.h
    src/bridge/bridge.cpp
    src/bridge/bridge.h
    src/utils/path_utils.cpp
    src/utils/path_utils.h
    src/utils/str_utils.cpp
    src/utils/str_utils.h
)

# Platform-specific bridge files (optional, add if needed in the future)
# if(WIN32)
#     list(APPEND MLOGGER_SOURCES
#         src/bridge/plateform/bridge_win.cpp
#         src/bridge/plateform/bridge_win.h
#     )
# endif()

if(WIN32)
    add_library(MLogger SHARED ${MLOGGER_SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_win"
        PREFIX ""
    )
elseif(ANDROID)
    add_library(MLogger SHARED ${MLOGGER_SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_android"
    )
elseif(IOS)
    add_library(MLogger STATIC ${MLOGGER_SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_ios"
    )
elseif(APPLE)
    add_library(MLogger SHARED ${MLOGGER_SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_macos"
    )
else()
    add_library(MLogger SHARED ${MLOGGER_SOURCES})
    set_target_properties(MLogger PROPERTIES
        OUTPUT_NAME "mlogger_linux"
    )
endif()

target_include_directories(MLogger
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
        $<INSTALL_INTERFACE:include>
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/external/spdlog/include
)

target_link_libraries(MLogger PRIVATE spdlog::spdlog)

if(WIN32)
    target_compile_definitions(MLogger PRIVATE
        _CRT_SECURE_NO_WARNINGS
        NOMINMAX
        BUILDING_DLL
    )
    if(MSVC)
        set_target_properties(MLogger PROPERTIES
            WINDOWS_EXPORT_ALL_SYMBOLS ON
        )
    else()
        target_link_options(MLogger PRIVATE -Wl,--export-all-symbols)
    endif()
elseif(ANDROID)
    target_compile_definitions(MLogger PRIVATE ANDROID)
endif()

if(MSVC)
    target_compile_options(MLogger PRIVATE /W4 /permissive-)
else()
    target_compile_options(MLogger PRIVATE -Wall -Wextra -Wpedantic)
endif()

install(TARGETS MLogger
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

option(BUILD_TESTS "Build test executables" ON)

if(BUILD_TESTS)
    function(add_test_executable test_name test_source)
        add_executable(${test_name} ${test_source})
        target_link_libraries(${test_name} PRIVATE MLogger spdlog::spdlog)
        target_include_directories(${test_name} PRIVATE
            ${CMAKE_CURRENT_SOURCE_DIR}/src
        )

        if(WIN32 AND NOT MSVC)
            set_target_properties(${test_name} PROPERTIES
                RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
            )
        endif()

        if(MSVC)
            if(test_source MATCHES "\\.c$")
                target_compile_options(${test_name} PRIVATE /W4)
            else()
                target_compile_options(${test_name} PRIVATE /W4 /permissive-)
            endif()
        else()
            target_compile_options(${test_name} PRIVATE -Wall -Wextra -Wpedantic)
        endif()
    endfunction()

    add_test_executable(test_mlogger tests/test_mlogger.cpp)
    add_test_executable(test_simple tests/test_simple.cpp)
    add_test_executable(test_c_interface tests/test_c_interface.c)
    add_test_executable(test_boundary tests/test_boundary.cpp)
    add_test_executable(test_error_handling tests/test_error_handling.cpp)
    add_test_executable(test_stress tests/test_stress.cpp)
    add_test_executable(test_memory tests/test_memory.cpp)
endif()
