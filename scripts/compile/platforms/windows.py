"""Windows platform builder"""

import os
import subprocess
import sys
import warnings
from typing import List, Optional

from .base import PlatformBuilder


class WindowsBuilder(PlatformBuilder):
    """Windows platform builder"""

    def __init__(self, platform: str, arch: str, build_dir, native_dir):
        super().__init__(platform, arch, build_dir, native_dir)
        self._generator: Optional[str] = None

    def _is_msys_environment(self) -> bool:
        """Check if running in MSYS/MSYS2/MSYS64 environment"""
        # Check common MSYS environment variables
        msys_vars = ["MSYSTEM", "MSYS", "MSYS2_PATH"]
        for var in msys_vars:
            if var in os.environ:
                return True

        # Check if we're in a MSYS shell by checking PATH
        path = os.environ.get("PATH", "")
        if "msys64" in path.lower() or "msys2" in path.lower() or "msys" in path.lower():
            return True

        # Check if we're in MSYS by checking shell
        shell = os.environ.get("SHELL", "")
        if "msys" in shell.lower():
            return True

        return False

    def _detect_visual_studio_generator(self) -> Optional[str]:
        """Detect available Visual Studio generator"""
        # Known Visual Studio versions (from newest to oldest)
        # Note: We also dynamically detect any "Visual Studio" generators
        known_vs_versions = [
            "Visual Studio 19 2026",  # Visual Studio 2026
            "Visual Studio 18 2025",  # Visual Studio 2025 (if exists)
            "Visual Studio 17 2022",
            "Visual Studio 16 2019",
            "Visual Studio 15 2017",
            "Visual Studio 14 2015",
        ]

        # Get list of available generators using cmake -G
        available_generators = ""
        try:
            result = subprocess.run(
                ["cmake", "-G"],
                capture_output=True,
                text=True,
                timeout=10,
            )
            if result.returncode == 0:
                available_generators = result.stdout
        except (subprocess.TimeoutExpired, subprocess.CalledProcessError, FileNotFoundError):
            # Fallback: try cmake --help if -G doesn't work
            try:
                result = subprocess.run(
                    ["cmake", "--help"],
                    capture_output=True,
                    text=True,
                    timeout=10,
                )
                if result.returncode == 0:
                    available_generators = result.stdout
            except (subprocess.TimeoutExpired, subprocess.CalledProcessError, FileNotFoundError):
                return None

        if not available_generators:
            return None

        # First, try known versions in order (newest first)
        for vs_version in known_vs_versions:
            if vs_version in available_generators:
                return vs_version

        # If no known version found, try to find any Visual Studio generator
        # Extract all Visual Studio generators dynamically
        import re
        vs_pattern = r"Visual Studio \d+ \d{4}"
        matches = re.findall(vs_pattern, available_generators)
        if matches:
            # Return the first match (usually the newest)
            return matches[0]

        return None

    def _detect_alternative_generator(self) -> str:
        """Detect alternative generator (MinGW or Ninja)"""
        # Try MinGW first, then Ninja
        alternatives = ["MinGW Makefiles", "Ninja", "Unix Makefiles"]

        # Get list of available generators
        try:
            result = subprocess.run(
                ["cmake", "-G"],
                capture_output=True,
                text=True,
                timeout=10,
            )
            if result.returncode == 0:
                available_generators = result.stdout
                for gen in alternatives:
                    if gen in available_generators:
                        return gen
        except (subprocess.TimeoutExpired, subprocess.CalledProcessError, FileNotFoundError):
            # Fallback: try cmake --help
            try:
                result = subprocess.run(
                    ["cmake", "--help"],
                    capture_output=True,
                    text=True,
                    timeout=10,
                )
                if result.returncode == 0:
                    available_generators = result.stdout
                    for gen in alternatives:
                        if gen in available_generators:
                            return gen
            except (subprocess.TimeoutExpired, subprocess.CalledProcessError, FileNotFoundError):
                pass

        # Fallback to MinGW Makefiles (will fail at configure time if not available)
        return "MinGW Makefiles"

    def get_cmake_args(self, **kwargs) -> List[str]:
        """Generate Windows CMake arguments"""
        args = []

        # Check if we're on Windows or need cross-compilation
        is_windows = sys.platform == "win32"

        if not is_windows:
            # Cross-compilation from non-Windows requires toolchain (e.g., MinGW)
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
                # Generator might be specified or auto-detected
                generator = kwargs.get("generator")
                if generator:
                    args.extend(["-G", generator])
                    self._generator = generator
            else:
                # Try to use MinGW generator if available
                generator = kwargs.get("generator", "MinGW Makefiles")
                args.extend(["-G", generator])
                self._generator = generator
                # Set system name for cross-compilation
                args.append("-DCMAKE_SYSTEM_NAME=Windows")
                warnings.warn(
                    "Cross-compiling for Windows from non-Windows system. "
                    "Consider using --toolchain for better compatibility.",
                    UserWarning,
                )
        else:
            # Native Windows build
            generator = kwargs.get("generator")

            if not generator:
                # Check if we're in MSYS/MSYS2/MSYS64 environment
                if self._is_msys_environment():
                    # In MSYS environment, prefer MinGW Makefiles (MSYS64 uses MinGW-w64)
                    generator = self._detect_alternative_generator()
                    if not generator.startswith("MinGW"):
                        warnings.warn(
                            f"MSYS environment detected but MinGW generator not found. "
                            f"Using {generator} instead. "
                            "Make sure MinGW-w64 is installed and in PATH.",
                            UserWarning,
                        )
                else:
                    # Auto-detect Visual Studio generator first
                    generator = self._detect_visual_studio_generator()

                    if not generator:
                        # Fallback to alternative generators
                        generator = self._detect_alternative_generator()
                        warnings.warn(
                            f"Visual Studio not found. Using alternative generator: {generator}. "
                            "For better compatibility, install Visual Studio or specify a generator with --generator.",
                            UserWarning,
                        )

            args.extend(["-G", generator])
            # Store generator for use in get_build_args
            self._generator = generator

            # Visual Studio generators need architecture specification
            if generator.startswith("Visual Studio"):
                if self.arch == "x86_64":
                    args.extend(["-A", "x64"])
                elif self.arch == "x86":
                    args.extend(["-A", "Win32"])
            # MinGW and Ninja use CMAKE_SYSTEM_PROCESSOR or toolchain files
            elif generator in ("MinGW Makefiles", "Ninja"):
                # These generators typically use CMAKE_SYSTEM_PROCESSOR
                # Architecture is usually handled automatically, but we can set it if needed
                pass

        return args

    def get_build_args(self) -> List[str]:
        """Get Windows build arguments"""
        # Visual Studio generators need --config flag
        if self._generator and self._generator.startswith("Visual Studio"):
            return ["--config", "Release"]
        # For other generators (MinGW, Ninja), --config is ignored but harmless
        # We could return [] but keeping it for compatibility
        return []

    def get_executable_extension(self) -> str:
        """Windows executables use .exe"""
        return ".exe"
