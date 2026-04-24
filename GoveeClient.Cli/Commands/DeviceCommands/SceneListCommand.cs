using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class SceneListCommand(CliClientService cliClientService) : AsyncCommand<SceneListCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : DeviceSelectionSettings
  {
    [CommandOption("--cloud-only")]
    [Description("Only returns standard cloud scenes and excludes DIY scenes.")]
    public bool CloudOnly { get; init; }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);
    IReadOnlyList<DeviceScene> scenes = await _cliClientService.Client.GetScenes(device, includeDiyScenes: !settings.CloudOnly);

    if (scenes.Count == 0)
    {
      AnsiConsole.MarkupLine($"[yellow]No scenes were returned for {device.DeviceName}.[/]");
      return 0;
    }

    Table table = new();
    table.AddColumn("Name");
    table.AddColumn("Value");
    table.AddColumn("Id");
    table.AddColumn("Param Id");

    foreach (DeviceScene scene in scenes)
    {
      table.AddRow(
        scene.Name,
        scene.Value?.ToString() ?? "-",
        scene.Id?.ToString() ?? "-",
        scene.ParamId?.ToString() ?? "-");
    }

    AnsiConsole.Write(table);
    return 0;
  }
}
