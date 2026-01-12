import sys
from typing import List

from .base import PlatformBuilder


class LinuxBuilder(PlatformBuilder):
    def get_cmake_args(self, **kwargs) -> List[str]:
        args = []
        is_linux = sys.platform == "linux"

        if not is_linux:
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
            else:
                args.append("-DCMAKE_SYSTEM_NAME=Linux")
                import warnings

                warnings.warn(
                    "Cross-compiling for Linux from non-Linux system. "
                    "Consider using --toolchain for better compatibility.",
                    UserWarning,
                )
        return args

    def get_build_args(self) -> List[str]:
        import os

        return ["-j", os.environ.get("JOBS", "4")]
