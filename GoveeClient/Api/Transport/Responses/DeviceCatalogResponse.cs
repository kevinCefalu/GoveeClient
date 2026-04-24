using GoveeClient.Api.Transport.Capabilities;
using GoveeClient.Api.Transport.Common;
using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Responses;

internal sealed record DeviceCatalogResponse(
  [property: JsonPropertyName("code")] int Code,
  [property: JsonPropertyName("message")] string? Message,
  [property: JsonPropertyName("data")] DeviceSummary[] Data) : ITransportResponse
{
  public string? EffectiveMessage => Message;
}

internal sealed record DeviceSummary(
  [property: JsonPropertyName("sku")] string Sku,
  [property: JsonPropertyName("device")] string DeviceId,
  [property: JsonPropertyName("deviceName")] string? Name,
  [property: JsonPropertyName("type")] string? DeviceType,
  [property: JsonPropertyName("capabilities")] DeviceCapabilityContract[]? Capabilities);
