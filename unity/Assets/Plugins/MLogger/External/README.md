# Native Library Directory

This directory contains the native library files for each supported platform.

## Directory Structure

```
External/
├── Linux/
│   ├── x86_64/         # Linux 64-bit
│   └── x86/            # Linux 32-bit
├── Windows/
│   ├── x86_64/         # Windows 64-bit
│   └── x86/            # Windows 32-bit
├── macOS/
│   ├── x86_64/         # macOS Intel
│   └── arm64/          # macOS Apple Silicon
├── Android/
│   ├── arm64-v8a/      # Android ARM 64-bit
│   ├── armeabi-v7a/    # Android ARM 32-bit
│   ├── x86_64/         # Android x86 64-bit
│   └── x86/            # Android x86 32-bit
└── iOS/
    └── arm64/          # iOS ARM 64-bit
```

## Notes

- Make sure the library file names are consistent with the DLL names specified in `MLoggerNative.cs`
- Linux: `libmlogger_linux.so`
- Windows: `mlogger_win.dll`
- macOS: `libmlogger_macos.dylib`
- Android: `libmlogger_android.so`
- iOS: `libmlogger_ios.a`