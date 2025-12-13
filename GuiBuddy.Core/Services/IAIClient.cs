using System.Threading.Tasks;
using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IAIClient
{
    Task<AIResponse> SendAsync(AIRequest request);
}
