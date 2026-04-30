using GoveeClient;
using GoveeClient.HomeAssistant.AddOn.Infrastructure;
using GoveeClient.HomeAssistant.AddOn.Options;
using GoveeClient.HomeAssistant.AddOn.Services;
using Microsoft.Extensions.Hosting;
using GoveeClient.Shared.Models;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Configuration.Add<HassOptionsConfigurationSource>(_ => { });
builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddOptions<AddOnOptions>()
    .Bind(builder.Configuration.GetSection(AddOnOptions.SectionName));

builder.Services.AddGoveeClient(opts =>
{
    string? rawKey = builder.Configuration["GoveeClient:ApiKey"];
    if (!string.IsNullOrEmpty(rawKey) && Guid.TryParse(rawKey, out Guid parsed))
        opts.ApiKey = parsed;
});

builder.Services.AddHostedService<GoVeeMqttBridgeWorker>();

IHost host = builder.Build();
await host.RunAsync();
