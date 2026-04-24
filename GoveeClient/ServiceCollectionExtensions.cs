using GoveeClient.Api;
using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services;
using GoveeClient.Shared.Services.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GoveeClient;

public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    public IServiceCollection AddGoveeClient(IOptions<ClientOptions> options)
    {
      services.AddSingleton(options);
      services.AddScoped<ICloudApiService, CloudApiService>();
      services.AddScoped<ILanApiService, LanApiService>();
      services.AddScoped<IGoveeClient, GoveeClient>();
      return services;
    }

    public IServiceCollection AddGoveeClient(Action<ClientOptions> options)
    {
      ClientOptions clientOptions = new();
      options(clientOptions);

      return services.AddGoveeClient(Options.Create(clientOptions));
    }
  }
}
