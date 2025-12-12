using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;
using Reactive.Bindings;

namespace GuiBuddy.App.ViewModels;

public class ChatViewModel
{
    private readonly IChatService _chatService;
    private readonly IUIMapService _uiMapService; // To get context if needed, but Context is passed from MainViewModel ideally
    // For simplicity, we might assume MainViewModel manages the context passing, 
    // OR ChatViewModel has access to the current map via some shared state service.
    // Given the architecture, let's inject dependencies. 
    // Context (UiNode) logic needs to be solved.
    // Option A: ChatViewModel has a property CurrentContext which MainViewModel updates.
    
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ReactiveProperty<string> InputText { get; } = new("");
    public ReactiveCommand SendCommand { get; }

    public UiNode? CurrentContext { get; set; } // Set by parent ViewModel

    public ChatViewModel(IChatService chatService)
    {
        _chatService = chatService;

        SendCommand = InputText
            .Select(x => !string.IsNullOrWhiteSpace(x))
            .ToReactiveCommand();
        
        SendCommand.Subscribe(async _ => await SendMessage());
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText.Value)) return;

        string userText = InputText.Value;
        InputText.Value = ""; // Clear input

        Messages.Add(new ChatMessage("User", userText));

        // Add a temporary "Typing..." or similar if desired, but for Mock is fast.
        
        string response = await _chatService.SendMessageAsync(userText, CurrentContext);
        
        Messages.Add(new ChatMessage("GuiBuddy", response));
    }
}
