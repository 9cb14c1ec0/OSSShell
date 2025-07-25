# OSSShell - Windows Shell Replacement

A lightweight window manager and taskbar for Windows built with .NET 8 and C#.

## Features

- **Window Management**: Track and manage all open windows
- **Taskbar**: Display buttons for all open windows with minimize/restore functionality
- **System Tray**: Basic system tray icon with exit option
- **Clock**: Date and time display
- **Start Button**: Launch Windows Start Menu

## Building

### Requirements
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (recommended)

### Build Steps
1. Clone the repository
2. Open `OSSShell.sln` in Visual Studio
3. Build the solution (Ctrl+Shift+B)
4. Run `OSSShell.Host`

## Usage

1. Run `OSSShell.Host.exe`
2. The taskbar will appear at the bottom of your primary screen
3. Open applications will automatically appear as buttons on the taskbar
4. Click a button to activate/restore the window
5. Right-click the system tray icon to exit

## Architecture

- **OSSShell.Interop**: Win32 API wrappers
- **OSSShell.Core**: Core window management logic
- **OSSShell.Taskbar**: Taskbar UI components
- **OSSShell.Host**: Main application entry point

## Development Status

This is a basic implementation providing:
- Window enumeration and tracking
- Basic taskbar with task buttons
- Window activation/restore
- System tray integration
- Clock display
