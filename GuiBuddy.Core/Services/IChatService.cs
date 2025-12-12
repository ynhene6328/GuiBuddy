using System.Threading.Tasks;
using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IChatService
{
    Task<string> SendMessageAsync(string userMessage, UiNode? appContext);
}
