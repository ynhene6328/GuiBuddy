using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IUIMapService
{
    UIMap GenerateMap(UIElementInfo root);
    UiNode ConvertToAiNode(UiNode node);
}
