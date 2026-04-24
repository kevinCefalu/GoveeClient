using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Requests;

internal sealed record DeviceLookupRequest(
  [property: JsonPropertyName("requestId")] string RequestId,
  [property: JsonPropertyName("payload")] DeviceLookupTarget Payload);

internal sealed record DeviceLookupTarget(
  [property: JsonPropertyName("sku")] string Sku,
  [property: JsonPropertyName("device")] string Device);