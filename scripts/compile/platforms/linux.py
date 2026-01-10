"""Linux platform builder"""

import sys
from typing import List

from .base import PlatformBuilder


class LinuxBuilder(PlatformBuilder):
    """Linux platform builder"""

    def get_cmake_args(self, **kwargs) -> List[str]:
        """Generate Linux CMake arguments"""
        args = []

        # Check if we're on Linux or need cross-compilation
        is_linux = sys.platform == "linux"

        if not is_linux:
            # Cross-compilation from non-Linux requires toolchain
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
            else:
                # Try to set system name for cross-compilation
                # This is a best-effort attempt, but toolchain is recommended
                args.append("-DCMAKE_SYSTEM_NAME=Linux")
                # Warn but don't fail - some setups might work
                import warnings

                warnings.warn(
                    "Cross-compiling for Linux from non-Linux system. "
                    "Consider using --toolchain for better compatibility.",
                    UserWarning,
                )
        # Native Linux build doesn't need special configuration

        return args

    def get_build_args(self) -> List[str]:
        """Get Linux build arguments"""
        import os

        return ["-j", os.environ.get("JOBS", "4")]
