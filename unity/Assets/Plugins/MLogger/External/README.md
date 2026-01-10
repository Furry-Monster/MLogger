# Native 库文件目录

此目录包含各平台的 Native 库文件。

## 目录结构

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

## 注意事项

- 确保库文件名称与 `MLoggerNative.cs` 中的 DLL 名称一致
- Linux: `libmlogger_linux.so`
- Windows: `mlogger_win.dll`
- macOS: `libmlogger_macos.dylib`
- Android: `libmlogger_android.so`
- iOS: `libmlogger_ios.a`