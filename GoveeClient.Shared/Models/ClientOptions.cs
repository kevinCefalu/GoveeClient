
using System.ComponentModel;

namespace GoveeClient.Shared.Models;

public class ClientOptions
{
  [Description("The base URL for the Govee Cloud API. Defaults to 'https://openapi.api.govee.com/router/api/v1'.")]
  public string BaseUrl { get; set; } = "https://openapi.api.govee.com/router/api/v1";

  [Description("The API key to use for authenticating with the Govee Cloud API. Must be a valid GUID.")]
  public Guid? ApiKey { get; set; }
}
