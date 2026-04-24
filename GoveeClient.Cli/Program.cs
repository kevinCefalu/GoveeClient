using GoveeClient;
using GoveeClient.Cli.Commands;
using GoveeClient.Cli.Commands.DeviceCommands;
using GoveeClient.Cli.Infrastructure;
using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;

string userSettingsPath = CliConfigurationStore.GetDefaultSettingsPath();
IConfiguration configuration = BuildConfiguration(userSettingsPath);
ClientOptions clientOptions = new();
configuration.GetSection("GoveeClient").Bind(clientOptions);

ServiceCollection services = new();
services.AddSingleton(configuration);
services.AddSingleton(new CliConfigurationStore(userSettingsPath));
services.AddSingleton<CliClientService>();
services.AddGoveeClient(Options.Create(clientOptions));

TypeRegistrar registrar = new(services);
CommandApp app = new(registrar);
app.Configure(config =>
{
  config.SetApplicationName("govee");
  config.PropagateExceptions();

  config.AddCommand<AuthCommand>("auth")
    .WithDescription("Show, save, or clear the configured Govee API key.");

  config.AddBranch("device", device =>
  {
    device.SetDescription("Inspect and control devices on your Govee cloud account.");
    device.AddCommand<ListCommand>("list")
      .WithDescription("List devices available to the configured account.");
    device.AddCommand<GetCommand>("get")
      .WithDescription("Get the current state for a device.");
    device.AddCommand<PowerCommand>("power")
      .WithDescription("Turn a device on or off.");
    device.AddCommand<BrightnessCommand>("brightness")
      .WithDescription("Set device brightness from 1 to 100.");
    device.AddCommand<ColorCommand>("color")
      .WithDescription("Set a device RGB color.");
    device.AddCommand<SceneListCommand>("scenes")
      .WithDescription("List available scenes for a device.");
    device.AddCommand<SceneSetCommand>("scene")
      .WithDescription("Activate a scene by name.");
  });
});

return app.Run(args);

static IConfiguration BuildConfiguration(string userSettingsPath)
  => new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile(userSettingsPath, optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

