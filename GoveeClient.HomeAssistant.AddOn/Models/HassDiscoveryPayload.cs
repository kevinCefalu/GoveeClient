using System.Text.Json.Serialization;

namespace GoveeClient.HomeAssistant.AddOn.Models;

public record HassDiscoveryPayload(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unique_id")] string UniqueId,
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("state_topic")] string StateTopic,
    [property: JsonPropertyName("command_topic")] string CommandTopic,
    [property: JsonPropertyName("availability_topic")] string AvailabilityTopic,
    [property: JsonPropertyName("payload_available")] string PayloadAvailable,
    [property: JsonPropertyName("payload_not_available")] string PayloadNotAvailable,
    [property: JsonPropertyName("brightness")] bool Brightness,
    [property: JsonPropertyName("color_mode")] bool ColorMode,
    [property: JsonPropertyName("supported_color_modes")] IReadOnlyList<string> SupportedColorModes,
    [property: JsonPropertyName("device")] HassDeviceInfo Device);

public record HassDeviceInfo(
    [property: JsonPropertyName("identifiers")] IReadOnlyList<string> Identifiers,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("manufacturer")] string Manufacturer);
