using System.Windows;
using System.Windows.Automation;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Repositories;
using GuiBuddy.Infrastructure.Automation;

namespace GuiBuddy.Infrastructure.Repositories;

public class AutomationWindowRepository : IWindowRepository
{
    private readonly UIAutomationHelper _helper;

    public AutomationWindowRepository()
    {
        _helper = new UIAutomationHelper();
    }

    public IEnumerable<WindowInfo> GetAllWindows()
    {
        var elements = _helper.GetTopLevelWindows();
        foreach (var element in elements)
        {
            WindowInfo? info = null;
            try
            {
                // 基本的なプロパティの取得
                // アクセス不可などで例外が出る可能性があるためtry-catchで囲む
                var handle = (int)element.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty);
                var title = element.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
                var rect = (System.Windows.Rect)element.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
                var pid = (int)element.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty);

                info = new WindowInfo
                {
                    Handle = new IntPtr(handle),
                    Title = title ?? string.Empty,
                    Bounds = rect,
                    ProcessId = pid
                };
            }
            catch
            {
                // 取得できないウィンドウはスキップ
                continue;
            }

            if (info != null)
            {
                yield return info;
            }
        }
    }

    public UIElementInfo? GetWindowStructure(IntPtr windowHandle)
    {
        try
        {
            var element = AutomationElement.FromHandle(windowHandle);
            return _helper.GetElementStructure(element);
        }
        catch
        {
            return null;
        }
    }
    public bool ScrollToElement(IntPtr windowHandle, string automationId)
    {
        try
        {
            var root = AutomationElement.FromHandle(windowHandle);
            return _helper.ScrollToElement(root, automationId);
        }
        catch
        {
            return false;
        }
    }
}

