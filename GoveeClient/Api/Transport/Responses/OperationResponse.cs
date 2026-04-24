using GoveeClient.Api.Transport.Common;
using System.Text.Json.Serialization;

namespace GoveeClient.Api.Transport.Responses;

internal sealed record OperationResponse(
  [property: JsonPropertyName("code")] int Code,
  [property: JsonPropertyName("message")] string? Message,
  [property: JsonPropertyName("msg")] string? LegacyMessage) : ITransportResponse
{
  public string? EffectiveMessage => Message ?? LegacyMessage;
}
