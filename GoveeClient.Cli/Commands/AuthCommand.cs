using GoveeClient.Cli.Services;
using GoveeClient.Shared.Models;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GoveeClient.Cli.Commands;

public sealed class AuthCommand(CliConfigurationStore configurationStore, IOptions<ClientOptions> clientOptions) : Command<AuthCommand.Settings>
{
  private readonly CliConfigurationStore _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
  private readonly IOptions<ClientOptions> _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));

  public sealed class Settings : CommandSettings
  {
    [CommandOption("-k|--api-key <API_KEY>")]
    [Description("The API key to use for authentication.")]
    public Guid? ApiKey { get; init; }

    [CommandOption("--clear")]
    [Description("Removes the saved API key from the local CLI settings file.")]
    public bool Clear { get; init; }

    public override Spectre.Console.ValidationResult Validate()
    {
      if (Clear && ApiKey is not null)
      {
        return Spectre.Console.ValidationResult.Error("Specify either --api-key or --clear, not both.");
      }

      return Spectre.Console.ValidationResult.Success();
    }
  }

  protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    if (settings.Clear)
    {
      _configurationStore.ClearApiKey();
      AnsiConsole.MarkupLine($"[green]Saved API key cleared.[/] Path: {_configurationStore.SettingsFilePath}");
      return 0;
    }

    if (settings.ApiKey is not null)
    {
      _configurationStore.SaveApiKey(settings.ApiKey.Value);
      AnsiConsole.MarkupLine($"[green]API key saved.[/] Path: {_configurationStore.SettingsFilePath}");
      return 0;
    }

    Guid? configuredApiKey = _clientOptions.Value.ApiKey;
    string status = configuredApiKey is null ? "not configured" : configuredApiKey.Value.ToString();
    AnsiConsole.MarkupLine($"Configured API key: [yellow]{status}[/]");
    AnsiConsole.MarkupLine($"Settings file: {_configurationStore.SettingsFilePath}");
    return 0;
  }
}
