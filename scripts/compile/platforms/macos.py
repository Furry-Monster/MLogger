import sys
from typing import List

from .base import PlatformBuilder


class MacOSBuilder(PlatformBuilder):
    def get_cmake_args(self, **kwargs) -> List[str]:
        args = []
        is_macos = sys.platform == "darwin"

        if not is_macos:
            toolchain = kwargs.get("toolchain")
            if toolchain:
                args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")
            else:
                raise ValueError(
                    "Cross-compiling for macOS from non-macOS system requires --toolchain. "
                    "Consider using osxcross or build on macOS directly."
                )
        else:
            if self.arch == "arm64":
                args.append("-DCMAKE_OSX_ARCHITECTURES=arm64")
            elif self.arch == "x86_64":
                args.append("-DCMAKE_OSX_ARCHITECTURES=x86_64")
        return args

    def get_build_args(self) -> List[str]:
        import os

        return ["-j", os.environ.get("JOBS", "4")]
