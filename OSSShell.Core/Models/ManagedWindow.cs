namespace OSSShell.Core.Models;

public class ManagedWindow
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public IntPtr IconHandle { get; set; }
    public bool IsVisible { get; set; }
    public bool IsMinimized { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public override bool Equals(object? obj)
    {
        return obj is ManagedWindow window && Handle == window.Handle;
    }
    
    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }
}