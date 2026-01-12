#!/usr/bin/env python3
"""
Build Toolchain Check Script
Checks CMake, compilers, and generators availability
"""

import os
import platform
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple


class ToolchainChecker:
    """Build toolchain checker"""

    def __init__(self):
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.info: List[str] = []

    def check_command(self, command: List[str], timeout: int = 5) -> Tuple[bool, Optional[str]]:
        """Check if a command is available and get its version"""
        try:
            result = subprocess.run(
                command,
                capture_output=True,
                text=True,
                timeout=timeout,
            )
            if result.returncode == 0:
                return True, result.stdout.strip()
            return False, None
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False, None
        except Exception as e:
            return False, str(e)

    def check_cmake(self) -> bool:
        """Check CMake availability and version"""
        available, output = self.check_command(["cmake", "--version"])
        if not available:
            self.errors.append("CMake not found. Install CMake 3.20 or later.")
            return False

        # Extract version
        match = re.search(r"version\s+(\d+)\.(\d+)", output, re.IGNORECASE)
        if match:
            major, minor = int(match.group(1)), int(match.group(2))
            if major < 3 or (major == 3 and minor < 20):
                self.errors.append(
                    f"CMake version {major}.{minor} is too old. Requires 3.20 or later."
                )
                return False
            self.info.append(f"CMake version: {major}.{minor}")
        else:
            self.info.append(f"CMake found: {output.split()[0] if output else 'unknown'}")

        return True

    def check_cmake_generators(self) -> bool:
        """Check available CMake generators"""
        available, output = self.check_command(["cmake", "-G"])
        if not available:
            self.warnings.append("Could not list CMake generators")
            return False

        generators = []
        vs_generators = []
        mingw_generators = []

        for line in output.split("\n"):
            line = line.strip()
            if not line or line.startswith("*"):
                continue
            generators.append(line)
            if "Visual Studio" in line:
                vs_generators.append(line)
            if "MinGW" in line or "Ninja" in line:
                mingw_generators.append(line)

        if vs_generators:
            self.info.append(f"Visual Studio generators: {', '.join(vs_generators)}")
        if mingw_generators:
            self.info.append(f"MinGW/Ninja generators: {', '.join(mingw_generators)}")

        if not vs_generators and not mingw_generators:
            self.warnings.append("No Visual Studio or MinGW generators found")

        return True

    def check_c_compiler(self) -> bool:
        """Check C compiler availability"""
        system = platform.system()
        if system == "Windows":
            # Check MSVC
            available, _ = self.check_command(["cl"], timeout=2)
            if available:
                self.info.append("MSVC compiler found")
                return True

            # Check MinGW
            available, output = self.check_command(["gcc", "--version"])
            if available:
                match = re.search(r"gcc.*?(\d+\.\d+)", output)
                version = match.group(1) if match else "unknown"
                self.info.append(f"MinGW GCC found: {version}")
                return True

            self.errors.append("No C compiler found (MSVC or MinGW GCC)")
            return False

        elif system in ("Linux", "Darwin"):
            available, output = self.check_command(["gcc", "--version"])
            if available:
                match = re.search(r"gcc.*?(\d+\.\d+)", output)
                version = match.group(1) if match else "unknown"
                self.info.append(f"GCC found: {version}")
                return True

            self.errors.append("GCC not found")
            return False

        return True

    def check_cxx_compiler(self) -> bool:
        """Check C++ compiler availability"""
        system = platform.system()
        if system == "Windows":
            # Check MSVC
            available, _ = self.check_command(["cl"], timeout=2)
            if available:
                return True

            # Check MinGW
            available, output = self.check_command(["g++", "--version"])
            if available:
                match = re.search(r"g\+\+.*?(\d+\.\d+)", output)
                version = match.group(1) if match else "unknown"
                self.info.append(f"MinGW G++ found: {version}")
                return True

            self.errors.append("No C++ compiler found (MSVC or MinGW G++)")
            return False

        elif system in ("Linux", "Darwin"):
            available, output = self.check_command(["g++", "--version"])
            if available:
                match = re.search(r"g\+\+.*?(\d+\.\d+)", output)
                version = match.group(1) if match else "unknown"
                self.info.append(f"G++ found: {version}")
                return True

            # Check clang++
            available, output = self.check_command(["clang++", "--version"])
            if available:
                self.info.append("Clang++ found")
                return True

            self.errors.append("G++ or Clang++ not found")
            return False

        return True

    def check_visual_studio(self) -> bool:
        """Check Visual Studio installation (Windows only)"""
        if platform.system() != "Windows":
            return True

        # Check for Visual Studio installations
        vs_paths = [
            Path("C:/Program Files/Microsoft Visual Studio"),
            Path("C:/Program Files (x86)/Microsoft Visual Studio"),
        ]

        found = False
        for vs_path in vs_paths:
            if vs_path.exists():
                versions = [d for d in vs_path.iterdir() if d.is_dir()]
                if versions:
                    found = True
                    self.info.append(f"Visual Studio found at: {vs_path}")
                    break

        if not found:
            self.warnings.append("Visual Studio not found (optional for MinGW builds)")

        return True

    def check_mingw(self) -> bool:
        """Check MinGW/MSYS installation"""
        system = platform.system()
        if system != "Windows":
            return True

        # Check for MinGW in PATH
        available, output = self.check_command(["gcc", "--version"])
        if available:
            if "mingw" in output.lower() or "msys" in output.lower():
                self.info.append("MinGW/MSYS found in PATH")
                return True

        # Check common MSYS paths
        msys_paths = [
            Path("C:/msys64"),
            Path("C:/msys32"),
            Path("C:/msys2"),
        ]

        for msys_path in msys_paths:
            if msys_path.exists():
                self.info.append(f"MSYS found at: {msys_path}")
                return True

        self.warnings.append("MinGW/MSYS not found (optional if using Visual Studio)")

        return True

    def check_python(self) -> bool:
        """Check Python version"""
        version = sys.version_info
        if version.major < 3 or (version.major == 3 and version.minor < 6):
            self.errors.append(
                f"Python {version.major}.{version.minor} is too old. Requires 3.6 or later."
            )
            return False

        self.info.append(f"Python version: {version.major}.{version.minor}.{version.micro}")
        return True

    def check_environment(self) -> bool:
        """Check build environment"""
        system = platform.system()
        self.info.append(f"Platform: {system} {platform.machine()}")

        # Check MSYS environment
        if system == "Windows":
            msys_vars = ["MSYSTEM", "MSYS", "MSYS2_PATH"]
            msys_detected = any(var in os.environ for var in msys_vars)
            if msys_detected:
                self.info.append("MSYS environment detected")

        return True

    def run_all_checks(self) -> Tuple[bool, List[str], List[str], List[str]]:
        """Run all toolchain checks"""
        self.errors.clear()
        self.warnings.clear()
        self.info.clear()

        checks = [
            ("Python", self.check_python),
            ("CMake", self.check_cmake),
            ("CMake Generators", self.check_cmake_generators),
            ("C Compiler", self.check_c_compiler),
            ("C++ Compiler", self.check_cxx_compiler),
            ("Environment", self.check_environment),
        ]

        # Platform-specific checks
        if platform.system() == "Windows":
            checks.extend([
                ("Visual Studio", self.check_visual_studio),
                ("MinGW/MSYS", self.check_mingw),
            ])

        for check_name, check_func in checks:
            try:
                check_func()
            except Exception as e:
                self.errors.append(f"{check_name} check failed with exception: {e}")

        return len(self.errors) == 0, self.errors, self.warnings, self.info


def main():
    """Main entry point"""
    import argparse

    parser = argparse.ArgumentParser(description="Build Toolchain Check")
    parser.add_argument(
        "--verbose", "-v", action="store_true", help="Verbose output"
    )

    args = parser.parse_args()

    checker = ToolchainChecker()
    success, errors, warnings, info = checker.run_all_checks()

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
        print("✓ Toolchain check passed")
        return 0
    else:
        print("✗ Toolchain check failed")
        return 1


if __name__ == "__main__":
    sys.exit(main())
