namespace GuiBuddy.Core.Models;

public class ChatMessage
{
    public string Sender { get; set; } = string.Empty; // "User" or "AI"
    public string Text { get; set; } = string.Empty;

    public ChatMessage(string sender, string text)
    {
        Sender = sender;
        Text = text;
    }
}
