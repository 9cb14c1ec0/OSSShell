using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OSSShell.Core.Models;
using OSSShell.Core.Services;
using OSSShell.Interop;

namespace OSSShell.Taskbar;

public partial class TaskbarWindow : Window
{
    private readonly WindowManager _windowManager;
    private readonly Dictionary<IntPtr, TaskButton> _taskButtons = new();
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _refreshTimer;
    
    public TaskbarWindow()
    {
        InitializeComponent();
        
        _windowManager = new WindowManager();
        _windowManager.WindowAdded += OnWindowAdded;
        _windowManager.WindowRemoved += OnWindowRemoved;
        _windowManager.WindowUpdated += OnWindowUpdated;
        
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += UpdateClock;
        
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += (s, e) => _windowManager.RefreshWindows();
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionTaskbar();
        
        UpdateClock(null, null);
        _clockTimer.Start();
        
        _windowManager.RefreshWindows();
        _refreshTimer.Start();
        
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        NativeMethods.RegisterShellHookWindow(hwnd);
    }
    
    private void PositionTaskbar()
    {
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Left = 0;
        Top = screenHeight - Height;
    }
    
    private void UpdateClock(object? sender, EventArgs? e)
    {
        ClockText.Text = DateTime.Now.ToString("h:mm tt\nM/d/yyyy");
    }
    
    private void OnWindowAdded(object? sender, ManagedWindow window)
    {
        Dispatcher.Invoke(() =>
        {
            var button = new TaskButton(window);
            button.Click += TaskButton_Click;
            
            _taskButtons[window.Handle] = button;
            TaskButtonPanel.Children.Add(button);
        });
    }
    
    private void OnWindowRemoved(object? sender, ManagedWindow window)
    {
        Dispatcher.Invoke(() =>
        {
            if (_taskButtons.TryGetValue(window.Handle, out var button))
            {
                TaskButtonPanel.Children.Remove(button);
                _taskButtons.Remove(window.Handle);
            }
        });
    }
    
    private void OnWindowUpdated(object? sender, ManagedWindow window)
    {
        Dispatcher.Invoke(() =>
        {
            if (_taskButtons.TryGetValue(window.Handle, out var button))
            {
                button.UpdateWindow(window);
            }
        });
    }
    
    private void TaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is TaskButton button)
        {
            _windowManager.ActivateWindow(button.WindowHandle);
        }
    }
    
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var startMenu = new StartMenu();
        
        // Position the start menu at the bottom left, above the taskbar
        startMenu.Left = 0;
        startMenu.Top = SystemParameters.PrimaryScreenHeight - Height - startMenu.Height;
        
        startMenu.Show();
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        NativeMethods.DeregisterShellHookWindow(hwnd);
        
        _clockTimer.Stop();
        _refreshTimer.Stop();
    }
}