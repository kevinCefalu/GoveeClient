namespace GoveeClient.Api.Transport.Common;

internal interface ITransportResponse
{
  int Code { get; }

  string? EffectiveMessage { get; }
}
