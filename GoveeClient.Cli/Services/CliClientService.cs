using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services;
using Microsoft.Extensions.Options;

namespace GoveeClient.Cli.Services;

public sealed class CliClientService(IGoveeClient client, IOptions<ClientOptions> clientOptions)
{
  private readonly IGoveeClient _client = client ?? throw new ArgumentNullException(nameof(client));
  private readonly IOptions<ClientOptions> _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));

  public IGoveeClient Client => _client;

  public async Task<IReadOnlyList<Device>> GetDevicesAsync(Guid? apiKeyOverride)
  {
    ConfigureApiKey(apiKeyOverride);
    IEnumerable<Device?> devices = await _client.GetDevices(onlyLan: false);
    return [.. devices.Where(device => device is not null).Cast<Device>()];
  }

  public async Task<Device> ResolveDeviceAsync(Guid? apiKeyOverride, string? deviceId, string? deviceName)
  {
    IReadOnlyList<Device> devices = await GetDevicesAsync(apiKeyOverride);
    Device? device = !string.IsNullOrWhiteSpace(deviceId)
      ? devices.FirstOrDefault(candidate => string.Equals(candidate.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
      : devices.FirstOrDefault(candidate => string.Equals(candidate.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));

    if (device is null)
    {
      throw new InvalidOperationException($"No device matched '{deviceId ?? deviceName}'. Run 'govee device list' to inspect available devices.");
    }

    return device;
  }

  private void ConfigureApiKey(Guid? apiKeyOverride)
  {
    Guid? apiKey = apiKeyOverride ?? _clientOptions.Value.ApiKey;
    if (apiKey is null)
    {
      throw new InvalidOperationException("No Govee API key is configured. Run 'govee auth --api-key <guid>' or set GoveeClient__ApiKey.");
    }

    _client.ApiKey = apiKey.Value;
  }
}
