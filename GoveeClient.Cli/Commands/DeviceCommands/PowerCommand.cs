using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class PowerCommand(CliClientService cliClientService) : AsyncCommand<PowerCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : DeviceSelectionSettings
  {
    [CommandOption("--on")]
    [Description("Turns the device on.")]
    public bool On { get; init; }

    [CommandOption("--off")]
    [Description("Turns the device off.")]
    public bool Off { get; init; }

    public override ValidationResult Validate()
    {
      ValidationResult baseValidation = base.Validate();
      if (!baseValidation.Successful)
      {
        return baseValidation;
      }

      if (On == Off)
      {
        return ValidationResult.Error("Specify exactly one of --on or --off.");
      }

      return ValidationResult.Success();
    }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);
    PowerState targetState = settings.On ? PowerState.On : PowerState.Off;

    await _cliClientService.Client.SetPowerState(device, targetState);
    AnsiConsole.MarkupLine($"[green]{device.DeviceName}[/] is now [yellow]{targetState}[/].");
    return 0;
  }
}
