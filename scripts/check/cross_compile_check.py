#!/usr/bin/env python3

import os
import platform
import subprocess
import sys
from pathlib import Path
from typing import List, Optional, Tuple


class CrossCompileChecker:
    def __init__(self):
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.info: List[str] = []

    def check_command(self, command: List[str], timeout: int = 5) -> bool:
        try:
            result = subprocess.run(
                command, capture_output=True, text=True, timeout=timeout
            )
            return result.returncode == 0
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False

    def check_android_ndk(self) -> bool:
        ndk_home = os.environ.get("ANDROID_NDK_HOME") or os.environ.get(
            "ANDROID_NDK_ROOT"
        )
        paths = (
            [Path(ndk_home)]
            if ndk_home
            else [
                Path.home() / "AppData/Local/Android/Sdk/ndk",
                Path("/opt/android-ndk"),
                Path("/usr/local/android-ndk"),
            ]
        )

        for ndk_path in paths:
            if ndk_path.exists():
                toolchain_file = (
                    ndk_path / "build" / "cmake" / "android.toolchain.cmake"
                )
                if toolchain_file.exists():
                    self.info.append(f"Android NDK found: {ndk_path}")
                    return True
                self.warnings.append(
                    f"Android NDK found but toolchain file missing: {toolchain_file}"
                )

        self.warnings.append(
            "Android NDK not found. Set ANDROID_NDK_HOME environment variable."
        )
        return False

    def check_ios_toolchain(self) -> bool:
        if platform.system() != "Darwin":
            return True
        if not self.check_command(["xcodebuild", "-version"]):
            self.warnings.append("Xcode not found (required for iOS builds)")
            return False
        if not self.check_command(["xcrun", "--find", "clang"]):
            self.warnings.append("Xcode Command Line Tools not found")
            return False
        self.info.append("iOS toolchain available")
        return True

    def check_linux_cross_compile(self) -> bool:
        if platform.system() == "Linux":
            return True
        toolchains = [
            "x86_64-linux-gnu-gcc",
            "aarch64-linux-gnu-gcc",
            "arm-linux-gnueabihf-gcc",
        ]
        for tc in toolchains:
            if self.check_command([tc, "--version"]):
                self.info.append(f"Linux cross-compiler found: {tc}")
                return True
        self.warnings.append("Linux cross-compilation toolchain not found")
        return True

    def check_windows_cross_compile(self) -> bool:
        if platform.system() == "Windows":
            return True
        for cmd, desc in [
            (
                ["x86_64-w64-mingw32-gcc", "--version"],
                "MinGW cross-compiler found for Windows",
            ),
            (
                ["i686-w64-mingw32-gcc", "--version"],
                "MinGW cross-compiler found for Windows (32-bit)",
            ),
        ]:
            if self.check_command(cmd):
                self.info.append(desc)
                return True
        self.warnings.append("Windows cross-compilation toolchain not found")
        return True

    def check_macos_cross_compile(self) -> bool:
        if platform.system() == "Darwin":
            return True
        paths = (
            [Path(os.environ.get("OSXCROSS_PATH"))]
            if os.environ.get("OSXCROSS_PATH")
            else [Path("/opt/osxcross"), Path("/usr/local/osxcross")]
        )
        for osxcross_path in paths:
            if osxcross_path.exists():
                self.info.append(f"osxcross found: {osxcross_path}")
                return True
        self.warnings.append(
            "macOS cross-compilation toolchain (osxcross) not found. Set OSXCROSS_PATH environment variable."
        )
        return True

    def check_platform_specific(self, target_platform: str) -> bool:
        current_platform = platform.system().lower()
        if target_platform.lower() == current_platform:
            return True
        checks = {
            "android": self.check_android_ndk,
            "ios": self.check_ios_toolchain,
            "linux": self.check_linux_cross_compile,
            "windows": self.check_windows_cross_compile,
            "macos": self.check_macos_cross_compile,
        }
        check_func = checks.get(target_platform.lower())
        return check_func() if check_func else True

    def check_all_platforms(self):
        for plat in ["linux", "windows", "macos", "android", "ios"]:
            self.check_platform_specific(plat)

    def run_all_checks(
        self, target_platform: Optional[str] = None
    ) -> Tuple[bool, List[str], List[str], List[str]]:
        self.errors.clear()
        self.warnings.clear()
        self.info.clear()
        if target_platform:
            self.check_platform_specific(target_platform)
        else:
            self.check_all_platforms()
        return True, self.errors, self.warnings, self.info


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Cross-Compilation Check")
    parser.add_argument(
        "--platform", type=str, choices=["linux", "windows", "macos", "android", "ios"]
    )
    parser.add_argument("--verbose", "-v", action="store_true")
    args = parser.parse_args()

    checker = CrossCompileChecker()
    success, errors, warnings, info = checker.run_all_checks(args.platform)

    for msg in info:
        print(f"  [INFO] {msg}")
    for warning in warnings:
        print(f"  [WARN] {warning}")
    for error in errors:
        print(f"  [ERROR] {error}")

    status = "[PASS]" if success else "[FAIL]"
    print(f"{status} Cross-compilation check {'completed' if success else 'failed'}")
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
