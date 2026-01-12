from pathlib import Path
import subprocess
from typing import List

from .base import PlatformBuilder


class IOSBuilder(PlatformBuilder):
    def get_cmake_args(self, **kwargs) -> List[str]:
        import sys

        args = []
        is_macos = sys.platform == "darwin"

        if not is_macos:
            raise ValueError(
                "iOS builds can only be performed on macOS. "
                "Please run this script on a macOS system."
            )

        args.append("-DCMAKE_SYSTEM_NAME=iOS")
        if self.arch == "arm64":
            args.append("-DCMAKE_OSX_ARCHITECTURES=arm64")
        elif self.arch == "x86_64":
            args.append("-DCMAKE_OSX_ARCHITECTURES=x86_64")

        ios_sdk = kwargs.get("ios_sdk", "iphoneos")
        if ios_sdk.startswith("/"):
            args.append(f"-DCMAKE_OSX_SYSROOT={ios_sdk}")
        else:
            try:
                result = subprocess.run(
                    ["xcrun", "--sdk", ios_sdk, "--show-sdk-path"],
                    capture_output=True,
                    text=True,
                    check=True,
                )
                args.append(f"-DCMAKE_OSX_SYSROOT={result.stdout.strip()}")
            except (subprocess.CalledProcessError, FileNotFoundError):
                args.append(f"-DCMAKE_OSX_SYSROOT={ios_sdk}")
        return args

    def can_run_tests(self) -> bool:
        return False

    def get_library_path(self, lib_dir: Path, library_name: str) -> Path:
        path = lib_dir / library_name
        if not path.exists():
            alt_path = self.build_dir / library_name
            if alt_path.exists():
                return alt_path
        return path
