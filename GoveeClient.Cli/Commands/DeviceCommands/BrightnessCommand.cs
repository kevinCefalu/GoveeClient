using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class BrightnessCommand(CliClientService cliClientService) : AsyncCommand<BrightnessCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : DeviceSelectionSettings
  {
    [CommandOption("--value <BRIGHTNESS>")]
    [Description("Brightness percentage from 1 to 100.")]
    public int Value { get; init; }

    public override ValidationResult Validate()
    {
      ValidationResult baseValidation = base.Validate();
      if (!baseValidation.Successful)
      {
        return baseValidation;
      }

      if (Value is < 1 or > 100)
      {
        return ValidationResult.Error("Brightness must be between 1 and 100.");
      }

      return ValidationResult.Success();
    }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);

    await _cliClientService.Client.SetBrightness(device, settings.Value);
    AnsiConsole.MarkupLine($"[green]{device.DeviceName}[/] brightness set to [yellow]{settings.Value}%[/].");
    return 0;
  }
}
