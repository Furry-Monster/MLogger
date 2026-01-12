#!/usr/bin/env python3

from pathlib import Path
import sys

scripts_dir = Path(__file__).parent.parent
sys.path.insert(0, str(scripts_dir))

from check.cross_compile_check import CrossCompileChecker
from check.project_check import ProjectChecker
from check.toolchain_check import ToolchainChecker

SEPARATOR = "=" * 60


def _print_section(title: str):
    print(f"{SEPARATOR}\n{title}\n{SEPARATOR}")


def print_results(title, success, errors, warnings, info=None):
    _print_section(title)
    if info:
        for msg in info:
            print(f"  [INFO] {msg}")
    if warnings:
        for warning in warnings:
            print(f"  [WARN] {warning}")
    if errors:
        for error in errors:
            print(f"  [ERROR] {error}")
    status = "[PASS]" if success else "[FAIL]"
    print(f"{status} {title} {'passed' if success else 'failed'}\n")
    return success


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Run All Checks")
    parser.add_argument("--project-root", type=Path, default=Path(__file__).parent.parent.parent)
    parser.add_argument(
        "--platform", type=str, choices=["linux", "windows", "macos", "android", "ios"]
    )
    parser.add_argument("--verbose", "-v", action="store_true")
    args = parser.parse_args()

    all_passed = True

    project_checker = ProjectChecker(args.project_root)
    success, errors, warnings = project_checker.run_all_checks()
    all_passed &= print_results("Project Integrity Check", success, errors, warnings)

    toolchain_checker = ToolchainChecker()
    success, errors, warnings, info = toolchain_checker.run_all_checks()
    all_passed &= print_results("Toolchain Check", success, errors, warnings, info)

    cross_checker = CrossCompileChecker()
    success, errors, warnings, info = cross_checker.run_all_checks(args.platform)
    all_passed &= print_results("Cross-Compilation Check", success, errors, warnings, info)

    _print_section("")
    status = "[PASS]" if all_passed else "[FAIL]"
    print(f"{status} All checks {'passed' if all_passed else 'failed'}")
    return 0 if all_passed else 1


if __name__ == "__main__":
    sys.exit(main())
