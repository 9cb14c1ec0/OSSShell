# Window Manager & Taskbar Development Plan

## Project Overview
A lightweight window manager and taskbar for Windows using .NET and C#. This replaces only the window management and taskbar functionality of Windows Explorer.

## Core Components

### 1. Window Manager
- **Window tracking** - Monitor all top-level windows
- **Window switching** - Alt+Tab functionality
- **Window positioning** - Snap layouts, minimize/maximize
- **Focus management** - Active window tracking

### 2. Taskbar
- **Task buttons** - One button per window
- **System tray** - Notification area icons
- **Clock** - Date/time display
- **Start button** - Launch Windows start menu

## Technical Architecture

### Key APIs and Interop
```csharp
// Window enumeration
[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

// Window information
[DllImport("user32.dll")]
static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

// Shell hooks for window events
[DllImport("user32.dll")]
static extern bool RegisterShellHookWindow(IntPtr hWnd);
```

### Project Structure
```
OSSShell/
├── OSSShell.Core/          # Core window management logic
├── OSSShell.Taskbar/       # Taskbar UI and logic
├── OSSShell.Interop/       # Win32 API wrappers
└── OSSShell.Host/          # Main application entry
```

## Implementation Phases

### Phase 1: Foundation (2 weeks)
1. **Project setup**
   - .NET 8 WPF application
   - Win32 interop library
   - Dependency injection
   
2. **Window enumeration**
   - List all windows
   - Filter visible windows
   - Get window titles/icons

### Phase 2: Basic Taskbar (3 weeks)
1. **Taskbar window**
   - Always-on-top window
   - Bottom screen positioning
   - Auto-hide option
   
2. **Task buttons**
   - One button per window
   - Window title display
   - Click to activate
   
3. **Window events**
   - New window detection
   - Window close detection
   - Title change updates

### Phase 3: Window Management (3 weeks)
1. **Alt+Tab switcher**
   - Window preview list
   - Keyboard navigation
   - Live thumbnails
   
2. **Window controls**
   - Minimize/restore
   - Maximize toggle
   - Close window
   
3. **Window positioning**
   - Snap to edges
   - Win+Arrow keys
   - Multi-monitor support

### Phase 4: System Integration (2 weeks)
1. **System tray**
   - Icon hosting
   - Tooltip support
   - Click/right-click events
   
2. **Clock widget**
   - Time display
   - Date on hover
   - Calendar popup
   
3. **Start button**
   - Launch default start menu
   - Custom menu option

## Key Code Examples

### Window Enumeration
```csharp
public class WindowManager
{
    private readonly List<Window> _windows = new();
    
    public void RefreshWindows()
    {
        _windows.Clear();
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0)
            {
                _windows.Add(new Window { Handle = hWnd });
            }
            return true;
        }, IntPtr.Zero);
    }
}
```

### Taskbar Button
```csharp
public class TaskButton : Button
{
    public IntPtr WindowHandle { get; set; }
    
    protected override void OnClick()
    {
        // Restore if minimized
        if (IsIconic(WindowHandle))
            ShowWindow(WindowHandle, SW_RESTORE);
        
        // Bring to front
        SetForegroundWindow(WindowHandle);
    }
}
```

## Development Requirements

### Tools
- Visual Studio 2022
- .NET 8 SDK
- Windows 10/11 SDK

### NuGet Packages
- `PInvoke.User32` - Win32 API wrappers
- `Microsoft.Extensions.DependencyInjection` - DI container
- `WPF-UI` or `ModernWpf` - Modern UI styling

## Testing Strategy

1. **Manual testing scenarios**
   - Open/close various applications
   - Multi-monitor window movement
   - System tray interaction
   
2. **Automated tests**
   - Window enumeration logic
   - Event handling
   - State management

## Deployment

1. **Single EXE** - Self-contained deployment
2. **Auto-start** - Registry or startup folder
3. **Settings** - JSON configuration file

## Timeline Summary

- **Week 1-2**: Foundation and window enumeration
- **Week 3-5**: Basic taskbar with task buttons
- **Week 6-8**: Window management features
- **Week 9-10**: System tray and polish

Total development time: ~10 weeks for MVP