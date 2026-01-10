"""Base platform builder"""

from abc import ABC, abstractmethod
from pathlib import Path
from typing import List


class PlatformBuilder(ABC):
    """Base class for platform-specific builders"""

    def __init__(self, platform: str, arch: str, build_dir: Path, native_dir: Path):
        self.platform = platform
        self.arch = arch
        self.build_dir = build_dir
        self.native_dir = native_dir

    @abstractmethod
    def get_cmake_args(self, **kwargs) -> List[str]:
        """Generate platform-specific CMake arguments"""
        pass

    @abstractmethod
    def get_build_args(self) -> List[str]:
        """Get platform-specific build arguments"""
        pass

    def get_test_executables(self) -> List[str]:
        """Get list of test executable names"""
        return ["test_mlogger", "test_simple", "test_c_interface"]

    def get_executable_extension(self) -> str:
        """Get executable extension for this platform"""
        return ""

    def can_run_tests(self) -> bool:
        """Whether tests can be run on this platform"""
        return True

    def get_library_path(self, lib_dir: Path, library_name: str) -> Path:
        """Get the actual library file path (may need platform-specific logic)"""
        return lib_dir / library_name
