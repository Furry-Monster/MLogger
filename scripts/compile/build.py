#!/usr/bin/env python3
"""
MLogger Native Library Build Script
"""

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

from platforms import (
    AndroidBuilder,
    IOSBuilder,
    LinuxBuilder,
    MacOSBuilder,
    PlatformBuilder,
    WindowsBuilder,
)

# Platform and architecture definitions
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

# Project directories
PROJECT_ROOT = Path(__file__).parent.parent.parent
NATIVE_DIR = PROJECT_ROOT / "native"
BUILD_DIR = NATIVE_DIR / "build"
UNITY_PLUGINS_DIR = (
    PROJECT_ROOT / "unity" / "Assets" / "Plugins" / "MLogger" / "External"
)

# Platform builder mapping
PLATFORM_BUILDERS = {
    "linux": LinuxBuilder,
    "windows": WindowsBuilder,
    "macos": MacOSBuilder,
    "android": AndroidBuilder,
    "ios": IOSBuilder,
}


def get_current_platform() -> str:
    """Get current platform"""
    platform = sys.platform
    if platform == "win32":
        return "windows"
    elif platform == "darwin":
        return "macos"
    elif platform == "linux":
        return "linux"
    raise ValueError(f"Unsupported platform: {platform}")


def get_current_arch() -> str:
    """Get current architecture"""
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
    """Get platform-specific builder"""
    builder_class = PLATFORM_BUILDERS.get(platform)
    if not builder_class:
        raise ValueError(f"Unsupported platform: {platform}")

    build_dir = BUILD_DIR / f"{platform}-{arch}"
    return builder_class(platform, arch, build_dir, NATIVE_DIR)


def configure_cmake(
    platform: str,
    arch: str,
    builder: "PlatformBuilder",
    verbose: bool = False,
    **kwargs,
):
    """Configure CMake"""
    print(f"\n{'=' * 60}")
    print(f"[STEP 1/4] CONFIGURE - Configuring CMake for {platform}-{arch}")
    print(f"{'=' * 60}")

    build_dir = builder.build_dir
    build_dir.mkdir(parents=True, exist_ok=True)

    args = ["-B", str(build_dir), "-S", str(NATIVE_DIR)]
    args.extend(builder.get_cmake_args(**kwargs))
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
        print(f"✓ [STEP 1/4] CMake configuration completed for {platform}-{arch}")
        
        # Copy compile_commands.json to source directory for clangd/IDE support
        # This helps IDEs find the compilation database for IntelliSense
        compile_commands_src = build_dir / "compile_commands.json"
        compile_commands_dst = NATIVE_DIR / "compile_commands.json"
        if compile_commands_src.exists():
            shutil.copy2(compile_commands_src, compile_commands_dst)
            if verbose:
                print(f"  Copied compile_commands.json to {compile_commands_dst}")
    except subprocess.CalledProcessError:
        print(f"✗ [STEP 1/4] CMake configuration failed for {platform}-{arch}")
        if not verbose:
            print("  (Use --verbose to see detailed error messages)")
        raise


def build_project(
    platform: str, arch: str, builder: "PlatformBuilder", verbose: bool = False
):
    """Build the project"""
    print(f"\n{'=' * 60}")
    print(f"[STEP 2/4] BUILD - Building for {platform}-{arch}")
    print(f"{'=' * 60}")

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
        print(f"✓ [STEP 2/4] Build completed for {platform}-{arch}")
    except subprocess.CalledProcessError:
        print(f"✗ [STEP 2/4] Build failed for {platform}-{arch}")
        if not verbose:
            print("  (Use --verbose to see detailed error messages)")
        raise


