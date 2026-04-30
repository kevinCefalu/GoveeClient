using GoveeClient.Api;
using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services;
using GoveeClient.Shared.Services.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ClientImplementation = GoveeClient.GoveeClient;

namespace GoveeClient.Test;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
  [TestMethod]
  public void AddGoveeClient_WithOptionsAction_RegistersClientServices()
  {
    ServiceCollection services = new();
    Guid expectedApiKey = Guid.NewGuid();

    services.AddGoveeClient(options =>
    {
      options.BaseUrl = "https://example.com/api";
      options.ApiKey = expectedApiKey;
    });

    using ServiceProvider serviceProvider = services.BuildServiceProvider();
    using IServiceScope scope = serviceProvider.CreateScope();

    IOptions<ClientOptions> options = scope.ServiceProvider.GetRequiredService<IOptions<ClientOptions>>();
    IGoveeClient client = scope.ServiceProvider.GetRequiredService<IGoveeClient>();
    ICloudApiService cloudApiService = scope.ServiceProvider.GetRequiredService<ICloudApiService>();
    ILanApiService lanApiService = scope.ServiceProvider.GetRequiredService<ILanApiService>();

    Assert.AreEqual("https://example.com/api", options.Value.BaseUrl);
    Assert.AreEqual(expectedApiKey, options.Value.ApiKey);
    Assert.IsInstanceOfType<ClientImplementation>(client);
    Assert.IsInstanceOfType<CloudApiService>(cloudApiService);
    Assert.IsInstanceOfType<LanApiService>(lanApiService);
  }

  [TestMethod]
  public void AddGoveeClient_WithPrebuiltOptions_RegistersSameOptionsInstance()
  {
    ServiceCollection services = new();
    IOptions<ClientOptions> expectedOptions = Options.Create(new ClientOptions
    {
      BaseUrl = "https://example.com/router/api/v1",
      ApiKey = Guid.NewGuid()
    });

    services.AddGoveeClient(expectedOptions);

    using ServiceProvider serviceProvider = services.BuildServiceProvider();

    IOptions<ClientOptions> actualOptions = serviceProvider.GetRequiredService<IOptions<ClientOptions>>();

    Assert.AreSame(expectedOptions, actualOptions);
  }
}
