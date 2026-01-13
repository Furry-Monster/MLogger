#!/usr/bin/env python3

import argparse
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

from platforms import (
    AndroidBuilder,
    IOSBuilder,
    LinuxBuilder,
    MacOSBuilder,
    PlatformBuilder,
    WindowsBuilder,
)

PLATFORM_UNITY_MAP = {
    "linux": "Linux",
    "windows": "Windows",
    "macos": "macOS",
    "android": "Android",
    "ios": "iOS",
}

ARCH_UNITY_MAP = {
    "x86": "x86",
    "x86_64": "x86_64",
    "arm64": "arm64",
    "arm64-v8a": "arm64-v8a",
    "armeabi-v7a": "armeabi-v7a",
}

LIBRARY_NAMES = {
    "linux": "libmlogger_linux.so",
    "windows": "mlogger_win.dll",
    "macos": "libmlogger_macos.dylib",
    "android": "libmlogger_android.so",
    "ios": "libmlogger_ios.a",
}

PROJECT_ROOT = Path(__file__).parent.parent.parent
NATIVE_DIR = PROJECT_ROOT / "native"
BUILD_DIR = NATIVE_DIR / "build"
UNITY_PLUGINS_DIR = PROJECT_ROOT / "unity" / "Assets" / "Plugins" / "MLogger" / "External"

PLATFORM_BUILDERS = {
    "linux": LinuxBuilder,
    "windows": WindowsBuilder,
    "macos": MacOSBuilder,
    "android": AndroidBuilder,
    "ios": IOSBuilder,
}

SEPARATOR = "=" * 60
BASIC_TESTS = ["test_mlogger", "test_simple", "test_c_interface"]
ENHANCED_TESTS = [
    "test_boundary",
    "test_error_handling",
    "test_stress",
    "test_memory",
]


def get_current_platform() -> str:
    platform = sys.platform
    if platform == "win32":
        return "windows"
    elif platform == "darwin":
        return "macos"
    elif platform == "linux":
        return "linux"
    raise ValueError(f"Unsupported platform: {platform}")


def get_current_arch() -> str:
    import platform

    machine = platform.machine().lower()
    if machine in ("x86_64", "amd64"):
        return "x86_64"
    elif machine in ("i386", "i686", "x86"):
        return "x86"
    elif machine in ("arm64", "aarch64"):
        return "arm64"
    raise ValueError(f"Unsupported architecture: {machine}")


def get_builder(platform: str, arch: str) -> "PlatformBuilder":
    builder_class = PLATFORM_BUILDERS.get(platform)
    if not builder_class:
        raise ValueError(f"Unsupported platform: {platform}")
    build_dir = BUILD_DIR / f"{platform}-{arch}"
    return builder_class(platform, arch, build_dir, NATIVE_DIR)


def _remove_file_with_retry(
    file_path: Path, max_retries: int = 3, retry_delay: float = 0.5
) -> bool:
    for attempt in range(max_retries):
        try:
            if file_path.exists():
                file_path.unlink()
            return True
        except (OSError, PermissionError):
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
            else:
                try:
                    file_path.rename(file_path.with_suffix(file_path.suffix + ".old"))
                    return True
                except (OSError, PermissionError):
                    return False
    return False


def _remove_directory_with_retry(
    dir_path: Path, max_retries: int = 3, retry_delay: float = 0.5
) -> bool:
    for attempt in range(max_retries):
        try:
            if dir_path.exists():
                shutil.rmtree(dir_path)
            return True
        except (OSError, PermissionError):
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
            else:
                try:
                    backup_name = dir_path.with_name(dir_path.name + ".old")
                    if backup_name.exists():
                        shutil.rmtree(backup_name)
                    dir_path.rename(backup_name)
                    return True
                except (OSError, PermissionError):
                    return False
    return False


def check_and_clean_cmake_cache(
    build_dir: Path, current_generator: str, verbose: bool = False
) -> bool:
    cmake_cache = build_dir / "CMakeCache.txt"
    if not cmake_cache.exists():
        return False

    cached_generator = None
    try:
        with open(cmake_cache, encoding="utf-8") as f:
            match = re.search(r"CMAKE_GENERATOR:INTERNAL=(.+)", f.read())
            if match:
                cached_generator = match.group(1).strip()
    except (OSError, UnicodeDecodeError, PermissionError) as e:
        if verbose:
            print(f"  [WARN] Could not read CMakeCache.txt: {e}")

    if cached_generator is None or cached_generator != current_generator:
        if verbose:
            if cached_generator:
                print(f"  [INFO] Generator mismatch: {cached_generator} -> {current_generator}")
            print("  [INFO] Cleaning CMake cache...")
        _clean_cmake_cache_files(build_dir, verbose)
        return True
    return False


