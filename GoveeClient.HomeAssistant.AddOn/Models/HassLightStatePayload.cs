using System.Text.Json.Serialization;

namespace GoveeClient.HomeAssistant.AddOn.Models;

public record HassLightStatePayload(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("brightness"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Brightness = null,
    [property: JsonPropertyName("color"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] HassRgbColor? Color = null,
    [property: JsonPropertyName("color_mode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ColorMode = null);

public record HassRgbColor(
    [property: JsonPropertyName("r")] int R,
    [property: JsonPropertyName("g")] int G,
    [property: JsonPropertyName("b")] int B);
