using System.Text.Json;
using System.Text.Json.Nodes;

namespace GoveeClient.Cli.Services;

public sealed class CliConfigurationStore(string settingsFilePath)
{
  private readonly string _settingsFilePath = settingsFilePath ?? throw new ArgumentNullException(nameof(settingsFilePath));

  public string SettingsFilePath => _settingsFilePath;

  public static string GetDefaultSettingsPath()
    => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "GoveeClient.Cli",
      "settings.json");

  public void SaveApiKey(Guid apiKey)
    => SaveValue(apiKey.ToString());

  public void ClearApiKey()
    => SaveValue(string.Empty);

  private void SaveValue(string apiKey)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

    JsonObject root = File.Exists(_settingsFilePath)
      ? JsonNode.Parse(File.ReadAllText(_settingsFilePath))?.AsObject() ?? []
      : [];

    JsonObject goveeClientSection = root["GoveeClient"] as JsonObject ?? [];
    goveeClientSection["ApiKey"] = apiKey;
    root["GoveeClient"] = goveeClientSection;

    File.WriteAllText(_settingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
  }
}