def _print_section(title: str):
    print(f"\n{SEPARATOR}")
    print(title)
    print(SEPARATOR)


def _clean_cmake_cache_files(build_dir: Path, verbose: bool = False) -> tuple[bool, bool]:
    cmake_cache = build_dir / "CMakeCache.txt"
    cmake_files_dir = build_dir / "CMakeFiles"
    cache_removed = _remove_file_with_retry(cmake_cache, max_retries=3, retry_delay=0.5)
    dir_removed = _remove_directory_with_retry(cmake_files_dir, max_retries=3, retry_delay=0.5)
    if verbose and not (cache_removed and dir_removed):
        print("  [WARN] Some cache files could not be removed")
        print(f"    CMakeCache.txt removed: {cache_removed}")
        print(f"    CMakeFiles directory removed: {dir_removed}")
    return cache_removed, dir_removed


def configure_cmake(
    platform: str,
    arch: str,
    builder: "PlatformBuilder",
    verbose: bool = False,
    clean: bool = False,
    **kwargs,
):
    _print_section(f"[STEP 1/4] CONFIGURE - Configuring CMake for {platform}-{arch}")

    build_dir = builder.build_dir
    build_dir.mkdir(parents=True, exist_ok=True)

    cmake_args = builder.get_cmake_args(**kwargs)
    current_generator = None
    for i, arg in enumerate(cmake_args):
        if arg == "-G" and i + 1 < len(cmake_args):
            current_generator = cmake_args[i + 1]
            break

    if current_generator:
        if clean:
            cmake_cache = build_dir / "CMakeCache.txt"
            cmake_files_dir = build_dir / "CMakeFiles"
            if cmake_cache.exists() or cmake_files_dir.exists():
                if verbose:
                    print("  [INFO] Force cleaning CMake cache...")
                _clean_cmake_cache_files(build_dir, verbose)
        else:
            check_and_clean_cmake_cache(build_dir, current_generator, verbose)

    args = ["-B", str(build_dir), "-S", str(NATIVE_DIR)]
    args.extend(cmake_args)
    args.append("-DCMAKE_BUILD_TYPE=Release")
    args.append("-DBUILD_TESTS=ON")

    if verbose:
        print(f"CMake command: cmake {' '.join(args)}")
        print(f"Working directory: {PROJECT_ROOT}")

    try:
        subprocess.run(
            ["cmake"] + args,
            check=True,
            cwd=PROJECT_ROOT,
            stdout=None if verbose else subprocess.DEVNULL,
            stderr=None if verbose else subprocess.STDOUT,
        )
        print(f"[PASS] [STEP 1/4] CMake configuration completed for {platform}-{arch}")

        compile_commands_src = build_dir / "compile_commands.json"
        compile_commands_dst = NATIVE_DIR / "compile_commands.json"
        if compile_commands_src.exists():
            shutil.copy2(compile_commands_src, compile_commands_dst)
            if verbose:
                print(f"  [INFO] Copied compile_commands.json to {compile_commands_dst}")
    except subprocess.CalledProcessError:
        print(f"[FAIL] [STEP 1/4] CMake configuration failed for {platform}-{arch}")
        if not verbose:
            print("  (Use --verbose to see detailed error messages)")
        raise


def build_project(platform: str, arch: str, builder: "PlatformBuilder", verbose: bool = False):
    _print_section(f"[STEP 2/4] BUILD - Building for {platform}-{arch}")

    build_dir = builder.build_dir
    if not build_dir.exists():
        raise ValueError(f"Build directory does not exist: {build_dir}")

    build_cmd = ["cmake", "--build", "."]
    build_cmd.extend(builder.get_build_args())

    if verbose:
        print(f"Build command: {' '.join(build_cmd)}")
        print(f"Working directory: {build_dir}")

    try:
        subprocess.run(
            build_cmd,
            check=True,
            cwd=build_dir,
            stdout=None if verbose else subprocess.DEVNULL,
            stderr=None if verbose else subprocess.STDOUT,
        )
        print(f"[PASS] [STEP 2/4] Build completed for {platform}-{arch}")
    except subprocess.CalledProcessError:
        print(f"[FAIL] [STEP 2/4] Build failed for {platform}-{arch}")
        if not verbose:
            print("  (Use --verbose to see detailed error messages)")
        raise


