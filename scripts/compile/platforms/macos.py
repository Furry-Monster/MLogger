"""macOS platform builder"""

import sys
from typing import List

from .base import PlatformBuilder


class MacOSBuilder(PlatformBuilder):
    """macOS platform builder"""

    def get_cmake_args(self, **kwargs) -> List[str]:
        """Generate macOS CMake arguments"""
        args = []

        # Check if we're on macOS or need cross-compilation
        is_macos = sys.platform == "darwin"

        if not is_macos:
            # Cross-compilation from non-macOS requires toolchain
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
            else:
                raise ValueError(
                    "Cross-compiling for macOS from non-macOS system requires --toolchain. "
                    "Consider using osxcross or build on macOS directly."
                )
        else:
            # Native macOS build - set architecture
            if self.arch == "arm64":
                args.append("-DCMAKE_OSX_ARCHITECTURES=arm64")
            elif self.arch == "x86_64":
                args.append("-DCMAKE_OSX_ARCHITECTURES=x86_64")

        return args

    def get_build_args(self) -> List[str]:
        """Get macOS build arguments"""
        import os

        return ["-j", os.environ.get("JOBS", "4")]
