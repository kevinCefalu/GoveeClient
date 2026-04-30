namespace GoveeClient.HomeAssistant.AddOn.Options;

public class AddOnOptions
{
    public const string SectionName = "AddOn";

    public string MqttHost { get; set; } = "core-mosquitto";
    public int MqttPort { get; set; } = 1883;
    public string MqttUsername { get; set; } = string.Empty;
    public string MqttPassword { get; set; } = string.Empty;
    public string DiscoveryPrefix { get; set; } = "homeassistant";
    public int PollIntervalSeconds { get; set; } = 30;
}
