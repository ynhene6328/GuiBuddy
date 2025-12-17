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
        cacheRequest.Add(AutomationElement.IsOffscreenProperty);
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
            Bounds = element.Cached.BoundingRectangle,
            IsOffscreen = element.Cached.IsOffscreen
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

    public bool ScrollToElement(AutomationElement root, string targetAutomationId)
    {
        if (root == null) return false;

        // ターゲット要素を名前やIDで検索
        // ここではAutomationIdを主キーとするが、必要に応じて他の条件も追加可能
        // 注: TreeScope.Descendantsは重いため、本来は必要な階層まで絞るか、IDが一意であることを前提とする
        var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, targetAutomationId);
        var targetElement = root.FindFirst(TreeScope.Descendants, condition);

        if (targetElement == null) return false;

        return EnsureElementVisible(targetElement);
    }

    private bool EnsureElementVisible(AutomationElement element)
    {
        if (element == null) return false;

        try
        {
            // 1. ScrollItemPattern (リスト項目など自体がスクロール機能を持つ親に属する場合)
            object patternObj;
            if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out patternObj))
            {
                var scrollItemPattern = (ScrollItemPattern)patternObj;
                scrollItemPattern.ScrollIntoView();
                return true;
            }

            // 2. 親を遡って ScrollPattern を持つコンテナを探す
            var current = element;
            var walker = TreeWalker.ControlViewWalker;
            
            while (current != null && current != AutomationElement.RootElement)
            {
                if (current.TryGetCurrentPattern(ScrollPattern.Pattern, out patternObj))
                {
                    var scrollPattern = (ScrollPattern)patternObj;
                    
                    // 単純にScrollIntoView相当がないため、位置計算が必要だが、
                    // ScrollPatternしかない場合は「スクロール可能」とみなして
                    // VerticalScrollPercentなどを調整する...というのは難易度が高い。
                    // 多くの場合は ScrollItemPattern でカバーできるはず。
                    
                    // 簡易的な対応として、要素が可視になるまで少しずつスクロールする、などのロジックが考えられるが
                    // ここでは一旦 ScrollItemPattern メインとする。
                    // 親がScrollPatternを持っていても、どの子を表示したいかがわからないと制御できない。
                    
                    // ただし、もしターゲット要素自体が ScrollPattern を持っている（例: テキストボックス自身）場合は
                    // コンテンツ自体のスクロールなので、要素自体の可視化には寄与しないことが多い。
                    
                    break; 
                }
                current = walker.GetParent(current);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}
