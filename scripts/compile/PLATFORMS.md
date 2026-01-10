# Platform Build Support Matrix

## Native Build (Recommended)

| Platform | Build On | Status | Notes |
|----------|----------|--------|-------|
| Linux | Linux | ✅ Native | No special requirements |
| Windows | Windows | ✅ Native | Requires Visual Studio or MinGW |
| macOS | macOS | ✅ Native | Requires Xcode Command Line Tools |
| Android | Any | ⚠️ Cross-compile | Always requires Android NDK toolchain |
| iOS | macOS | ✅ Native | Requires Xcode (iOS can only be built on macOS) |

## Cross-Compilation Support

### Linux → Other Platforms

| Target | Toolchain Required | Generator | Notes |
|--------|-------------------|-----------|-------|
| Windows | Optional (MinGW) | MinGW Makefiles | Can try without toolchain |
| macOS | Required (osxcross) | Unix Makefiles | Must provide toolchain |
| Android | Required (NDK) | Unix Makefiles | Must provide toolchain |
| iOS | ❌ Not possible | - | iOS can only be built on macOS |

### Windows → Other Platforms

| Target | Toolchain Required | Generator | Notes |
|--------|-------------------|-----------|-------|
| Linux | Optional | Unix Makefiles | Can try without toolchain |
| macOS | Required (osxcross) | Unix Makefiles | Must provide toolchain |
| Android | Required (NDK) | Unix Makefiles | Must provide toolchain |
| iOS | ❌ Not possible | - | iOS can only be built on macOS |

### macOS → Other Platforms

| Target | Toolchain Required | Generator | Notes |
|--------|-------------------|-----------|-------|
| Linux | Optional | Unix Makefiles | Can try without toolchain |
| Windows | Optional (MinGW) | MinGW Makefiles | Can try without toolchain |
| Android | Required (NDK) | Unix Makefiles | Must provide toolchain |
| iOS | ✅ Native | Xcode | No toolchain needed |

## Toolchain Examples

### Android NDK
```bash
python3 scripts/compile/build.py \
  --platform=android \
  --arch=arm64-v8a \
  --toolchain=$ANDROID_NDK/build/cmake/android.toolchain.cmake
```

### macOS Cross-Compilation (osxcross)
```bash
python3 scripts/compile/build.py \
  --platform=macos \
  --arch=x86_64 \
  --toolchain=/path/to/osxcross/toolchain.cmake
```

### Windows Cross-Compilation (MinGW)
```bash
python3 scripts/compile/build.py \
  --platform=windows \
  --arch=x86_64 \
  --toolchain=/path/to/mingw/toolchain.cmake
```

## Platform-Specific Notes

### Linux
- Native builds work out of the box
- Cross-compilation from Windows/macOS may work without toolchain, but toolchain is recommended

### Windows
- Native builds require Visual Studio or MinGW
- Cross-compilation from Linux/macOS can try MinGW generator, but toolchain is recommended

### macOS
- Native builds work with Xcode Command Line Tools
- Cross-compilation from Linux/Windows **requires** toolchain (e.g., osxcross)

### Android
- **Always** requires Android NDK toolchain (cross-compilation only)
- Can be built from any platform with proper NDK setup

### iOS
- **Only** can be built on macOS
- Requires Xcode and iOS SDK
- Cannot be cross-compiled from other platforms
