using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class ListCommand(CliClientService cliClientService) : AsyncCommand<ListCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : CommandSettings
  {
    [CommandOption("-k|--api-key <API_KEY>")]
    [Description("Overrides the configured Govee API key for this invocation.")]
    public Guid? ApiKey { get; init; }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    IReadOnlyList<Device> devices = await _cliClientService.GetDevicesAsync(settings.ApiKey);

    if (devices.Count == 0)
    {
      AnsiConsole.MarkupLine("[yellow]No devices were returned for the configured account.[/]");
      return 0;
    }

    Table table = new();
    table.AddColumn("Name");
    table.AddColumn("Type");
    table.AddColumn("Device Id");
    table.AddColumn("Model");
    table.AddColumn("Capabilities");

    foreach (Device device in devices.OrderBy(device => device.DeviceName, StringComparer.OrdinalIgnoreCase))
    {
      table.AddRow(
        device.DeviceName,
        string.IsNullOrWhiteSpace(device.DeviceType) ? "-" : device.DeviceType,
        device.DeviceId,
        device.Model,
        BuildCapabilitySummary(device));
    }

    AnsiConsole.Write(table);
    return 0;
  }

  private static string BuildCapabilitySummary(Device device)
  {
    IReadOnlyList<DeviceCapability>? capabilities = device.Capabilities;
    if (capabilities is null || capabilities.Count == 0)
    {
      return "-";
    }

    return string.Join(", ",
      capabilities
        .Select(capability => capability.Instance)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(instance => instance, StringComparer.OrdinalIgnoreCase));
  }
}
