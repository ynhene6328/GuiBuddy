using System.Threading.Tasks;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

public class MockAIClient : IAIClient
{
    public Task<string> SendAsync(string prompt)
    {
        // Simple mock logic for testing
        // If prompt contains "ボタン", assume user wants to find a button.
        // We act as if we found a button with ID 2 (usually a child of root).
        if (prompt.Contains("ボタン"))
        {
            return Task.FromResult("ボタンが見つかりました。「検索」ボタンを操作します。[HIGHLIGHT:2]");
        }

        return Task.FromResult("はい、何でしょうか？ 具体的な操作を指示してください。");
    }
}
