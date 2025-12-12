using System.Threading.Tasks;

namespace GuiBuddy.Core.Services;

public interface IAIClient
{
    Task<string> SendAsync(string prompt);
}
