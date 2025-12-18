using System.Text.Json.Serialization;

namespace GuiBuddy.Core.Models;

public class UiNode
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AutomationId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UiBounds? Bounds { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hint { get; set; }

    [JsonIgnore]
    public bool IsOffscreen { get; set; }
    
    [JsonIgnore]
    public string RuntimeId { get; set; } = string.Empty;

    public List<UiNode> Children { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Actions { get; set; }

    public UiNode CloneWithoutBounds()
    {
        return new UiNode
        {
            Id = this.Id,
            Type = this.Type,
            Name = this.Name,
            AutomationId = this.AutomationId,
            Bounds = null, // Remove Bounds
            Hint = this.Hint,
            Children = this.Children.Select(c => c.CloneWithoutBounds()).ToList(),
            Actions = this.Actions // Keep Actions if needed, or create new list
        };
    }
}
