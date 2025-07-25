using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using OSSShell.Core.Models;
using OSSShell.Interop;

namespace OSSShell.Core.Services;

public class WindowManager
{
    private readonly ConcurrentDictionary<IntPtr, ManagedWindow> _windows = new();
    private readonly List<IntPtr> _ignoredWindows = new();
    
    public event EventHandler<ManagedWindow>? WindowAdded;
    public event EventHandler<ManagedWindow>? WindowRemoved;
    public event EventHandler<ManagedWindow>? WindowUpdated;
    
    public IEnumerable<ManagedWindow> Windows => _windows.Values.Where(w => w.IsVisible);
    
    public WindowManager()
    {
        InitializeIgnoredWindows();
    }
    
    private void InitializeIgnoredWindows()
    {
        _ignoredWindows.Add(NativeMethods.GetDesktopWindow());
        _ignoredWindows.Add(NativeMethods.GetShellWindow());
    }
    
    public void RefreshWindows()
    {
        var currentWindows = new HashSet<IntPtr>();
        
        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (ShouldManageWindow(hWnd))
            {
                currentWindows.Add(hWnd);
                
                if (_windows.TryGetValue(hWnd, out var existingWindow))
                {
                    UpdateWindow(existingWindow);
                }
                else
                {
                    var newWindow = CreateManagedWindow(hWnd);
                    if (newWindow != null)
                    {
                        _windows[hWnd] = newWindow;
                        WindowAdded?.Invoke(this, newWindow);
                    }
                }
            }
            return true;
        }, IntPtr.Zero);
        
        var windowsToRemove = _windows.Keys.Except(currentWindows).ToList();
        foreach (var handle in windowsToRemove)
        {
            if (_windows.TryRemove(handle, out var removedWindow))
            {
                WindowRemoved?.Invoke(this, removedWindow);
            }
        }
    }
    
    private bool ShouldManageWindow(IntPtr hWnd)
    {
        if (_ignoredWindows.Contains(hWnd))
            return false;
            
        if (!NativeMethods.IsWindowVisible(hWnd))
            return false;
            
        int textLength = NativeMethods.GetWindowTextLength(hWnd);
        if (textLength == 0)
            return false;
            
        var className = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, className, className.Capacity);
        string classNameStr = className.ToString();
        
        if (classNameStr == "Progman" || classNameStr == "WorkerW")
            return false;
            
        NativeMethods.GetWindowRect(hWnd, out var rect);
        if (rect.Width <= 0 || rect.Height <= 0)
            return false;
            
        return true;
    }
    
    private ManagedWindow? CreateManagedWindow(IntPtr hWnd)
    {
        var window = new ManagedWindow
        {
            Handle = hWnd,
            IsVisible = true,
            LastUpdated = DateTime.Now
        };
        
        UpdateWindowInfo(window);
        
        return window;
    }
    
    private void UpdateWindow(ManagedWindow window)
    {
        UpdateWindowInfo(window);
        window.LastUpdated = DateTime.Now;
        WindowUpdated?.Invoke(this, window);
    }
    
    private void UpdateWindowInfo(ManagedWindow window)
    {
        int textLength = NativeMethods.GetWindowTextLength(window.Handle);
        if (textLength > 0)
        {
            var titleBuilder = new StringBuilder(textLength + 1);
            NativeMethods.GetWindowText(window.Handle, titleBuilder, titleBuilder.Capacity);
            window.Title = titleBuilder.ToString();
        }
        
        NativeMethods.GetWindowThreadProcessId(window.Handle, out uint processId);
        window.ProcessId = processId;
        
        try
        {
            using var process = Process.GetProcessById((int)processId);
            window.ProcessName = process.ProcessName;
        }
        catch
        {
            window.ProcessName = "Unknown";
        }
        
        window.IsMinimized = NativeMethods.IsIconic(window.Handle);
    }
    
    public void ActivateWindow(IntPtr handle)
    {
        if (NativeMethods.IsIconic(handle))
        {
            NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
        }
        NativeMethods.SetForegroundWindow(handle);
    }
    
    public void MinimizeWindow(IntPtr handle)
    {
        NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);
    }
    
    public void MaximizeWindow(IntPtr handle)
    {
        NativeMethods.ShowWindow(handle, NativeMethods.SW_MAXIMIZE);
    }
    
    public void CloseWindow(IntPtr handle)
    {
        NativeMethods.SendMessage(handle, NativeMethods.WM_SYSCOMMAND, 
            (IntPtr)NativeMethods.SC_CLOSE, IntPtr.Zero);
    }
}