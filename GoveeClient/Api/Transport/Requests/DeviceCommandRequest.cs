using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Requests;

internal sealed record DeviceCommandRequest(
  [property: JsonPropertyName("requestId")] string RequestId,
  [property: JsonPropertyName("payload")] DeviceCommandBody Payload);

internal sealed record DeviceCommandBody(
  [property: JsonPropertyName("sku")] string Sku,
  [property: JsonPropertyName("device")] string Device,
  [property: JsonPropertyName("capability")] CommandCapability Payload);

internal sealed record CommandCapability(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("instance")] string Instance,
  [property: JsonPropertyName("value")] object? Value);