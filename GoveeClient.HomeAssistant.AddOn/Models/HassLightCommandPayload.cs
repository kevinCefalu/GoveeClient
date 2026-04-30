using System.Text.Json.Serialization;

namespace GoveeClient.HomeAssistant.AddOn.Models;

public record HassLightCommandPayload(
    [property: JsonPropertyName("state")] string? State = null,
    [property: JsonPropertyName("brightness")] int? Brightness = null,
    [property: JsonPropertyName("color")] HassRgbColor? Color = null);
