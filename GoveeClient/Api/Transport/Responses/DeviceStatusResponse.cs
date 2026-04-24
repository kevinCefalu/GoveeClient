using GoveeClient.Api.Transport.Capabilities;
using GoveeClient.Api.Transport.Common;
using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Responses;

internal sealed record DeviceStatusResponse(
  [property: JsonPropertyName("code")] int Code,
  [property: JsonPropertyName("msg")] string? Message,
  [property: JsonPropertyName("payload")] DeviceStatusPayload? Payload) : ITransportResponse
{
  public string? EffectiveMessage => Message;
}

internal sealed record DeviceStatusPayload(
  [property: JsonPropertyName("capabilities")] StateCapabilityContract[] Capabilities);
