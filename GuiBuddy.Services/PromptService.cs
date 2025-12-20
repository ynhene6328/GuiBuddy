using System.Text;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Services;

/// <summary>
/// AIへの送信プロンプトを構築するサービスの実装。
/// </summary>
public class PromptService : IPromptService
{
    public string BuildFullPrompt(AIRequest request)
    {
        var sb = new StringBuilder();

        // System Instruction
        if (!string.IsNullOrEmpty(request.SystemInstruction))
        {
            sb.AppendLine("[System Instruction]");
            sb.AppendLine(request.SystemInstruction);
            sb.AppendLine();
        }

        // USER_GOAL
        if (!string.IsNullOrEmpty(request.UserGoal))
        {
            sb.AppendLine("[Current USER_GOAL]");
            sb.AppendLine(request.UserGoal);
            sb.AppendLine();
        }
        // Context (UI Elements)
        if (request.Context != null)
        {
            sb.AppendLine("[Current UI Context]");
            sb.AppendLine(SummarizeUiTree(request.Context));
            sb.AppendLine();
        }

        // User Message
        sb.AppendLine("[User Request]");
        sb.AppendLine(request.UserMessage);

        return sb.ToString();
    }

    private string SummarizeUiTree(UiNode root)
    {
        var sb = new StringBuilder();
        SummarizeNodeRecursive(root, sb, 0);
        return sb.ToString();
    }

    private void SummarizeNodeRecursive(UiNode node, StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        
        // 基本情報: ID, Type, Name
        var line = $"{indent}- ID:{node.Id}, Type:{node.Type}";
        if (!string.IsNullOrEmpty(node.Name))
        {
            line += $", Name:\"{node.Name}\"";
        }
        // Hintがあれば追加 (以前のロジックではHintだけ特別扱いだったが、ここでまとめて出す)
        if (!string.IsNullOrEmpty(node.Hint))
        {
            line += $", Hint:\"{node.Hint}\"";
        }
        
        sb.AppendLine(line);

        foreach (var child in node.Children)
        {
            SummarizeNodeRecursive(child, sb, depth + 1);
        }
    }
}
