#!/usr/bin/env python3
"""
Project Integrity Check Script
Checks project structure, git submodules, and required files
"""

import subprocess
import sys
from pathlib import Path
from typing import List, Tuple


class ProjectChecker:
    """Project integrity checker"""

    def __init__(self, project_root: Path):
        self.project_root = project_root.resolve()
        self.errors: List[str] = []
        self.warnings: List[str] = []

    def check_git_submodules(self) -> bool:
        """Check if git submodules are initialized"""
        gitmodules_path = self.project_root / ".gitmodules"
        if not gitmodules_path.exists():
            return True  # No submodules to check

        # Read .gitmodules to find submodule paths
        submodule_paths = []
        try:
            with open(gitmodules_path, "r", encoding="utf-8") as f:
                for line in f:
                    if line.strip().startswith("path ="):
                        path = line.split("=", 1)[1].strip()
                        submodule_paths.append(self.project_root / path)
        except IOError as e:
            self.errors.append(f"Failed to read .gitmodules: {e}")
            return False

        all_ok = True
        for submodule_path in submodule_paths:
            if not submodule_path.exists():
                self.errors.append(f"Submodule not found: {submodule_path.relative_to(self.project_root)}")
                all_ok = False
            elif not (submodule_path / ".git").exists() and not (submodule_path / "CMakeLists.txt").exists():
                # Check if it's actually initialized (has .git or key files)
                self.errors.append(
                    f"Submodule not initialized: {submodule_path.relative_to(self.project_root)}. "
                    "Run: git submodule update --init --recursive"
                )
                all_ok = False

        return all_ok

    def check_project_structure(self) -> bool:
        """Check required project directories and files"""
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

        required_files = [
            "native/CMakeLists.txt",
            "scripts/compile/build.py",
        ]

        all_ok = True
        for dir_path in required_dirs:
            full_path = self.project_root / dir_path
            if not full_path.exists():
                self.errors.append(f"Required directory missing: {dir_path}")
                all_ok = False
            elif not full_path.is_dir():
                self.errors.append(f"Path exists but is not a directory: {dir_path}")
                all_ok = False

        for file_path in required_files:
            full_path = self.project_root / file_path
            if not full_path.exists():
                self.errors.append(f"Required file missing: {file_path}")
                all_ok = False
            elif not full_path.is_file():
                self.errors.append(f"Path exists but is not a file: {file_path}")
                all_ok = False

        return all_ok

    def check_source_files(self) -> bool:
        """Check if essential source files exist"""
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
            full_path = self.project_root / file_path
            if not full_path.exists():
                self.warnings.append(f"Source file missing: {file_path}")
                all_ok = False

        return all_ok

    def check_cmake_lists(self) -> bool:
        """Check if CMakeLists.txt is valid"""
        cmake_file = self.project_root / "native/CMakeLists.txt"
        if not cmake_file.exists():
            self.errors.append("native/CMakeLists.txt not found")
            return False

        try:
            with open(cmake_file, "r", encoding="utf-8") as f:
                content = f.read()
                # Basic validation
                if "project(" not in content:
                    self.warnings.append("CMakeLists.txt may be missing project() declaration")
                if "add_library" not in content:
                    self.warnings.append("CMakeLists.txt may be missing add_library()")
        except IOError as e:
            self.errors.append(f"Failed to read CMakeLists.txt: {e}")
            return False

        return True

    def run_all_checks(self) -> Tuple[bool, List[str], List[str]]:
        """Run all project integrity checks"""
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
    """Main entry point"""
    import argparse

    parser = argparse.ArgumentParser(description="Project Integrity Check")
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path(__file__).parent.parent.parent,
        help="Project root directory",
    )
    parser.add_argument(
        "--verbose", "-v", action="store_true", help="Verbose output"
    )

    args = parser.parse_args()

    checker = ProjectChecker(args.project_root)
    success, errors, warnings = checker.run_all_checks()

    if args.verbose:
        print(f"Project root: {checker.project_root}")
        print()

    if errors:
        print("Errors:")
        for error in errors:
            print(f"  ✗ {error}")
        print()

    if warnings:
        print("Warnings:")
        for warning in warnings:
            print(f"  ⚠ {warning}")
        print()

    if success:
        print("✓ Project integrity check passed")
        return 0
    else:
        print("✗ Project integrity check failed")
        return 1


if __name__ == "__main__":
    sys.exit(main())
