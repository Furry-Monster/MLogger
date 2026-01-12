import os
from abc import ABC, abstractmethod
from pathlib import Path
from typing import List


class PlatformBuilder(ABC):
    def __init__(self, platform: str, arch: str, build_dir: Path, native_dir: Path):
        self.platform = platform
        self.arch = arch
        self.build_dir = build_dir
        self.native_dir = native_dir

    @abstractmethod
    def get_cmake_args(self, **kwargs) -> List[str]:
        pass

    def get_build_args(self) -> List[str]:
        return ["-j", os.environ.get("JOBS", "4")]

    def get_test_executables(self) -> List[str]:
        return [
            # Basic tests
            "test_mlogger",
            "test_simple",
            "test_c_interface",
            # Enhanced tests
            "test_boundary",
            "test_error_handling",
            "test_stress",
            "test_memory",
        ]

    def get_executable_extension(self) -> str:
        return ""

    def can_run_tests(self) -> bool:
        return True

    def get_library_path(self, lib_dir: Path, library_name: str) -> Path:
        return lib_dir / library_name
