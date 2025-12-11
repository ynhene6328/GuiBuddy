using System.Windows.Automation;
using System.Diagnostics;
using GuiBuddy.Core.Models;
using System.Windows;

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

    public UIElementInfo? GetElementStructure(AutomationElement root)
    {
        if (root == null) return null;

        // CacheRequestを作成して、必要なプロパティを一度に取得する
        var cacheRequest = new CacheRequest();
        cacheRequest.Add(AutomationElement.NameProperty);
        cacheRequest.Add(AutomationElement.ControlTypeProperty);
        cacheRequest.Add(AutomationElement.AutomationIdProperty);
        cacheRequest.Add(AutomationElement.HelpTextProperty);
        cacheRequest.Add(AutomationElement.ClassNameProperty);
        cacheRequest.Add(AutomationElement.BoundingRectangleProperty);
        cacheRequest.TreeScope = TreeScope.Element | TreeScope.Subtree;

        // キャッシュを有効にして要素を取得
        using (cacheRequest.Activate())
        {
            // ルート要素の情報を更新（キャッシュに乗せる）
            // 注意: 既に取得済みのAutomationElementに対してBuildUpdatedCacheを呼ぶ
            try
            {
                var updatedRoot = root.GetUpdatedCache(cacheRequest);
                return BuildTreeRecursive(updatedRoot);
            }
            catch
            {
                return null;
            }
        }
    }

    private UIElementInfo BuildTreeRecursive(AutomationElement element, int depth = 0)
    {
        if (element == null) return new UIElementInfo { Name = "Error: Element is null" };
        // if (depth > 20) return new UIElementInfo { Name = "Error: Max depth reached" };

        string controlType = "Unknown";
        try
        {
            // LocalizedControlTypeではなく、ProgrammaticName ("ControlType.Button"など) を使用する
            // これにより言語に依存せず判定が可能になる
            var progName = element.Cached.ControlType?.ProgrammaticName;
            if (!string.IsNullOrEmpty(progName) && progName.StartsWith("ControlType."))
            {
                controlType = progName.Substring("ControlType.".Length);
            }
            else
            {
                controlType = element.Cached.ControlType?.LocalizedControlType ?? "Unknown";
            }
        }
        catch { }

        var info = new UIElementInfo
        {
            Name = element.Cached.Name ?? string.Empty,
            ControlType = controlType,
            AutomationId = element.Cached.AutomationId ?? string.Empty,
            HelpText = element.Cached.HelpText ?? string.Empty,
            ClassName = element.Cached.ClassName ?? string.Empty,
            Bounds = element.Cached.BoundingRectangle
        };

        // ControlViewWalkerを使用して子要素を走査
        // 注意: CacheRequestを使用している場合、TreeWalkerはキャッシュされた構造を使用できない場合があるため
        // FindAll(TreeScope.Children, Condition.TrueCondition) を使用する方が安全かつ高速な場合がある
        // ここではCacheRequestのTreeScopeにChildrenを含めているため、CachedChildrenを使用する

        foreach (AutomationElement child in element.CachedChildren)
        {
            // ControlView相当のフィルタリングを行う（必要であれば）
            // ここでは全てのCachedChildrenを追加する
            var childInfo = BuildTreeRecursive(child, depth + 1);
            info.Children.Add(childInfo);
        }

        return info;
    }
}
