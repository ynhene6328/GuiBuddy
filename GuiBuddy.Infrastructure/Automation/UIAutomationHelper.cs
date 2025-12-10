using System.Windows.Automation;
using System.Diagnostics;

namespace GuiBuddy.Infrastructure.Automation;

public class UIAutomationHelper
{
    public IEnumerable<AutomationElement> GetTopLevelWindows()
    {
        var root = AutomationElement.RootElement;
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
        var collection = root.FindAll(TreeScope.Children, condition);

        foreach (AutomationElement element in collection)
        {
            yield return element;
        }
    }
}