def run_tests(
    platform: str, arch: str, builder: "PlatformBuilder", verbose: bool = False
):
    """Run integration tests"""
    if not builder.can_run_tests():
        print(f"\n{'=' * 60}")
        print(
            f"[STEP 3/4] TEST - Skipping tests for {platform} (cannot run executables)"
        )
        print(f"{'=' * 60}")
        return

    print(f"\n{'=' * 60}")
    print(f"[STEP 3/4] TEST - Running tests for {platform}-{arch}")
    print(f"{'=' * 60}")

    build_dir = builder.build_dir
    bin_dir = build_dir / "bin"

    test_executables = builder.get_test_executables()
    exe_ext = builder.get_executable_extension()

    all_passed = True
    for test_name in test_executables:
        test_path = bin_dir / f"{test_name}{exe_ext}"
        if not test_path.exists():
            print(f"⚠ Test executable not found: {test_path}")
            continue

        try:
            print(f"  Running {test_name}...")
            subprocess.run(
                [str(test_path)],
                check=True,
                cwd=bin_dir,
                stdout=None if verbose else subprocess.DEVNULL,
                stderr=None if verbose else subprocess.STDOUT,
            )
            print(f"  ✓ {test_name} passed")
        except subprocess.CalledProcessError:
            print(f"  ✗ {test_name} failed")
            all_passed = False

    if not all_passed:
        raise RuntimeError(f"Some tests failed for {platform}-{arch}")

    print(f"✓ [STEP 3/4] All tests passed for {platform}-{arch}")


def copy_library_to_unity(
    platform: str, arch: str, builder: "PlatformBuilder", verbose: bool = False
):
    """Copy library file to Unity directory"""
    print(f"\n{'=' * 60}")
    print("[STEP 4/4] COPY - Copying library to Unity directory")
    print(f"{'=' * 60}")

    build_dir = builder.build_dir
    lib_dir = build_dir / "lib"
    library_name = LIBRARY_NAMES[platform]

    source_path = builder.get_library_path(lib_dir, library_name)

    if not source_path.exists():
        raise FileNotFoundError(f"Library file not found: {source_path}")

    unity_platform_dir = PLATFORM_UNITY_MAP[platform]
    unity_arch_dir = ARCH_UNITY_MAP[arch]
    unity_target_dir = UNITY_PLUGINS_DIR / unity_platform_dir / unity_arch_dir
    unity_target_dir.mkdir(parents=True, exist_ok=True)

    target_path = unity_target_dir / library_name

    if verbose:
        print(f"Source: {source_path}")
        print(f"Target: {target_path}")

    shutil.copy2(source_path, target_path)
    print(f"✓ [STEP 4/4] Copied {library_name} to {unity_target_dir}")


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
        "--skip-copy", action="store_true", help="Skip copying to Unity"
    )
    parser.add_argument("--generator", type=str, help="CMake generator (Windows only)")
    parser.add_argument(
        "--toolchain",
        type=str,
        help="CMake toolchain file (required for Android, recommended for cross-compilation)",
    )
    parser.add_argument("--android-abi", type=str, help="Android ABI")
    parser.add_argument("--ios-sdk", type=str, help="iOS SDK path or name")
    parser.add_argument(
        "--verbose", "-v", action="store_true", help="Enable verbose output"
    )

    args = parser.parse_args()

    print("\n" + "=" * 60)
    print("MLogger Native Library Build Script")
    print("=" * 60)
    print(f"Platform: {args.platform}")
    print(f"Architecture: {args.arch}")
    if args.verbose:
        print("Verbose mode: ON")
    print("=" * 60)

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
        configure_cmake(args.platform, args.arch, builder, args.verbose, **kwargs)
        build_project(args.platform, args.arch, builder, args.verbose)

        if not args.skip_tests:
            run_tests(args.platform, args.arch, builder, args.verbose)
        else:
            print(f"\n{'=' * 60}")
            print("[SKIP] Tests skipped")
            print(f"{'=' * 60}")

        if not args.skip_copy:
            copy_library_to_unity(args.platform, args.arch, builder, args.verbose)
        else:
            print(f"\n{'=' * 60}")
            print("[SKIP] Copy to Unity skipped")
            print(f"{'=' * 60}")

        print(f"\n{'=' * 60}")
        print("✓ BUILD COMPLETED SUCCESSFULLY!")
        print(f"{'=' * 60}\n")
    except Exception as e:
        print(f"\n{'=' * 60}")
        print("✗ Build failed!")
        print(f"{'=' * 60}")
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