def run_tests(
    platform: str,
    arch: str,
    builder: "PlatformBuilder",
    verbose: bool = False,
    test_filter: str = None,
):
    if not builder.can_run_tests():
        _print_section(f"[STEP 3/4] TEST - Skipping tests for {platform} (cannot run executables)")
        return

    title = f"[STEP 3/4] TEST - Running tests for {platform}-{arch}"
    if test_filter:
        title += f"\n  Filter: {test_filter}"
    _print_section(title)

    build_dir = builder.build_dir
    bin_dir = build_dir / "bin"
    test_executables = builder.get_test_executables()
    exe_ext = builder.get_executable_extension()

    if test_filter:
        test_executables = [t for t in test_executables if test_filter.lower() in t.lower()]
        if not test_executables:
            print(f"  [WARN] No tests match filter: {test_filter}")
            return

    test_categories = {
        "Basic Tests": [t for t in test_executables if t in BASIC_TESTS],
        "Enhanced Tests": [t for t in test_executables if t in ENHANCED_TESTS],
    }

    all_passed = True
    total_tests = 0
    passed_tests = 0

    for category, tests in test_categories.items():
        if not tests:
            continue

        print(f"\n  [{category}]")
        for test_name in tests:
            test_path = bin_dir / f"{test_name}{exe_ext}"
            if not test_path.exists():
                print(f"    [WARN] Test executable not found: {test_name}")
                continue

            total_tests += 1
            try:
                print(f"    Running {test_name}...", end=" ", flush=True)
                result = subprocess.run(
                    [str(test_path)],
                    check=True,
                    cwd=bin_dir,
                    capture_output=True,
                    text=True,
                )
                print("[PASS]")
                passed_tests += 1
                if verbose:
                    if result.stdout:
                        for line in result.stdout.splitlines():
                            print(f"      {line}")
                    if result.stderr:
                        for line in result.stderr.splitlines():
                            print(f"      [STDERR] {line}")
            except subprocess.CalledProcessError as e:
                print("[FAIL]")
                all_passed = False
                if e.stdout:
                    for line in e.stdout.splitlines():
                        print(f"      {line}")
                if e.stderr:
                    for line in e.stderr.splitlines():
                        print(f"      [STDERR] {line}")
                if not verbose:
                    print("      Run with --verbose to see full test output")

    print(f"\n  Test Summary: {passed_tests}/{total_tests} passed")

    if not all_passed:
        raise RuntimeError(f"Some tests failed for {platform}-{arch}")

    print(f"[PASS] [STEP 3/4] All tests passed for {platform}-{arch}")


def _find_library_path(
    build_dir: Path, library_name: str, platform: str, builder: "PlatformBuilder"
) -> Path:
    lib_dir = build_dir / "bin" if platform == "windows" else build_dir / "lib"
    source_path = builder.get_library_path(lib_dir, library_name)

    if not source_path.exists():
        alt_paths = [build_dir / "bin" / library_name, build_dir / "lib" / library_name]
        for alt_path in alt_paths:
            if alt_path.exists():
                return alt_path
        raise FileNotFoundError(
            f"Library file not found: {source_path}\n"
            f"  Searched in: {lib_dir}\n"
            f"  Also tried: {[str(p) for p in alt_paths]}"
        )
    return source_path


