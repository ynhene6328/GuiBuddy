using System.Windows;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Services;

public class UIMapService : IUIMapService
{
    private int _nodeIdCounter = 1;

    // 操作対象として重要なコントロールタイプ (ProgrammaticNameに基づくPascalCase)
    private static readonly HashSet<string> InterestingTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Button", "Edit", "CheckBox", "RadioButton", "ComboBox", "List", "ListItem", 
        "Tree", "TreeItem", "MenuItem", "TabItem", "Hyperlink", "Slider", "Document", 
        "DataGrid", "DataItem", "Window", "Pane", "Group", "Text"
    };

    // ノイズとして無視するタイプ
    private static readonly HashSet<string> IgnoredTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Image", "Border", "Separator", "ScrollBar", "Thumb", "TitleBar", "Header", "HeaderItem"
    };

    private static readonly HashSet<string> SectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Window", "Pane", "Group", "Tab", "MenuBar", "ToolBar", "StatusBar", "Document", "Custom"
    };

    public UIMap GenerateMap(UIElementInfo root)
    {
        _nodeIdCounter = 1;
        var map = new UIMap
        {
            WindowName = root.Name
        };

        map.Root = ConvertToUiNode(root);

        return map;
    }

    public UiNode ConvertToAiNode(UiNode node)
    {
        return node.CloneWithoutBounds();
    }

    private UiNode? ConvertToUiNode(UIElementInfo element)
    {
        if (element == null) return null;

        string controlType = GetControlType(element);

        // ノイズ除去
        if (IgnoredTypes.Contains(controlType))
        {
            // ノイズだが子要素に重要なものがあるかもしれないので、子要素だけを引き上げる
            // ただし、単一のノードを返すメソッド設計上、引き上げは親側で行うか、ここでリストを返す必要がある
            // 簡易化のため、ここでは「ノイズ要素自体はnullを返し、呼び出し元で再帰的に処理する」アプローチを取るには
            // 構造を変える必要がある。
            // 今回の要件「ノイズ除去フィルタ」は、単純にその要素を無視するだけでなく、
            // 「意味のないPane」などをスキップして子を探す処理が必要。
            // ConvertToUiNodeList的なメソッドにするのが適切。
            return null; 
        }

        // ノイズ判定（名前のないPaneなど）
        if (IsNoiseContainer(element, controlType))
        {
            // このコンテナ自体はUiNodeにしないが、子供たちは処理して返したい。
            // しかし戻り値は単一UiNode。
            // したがって、親から呼ばれるときに「リストを返す」メソッドを別に用意して呼ぶのがベター。
            // ここでは再設計: Traverseメソッドでリストに追加していく方式にするのが良いが、
            // 階層構造を作るので、とりあえずこの要件は「ノイズコンテナは無視、その子供も無視」ではなく
            // 「ノイズコンテナの中身を親に直接ぶら下げる」のが理想。
            // 実装簡略化のため、まずはIgnoredTypesのみ対応し、Container Flatteningは次で行う。
        }

        var node = new UiNode
        {
            Id = _nodeIdCounter++,
            Type = controlType,
            Name = string.IsNullOrWhiteSpace(element.Name) ? null : element.Name,
            AutomationId = string.IsNullOrWhiteSpace(element.AutomationId) ? null : element.AutomationId,
            Bounds = ConvertBounds(element.Bounds),
            IsOffscreen = element.IsOffscreen
            // Role = ... LocalizedControlTypeを入れる？
        };

        // 子要素の処理
        foreach (var child in element.Children)
        {
            // Flattening logic
            var childNodes = ConvertToUiNodeList(child);
            node.Children.AddRange(childNodes);
        }

        return node;
    }

    // 要素をリストとして返す（Flattening対応）
    private List<UiNode> ConvertToUiNodeList(UIElementInfo element)
    {
        var result = new List<UiNode>();
        if (element == null) return result;

        string controlType = GetControlType(element);

        // 完全な無視対象
        if (IgnoredTypes.Contains(controlType))
        {
            // 子要素も含めて無視するか、子要素だけ救うか。
            // 一般的にScrollBarなどは子要素もGUI操作対象外でよい。
            return result; 
        }

        // ノイズコンテナ（名前のないGroup/Pane/Customなど） -> Flattening対象
        if (IsFlattenTarget(element, controlType))
        {
            foreach (var child in element.Children)
            {
                result.AddRange(ConvertToUiNodeList(child));
            }
            return result;
        }

        // Type Refinement: "Unknown" -> "Container"
        if (controlType.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            controlType = "Container";
        }

        var node = new UiNode
        {
            Id = _nodeIdCounter++,
            Type = controlType,
            Name = string.IsNullOrWhiteSpace(element.Name) ? null : element.Name,
            AutomationId = string.IsNullOrWhiteSpace(element.AutomationId) ? null : element.AutomationId,
            Bounds = ConvertBounds(element.Bounds),
            Hint = GenerateHint(element, controlType),
            IsOffscreen = element.IsOffscreen
        };

        foreach (var child in element.Children)
        {
            node.Children.AddRange(ConvertToUiNodeList(child));
        }

        result.Add(node);
        return result;
    }

    private string? GenerateHint(UIElementInfo element, string controlType)
    {
        string hint = "";

        // Level 1: HelpTextを優先採用
        if (!string.IsNullOrEmpty(element.HelpText))
        {
            return element.HelpText;
        }

        string name = element.Name ?? "";
        string automationId = element.AutomationId ?? "";
        string className = element.ClassName ?? "";

        // Level 2: Name + ControlType の組み合わせ
        if (!string.IsNullOrEmpty(name))
        {
             // 冗長な繰り返しを避ける（例: "OK Button" + "Button" -> "OK Button"）
             // 日本語のNameだと "ホーム Button" のようになるので有用
             // 英語の場合でNameにTypeが含まれているかは簡易チェック
             if (name.EndsWith(controlType, StringComparison.OrdinalIgnoreCase))
             {
                 return name;
             }
             return $"{name} {controlType}";
        }

        // Level 3: AutomationId からの意味推測
        if (automationId.Contains("Search", StringComparison.OrdinalIgnoreCase)) hint = "Search box";
        else if (automationId.Contains("Address", StringComparison.OrdinalIgnoreCase)) hint = "Address bar";
        
        if (!string.IsNullOrEmpty(hint)) return hint;

        // Level 4: ClassName / ControlType × 親コンテキストからの推測
        // ※ ReBarWindow32等はWin32アプリ（エクスプローラー等）で一般的なツールバーコンテナのクラス名
        if (className.Contains("CommandBar") && controlType == "Button") return "Toolbar button";
        if (className.Contains("ReBarWindow32")) return "Main toolbar area";
        if (className.Contains("UIItemsView")) return "File list";

        // Level 5 (Simple): 親要素からの推測ロジック
        // 現在のメソッドシグネチャでは親要素を渡していないためスキップ。
        // 必要に応じて親コンテキストを渡すように拡張する。

        // Fallback: 名前を持たないコンテナの場合
        if (IsSectionType(controlType))
        {
             // return controlType; // 型名をそのまま返すのは冗長なため現在はコメントアウト
        }

        return null;
    }

    private bool IsSectionType(string type) => SectionTypes.Contains(type);

    private bool IsFlattenTarget(UIElementInfo element, string controlType)
    {
        // 名前がなく、かつPane/Group/Customの場合はFlattening対象とする
        if (string.IsNullOrWhiteSpace(element.Name))
        {
            if (controlType == "Pane" || controlType == "Group" || controlType == "Custom")
            {
                // AutomationIdとClassName, HelpTextも情報がない場合のみFlattenする
                if (string.IsNullOrWhiteSpace(element.AutomationId) && 
                    string.IsNullOrWhiteSpace(element.HelpText))
                {
                    return true;
                }
            }
        }
        return false;
    }


    private bool IsNoiseContainer(UIElementInfo element, string controlType)
    {
        // IsFlattenTargetと同じロジックを使用
        return IsFlattenTarget(element, controlType);
    }

    private string GetControlType(UIElementInfo element)
    {
        // UIElementInfoのControlTypeには、修正により "Button" などが入っているはずだが
        // 古いデータやLocalizedが入っている可能性も考慮しつつ、Info側で正規化済みと仮定したいが
        // 念のためここでもチェックしてもよい。
        // 前回の修正でUIAutomationHelperがProgrammaticNameから抽出するようになったので、
        // 英語のPascalCase ("Button" 等) が入っているはず。
        return element.ControlType;
    }

    private UiBounds? ConvertBounds(Rect rect)
    {
        if (rect.IsEmpty) return null;
        return new UiBounds
        {
            X = (int)rect.X,
            Y = (int)rect.Y,
            Width = (int)rect.Width,
            Height = (int)rect.Height
        };
    }
}
