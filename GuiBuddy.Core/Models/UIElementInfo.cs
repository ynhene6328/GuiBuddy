using System.Windows;

namespace GuiBuddy.Core.Models;

public class UIElementInfo
{
    public string Name { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public List<UIElementInfo> Children { get; set; } = new();
}
