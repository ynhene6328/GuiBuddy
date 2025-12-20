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

    // 末端のアクション要素として扱うタイプ（これらは子要素を持っていても無視する）
    private static readonly HashSet<string> LeafActionableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Button", "CheckBox", "RadioButton", "Hyperlink", "Slider", "ProgressBar"
        // Editは中身（Text）が重要になる場合もあるが、基本はValuePatternで取れるのでLeaf扱いで良いか？
        // 文書作成アプリのDocumentなどは構造が重要なので含めない。
        // 通常のTextBox (Edit) はLeaf扱いで、入力値はPatternから取る想定。
    };

    // 識別情報がなくても残すべき入力系タイプ
    private static readonly HashSet<string> InputTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Edit", "Document", "ComboBox", "List", "DataGrid"
    };

    private UiNode? ConvertToUiNode(UIElementInfo element)
    {
        // リストベースの再帰メソッドのラッパー
        var nodes = ConvertToUiNodeList(element, parentIsScrollable: false);
        return nodes.FirstOrDefault();
    }

    // 要素をリストとして返す（階層化と剪定に対応）
    private List<UiNode> ConvertToUiNodeList(UIElementInfo element, bool parentIsScrollable)
    {
        var result = new List<UiNode>();
        if (element == null) return result;

        string controlType = GetControlType(element);

        // 1. 完全な無視対象 (IgnoredTypes) -> 子も含めて削除 (トップダウン剪定)
        if (IgnoredTypes.Contains(controlType))
        {
            return result; 
        }

        // コンテキストの更新
        // 要素自体がスクロール可能なら、その子供たちはスクロールコンテキスト内にある
        bool currentIsScrollableContext = parentIsScrollable || element.IsScrollPatternAvailable;

        // 2. 子要素の再帰処理 (ボトムアップ処理 パート1)
        var validChildren = new List<UiNode>();
        foreach (var child in element.Children)
        {
            validChildren.AddRange(ConvertToUiNodeList(child, currentIsScrollableContext));
        }

        // 3. 自分自身を評価 (剪定チェック)
        bool isActionable = IsActionable(element);
        bool hasValidChildren = validChildren.Count > 0;
        bool isLabel = (controlType == "Text" && !string.IsNullOrWhiteSpace(element.Name));
        
        // 追加ルール: 末端アクション要素の場合、子要素を強制破棄 (Leaf Pruning)
        // ボタンの中にアイコンやテキストがあっても、ボタン自体で説明されていればノイズになるため
        if (isActionable && LeafActionableTypes.Contains(controlType))
        {
            validChildren.Clear();
            hasValidChildren = false;
        }

        // 「隠れたスクロール対象」ルール:
        // 画面外(Offscreen)は通常剪定されるが、以下なら残す:
        // - 親(またはコンテキスト)がスクロール可能 かつ 自身がアクション可能
        bool isOffscreen = element.IsOffscreen;
        bool keepIfOffscreen = isOffscreen && currentIsScrollableContext && isActionable;

        bool shouldKeep = false;

        if (!isOffscreen) 
        {
            // 画面内(On-screen)のルール
            if (isActionable) shouldKeep = true;
            else if (isLabel) shouldKeep = true;
            else if (hasValidChildren) shouldKeep = true; // コンテナとして残す
        }
        else
        {
            // 画面外(Off-screen)のルール
            if (keepIfOffscreen) shouldKeep = true;
            else if (hasValidChildren) shouldKeep = true; // 有効な子を持つコンテナは残す（構造維持）
        }

        // 追加ルール: 空のアクション要素の剪定 (Empty Actionable Pruning)
        // アクション可能と判定されても、識別情報がなく、子要素もない場合は削除する
        // ただし、入力欄(Edit)などは空でも意味があるため例外とする
        if (shouldKeep && isActionable && !hasValidChildren)
        {
            if (string.IsNullOrWhiteSpace(element.Name) && 
                string.IsNullOrWhiteSpace(element.AutomationId) && 
                string.IsNullOrWhiteSpace(element.HelpText))
            {
                // 入力系タイプでなければ削除
                if (!InputTypes.Contains(controlType))
                {
                    shouldKeep = false;
                }
            }
        }

        // コンテナの平坦化 (Flattening) ルール:
        // 「名前のないPane/Group」などは、そのまま残さずに子要素を親に昇格させる
        bool isFlattenTarget = IsFlattenTarget(element, controlType);
        
        // 保持すると決めたが、平坦化対象である場合 -> 子要素リストをそのまま返す
        if (shouldKeep && isFlattenTarget)
        {
            return validChildren;
        }

        if (shouldKeep)
        {
             // 型名の整理: "Unknown" -> "Container"
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
                IsOffscreen = element.IsOffscreen,
                RuntimeId = element.RuntimeId
            };
            
            node.Children.AddRange(validChildren);
            result.Add(node);
        }

        // 保持しない場合(shouldKeep == false)は空リストを返す（剪定）
        return result;
    }

    private bool IsActionable(UIElementInfo element)
    {
        // パターン利用可能フラグをチェック
        if (element.IsInvokePatternAvailable) return true;
        if (element.IsSelectionItemPatternAvailable) return true;
        if (element.IsTogglePatternAvailable) return true;
        // IsReadOnlyでも値が読めれば有用な場合があるが、操作対象としては微妙。
        // ここでは「情報の取得」もアクションの一種と捉えてTrueにする。
        if (element.IsValuePatternAvailable) return true; 
        if (element.IsExpandCollapsePatternAvailable) return true;
        if (element.IsScrollPatternAvailable) return true;
        if (element.IsScrollItemPatternAvailable) return true;
        
        // 特殊ケース: 編集可能テキスト
        if (element.ControlType == "Edit" || element.ControlType == "Document") return true;

        return false;
    }

    private string? GenerateHint(UIElementInfo element, string controlType)
    {
        string hint = "";

        // レベル 1: HelpTextを優先採用
        if (!string.IsNullOrEmpty(element.HelpText))
        {
            return element.HelpText;
        }

        string name = element.Name ?? "";
        string automationId = element.AutomationId ?? "";
        string className = element.ClassName ?? "";

        // レベル 2: Name + ControlType の組み合わせロジックは削除
        // 理由: 情報量が増えず、JSONサイズを圧迫するため。NameはUiNode.Nameで確認可能。
        
        // レベル 3: AutomationId からの意味推測
        if (automationId.Contains("Search", StringComparison.OrdinalIgnoreCase)) hint = "Search box";
        else if (automationId.Contains("Address", StringComparison.OrdinalIgnoreCase)) hint = "Address bar";
        
        if (!string.IsNullOrEmpty(hint)) return hint;

        // Level 4: ClassName / ControlType × 親コンテキストからの推測
        // ※ ReBarWindow32等はWin32アプリ（エクスプローラー等）で一般的なツールバーコンテナのクラス名
        if (className.Contains("CommandBar") && controlType == "Button") return "Toolbar button";
        if (className.Contains("ReBarWindow32")) return "Main toolbar area";
        if (className.Contains("UIItemsView")) return "File list";

        return null;
    }

    private bool IsSectionType(string type) => SectionTypes.Contains(type);

    private bool IsFlattenTarget(UIElementInfo element, string controlType)
    {
        // 以下の条件すべてを満たす場合にFlattenする
        // 1. ControlTypeがコンテナ系 (Pane, Group, Custom, Unknown)
        // 2. Actionableではない (IsActionable check is done in caller, but logic here assumes caller checked it or we re-check?
        //    Actually caller uses this AFTER deciding to keep. If it was actionable, it would be kept.
        //    Flattening is for structure simplification. Even actionable pane might be worth flattening if it has NO info?
        //    No, if it's actionable, it should start an action. Keep it.
        //    So IsFlattenTarget implies !IsActionable.
        
        if (IsActionable(element)) return false;

        // 3. 名前などの識別情報がない
        if (string.IsNullOrWhiteSpace(element.Name) && 
            string.IsNullOrWhiteSpace(element.AutomationId) && 
            string.IsNullOrWhiteSpace(element.HelpText))
        {
             if (controlType == "Pane" || controlType == "Group" || controlType == "Custom" || controlType == "Unknown")
             {
                 return true;
             }
        }
        return false;
    }

    private bool IsNoiseContainer(UIElementInfo element, string controlType)
    {
        return IsFlattenTarget(element, controlType);
    }

    private string GetControlType(UIElementInfo element)
    {
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
