using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class SceneSetCommand(CliClientService cliClientService) : AsyncCommand<SceneSetCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : DeviceSelectionSettings
  {
    [CommandOption("--scene <SCENE_NAME>")]
    [Description("The exact scene name to activate.")]
    public string? SceneName { get; init; }

    public override ValidationResult Validate()
    {
      ValidationResult baseValidation = base.Validate();
      if (!baseValidation.Successful)
      {
        return baseValidation;
      }

      if (string.IsNullOrWhiteSpace(SceneName))
      {
        return ValidationResult.Error("Specify --scene.");
      }

      return ValidationResult.Success();
    }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);
    IReadOnlyList<DeviceScene> scenes = await _cliClientService.Client.GetScenes(device, includeDiyScenes: true);

    DeviceScene? scene = scenes.FirstOrDefault(candidate =>
      string.Equals(candidate.Name, settings.SceneName, StringComparison.OrdinalIgnoreCase));

    if (scene is null)
    {
      throw new InvalidOperationException($"Scene '{settings.SceneName}' was not found for device '{device.DeviceName}'.");
    }

    await _cliClientService.Client.ActivateScene(device, scene);
    AnsiConsole.MarkupLine($"[green]{device.DeviceName}[/] switched to scene [yellow]{scene.Name}[/].");
    return 0;
  }
}
