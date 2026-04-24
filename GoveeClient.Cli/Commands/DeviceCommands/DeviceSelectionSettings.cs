using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands.DeviceCommands;

public class DeviceSelectionSettings : CommandSettings
{
  [CommandOption("-k|--api-key <API_KEY>")]
  [Description("Overrides the configured Govee API key for this invocation.")]
  public Guid? ApiKey { get; init; }

  [CommandOption("--id <DEVICE_ID>")]
  [Description("The Govee device identifier.")]
  public string? DeviceId { get; init; }

  [CommandOption("--name <DEVICE_NAME>")]
  [Description("The device name returned by the Govee account.")]
  public string? DeviceName { get; init; }

  public override Spectre.Console.ValidationResult Validate()
  {
    bool hasDeviceId = !string.IsNullOrWhiteSpace(DeviceId);
    bool hasDeviceName = !string.IsNullOrWhiteSpace(DeviceName);

    if (hasDeviceId == hasDeviceName)
    {
      return Spectre.Console.ValidationResult.Error("Specify exactly one of --id or --name.");
    }

    return Spectre.Console.ValidationResult.Success();
  }
}
