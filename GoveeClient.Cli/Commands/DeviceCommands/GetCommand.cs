using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class GetCommand(CliClientService cliClientService) : AsyncCommand<DeviceSelectionSettings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  protected override async Task<int> ExecuteAsync(CommandContext context, DeviceSelectionSettings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);
    DeviceState? state = await _cliClientService.Client.GetDeviceState(device);

    if (state is null)
    {
      AnsiConsole.MarkupLine($"[yellow]No state was returned for {device.DeviceName} ({device.DeviceId}).[/]");
      return 0;
    }

    Table table = new();
    table.AddColumn("Property");
    table.AddColumn("Value");
    table.AddRow("Name", device.DeviceName);
    table.AddRow("Device Id", device.DeviceId);
    table.AddRow("Model", device.Model);
    table.AddRow("Type", string.IsNullOrWhiteSpace(device.DeviceType) ? "-" : device.DeviceType);
    table.AddRow("Power", state.State.ToString());
    table.AddRow("Brightness", state.Brightness.ToString());
    table.AddRow("Color", state.Color is null ? "-" : $"{state.Color.R},{state.Color.G},{state.Color.B}");
    table.AddRow("Color Temp", state.ColorTempInKelvin.ToString());

    AnsiConsole.Write(table);

    IReadOnlyList<DeviceCapability>? capabilities = device.Capabilities;
    if (capabilities is not null && capabilities.Count > 0)
    {
      AnsiConsole.WriteLine();
      AnsiConsole.Write(BuildCapabilitiesTable(capabilities));
    }

    return 0;
  }

  private static Table BuildCapabilitiesTable(IReadOnlyList<DeviceCapability> capabilities)
  {
    Table table = new();
    table.AddColumn("Capability Type");
    table.AddColumn("Instance");
    table.AddColumn("Data Type");
    table.AddColumn("Details");

    foreach (DeviceCapability capability in capabilities.OrderBy(capability => capability.Instance, StringComparer.OrdinalIgnoreCase))
    {
      table.AddRow(
          capability.Type,
          capability.Instance,
          capability.Parameters?.DataType ?? "-",
          BuildCapabilityDetails(capability));
    }

    return table;
  }

  private static string BuildCapabilityDetails(DeviceCapability capability)
  {
    List<string> details = [];
    DeviceCapabilityParameters? parameters = capability.Parameters;
    if (parameters?.Range is { } range)
    {
      details.Add($"range {range.Min?.ToString() ?? "?"}-{range.Max?.ToString() ?? "?"}");
    }

    if (parameters?.Options is { Count: > 0 } options)
    {
      details.Add($"options {options.Count}");
    }

    if (parameters?.Fields is { Count: > 0 } fields)
    {
      details.Add($"fields {string.Join(", ", fields.Select(field => field.FieldName))}");
    }

    return details.Count == 0 ? "-" : string.Join(" | ", details);
  }
}
