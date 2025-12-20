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
    public bool IsOffscreen { get; set; }
    public string RuntimeId { get; set; } = string.Empty;
    
    // Pattern Availability Flags
    public bool IsInvokePatternAvailable { get; set; }
    public bool IsSelectionItemPatternAvailable { get; set; }
    public bool IsTogglePatternAvailable { get; set; }
    public bool IsValuePatternAvailable { get; set; }
    public bool IsExpandCollapsePatternAvailable { get; set; }
    public bool IsScrollPatternAvailable { get; set; }
    public bool IsScrollItemPatternAvailable { get; set; }

    public List<UIElementInfo> Children { get; set; } = new();
}
