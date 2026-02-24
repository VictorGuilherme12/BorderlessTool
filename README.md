# 🎮 BorderlessTool

A Windows tool to **manage monitors** and force old/legacy games into **windowed borderless** mode — even when the game doesn't support it natively or ignores `.ini` tweaks.

Built with .NET 10 and Win32 interop.

## Why?

Switching resolution between gaming and desktop use is annoying. Many older games only offer fullscreen exclusive or windowed with borders, and some don't respect config file changes.

BorderlessTool solves both problems in one place:
- **Quickly switch monitor resolution** (4K ↔ Full HD) and set which monitor is primary
- **Detect running games** and force them into borderless windowed mode

## Features

- **Game detection**
  - Automatically detects running games by scanning loaded GPU DLLs (`d3d11`, `d3d12`, `dxgi`, `vulkan-1`)
  - Filters out system processes, overlays (NVIDIA, AMD, GameBar), browsers, and common apps
- **Monitor management**
  - List all active monitors with resolution and primary status
  - Switch resolution to 4K (3840×2160) or Full HD (1920×1080)
  - Set any monitor as primary

## Screenshot

![BorderlessTool](https://github.com/user-attachments/assets/2fb57f21-f7c2-434d-9f40-d80a2e3a18d7)

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting started

git clone https://github.com/VictorGuilherme12/BorderlessTool.git cd BorderlessTool dotnet run

> **Note:** Some operations (set primary monitor, detect game DLLs) may require running as **Administrator**.

## Project structure

| File | Description |
|---|---|
| `Program.cs` | Entry point and main loop |
| `ConsoleUI.cs` | Console menu, input/output helpers |
| `MonitorUtils.cs` | Win32 interop for monitor enumeration and settings |
| `MonitorModels.cs` | `MonitorInfo` record and `MonitorStatus` enum |
| `GameWindowDetector.cs` | Game detection via loaded GPU DLLs |

## How game detection works

1. Enumerate all running processes
2. Check if the process has loaded DirectX/Vulkan DLLs (`d3d11.dll`, `d3d12.dll`, `dxgi.dll`, `vulkan-1.dll`)
3. Exclude system paths (`\Windows\`, `\WindowsApps\`)
4. Exclude known non-game processes (overlays, browsers, dev tools)
5. If a match is found → game detected ✅

## Roadmap

- [X] Force windowed borderless on detected game (remove borders + resize to monitor)
- [ ] Custom resolution input
- [ ] Save monitor profiles (JSON)
- [ ] Multi-monitor positioning

## License

MIT
