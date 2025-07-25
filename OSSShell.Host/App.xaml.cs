using System.Windows;
using OSSShell.Core.Services;
using OSSShell.Taskbar;

namespace OSSShell.Host;

public partial class App : System.Windows.Application
{
    private TaskbarWindow? _taskbarWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private AutoStartService? _autoStartService;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        CreateTrayIcon();
        
        _taskbarWindow = new TaskbarWindow();
        _taskbarWindow.Show();
        
        _autoStartService = new AutoStartService();
        _autoStartService.StartLatestSimplyShip();
    }
    
    private void CreateTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "OSSShell"
        };
        
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Shutdown();
        contextMenu.Items.Add(exitItem);
        
        _trayIcon.ContextMenuStrip = contextMenu;
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}