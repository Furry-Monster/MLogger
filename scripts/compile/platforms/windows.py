import os
import re
import subprocess
import sys
import warnings
from typing import List, Optional

from .base import PlatformBuilder


class WindowsBuilder(PlatformBuilder):
    def __init__(self, platform: str, arch: str, build_dir, native_dir):
        super().__init__(platform, arch, build_dir, native_dir)
        self._generator: Optional[str] = None

    def _is_msys_environment(self) -> bool:
        msys_vars = ["MSYSTEM", "MSYS", "MSYS2_PATH"]
        if any(var in os.environ for var in msys_vars):
            return True
        path = os.environ.get("PATH", "").lower()
        if "msys64" in path or "msys2" in path or "msys" in path:
            return True
        shell = os.environ.get("SHELL", "").lower()
        return "msys" in shell

    def _detect_visual_studio_generator(self) -> Optional[str]:
        known_vs_versions = [
            "Visual Studio 19 2026",
            "Visual Studio 18 2025",
            "Visual Studio 17 2022",
            "Visual Studio 16 2019",
            "Visual Studio 15 2017",
            "Visual Studio 14 2015",
        ]

        available_generators = ""
        try:
            result = subprocess.run(
                ["cmake", "-G"], capture_output=True, text=True, timeout=10
            )
            if result.returncode == 0:
                available_generators = result.stdout
        except (
            subprocess.TimeoutExpired,
            subprocess.CalledProcessError,
            FileNotFoundError,
        ):
            try:
                result = subprocess.run(
                    ["cmake", "--help"], capture_output=True, text=True, timeout=10
                )
                if result.returncode == 0:
                    available_generators = result.stdout
            except (
                subprocess.TimeoutExpired,
                subprocess.CalledProcessError,
                FileNotFoundError,
            ):
                return None

        if not available_generators:
            return None

        for vs_version in known_vs_versions:
            if vs_version in available_generators:
                return vs_version

        matches = re.findall(r"Visual Studio \d+ \d{4}", available_generators)
        return matches[0] if matches else None

    def _detect_alternative_generator(self) -> str:
        alternatives = ["MinGW Makefiles", "Ninja", "Unix Makefiles"]
        try:
            result = subprocess.run(
                ["cmake", "-G"], capture_output=True, text=True, timeout=10
            )
            if result.returncode == 0:
                for gen in alternatives:
                    if gen in result.stdout:
                        return gen
        except (
            subprocess.TimeoutExpired,
            subprocess.CalledProcessError,
            FileNotFoundError,
        ):
            try:
                result = subprocess.run(
                    ["cmake", "--help"], capture_output=True, text=True, timeout=10
                )
                if result.returncode == 0:
                    for gen in alternatives:
                        if gen in result.stdout:
                            return gen
            except (
                subprocess.TimeoutExpired,
                subprocess.CalledProcessError,
                FileNotFoundError,
            ):
                pass
        return "MinGW Makefiles"

    def get_cmake_args(self, **kwargs) -> List[str]:
        args = []
        is_windows = sys.platform == "win32"

        if not is_windows:
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
                generator = kwargs.get("generator")
                if generator:
                    args.extend(["-G", generator])
                    self._generator = generator
            else:
                generator = kwargs.get("generator", "MinGW Makefiles")
                args.extend(["-G", generator])
                self._generator = generator
                args.append("-DCMAKE_SYSTEM_NAME=Windows")
                warnings.warn(
                    "Cross-compiling for Windows from non-Windows system. "
                    "Consider using --toolchain for better compatibility.",
                    UserWarning,
                )
        else:
            generator = kwargs.get("generator")
            if not generator:
                if self._is_msys_environment():
                    generator = self._detect_alternative_generator()
                    if not generator.startswith("MinGW"):
                        warnings.warn(
                            f"MSYS environment detected but MinGW generator not found. "
                            f"Using {generator} instead. "
                            "Make sure MinGW-w64 is installed and in PATH.",
                            UserWarning,
                        )
                else:
                    generator = self._detect_visual_studio_generator()
                    if not generator:
                        generator = self._detect_alternative_generator()
                        warnings.warn(
                            f"Visual Studio not found. Using alternative generator: {generator}. "
                            "For better compatibility, install Visual Studio or specify a generator with --generator.",
                            UserWarning,
                        )

            args.extend(["-G", generator])
            self._generator = generator

            if generator.startswith("Visual Studio"):
                if self.arch == "x86_64":
                    args.extend(["-A", "x64"])
                elif self.arch == "x86":
                    args.extend(["-A", "Win32"])

        return args

    def get_build_args(self) -> List[str]:
        if self._generator and self._generator.startswith("Visual Studio"):
            return ["--config", "Release"]
        return []

    def get_executable_extension(self) -> str:
        return ".exe"
