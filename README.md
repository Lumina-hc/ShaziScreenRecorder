# 傻子录屏 · ShaziScreenRecorder

<div align="center">

[中文版](#中文文档) · [English](#english)

</div>

---

## 中文文档

[跳转到 English](#english)

一个简洁、深色风格的 Windows 屏幕录制工具。基于 WPF 与 ScreenRecorderLib 构建，支持整屏 / 指定窗口 / 自定义区域录制、系统声音与麦克风独立开关，以及置顶悬浮窗快速控制。

### 功能特性

- **三种录制范围**：整个屏幕、指定窗口、自定义框选区域
- **双音源**：系统声音与麦克风独立开关，可选输入设备
- **悬浮窗**：置顶胶囊浮窗，一键开始 / 停止 / 暂停，实时计时
- **深色 PS 风格界面**：自定义矢量图标与控件模板，无图标字体依赖
- **高清参数**：30 / 60 FPS 与码率预设，H.264 硬件编码
- **标准 MP4 输出**：文件自动命名并列表展示，可打开 / 删除 / 查看目录

### 截图

将应用截图另存为 `image.png` 放在仓库根目录，即可在下方显示；网站（GitHub Pages）的截图放在 `docs/image.png`。

`image.png`

### 系统要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 10 1909+ / Windows 11 |
| 运行时 | .NET 10 Desktop Runtime |
| 架构 | x64 |

### 构建

前置：需要 .NET SDK 10 或更高版本。

```bash
dotnet build ShaziScreenRecorder.csproj -c Release
```

产物位于 `bin/x64/Release/net10.0-windows10.0.19041.0/ShaziScreenRecorder.exe`。

### 使用

1. 选择录制范围：整个屏幕 / 指定窗口 / 自定义区域（点击「框选区域」拖拽选择）。
2. 按需打开系统声音或麦克风。
3. 点击中央大圆钮开始录制，再次点击或点「停止」结束。
4. 顶部「浮窗」可将控制收进置顶悬浮窗，主界面可隐藏。

---

## English

[Jump to 中文版](#中文文档)

A simple, dark Photoshop-style screen recorder for Windows. Built on WPF and ScreenRecorderLib, with full-screen / window / region capture, independent system-audio and microphone toggles, and a topmost floating bar for quick control.

### Introduction

Stupid-simple screen recording: open it and record. Clean UI with one button for start / pause / stop. Output is standard MP4, saved automatically to your Videos folder, and the list auto-scans the selected directory for existing videos.

### Features

- **Three capture modes**: full screen, a specific window, or a custom region
- **Two audio sources**: system audio and microphone as independent toggles with device selection
- **Floating bar**: a topmost capsule to start, stop or pause, with a live timer
- **Dark PS-style UI**: custom vector icons and control templates, no icon-font dependency
- **HD presets**: 30 / 60 FPS with bitrate presets, H.264 hardware encoding
- **Standard MP4 output**: auto-named files, auto-scanned list, open / delete actions

### Screenshot

Save a screenshot of the app as `image.png` in the repository root to show it here; the GitHub Pages screenshot lives at `docs/image.png`.

`image.png`

### Requirements

| Item | Requirement |
| --- | --- |
| OS | Windows 10 1909+ / Windows 11 |
| Runtime | .NET 10 Desktop Runtime |
| Architecture | x64 |

### Build

Prerequisites: .NET SDK 10 or later.

```bash
dotnet build ShaziScreenRecorder.csproj -c Release
```

Output lands at `bin/x64/Release/net10.0-windows10.0.19041.0/ShaziScreenRecorder.exe`.

### Usage

1. Pick a scope: full screen, a window, or a custom region (drag with «框选区域»).
2. Toggle system audio or microphone as needed.
3. Press the big center button to start; press it again or hit «停止» to stop.
4. «浮窗» collapses controls into the topmost floating bar while the main window hides.

---

## 许可证 · License

Apache-2.0，详情见 [LICENSE](LICENSE) / Licensed under [Apache-2.0](LICENSE).