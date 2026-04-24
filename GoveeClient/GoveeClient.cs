using GoveeClient.Shared.Models;
using GoveeClient.Shared.Services;
using GoveeClient.Shared.Services.Api;

namespace GoveeClient;

public class GoveeClient(
  ICloudApiService cloudApiService,
  ILanApiService lanApiService
) : IGoveeClient
{
  private readonly ICloudApiService _cloudApiService = cloudApiService
    ?? throw new ArgumentNullException(nameof(cloudApiService));

  private readonly ILanApiService _lanApiService = lanApiService
    ?? throw new ArgumentNullException(nameof(lanApiService));

  public Guid? ApiKey
  {
    get => field ?? _cloudApiService.ApiKey;
    set
    {
      field = _cloudApiService.ApiKey = value;
    }
  }

  public async Task<IEnumerable<Device?>> GetDevices(bool onlyLan = true)
  {
    IReadOnlyList<Device> devices = await _cloudApiService.GetDevices();
    return devices.Cast<Device?>();
  }

  public Task<DeviceState?> GetDeviceState(Device device, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);
    return _cloudApiService.GetDeviceState(device);
  }

  public Task SetPowerState(Device device, PowerState powerState, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);
    EnsureCapabilitySupported(device, "devices.capabilities.on_off", "powerSwitch");
    return _cloudApiService.ControlDevice(device, "devices.capabilities.on_off", "powerSwitch", powerState == PowerState.On ? 1 : 0);
  }

  public Task SetBrightness(Device device, int brightness, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);

    DeviceCapability brightnessCapability = EnsureCapabilitySupported(device, "devices.capabilities.range", "brightness");
    EnsureValueInRange(brightnessCapability, brightness, nameof(brightness), "Brightness");

    return _cloudApiService.ControlDevice(device, "devices.capabilities.range", "brightness", brightness);
  }

  public Task SetColor(Device device, RgbColor color, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(color);

    DeviceCapability colorCapability = EnsureCapabilitySupported(device, "devices.capabilities.color_setting", "colorRgb");

    int rgb = ((color.R & 0xFF) << 16) | ((color.G & 0xFF) << 8) | (color.B & 0xFF);
    EnsureValueInRange(colorCapability, rgb, nameof(color), "Color");
    return _cloudApiService.ControlDevice(device, "devices.capabilities.color_setting", "colorRgb", rgb);
  }

  public async Task<IReadOnlyList<DeviceScene>> GetScenes(Device device, bool includeDiyScenes = true, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);
    EnsureCapabilitySupported(device, "devices.capabilities.dynamic_scene", "lightScene");

    IReadOnlyList<DeviceScene> scenes = await _cloudApiService.GetScenes(device);
    if (!includeDiyScenes)
    {
      return scenes;
    }

    IReadOnlyList<DeviceScene> diyScenes = await _cloudApiService.GetDiyScenes(device);
    return [.. scenes, .. diyScenes];
  }

  public Task ActivateScene(Device device, DeviceScene scene, bool useUdp = true)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(scene);
    EnsureCapabilitySupported(device, "devices.capabilities.dynamic_scene", "lightScene");

    object sceneValue = scene switch
    {
      { Id: not null, ParamId: not null } => new { id = scene.Id.Value, paramId = scene.ParamId.Value },
      { Value: not null } => scene.Value.Value,
      _ => throw new ArgumentException("Scene must define either a value or an id/paramId pair.", nameof(scene))
    };

    return _cloudApiService.ControlDevice(device, "devices.capabilities.dynamic_scene", "lightScene", sceneValue);
  }

  private static DeviceCapability EnsureCapabilitySupported(Device device, string capabilityType, string capabilityInstance)
  {
    IReadOnlyList<DeviceCapability>? capabilities = device.Capabilities;
    if (capabilities is null || capabilities.Count == 0)
    {
      throw new InvalidOperationException($"Device '{device.DeviceName}' does not include capability metadata, so support for '{capabilityInstance}' cannot be validated.");
    }

    DeviceCapability? capability = capabilities.FirstOrDefault(candidate =>
      string.Equals(candidate.Type, capabilityType, StringComparison.OrdinalIgnoreCase)
      && string.Equals(candidate.Instance, capabilityInstance, StringComparison.OrdinalIgnoreCase));

    if (capability is null)
    {
      throw new InvalidOperationException($"Device '{device.DeviceName}' does not support '{capabilityInstance}'.");
    }

    return capability;
  }

  private static void EnsureValueInRange(DeviceCapability capability, int value, string paramName, string displayName)
  {
    DeviceCapabilityRange? range = capability.Parameters?.Range;
    if (range is null)
    {
      return;
    }

    if (range.Min is int min && value < min)
    {
      throw new ArgumentOutOfRangeException(paramName, value, $"{displayName} must be at least {min} for capability '{capability.Instance}'.");
    }

    if (range.Max is int max && value > max)
    {
      throw new ArgumentOutOfRangeException(paramName, value, $"{displayName} must be at most {max} for capability '{capability.Instance}'.");
    }
  }
}
