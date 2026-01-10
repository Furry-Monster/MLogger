"""Windows platform builder"""

import sys
from typing import List

from .base import PlatformBuilder


class WindowsBuilder(PlatformBuilder):
    """Windows platform builder"""

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
            else:
                # Try to use MinGW generator if available
                generator = kwargs.get("generator", "MinGW Makefiles")
                args.extend(["-G", generator])
                # Set system name for cross-compilation
                args.append("-DCMAKE_SYSTEM_NAME=Windows")
                import warnings

                warnings.warn(
                    "Cross-compiling for Windows from non-Windows system. "
                    "Consider using --toolchain for better compatibility.",
                    UserWarning,
                )
        else:
            # Native Windows build
            generator = kwargs.get("generator", "Visual Studio 17 2022")
            args.extend(["-G", generator])

            if self.arch == "x86_64":
                args.extend(["-A", "x64"])
            elif self.arch == "x86":
                args.extend(["-A", "Win32"])

        return args

    def get_build_args(self) -> List[str]:
        """Get Windows build arguments"""
        return ["--config", "Release"]

    def get_executable_extension(self) -> str:
        """Windows executables use .exe"""
        return ".exe"
