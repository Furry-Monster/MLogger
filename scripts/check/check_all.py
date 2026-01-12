#!/usr/bin/env python3
"""
Run All Checks
Convenience script to run all check scripts
"""

import sys
from pathlib import Path

# Add scripts directory to path
scripts_dir = Path(__file__).parent.parent
sys.path.insert(0, str(scripts_dir))

from check.project_check import ProjectChecker
from check.toolchain_check import ToolchainChecker
from check.cross_compile_check import CrossCompileChecker


def main():
    """Run all checks"""
    import argparse

    parser = argparse.ArgumentParser(description="Run All Checks")
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path(__file__).parent.parent.parent,
        help="Project root directory",
    )
    parser.add_argument(
        "--platform",
        type=str,
        choices=["linux", "windows", "macos", "android", "ios"],
        help="Target platform for cross-compilation check",
    )
    parser.add_argument(
        "--verbose", "-v", action="store_true", help="Verbose output"
    )

    args = parser.parse_args()

    all_passed = True

    # Project integrity check
    print("=" * 60)
    print("Project Integrity Check")
    print("=" * 60)
    project_checker = ProjectChecker(args.project_root)
    success, errors, warnings = project_checker.run_all_checks()
    if errors:
        for error in errors:
            print(f"  ✗ {error}")
    if warnings:
        for warning in warnings:
            print(f"  ⚠ {warning}")
    if success:
        print("✓ Project integrity check passed\n")
    else:
        print("✗ Project integrity check failed\n")
        all_passed = False

    # Toolchain check
    print("=" * 60)
    print("Toolchain Check")
    print("=" * 60)
    toolchain_checker = ToolchainChecker()
    success, errors, warnings, info = toolchain_checker.run_all_checks()
    if info:
        for msg in info:
            print(f"  ℹ {msg}")
    if warnings:
        for warning in warnings:
            print(f"  ⚠ {warning}")
    if errors:
        for error in errors:
            print(f"  ✗ {error}")
    if success:
        print("✓ Toolchain check passed\n")
    else:
        print("✗ Toolchain check failed\n")
        all_passed = False

    # Cross-compilation check
    print("=" * 60)
    print("Cross-Compilation Check")
    print("=" * 60)
    cross_checker = CrossCompileChecker()
    success, errors, warnings, info = cross_checker.run_all_checks(args.platform)
    if info:
        for msg in info:
            print(f"  ℹ {msg}")
    if warnings:
        for warning in warnings:
            print(f"  ⚠ {warning}")
    if errors:
        for error in errors:
            print(f"  ✗ {error}")
    if success:
        print("✓ Cross-compilation check completed\n")
    else:
        print("✗ Cross-compilation check failed\n")
        all_passed = False

    print("=" * 60)
    if all_passed:
        print("✓ All checks passed")
        return 0
    else:
        print("✗ Some checks failed")
        return 1


if __name__ == "__main__":
    sys.exit(main())
