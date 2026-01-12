#!/usr/bin/env python3

from pathlib import Path
import sys
from typing import List, Tuple


class ProjectChecker:
    def __init__(self, project_root: Path):
        self.project_root = project_root.resolve()
        self.errors: List[str] = []
        self.warnings: List[str] = []

    def check_git_submodules(self) -> bool:
        gitmodules_path = self.project_root / ".gitmodules"
        if not gitmodules_path.exists():
            return True

        submodule_paths = []
        try:
            with open(gitmodules_path, "r", encoding="utf-8") as f:
                for line in f:
                    if line.strip().startswith("path ="):
                        submodule_paths.append(self.project_root / line.split("=", 1)[1].strip())
        except IOError as e:
            self.errors.append(f"Failed to read .gitmodules: {e}")
            return False

        all_ok = True
        for submodule_path in submodule_paths:
            if not submodule_path.exists():
                self.errors.append(
                    f"Submodule not found: {submodule_path.relative_to(self.project_root)}"
                )
                all_ok = False
            elif (
                not (submodule_path / ".git").exists()
                and not (submodule_path / "CMakeLists.txt").exists()
            ):
                self.errors.append(
                    f"Submodule not initialized: {submodule_path.relative_to(self.project_root)}. "
                    "Run: git submodule update --init --recursive"
                )
                all_ok = False
        return all_ok

    def check_project_structure(self) -> bool:
        required_dirs = [
            "native",
            "native/src",
            "native/src/core",
            "native/src/bridge",
            "native/src/utils",
            "native/external",
            "scripts/compile",
            "unity/Assets/Plugins/MLogger",
        ]
        required_files = ["native/CMakeLists.txt", "scripts/compile/build.py"]

        all_ok = True
        for path_str in required_dirs + required_files:
            full_path = self.project_root / path_str
            if not full_path.exists():
                self.errors.append(
                    f"Required {'directory' if path_str in required_dirs else 'file'} missing: {path_str}"
                )
                all_ok = False
            elif (path_str in required_dirs and not full_path.is_dir()) or (
                path_str in required_files and not full_path.is_file()
            ):
                self.errors.append(f"Path exists but wrong type: {path_str}")
                all_ok = False
        return all_ok

    def check_source_files(self) -> bool:
        source_files = [
            "native/src/core/logger_manager.h",
            "native/src/core/logger_manager.cpp",
            "native/src/core/logger_config.h",
            "native/src/core/logger_config.cpp",
            "native/src/bridge/bridge.h",
            "native/src/bridge/bridge.cpp",
        ]
        all_ok = True
        for file_path in source_files:
            if not (self.project_root / file_path).exists():
                self.warnings.append(f"Source file missing: {file_path}")
                all_ok = False
        return all_ok

    def check_cmake_lists(self) -> bool:
        cmake_file = self.project_root / "native/CMakeLists.txt"
        if not cmake_file.exists():
            self.errors.append("native/CMakeLists.txt not found")
            return False
        try:
            content = cmake_file.read_text(encoding="utf-8")
            if "project(" not in content:
                self.warnings.append("CMakeLists.txt may be missing project() declaration")
            if "add_library" not in content:
                self.warnings.append("CMakeLists.txt may be missing add_library()")
        except IOError as e:
            self.errors.append(f"Failed to read CMakeLists.txt: {e}")
            return False
        return True

    def run_all_checks(self) -> Tuple[bool, List[str], List[str]]:
        self.errors.clear()
        self.warnings.clear()
        checks = [
            ("Project Structure", self.check_project_structure),
            ("Git Submodules", self.check_git_submodules),
            ("Source Files", self.check_source_files),
            ("CMakeLists.txt", self.check_cmake_lists),
        ]
        for check_name, check_func in checks:
            try:
                check_func()
            except Exception as e:
                self.errors.append(f"{check_name} check failed with exception: {e}")
        return len(self.errors) == 0, self.errors, self.warnings


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Project Integrity Check")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).parent.parent.parent)
    parser.add_argument("--verbose", "-v", action="store_true")
    args = parser.parse_args()

    checker = ProjectChecker(args.project_root)
    success, errors, warnings = checker.run_all_checks()

    if args.verbose:
        print(f"Project root: {checker.project_root}\n")
    for error in errors:
        print(f"  [ERROR] {error}")
    for warning in warnings:
        print(f"  [WARN] {warning}")

    status = "[PASS]" if success else "[FAIL]"
    print(f"\n{status} Project integrity check {'passed' if success else 'failed'}")
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
