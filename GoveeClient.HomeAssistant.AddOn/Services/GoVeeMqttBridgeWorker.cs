using System.Text;
using System.Text.Json;
using GoveeClient.HomeAssistant.AddOn.Models;
using GoveeClient.HomeAssistant.AddOn.Options;
using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace GoveeClient.HomeAssistant.AddOn.Services;

public class GoVeeMqttBridgeWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AddOnOptions> addOnOptions,
    ILogger<GoVeeMqttBridgeWorker> logger) : BackgroundService
{
    private readonly AddOnOptions _options = addOnOptions.Value;
    private readonly Dictionary<string, Device> _deviceMap = [];

    private const string BridgeAvailabilityTopic = "govee/bridge/status";
    private const string Online = "online";
    private const string Offline = "offline";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IGoveeClient goveeClient = scope.ServiceProvider.GetRequiredService<IGoveeClient>();

        using IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
        mqttClient.ApplicationMessageReceivedAsync +=
            msg => HandleCommandAsync(goveeClient, msg, stoppingToken);

        MqttClientOptions mqttOptions = BuildMqttOptions();

        try
        {
            logger.LogInformation(
                "Connecting to MQTT broker {Host}:{Port}...",
                _options.MqttHost, _options.MqttPort);

            await mqttClient.ConnectAsync(mqttOptions, stoppingToken);
            logger.LogInformation("Connected to MQTT broker.");

            await PublishAvailabilityAsync(mqttClient, Online, stoppingToken);
            await DiscoverDevicesAsync(goveeClient, mqttClient, stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
            while (!stoppingToken.IsCancellationRequested)
            {
                await PollAndPublishStatesAsync(goveeClient, mqttClient, stoppingToken);
                try { await timer.WaitForNextTickAsync(stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                await PublishAvailabilityAsync(mqttClient, Offline, CancellationToken.None);
                await mqttClient.DisconnectAsync(cancellationToken: CancellationToken.None);
            }
        }
    }

    private MqttClientOptions BuildMqttOptions()
    {
        MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithClientId("govee-mqtt-bridge")
            .WithCleanSession(false)
            .WithWillTopic(BridgeAvailabilityTopic)
            .WithWillPayload(Offline)
            .WithWillRetain(true)
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        if (!string.IsNullOrEmpty(_options.MqttUsername))
            builder = builder.WithCredentials(_options.MqttUsername, _options.MqttPassword);

        return builder.Build();
    }

    private async Task DiscoverDevicesAsync(
        IGoveeClient goveeClient, IMqttClient mqttClient, CancellationToken ct)
    {
        logger.LogInformation("Fetching Govee devices...");
        IEnumerable<Device?> devices = await goveeClient.GetDevices();
        _deviceMap.Clear();

        foreach (Device? device in devices)
        {
            if (device is null) continue;

            string safeId = SanitizeDeviceId(device.DeviceId);
            _deviceMap[safeId] = device;

            await PublishDiscoveryAsync(
                mqttClient, device, safeId,
                stateTopic: $"govee/{safeId}/state",
                commandTopic: $"govee/{safeId}/set",
                ct);

            logger.LogInformation(
                "Discovered device: {Name} ({Id})", device.DeviceName, safeId);
        }

        if (_deviceMap.Count > 0)
        {
            await mqttClient.SubscribeAsync(
                new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f =>
                        f.WithTopic("govee/+/set")
                         .WithAtLeastOnceQoS())
                    .Build(),
                ct);
        }

        logger.LogInformation(
            "Discovery complete. {Count} device(s) registered.", _deviceMap.Count);
    }

    private async Task PublishDiscoveryAsync(
        IMqttClient mqttClient, Device device, string safeId,
        string stateTopic, string commandTopic, CancellationToken ct)
    {
        bool hasBrightness = HasCapability(device, "devices.capabilities.range", "brightness");
        bool hasColor = HasCapability(device, "devices.capabilities.color_setting", "colorRgb");

        IReadOnlyList<string> colorModes = hasColor    ? ["rgb"]
            : hasBrightness ? ["brightness"]
            : ["onoff"];

        HassDiscoveryPayload payload = new(
            Name: device.DeviceName,
            UniqueId: $"govee_{safeId}",
            Schema: "json",
            StateTopic: stateTopic,
            CommandTopic: commandTopic,
            AvailabilityTopic: BridgeAvailabilityTopic,
            PayloadAvailable: Online,
            PayloadNotAvailable: Offline,
            Brightness: hasBrightness || hasColor,
            ColorMode: true,
            SupportedColorModes: colorModes,
            Device: new HassDeviceInfo(
                Identifiers: [$"govee_{safeId}"],
                Name: device.DeviceName,
                Model: device.Model,
                Manufacturer: "Govee"));

        string discoveryTopic = $"{_options.DiscoveryPrefix}/light/{safeId}/config";
        await PublishJsonAsync(mqttClient, discoveryTopic, payload, retain: true, ct);
    }

    private async Task PollAndPublishStatesAsync(
        IGoveeClient goveeClient, IMqttClient mqttClient, CancellationToken ct)
    {
        foreach ((string safeId, Device device) in _deviceMap)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                DeviceState? state = await goveeClient.GetDeviceState(device);
                if (state is null) continue;

                await PublishJsonAsync(
                    mqttClient, $"govee/{safeId}/state",
                    BuildStatePayload(state), retain: true, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to poll state for device {Name}.", device.DeviceName);
            }
        }
    }

    private static HassLightStatePayload BuildStatePayload(DeviceState state)
    {
        int? haBrightness = state.Brightness > 0
            ? (int)Math.Round(state.Brightness / 100.0 * 255)
            : null;

        HassRgbColor? color = state.Color is { } c ? new(c.R, c.G, c.B) : null;
        string? colorMode = state.State == PowerState.On
            ? (color is not null ? "rgb" : "brightness")
            : null;

        return new HassLightStatePayload(
            State: state.State == PowerState.On ? "ON" : "OFF",
            Brightness: haBrightness,
            Color: color,
            ColorMode: colorMode);
    }

    private async Task HandleCommandAsync(
        IGoveeClient goveeClient,
        MqttApplicationMessageReceivedEventArgs e,
        CancellationToken ct)
    {
        string topic = e.ApplicationMessage.Topic;
        string[] parts = topic.Split('/');

        if (parts.Length != 3
            || !string.Equals(parts[0], "govee", StringComparison.Ordinal)
            || !string.Equals(parts[2], "set", StringComparison.Ordinal))
            return;

        string safeId = parts[1];
        if (!_deviceMap.TryGetValue(safeId, out Device? device))
        {
            logger.LogWarning("Received command for unknown device ID '{Id}'.", safeId);
            return;
        }

        byte[] rawPayload = e.ApplicationMessage.PayloadSegment.ToArray();
        HassLightCommandPayload? command;
        try
        {
            command = JsonSerializer.Deserialize<HassLightCommandPayload>(rawPayload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid command payload for device {Name}.", device.DeviceName);
            return;
        }

        if (command is null) return;

        try
        {
            await ApplyCommandAsync(goveeClient, device, command, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply command to device {Name}.", device.DeviceName);
        }
    }

    private static async Task ApplyCommandAsync(
        IGoveeClient goveeClient, Device device,
        HassLightCommandPayload command, CancellationToken ct)
    {
        if (command.State is { } stateStr)
        {
            PowerState power = string.Equals(stateStr, "ON", StringComparison.OrdinalIgnoreCase)
                ? PowerState.On : PowerState.Off;
            await goveeClient.SetPowerState(device, power);
        }

        if (command.Brightness is { } haBrightness)
        {
            int goveeBrightness = Math.Clamp(
                (int)Math.Round(haBrightness / 255.0 * 100), 1, 100);
            await goveeClient.SetBrightness(device, goveeBrightness);
        }

        if (command.Color is { } c)
            await goveeClient.SetColor(device, new RgbColor(c.R, c.G, c.B));
    }

    private static Task PublishAvailabilityAsync(
        IMqttClient mqttClient, string payload, CancellationToken ct)
    {
        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(BridgeAvailabilityTopic)
            .WithPayload(payload)
            .WithRetainFlag()
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        return mqttClient.PublishAsync(message, ct);
    }

    private static async Task PublishJsonAsync<T>(
        IMqttClient mqttClient, string topic, T payload, bool retain, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(payload);
        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        await mqttClient.PublishAsync(message, ct);
    }

    private static bool HasCapability(Device device, string type, string instance) =>
        device.Capabilities?.Any(c =>
            string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.Instance, instance, StringComparison.OrdinalIgnoreCase)) ?? false;

    private static string SanitizeDeviceId(string deviceId) =>
        deviceId.Replace(":", "_").Replace("-", "_").ToLowerInvariant();
}
