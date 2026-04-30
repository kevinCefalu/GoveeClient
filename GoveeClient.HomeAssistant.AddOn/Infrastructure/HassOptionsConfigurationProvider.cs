using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GoveeClient.HomeAssistant.AddOn.Infrastructure;

/// <summary>
/// Reads /data/options.json written by the Home Assistant Supervisor and maps
/// its flat snake_case keys into the .NET configuration hierarchy.
/// </summary>
public class HassOptionsConfigurationSource : IConfigurationSource
{
    public string FilePath { get; set; } = "/data/options.json";

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new HassOptionsConfigurationProvider(FilePath);
}

public class HassOptionsConfigurationProvider(string filePath) : ConfigurationProvider
{
    private static readonly IReadOnlyDictionary<string, string> KeyMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["govee_api_key"]         = "GoveeClient:ApiKey",
            ["mqtt_host"]             = "AddOn:MqttHost",
            ["mqtt_port"]             = "AddOn:MqttPort",
            ["mqtt_username"]         = "AddOn:MqttUsername",
            ["mqtt_password"]         = "AddOn:MqttPassword",
            ["discovery_prefix"]      = "AddOn:DiscoveryPrefix",
            ["poll_interval_seconds"] = "AddOn:PollIntervalSeconds",
        };

    public override void Load()
    {
        if (!File.Exists(filePath))
            return;

        using FileStream stream = File.OpenRead(filePath);
        using JsonDocument doc = JsonDocument.Parse(stream);

        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            if (KeyMap.TryGetValue(prop.Name, out string? configKey))
                Data[configKey] = prop.Value.ToString();
        }
    }
}
