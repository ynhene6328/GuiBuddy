using System.Windows;

namespace GuiBuddy.Core.Models;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public int ProcessId { get; set; }
}