def copy_library_to_unity(
    platform: str, arch: str, builder: "PlatformBuilder", verbose: bool = False
):
    _print_section("[STEP 4/4] COPY - Copying library to Unity directory")

    build_dir = builder.build_dir
    library_name = LIBRARY_NAMES[platform]
    source_path = _find_library_path(build_dir, library_name, platform, builder)

    unity_platform_dir = PLATFORM_UNITY_MAP[platform]
    unity_arch_dir = ARCH_UNITY_MAP[arch]
    unity_target_dir = UNITY_PLUGINS_DIR / unity_platform_dir / unity_arch_dir
    unity_target_dir.mkdir(parents=True, exist_ok=True)
    target_path = unity_target_dir / library_name

    if verbose:
        print(f"  Copying from: {source_path}")
        print(f"  Copying to: {target_path}")

    # Try to remove existing target file if it exists (may be locked by Unity)
    if target_path.exists():
        if verbose:
            print(f"  Target file exists, attempting to remove it first...")
        if not _remove_file_with_retry(target_path, max_retries=5, retry_delay=1.0):
            error_msg = (
                f"Failed to remove existing file: {target_path}\n"
                f"  This usually means the file is locked by Unity Editor.\n"
                f"  Please close Unity Editor and try again, or manually delete the file."
            )
            print(f"[FAIL] [STEP 4/4] {error_msg}")
            raise PermissionError(error_msg)

    # Try to copy with retry mechanism
    max_retries = 3
    retry_delay = 0.5
    for attempt in range(max_retries):
        try:
            shutil.copy(source_path, target_path)
            print(f"[PASS] [STEP 4/4] Copied {library_name} to {unity_target_dir}")
            return
        except (OSError, PermissionError) as e:
            if attempt < max_retries - 1:
                if verbose:
                    print(f"  Copy attempt {attempt + 1} failed, retrying in {retry_delay}s...")
                time.sleep(retry_delay)
            else:
                error_msg = (
                    f"Failed to copy library after {max_retries} attempts: {e}\n"
                    f"  Source: {source_path}\n"
                    f"  Target: {target_path}\n"
                    f"  This usually means the target file is locked by Unity Editor.\n"
                    f"  Please close Unity Editor and try again."
                )
                print(f"[FAIL] [STEP 4/4] {error_msg}")
                if verbose:
                    import traceback
                    traceback.print_exc()
                raise PermissionError(error_msg) from e
        except Exception as e:
            print(f"[FAIL] [STEP 4/4] Failed to copy library: {e}")
            if verbose:
                import traceback
                traceback.print_exc()
            raise


def main():
    parser = argparse.ArgumentParser(description="MLogger Native Library Build Script")
    parser.add_argument(
        "--platform",
        type=str,
        default=get_current_platform(),
        choices=["linux", "windows", "macos", "android", "ios"],
        help="Target platform",
    )
    parser.add_argument(
        "--arch",
        type=str,
        default=get_current_arch(),
        choices=["x86", "x86_64", "arm64", "arm64-v8a", "armeabi-v7a"],
        help="Target architecture",
    )
    parser.add_argument("--skip-tests", action="store_true", help="Skip running tests")
    parser.add_argument(
        "--test-filter",
        type=str,
        help="Run only tests matching the filter string (case-insensitive)",
    )
    parser.add_argument("--skip-copy", action="store_true", help="Skip copying to Unity")
    parser.add_argument("--generator", type=str, help="CMake generator (Windows only)")
    parser.add_argument(
        "--toolchain",
        type=str,
        help="CMake toolchain file (required for Android, recommended for cross-compilation)",
    )
    parser.add_argument("--android-abi", type=str, help="Android ABI")
    parser.add_argument("--ios-sdk", type=str, help="iOS SDK path or name")
    parser.add_argument("--verbose", "-v", action="store_true", help="Enable verbose output")
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Force clean CMake cache before configuring",
    )

    args = parser.parse_args()

    print(f"\n{SEPARATOR}")
    print("MLogger Native Library Build Script")
    print(SEPARATOR)
    print(f"Platform: {args.platform}")
    print(f"Architecture: {args.arch}")
    if args.verbose:
        print("Verbose mode: ON")
    print(SEPARATOR)

    builder = get_builder(args.platform, args.arch)

    kwargs = {}
    if args.generator:
        kwargs["generator"] = args.generator
    if args.toolchain:
        kwargs["toolchain"] = args.toolchain
    if args.android_abi:
        kwargs["android_abi"] = args.android_abi
    if args.ios_sdk:
        kwargs["ios_sdk"] = args.ios_sdk

    try:
        configure_cmake(args.platform, args.arch, builder, args.verbose, args.clean, **kwargs)
        build_project(args.platform, args.arch, builder, args.verbose)

        if not args.skip_tests:
            run_tests(
                args.platform,
                args.arch,
                builder,
                args.verbose,
                test_filter=args.test_filter,
            )
        else:
            _print_section("[SKIP] Tests skipped")

        if not args.skip_copy:
            copy_library_to_unity(args.platform, args.arch, builder, args.verbose)
        else:
            _print_section("[SKIP] Copy to Unity skipped")

        _print_section("[PASS] BUILD COMPLETED SUCCESSFULLY!")
        print()
    except Exception as e:
        _print_section("[FAIL] Build failed!")
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
