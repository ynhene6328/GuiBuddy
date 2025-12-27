using System.Windows;
using System.Windows.Controls;
using GuiBuddy.Core.Models;

namespace GuiBuddy.App.TemplateSelectors;

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserMessageTemplate { get; set; }
    public DataTemplate? AiMessageTemplate { get; set; }
    public DataTemplate? SystemMessageTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is ChatMessage message)
        {
            if (message.Sender == "User")
            {
                return UserMessageTemplate;
            }
            if (message.Sender == "GuiBuddy-System")
            {
                return SystemMessageTemplate;
            }
            return AiMessageTemplate;
        }

        return base.SelectTemplate(item, container);
    }
}
