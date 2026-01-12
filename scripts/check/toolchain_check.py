#!/usr/bin/env python3

import os
import platform
import re
import subprocess
import sys
from pathlib import Path
from typing import List, Optional, Tuple


class ToolchainChecker:
    def __init__(self):
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.info: List[str] = []

    def check_command(
        self, command: List[str], timeout: int = 5
    ) -> Tuple[bool, Optional[str]]:
        try:
            result = subprocess.run(
                command, capture_output=True, text=True, timeout=timeout
            )
            return (
                (True, result.stdout.strip())
                if result.returncode == 0
                else (False, None)
            )
        except (FileNotFoundError, subprocess.TimeoutExpired):
            return False, None

    def check_cmake(self) -> bool:
        available, output = self.check_command(["cmake", "--version"])
        if not available:
            self.errors.append("CMake not found. Install CMake 3.20 or later.")
            return False
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
            self.info.append(
                f"CMake found: {output.split()[0] if output else 'unknown'}"
            )
        return True

    def check_cmake_generators(self) -> bool:
        available, output = self.check_command(["cmake", "-G"])
        if not available:
            self.warnings.append("Could not list CMake generators")
            return False
        vs_generators = [
            line.strip()
            for line in output.split("\n")
            if line.strip() and not line.startswith("*") and "Visual Studio" in line
        ]
        mingw_generators = [
            line.strip()
            for line in output.split("\n")
            if line.strip()
            and not line.startswith("*")
            and ("MinGW" in line or "Ninja" in line)
        ]
        if vs_generators:
            self.info.append(f"Visual Studio generators: {', '.join(vs_generators)}")
        if mingw_generators:
            self.info.append(f"MinGW/Ninja generators: {', '.join(mingw_generators)}")
        if not vs_generators and not mingw_generators:
            self.warnings.append("No Visual Studio or MinGW generators found")
        return True

    def check_c_compiler(self) -> bool:
        system = platform.system()
        if system == "Windows":
            if self.check_command(["cl"], timeout=2)[0]:
                self.info.append("MSVC compiler found")
                return True
            available, output = self.check_command(["gcc", "--version"])
            if available:
                version = re.search(r"gcc.*?(\d+\.\d+)", output)
                self.info.append(
                    f"MinGW GCC found: {version.group(1) if version else 'unknown'}"
                )
                return True
            self.errors.append("No C compiler found (MSVC or MinGW GCC)")
            return False
        elif system in ("Linux", "Darwin"):
            available, output = self.check_command(["gcc", "--version"])
            if available:
                version = re.search(r"gcc.*?(\d+\.\d+)", output)
                self.info.append(
                    f"GCC found: {version.group(1) if version else 'unknown'}"
                )
                return True
            self.errors.append("GCC not found")
            return False
        return True

    def check_cxx_compiler(self) -> bool:
        system = platform.system()
        if system == "Windows":
            if self.check_command(["cl"], timeout=2)[0]:
                return True
            available, output = self.check_command(["g++", "--version"])
            if available:
                version = re.search(r"g\+\+.*?(\d+\.\d+)", output)
                self.info.append(
                    f"MinGW G++ found: {version.group(1) if version else 'unknown'}"
                )
                return True
            self.errors.append("No C++ compiler found (MSVC or MinGW G++)")
            return False
        elif system in ("Linux", "Darwin"):
            available, output = self.check_command(["g++", "--version"])
            if available:
                version = re.search(r"g\+\+.*?(\d+\.\d+)", output)
                self.info.append(
                    f"G++ found: {version.group(1) if version else 'unknown'}"
                )
                return True
            if self.check_command(["clang++", "--version"])[0]:
                self.info.append("Clang++ found")
                return True
            self.errors.append("G++ or Clang++ not found")
            return False
        return True

    def check_visual_studio(self) -> bool:
        if platform.system() != "Windows":
            return True
        vs_paths = [
            Path("C:/Program Files/Microsoft Visual Studio"),
            Path("C:/Program Files (x86)/Microsoft Visual Studio"),
        ]
        for vs_path in vs_paths:
            if vs_path.exists() and any(d.is_dir() for d in vs_path.iterdir()):
                self.info.append(f"Visual Studio found at: {vs_path}")
                return True
        self.warnings.append("Visual Studio not found (optional for MinGW builds)")
        return True

    def check_mingw(self) -> bool:
        if platform.system() != "Windows":
            return True
        available, output = self.check_command(["gcc", "--version"])
        if available and ("mingw" in output.lower() or "msys" in output.lower()):
            self.info.append("MinGW/MSYS found in PATH")
            return True
        for msys_path in [Path("C:/msys64"), Path("C:/msys32"), Path("C:/msys2")]:
            if msys_path.exists():
                self.info.append(f"MSYS found at: {msys_path}")
                return True
        self.warnings.append("MinGW/MSYS not found (optional if using Visual Studio)")
        return True

    def check_python(self) -> bool:
        version = sys.version_info
        if version.major < 3 or (version.major == 3 and version.minor < 6):
            self.errors.append(
                f"Python {version.major}.{version.minor} is too old. Requires 3.6 or later."
            )
            return False
        self.info.append(
            f"Python version: {version.major}.{version.minor}.{version.micro}"
        )
        return True

    def check_environment(self) -> bool:
        system = platform.system()
        self.info.append(f"Platform: {system} {platform.machine()}")
        if system == "Windows" and any(
            var in os.environ for var in ["MSYSTEM", "MSYS", "MSYS2_PATH"]
        ):
            self.info.append("MSYS environment detected")
        return True

    def run_all_checks(self) -> Tuple[bool, List[str], List[str], List[str]]:
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
        if platform.system() == "Windows":
            checks.extend(
                [
                    ("Visual Studio", self.check_visual_studio),
                    ("MinGW/MSYS", self.check_mingw),
                ]
            )
        for check_name, check_func in checks:
            try:
                check_func()
            except Exception as e:
                self.errors.append(f"{check_name} check failed with exception: {e}")
        return len(self.errors) == 0, self.errors, self.warnings, self.info


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Build Toolchain Check")
    parser.add_argument("--verbose", "-v", action="store_true")
    args = parser.parse_args()

    checker = ToolchainChecker()
    success, errors, warnings, info = checker.run_all_checks()

    for msg in info:
        print(f"  [INFO] {msg}")
    for warning in warnings:
        print(f"  [WARN] {warning}")
    for error in errors:
        print(f"  [ERROR] {error}")

    status = "[PASS]" if success else "[FAIL]"
    print(f"{status} Toolchain check {'passed' if success else 'failed'}")
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
