using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public sealed class ColorCommand(CliClientService cliClientService) : AsyncCommand<ColorCommand.Settings>
{
  private readonly CliClientService _cliClientService = cliClientService ?? throw new ArgumentNullException(nameof(cliClientService));

  public sealed class Settings : DeviceSelectionSettings
  {
    [CommandOption("--red <RED>")]
    [Description("Red channel from 0 to 255.")]
    public int Red { get; init; }

    [CommandOption("--green <GREEN>")]
    [Description("Green channel from 0 to 255.")]
    public int Green { get; init; }

    [CommandOption("--blue <BLUE>")]
    [Description("Blue channel from 0 to 255.")]
    public int Blue { get; init; }

    public override ValidationResult Validate()
    {
      ValidationResult baseValidation = base.Validate();
      if (!baseValidation.Successful)
      {
        return baseValidation;
      }

      if (Red is < 0 or > 255 || Green is < 0 or > 255 || Blue is < 0 or > 255)
      {
        return ValidationResult.Error("RGB values must each be between 0 and 255.");
      }

      return ValidationResult.Success();
    }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    Device device = await _cliClientService.ResolveDeviceAsync(settings.ApiKey, settings.DeviceId, settings.DeviceName);
    RgbColor color = new(settings.Red, settings.Green, settings.Blue);

    await _cliClientService.Client.SetColor(device, color);
    AnsiConsole.MarkupLine($"[green]{device.DeviceName}[/] color set to [yellow]{settings.Red},{settings.Green},{settings.Blue}[/].");
    return 0;
  }
}
