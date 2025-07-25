using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OSSShell.Core.Models;

namespace OSSShell.Taskbar;

public class TaskButton : Button
{
    private ManagedWindow _window;
    
    public IntPtr WindowHandle => _window.Handle;
    
    public TaskButton(ManagedWindow window)
    {
        _window = window;
        
        Style = CreateButtonStyle();
        UpdateContent();
    }
    
    private Style CreateButtonStyle()
    {
        var style = new Style(typeof(Button));
        
        style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 45))));
        style.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
        style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(8, 4, 8, 4)));
        style.Setters.Add(new Setter(MarginProperty, new Thickness(2, 0, 2, 0)));
        style.Setters.Add(new Setter(MinWidthProperty, 150.0));
        style.Setters.Add(new Setter(MaxWidthProperty, 200.0));
        style.Setters.Add(new Setter(HeightProperty, 32.0));
        
        var hoverTrigger = new Trigger
        {
            Property = IsMouseOverProperty,
            Value = true
        };
        hoverTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(60, 60, 60))));
        style.Triggers.Add(hoverTrigger);
        
        var pressTrigger = new Trigger
        {
            Property = IsPressedProperty,
            Value = true
        };
        pressTrigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 30, 30))));
        style.Triggers.Add(pressTrigger);
        
        return style;
    }
    
    private void UpdateContent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        
        var textBlock = new TextBlock
        {
            Text = _window.Title,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 180
        };
        
        panel.Children.Add(textBlock);
        
        Content = panel;
        ToolTip = _window.Title;
        
        if (_window.IsMinimized)
        {
            Opacity = 0.7;
        }
        else
        {
            Opacity = 1.0;
        }
    }
    
    public void UpdateWindow(ManagedWindow window)
    {
        _window = window;
        UpdateContent();
    }
}