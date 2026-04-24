
using GoveeClient.Shared.Models;
using Microsoft.Extensions.Options;

namespace GoveeClient;

public sealed class AuthorizationHandler : DelegatingHandler
{
  private readonly IOptions<ClientOptions> _clientOptions;

  public AuthorizationHandler(IOptions<ClientOptions> clientOptions)
  {
    ArgumentNullException.ThrowIfNull(clientOptions);

    _clientOptions = clientOptions;
  }

  protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
  {
    request.Headers.Add("Govee-API-Key",
      _clientOptions.Value.ApiKey?.ToString() ?? string.Empty);

    return await base.SendAsync(request, cancellationToken);
  }
}
