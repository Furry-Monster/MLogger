#!/usr/bin/env python3
"""
Cross-Compilation Check Script
Checks cross-compilation toolchain availability for different platforms
"""

import os
import platform
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple


class CrossCompileChecker:
    """Cross-compilation checker"""

    def __init__(self):
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.info: List[str] = []

    def check_command(self, command: List[str], timeout: int = 5) -> bool:
        """Check if a command is available"""
        try:
            result = subprocess.run(
                command,
                capture_output=True,
                text=True,
                timeout=timeout,
            )
            return result.returncode == 0
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False
        except Exception:
            return False

    def check_android_ndk(self) -> bool:
        """Check Android NDK availability"""
        ndk_home = os.environ.get("ANDROID_NDK_HOME") or os.environ.get("ANDROID_NDK_ROOT")
        if ndk_home:
            ndk_path = Path(ndk_home)
            if ndk_path.exists():
                toolchain_file = ndk_path / "build" / "cmake" / "android.toolchain.cmake"
                if toolchain_file.exists():
                    self.info.append(f"Android NDK found: {ndk_home}")
                    return True
                else:
                    self.warnings.append(
                        f"Android NDK found but toolchain file missing: {toolchain_file}"
                    )
            else:
                self.warnings.append(f"ANDROID_NDK_HOME points to non-existent path: {ndk_home}")
        else:
            # Check common locations
            common_paths = [
                Path.home() / "AppData/Local/Android/Sdk/ndk",
                Path("/opt/android-ndk"),
                Path("/usr/local/android-ndk"),
            ]

            for ndk_path in common_paths:
                if ndk_path.exists():
                    toolchain_file = ndk_path / "build" / "cmake" / "android.toolchain.cmake"
                    if toolchain_file.exists():
                        self.info.append(f"Android NDK found at: {ndk_path}")
                        return True

        self.warnings.append(
            "Android NDK not found. Set ANDROID_NDK_HOME environment variable."
        )
        return False

    def check_ios_toolchain(self) -> bool:
        """Check iOS toolchain (macOS only)"""
        if platform.system() != "Darwin":
            return True  # Not applicable

        if not self.check_command(["xcodebuild", "-version"]):
            self.warnings.append("Xcode not found (required for iOS builds)")
            return False

        if not self.check_command(["xcrun", "--find", "clang"]):
            self.warnings.append("Xcode Command Line Tools not found")
            return False

        self.info.append("iOS toolchain available")
        return True

    def check_linux_cross_compile(self) -> bool:
        """Check Linux cross-compilation tools"""
        if platform.system() == "Linux":
            return True  # Native build

        # Check for cross-compilation toolchains
        toolchains = [
            "x86_64-linux-gnu-gcc",
            "aarch64-linux-gnu-gcc",
            "arm-linux-gnueabihf-gcc",
        ]

        found = False
        for toolchain in toolchains:
            if self.check_command([toolchain, "--version"]):
                self.info.append(f"Linux cross-compiler found: {toolchain}")
                found = True

        if not found:
            self.warnings.append("Linux cross-compilation toolchain not found")

        return True

    def check_windows_cross_compile(self) -> bool:
        """Check Windows cross-compilation tools"""
        if platform.system() == "Windows":
            return True  # Native build

        # Check for MinGW cross-compiler
        if self.check_command(["x86_64-w64-mingw32-gcc", "--version"]):
            self.info.append("MinGW cross-compiler found for Windows")
            return True

        if self.check_command(["i686-w64-mingw32-gcc", "--version"]):
            self.info.append("MinGW cross-compiler found for Windows (32-bit)")
            return True

        self.warnings.append("Windows cross-compilation toolchain not found")
        return True

    def check_macos_cross_compile(self) -> bool:
        """Check macOS cross-compilation tools"""
        if platform.system() == "Darwin":
            return True  # Native build

        # Check for osxcross
        osxcross_path = os.environ.get("OSXCROSS_PATH")
        if osxcross_path:
            osxcross = Path(osxcross_path)
            if osxcross.exists():
                self.info.append(f"osxcross found: {osxcross_path}")
                return True

        # Check common locations
        common_paths = [
            Path("/opt/osxcross"),
            Path("/usr/local/osxcross"),
        ]

        for osxcross_path in common_paths:
            if osxcross_path.exists():
                self.info.append(f"osxcross found at: {osxcross_path}")
                return True

        self.warnings.append(
            "macOS cross-compilation toolchain (osxcross) not found. "
            "Set OSXCROSS_PATH environment variable."
        )
        return True

    def check_platform_specific(self, target_platform: str) -> bool:
        """Check platform-specific cross-compilation requirements"""
        current_platform = platform.system().lower()
        if current_platform == "windows":
            current_platform = "windows"

        if target_platform == current_platform:
            return True  # Native build

        checks = {
            "android": self.check_android_ndk,
            "ios": self.check_ios_toolchain,
            "linux": self.check_linux_cross_compile,
            "windows": self.check_windows_cross_compile,
            "macos": self.check_macos_cross_compile,
        }

        check_func = checks.get(target_platform.lower())
        if check_func:
            return check_func()

        return True

    def check_all_platforms(self) -> Dict[str, bool]:
        """Check cross-compilation support for all platforms"""
        platforms = ["linux", "windows", "macos", "android", "ios"]
        results = {}

        for plat in platforms:
            results[plat] = self.check_platform_specific(plat)

        return results

    def run_all_checks(self, target_platform: Optional[str] = None) -> Tuple[bool, List[str], List[str], List[str]]:
        """Run cross-compilation checks"""
        self.errors.clear()
        self.warnings.clear()
        self.info.clear()

        if target_platform:
            self.check_platform_specific(target_platform)
        else:
            self.check_all_platforms()

        # No errors for cross-compilation (warnings only)
        return True, self.errors, self.warnings, self.info


def main():
    """Main entry point"""
    import argparse

    parser = argparse.ArgumentParser(description="Cross-Compilation Check")
    parser.add_argument(
        "--platform",
        type=str,
        choices=["linux", "windows", "macos", "android", "ios"],
        help="Target platform to check",
    )
    parser.add_argument(
        "--verbose", "-v", action="store_true", help="Verbose output"
    )

    args = parser.parse_args()

    checker = CrossCompileChecker()
    success, errors, warnings, info = checker.run_all_checks(args.platform)

    if info:
        print("Information:")
        for msg in info:
            print(f"  ℹ {msg}")
        print()

    if warnings:
        print("Warnings:")
        for warning in warnings:
            print(f"  ⚠ {warning}")
        print()

    if errors:
        print("Errors:")
        for error in errors:
            print(f"  ✗ {error}")
        print()

    if success:
        print("✓ Cross-compilation check completed")
        return 0
    else:
        print("✗ Cross-compilation check failed")
        return 1


if __name__ == "__main__":
    sys.exit(main())
