"""Android platform builder"""

from typing import List

from .base import PlatformBuilder


class AndroidBuilder(PlatformBuilder):
    """Android platform builder"""

    def get_cmake_args(self, **kwargs) -> List[str]:
        """Generate Android CMake arguments"""
        args = []

        toolchain = kwargs.get("toolchain")
        if not toolchain:
            raise ValueError("Android builds require --toolchain")

        args.append(f"-DCMAKE_TOOLCHAIN_FILE={toolchain}")

        abi = kwargs.get("android_abi") or {
            "arm64-v8a": "arm64-v8a",
            "armeabi-v7a": "armeabi-v7a",
            "x86_64": "x86_64",
            "x86": "x86",
        }.get(self.arch, "arm64-v8a")

        args.append(f"-DANDROID_ABI={abi}")
        args.append("-DANDROID_PLATFORM=android-21")

        return args

    def get_build_args(self) -> List[str]:
        """Get Android build arguments"""
        import os

        return ["-j", os.environ.get("JOBS", "4")]
